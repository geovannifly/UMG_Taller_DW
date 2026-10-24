using Microsoft.EntityFrameworkCore;
using Talle_DW.Common;
using Talle_DW.Data;
using Talle_DW.DTOs;
using Talle_DW.Entities;
using Talle_DW.Security;
using Talle_DW.Services.Interfaces;

namespace Talle_DW.Services;

/// <summary>
/// Flujo de repuestos: el mecánico solicita (previa consulta de existencias a la API de bodega)
/// y el bodeguero despacha o rechaza.
/// </summary>
public class SolicitudService(
    TallerDbContext db,
    IUsuarioActual actual,
    IBodegaService bodega,
    IBitacoraService bitacora) : ISolicitudService
{
    public async Task<List<SolicitudDto>> ListarAsync(int? ordenId, string? estado, bool soloMias)
    {
        var query = db.Solicitudes.AsNoTracking();

        if (ordenId.HasValue) query = query.Where(s => s.OrdenId == ordenId);
        if (!string.IsNullOrWhiteSpace(estado)) query = query.Where(s => s.Estado == estado);
        if (soloMias) query = query.Where(s => s.SolicitanteId == actual.Id);

        // Pendientes primero (las más antiguas arriba) y luego el historial reciente
        return await query
            .OrderBy(s => s.Estado == EstadosSolicitud.Pendiente ? 0 : 1)
            .ThenBy(s => s.Estado == EstadosSolicitud.Pendiente ? s.FechaSolicitud : DateTime.MaxValue)
            .ThenByDescending(s => s.FechaRespuesta)
            .Take(300)
            .Select(Proyecciones.Solicitud)
            .ToListAsync();
    }

    public async Task<SolicitudDto> CrearAsync(SolicitudCrearRequest request)
    {
        var orden = await db.Ordenes.FirstOrDefaultAsync(o => o.OrdenId == request.OrdenId)
            ?? throw new NoEncontradoException("La orden de trabajo no existe.");

        ReglasTaller.ValidarEnTaller(orden);
        ReglasTaller.ValidarResponsable(orden, actual);

        if (!await db.Diagnosticos.AnyAsync(d => d.OrdenId == orden.OrdenId))
            throw new ReglaNegocioException("Registre un diagnóstico que justifique el uso de repuestos antes de solicitarlos.");

        // Consulta previa de existencias en bodega antes de autorizar la asignación
        var existencia = await bodega.ConsultarExistenciaAsync(request.RepuestoId, request.Cantidad);
        if (!existencia.Suficiente)
            throw new ReglaNegocioException(existencia.Mensaje);

        var solicitud = new SolicitudRepuesto
        {
            OrdenId = orden.OrdenId,
            RepuestoId = request.RepuestoId,
            Cantidad = request.Cantidad,
            SolicitanteId = actual.Id,
            FechaSolicitud = DateTime.Now,
            Estado = EstadosSolicitud.Pendiente,
            Observacion = string.IsNullOrWhiteSpace(request.Observacion) ? null : request.Observacion.Trim()
        };
        db.Solicitudes.Add(solicitud);

        var detalle = $"Solicitó {request.Cantidad} x {existencia.Codigo} - {existencia.Nombre}.";
        if (orden.Estado is EstadosOrden.EnDiagnostico or EstadosOrden.EnReparacion)
            orden.CambiarEstado(EstadosOrden.EsperandoRepuestos, actual.Id, detalle);
        else
            orden.RegistrarEvento(actual.Id, detalle);

        await db.SaveChangesAsync();
        await bitacora.RegistrarAsync(Modulos.Solicitudes, "CREAR", $"Orden {orden.NumeroOrden}: {detalle}");

        return await ObtenerAsync(solicitud.SolicitudId);
    }

    public async Task CancelarAsync(int id)
    {
        var solicitud = await db.Solicitudes.Include(s => s.Orden).Include(s => s.Repuesto)
            .FirstOrDefaultAsync(s => s.SolicitudId == id)
            ?? throw new NoEncontradoException("La solicitud no existe.");

        if (solicitud.Estado != EstadosSolicitud.Pendiente)
            throw new ReglaNegocioException("Solo se pueden cancelar solicitudes pendientes.");
        if (!actual.EsAdministrador && solicitud.SolicitanteId != actual.Id)
            throw new AccesoDenegadoException("Solo quien realizó la solicitud puede cancelarla.");

        solicitud.Estado = EstadosSolicitud.Cancelado;
        solicitud.FechaRespuesta = DateTime.Now;
        solicitud.Orden.RegistrarEvento(actual.Id, $"Canceló la solicitud de {solicitud.Cantidad} x {solicitud.Repuesto.Nombre}.");

        await db.SaveChangesAsync();
        await bitacora.RegistrarAsync(Modulos.Solicitudes, "CANCELAR",
            $"Canceló la solicitud #{solicitud.SolicitudId} de la orden {solicitud.Orden.NumeroOrden}.");
    }

    public async Task<SolicitudDto> DespacharAsync(int id, DespacharRequest request)
    {
        await using var transaccion = await db.Database.BeginTransactionAsync();

        var solicitud = await db.Solicitudes.Include(s => s.Orden).Include(s => s.Repuesto)
            .FirstOrDefaultAsync(s => s.SolicitudId == id)
            ?? throw new NoEncontradoException("La solicitud no existe.");

        if (solicitud.Estado != EstadosSolicitud.Pendiente)
            throw new ReglaNegocioException("La solicitud ya fue atendida.");
        ReglasTaller.ValidarEnTaller(solicitud.Orden);
        if (!solicitud.Repuesto.Activo)
            throw new ReglaNegocioException("El repuesto está inactivo en el inventario.");

        var observacion = string.IsNullOrWhiteSpace(request.Observacion)
            ? $"Despacho para la orden {solicitud.Orden.NumeroOrden}"
            : request.Observacion.Trim();

        var movimiento = await bodega.MoverStockAsync(solicitud.RepuestoId, -solicitud.Cantidad, TiposMovimiento.Salida,
            solicitud.Orden.NumeroOrden, observacion);
        if (movimiento is null)
        {
            var stock = await db.Repuestos.Where(r => r.RepuestoId == solicitud.RepuestoId).Select(r => r.Stock).FirstAsync();
            throw new ReglaNegocioException(
                $"Existencia insuficiente de {solicitud.Repuesto.Nombre}: se requieren {solicitud.Cantidad} y hay {stock} en bodega.");
        }

        solicitud.Estado = EstadosSolicitud.Despachado;
        solicitud.BodegueroId = actual.Id;
        solicitud.FechaRespuesta = DateTime.Now;
        solicitud.PrecioUnitario = solicitud.Repuesto.PrecioVenta;

        var orden = solicitud.Orden;
        var detalle = $"Bodega despachó {solicitud.Cantidad} x {solicitud.Repuesto.Nombre}.";
        var quedanPendientes = await db.Solicitudes.AnyAsync(s =>
            s.OrdenId == orden.OrdenId && s.SolicitudId != id && s.Estado == EstadosSolicitud.Pendiente);

        if (!quedanPendientes && orden.Estado == EstadosOrden.EsperandoRepuestos)
            orden.CambiarEstado(EstadosOrden.EnReparacion, actual.Id, detalle + " Repuestos completos.");
        else
            orden.RegistrarEvento(actual.Id, detalle);

        await db.SaveChangesAsync();
        await transaccion.CommitAsync();

        await bitacora.RegistrarAsync(Modulos.Despachos, "DESPACHAR",
            $"Despachó {solicitud.Cantidad} x {solicitud.Repuesto.Codigo} para la orden {orden.NumeroOrden}.");
        return await ObtenerAsync(id);
    }

    public async Task<SolicitudDto> RechazarAsync(int id, RechazarRequest request)
    {
        var solicitud = await db.Solicitudes.Include(s => s.Orden).Include(s => s.Repuesto)
            .FirstOrDefaultAsync(s => s.SolicitudId == id)
            ?? throw new NoEncontradoException("La solicitud no existe.");

        if (solicitud.Estado != EstadosSolicitud.Pendiente)
            throw new ReglaNegocioException("La solicitud ya fue atendida.");

        solicitud.Estado = EstadosSolicitud.Rechazado;
        solicitud.BodegueroId = actual.Id;
        solicitud.FechaRespuesta = DateTime.Now;
        solicitud.MotivoRechazo = request.Motivo.Trim();
        solicitud.Orden.RegistrarEvento(actual.Id,
            $"Bodega rechazó {solicitud.Cantidad} x {solicitud.Repuesto.Nombre}: {solicitud.MotivoRechazo}");

        await db.SaveChangesAsync();
        await bitacora.RegistrarAsync(Modulos.Despachos, "RECHAZAR",
            $"Rechazó la solicitud #{id} de la orden {solicitud.Orden.NumeroOrden}: {solicitud.MotivoRechazo}");
        return await ObtenerAsync(id);
    }

    private async Task<SolicitudDto> ObtenerAsync(int id) =>
        await db.Solicitudes.AsNoTracking().Where(s => s.SolicitudId == id).Select(Proyecciones.Solicitud).FirstAsync();
}
