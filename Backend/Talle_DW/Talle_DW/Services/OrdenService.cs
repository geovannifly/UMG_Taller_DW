using Microsoft.EntityFrameworkCore;
using Talle_DW.Common;
using Talle_DW.Data;
using Talle_DW.DTOs;
using Talle_DW.Entities;
using Talle_DW.Security;
using Talle_DW.Services.Interfaces;

namespace Talle_DW.Services;

public class OrdenService(TallerDbContext db, IUsuarioActual actual, IBitacoraService bitacora) : IOrdenService
{
    private static readonly string[] EstadosAsignables =
        [EstadosOrden.Recibido, EstadosOrden.EnDiagnostico, EstadosOrden.EnReparacion, EstadosOrden.EsperandoRepuestos];

    public async Task<List<OrdenResumenDto>> ListarAsync(string? estado, string? buscar, int? mecanicoId, DateTime? desde, DateTime? hasta)
    {
        var query = db.Ordenes.AsNoTracking();

        if (estado == "ACTIVAS") query = query.Where(o => EstadosOrden.Activos.Contains(o.Estado));
        else if (!string.IsNullOrWhiteSpace(estado)) query = query.Where(o => o.Estado == estado);

        if (mecanicoId.HasValue) query = query.Where(o => o.MecanicoId == mecanicoId);
        if (desde.HasValue) query = query.Where(o => o.FechaIngreso >= desde.Value.Date);
        if (hasta.HasValue) query = query.Where(o => o.FechaIngreso < hasta.Value.Date.AddDays(1));

        if (!string.IsNullOrWhiteSpace(buscar))
        {
            var texto = buscar.Trim();
            query = query.Where(o => o.NumeroOrden.Contains(texto)
                                  || o.Vehiculo.Placa.Contains(texto)
                                  || (o.Cliente.Nombres + " " + o.Cliente.Apellidos).Contains(texto)
                                  || o.MotivoIngreso.Contains(texto));
        }

        return await query.OrderByDescending(o => o.OrdenId).Take(300).Select(Proyecciones.OrdenResumen).ToListAsync();
    }

    public async Task<OrdenDetalleDto> ObtenerAsync(int id)
    {
        var orden = await db.Ordenes.AsNoTracking().AsSplitQuery()
            .Include(o => o.Recepcionista)
            .Include(o => o.Mecanico)
            .Include(o => o.UsuarioSalida)
            .Include(o => o.Historial).ThenInclude(h => h.Usuario)
            .FirstOrDefaultAsync(o => o.OrdenId == id)
            ?? throw new NoEncontradoException("La orden de trabajo no existe.");

        var vehiculo = await db.Vehiculos.AsNoTracking().Where(v => v.VehiculoId == orden.VehiculoId)
            .Select(Proyecciones.Vehiculo).FirstAsync();
        var cliente = await db.Clientes.AsNoTracking().Where(c => c.ClienteId == orden.ClienteId)
            .Select(Proyecciones.Cliente).FirstAsync();
        var diagnosticos = await db.Diagnosticos.AsNoTracking().Where(d => d.OrdenId == id)
            .OrderBy(d => d.Fecha).Select(Proyecciones.Diagnostico).ToListAsync();
        var solicitudes = await db.Solicitudes.AsNoTracking().Where(s => s.OrdenId == id)
            .OrderByDescending(s => s.FechaSolicitud).Select(Proyecciones.Solicitud).ToListAsync();
        var factura = await db.Facturas.AsNoTracking()
            .Where(f => f.OrdenId == id)
            .OrderBy(f => f.Estado == EstadosFactura.Emitida ? 0 : 1).ThenByDescending(f => f.FacturaId)
            .Select(Proyecciones.FacturaResumen).FirstOrDefaultAsync();

        var historial = orden.Historial
            .OrderBy(h => h.Fecha).ThenBy(h => h.HistorialId)
            .Select(h => new HistorialOrdenDto(h.Fecha, h.EstadoAnterior, h.EstadoNuevo,
                FlujoOrden.Descripcion(h.EstadoNuevo), h.Usuario.NombreCompleto, h.Comentario))
            .ToList();

        var totalRepuestos = solicitudes.Where(s => s.Estado == EstadosSolicitud.Despachado).Sum(s => s.Subtotal ?? 0);

        return new OrdenDetalleDto(
            orden.OrdenId, orden.NumeroOrden, orden.Estado, FlujoOrden.Descripcion(orden.Estado), orden.Prioridad,
            orden.FechaIngreso, orden.FechaEstimada, orden.Kilometraje, orden.NivelCombustible, orden.MotivoIngreso,
            orden.Observaciones, orden.Recepcionista.NombreCompleto, orden.MecanicoId, orden.Mecanico?.NombreCompleto,
            orden.FechaSalida, orden.UsuarioSalida?.NombreCompleto, orden.ObservacionSalida,
            vehiculo, cliente, diagnosticos, solicitudes, historial, factura,
            FlujoOrden.Siguientes(orden.Estado),
            diagnosticos.Sum(d => d.CostoManoObra), totalRepuestos);
    }

    public async Task<OrdenDetalleDto> CrearAsync(OrdenCrearRequest request)
    {
        var vehiculo = await db.Vehiculos.Include(v => v.Cliente).FirstOrDefaultAsync(v => v.VehiculoId == request.VehiculoId)
            ?? throw new ReglaNegocioException("El vehículo seleccionado no existe.");

        if (!vehiculo.Activo) throw new ReglaNegocioException("El vehículo está inactivo.");
        if (!vehiculo.Cliente.Activo) throw new ReglaNegocioException("El propietario del vehículo está inactivo.");

        var ordenAbierta = await db.Ordenes.Where(o => o.VehiculoId == vehiculo.VehiculoId && EstadosOrden.Activos.Contains(o.Estado))
            .Select(o => o.NumeroOrden).FirstOrDefaultAsync();
        if (ordenAbierta is not null)
            throw new ReglaNegocioException($"El vehículo {vehiculo.Placa} ya tiene la orden {ordenAbierta} abierta en el taller.");

        ValidarFechaEstimada(request.FechaEstimada);

        var orden = new OrdenTrabajo
        {
            VehiculoId = vehiculo.VehiculoId,
            ClienteId = vehiculo.ClienteId,
            RecepcionistaId = actual.Id,
            FechaIngreso = DateTime.Now,
            Kilometraje = request.Kilometraje,
            NivelCombustible = request.NivelCombustible,
            MotivoIngreso = request.MotivoIngreso.Trim(),
            Observaciones = request.Observaciones?.Trim(),
            Prioridad = request.Prioridad,
            FechaEstimada = request.FechaEstimada,
            Estado = EstadosOrden.Recibido
        };
        orden.Historial.Add(new HistorialOrden
        {
            EstadoNuevo = EstadosOrden.Recibido,
            UsuarioId = actual.Id,
            Comentario = "Ingreso del vehículo al taller."
        });

        if (request.MecanicoId.HasValue)
        {
            var mecanico = await ObtenerMecanicoValidoAsync(request.MecanicoId.Value);
            orden.MecanicoId = mecanico.UsuarioId;
            orden.CambiarEstado(EstadosOrden.EnDiagnostico, actual.Id, $"Asignado al mecánico {mecanico.NombreCompleto}.");
        }

        db.Ordenes.Add(orden);
        await db.SaveChangesAsync();
        await bitacora.RegistrarAsync(Modulos.Ordenes, "CREAR",
            $"Abrió la orden {orden.NumeroOrden} para el vehículo {vehiculo.Placa}.");

        return await ObtenerAsync(orden.OrdenId);
    }

    public async Task<OrdenDetalleDto> ActualizarAsync(int id, OrdenEditarRequest request)
    {
        var orden = await BuscarAsync(id);
        if (orden.Estado is EstadosOrden.Entregado or EstadosOrden.Cancelado)
            throw new ReglaNegocioException("No se puede modificar una orden entregada o cancelada.");

        if (request.FechaEstimada != orden.FechaEstimada) ValidarFechaEstimada(request.FechaEstimada);

        orden.Kilometraje = request.Kilometraje;
        orden.NivelCombustible = request.NivelCombustible;
        orden.MotivoIngreso = request.MotivoIngreso.Trim();
        orden.Observaciones = request.Observaciones?.Trim();
        orden.Prioridad = request.Prioridad;
        orden.FechaEstimada = request.FechaEstimada;

        await db.SaveChangesAsync();
        await bitacora.RegistrarAsync(Modulos.Ordenes, "EDITAR", $"Actualizó los datos de la orden {orden.NumeroOrden}.");
        return await ObtenerAsync(id);
    }

    public async Task<OrdenDetalleDto> AsignarMecanicoAsync(int id, AsignarMecanicoRequest request)
    {
        var orden = await db.Ordenes.Include(o => o.Mecanico).FirstOrDefaultAsync(o => o.OrdenId == id)
            ?? throw new NoEncontradoException("La orden de trabajo no existe.");

        if (!EstadosAsignables.Contains(orden.Estado))
            throw new ReglaNegocioException($"No se puede asignar mecánico a una orden en estado '{FlujoOrden.Descripcion(orden.Estado)}'.");
        if (orden.MecanicoId == request.MecanicoId)
            throw new ReglaNegocioException("La orden ya está asignada a ese mecánico.");

        var mecanico = await ObtenerMecanicoValidoAsync(request.MecanicoId);
        var anterior = orden.Mecanico?.NombreCompleto;
        orden.MecanicoId = mecanico.UsuarioId;

        if (orden.Estado == EstadosOrden.Recibido)
            orden.CambiarEstado(EstadosOrden.EnDiagnostico, actual.Id, $"Asignado al mecánico {mecanico.NombreCompleto}.");
        else
            orden.RegistrarEvento(actual.Id, $"Reasignado de {anterior} a {mecanico.NombreCompleto}.");

        await db.SaveChangesAsync();
        await bitacora.RegistrarAsync(Modulos.Ordenes, "ASIGNAR",
            $"Asignó la orden {orden.NumeroOrden} al mecánico {mecanico.NombreCompleto}.");
        return await ObtenerAsync(id);
    }

    public async Task<OrdenDetalleDto> CancelarAsync(int id, CancelarOrdenRequest request)
    {
        var orden = await db.Ordenes.Include(o => o.Solicitudes).FirstOrDefaultAsync(o => o.OrdenId == id)
            ?? throw new NoEncontradoException("La orden de trabajo no existe.");

        if (!EstadosAsignables.Contains(orden.Estado))
            throw new ReglaNegocioException($"No se puede cancelar una orden en estado '{FlujoOrden.Descripcion(orden.Estado)}'.");
        if (orden.Solicitudes.Any(s => s.Estado == EstadosSolicitud.Despachado))
            throw new ReglaNegocioException("La orden ya tiene repuestos despachados por bodega; debe finalizarse y facturarse.");

        foreach (var solicitud in orden.Solicitudes.Where(s => s.Estado == EstadosSolicitud.Pendiente))
        {
            solicitud.Estado = EstadosSolicitud.Cancelado;
            solicitud.FechaRespuesta = DateTime.Now;
        }

        orden.CambiarEstado(EstadosOrden.Cancelado, actual.Id, request.Motivo.Trim());
        await db.SaveChangesAsync();
        await bitacora.RegistrarAsync(Modulos.Ordenes, "CANCELAR", $"Canceló la orden {orden.NumeroOrden}: {request.Motivo.Trim()}");
        return await ObtenerAsync(id);
    }

    public async Task<OrdenDetalleDto> RegistrarSalidaAsync(int id, RegistrarSalidaRequest request)
    {
        var orden = await BuscarAsync(id);

        if (orden.Estado != EstadosOrden.Finalizado)
            throw new ReglaNegocioException("Solo se puede entregar un vehículo cuya reparación esté finalizada.");
        if (!await db.Facturas.AnyAsync(f => f.OrdenId == id && f.Estado == EstadosFactura.Emitida))
            throw new ReglaNegocioException("La orden debe estar facturada antes de entregar el vehículo.");

        orden.FechaSalida = DateTime.Now;
        orden.UsuarioSalidaId = actual.Id;
        orden.ObservacionSalida = request.Observacion?.Trim();
        orden.CambiarEstado(EstadosOrden.Entregado, actual.Id, "Vehículo entregado al cliente.");

        await db.SaveChangesAsync();
        await bitacora.RegistrarAsync(Modulos.Ordenes, "SALIDA", $"Registró la salida del vehículo de la orden {orden.NumeroOrden}.");
        return await ObtenerAsync(id);
    }

    public Task<List<MecanicoDisponibleDto>> ListarMecanicosAsync() =>
        ConsultaMecanicos()
            .OrderBy(u => u.NombreCompleto)
            .Select(u => new MecanicoDisponibleDto(u.UsuarioId, u.NombreCompleto,
                db.Ordenes.Count(o => o.MecanicoId == u.UsuarioId && EstadosOrden.EnTaller.Contains(o.Estado))))
            .ToListAsync();

    /// <summary>Mecánicos: usuarios activos cuyo rol puede registrar diagnósticos (sin contar al administrador).</summary>
    private IQueryable<Usuario> ConsultaMecanicos() =>
        db.Usuarios.Where(u => u.Activo && u.Rol.Activo && !u.Rol.EsSistema
                            && u.Rol.Permisos.Any(p => p.Modulo.Codigo == Modulos.Diagnosticos && p.PuedeCrear));

    private async Task<Usuario> ObtenerMecanicoValidoAsync(int usuarioId) =>
        await ConsultaMecanicos().FirstOrDefaultAsync(u => u.UsuarioId == usuarioId)
        ?? throw new ReglaNegocioException("El usuario seleccionado no es un mecánico activo.");

    private async Task<OrdenTrabajo> BuscarAsync(int id) =>
        await db.Ordenes.FirstOrDefaultAsync(o => o.OrdenId == id)
        ?? throw new NoEncontradoException("La orden de trabajo no existe.");

    private static void ValidarFechaEstimada(DateOnly? fecha)
    {
        if (fecha.HasValue && fecha.Value < DateOnly.FromDateTime(DateTime.Today))
            throw new ReglaNegocioException("La fecha estimada de entrega no puede ser anterior a hoy.");
    }
}
