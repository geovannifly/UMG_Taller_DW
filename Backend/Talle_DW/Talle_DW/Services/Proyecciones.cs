using System.Linq.Expressions;
using Talle_DW.Common;
using Talle_DW.DTOs;
using Talle_DW.Entities;

namespace Talle_DW.Services;

/// <summary>
/// Proyecciones reutilizables (se traducen a SQL) y mapeos en memoria de entidades a DTOs.
/// </summary>
public static class Proyecciones
{
    public static readonly Expression<Func<OrdenTrabajo, OrdenResumenDto>> OrdenResumen = o => new OrdenResumenDto(
        o.OrdenId,
        o.NumeroOrden,
        o.FechaIngreso,
        o.Estado,
        FlujoOrden.Descripcion(o.Estado),
        o.Prioridad,
        o.VehiculoId,
        o.Vehiculo.Placa,
        o.Vehiculo.Marca + " " + o.Vehiculo.Modelo + " " + o.Vehiculo.Anio.ToString(),
        o.ClienteId,
        o.Cliente.Nombres + " " + o.Cliente.Apellidos,
        o.MecanicoId,
        o.Mecanico != null ? o.Mecanico.NombreCompleto : null,
        o.FechaEstimada,
        o.MotivoIngreso);

    public static readonly Expression<Func<Cliente, ClienteDto>> Cliente = c => new ClienteDto(
        c.ClienteId,
        c.Nombres,
        c.Apellidos,
        c.Nombres + " " + c.Apellidos,
        c.Nit,
        c.Dpi,
        c.Telefono,
        c.Correo,
        c.Direccion,
        c.Activo,
        c.FechaRegistro,
        c.Vehiculos.Count());

    public static readonly Expression<Func<Vehiculo, VehiculoDto>> Vehiculo = v => new VehiculoDto(
        v.VehiculoId,
        v.ClienteId,
        v.Cliente.Nombres + " " + v.Cliente.Apellidos,
        v.Placa,
        v.Marca,
        v.Modelo,
        v.Anio,
        v.Color,
        v.Vin,
        v.Activo,
        v.FechaRegistro,
        v.Ordenes.Any(o => EstadosOrden.Activos.Contains(o.Estado)));

    public static readonly Expression<Func<SolicitudRepuesto, SolicitudDto>> Solicitud = s => new SolicitudDto(
        s.SolicitudId,
        s.OrdenId,
        s.Orden.NumeroOrden,
        s.Orden.Vehiculo.Placa,
        s.RepuestoId,
        s.Repuesto.Codigo,
        s.Repuesto.Nombre,
        s.Cantidad,
        s.Estado,
        s.FechaSolicitud,
        s.Solicitante.NombreCompleto,
        s.Bodeguero != null ? s.Bodeguero.NombreCompleto : null,
        s.FechaRespuesta,
        s.PrecioUnitario,
        s.PrecioUnitario * s.Cantidad,
        s.Observacion,
        s.MotivoRechazo,
        s.Repuesto.Stock);

    public static readonly Expression<Func<MovimientoInventario, MovimientoDto>> Movimiento = m => new MovimientoDto(
        m.MovimientoId,
        m.Fecha,
        m.RepuestoId,
        m.Repuesto.Codigo,
        m.Repuesto.Nombre,
        m.Tipo,
        m.Cantidad,
        m.StockAnterior,
        m.StockNuevo,
        m.Referencia,
        m.Observacion,
        m.Usuario.NombreCompleto);

    public static readonly Expression<Func<Factura, FacturaResumenDto>> FacturaResumen = f => new FacturaResumenDto(
        f.FacturaId,
        f.NumeroFactura,
        f.FechaEmision,
        f.OrdenId,
        f.Orden.NumeroOrden,
        f.NitCliente,
        f.NombreCliente,
        f.Total,
        f.MetodoPago,
        f.Estado);

    public static readonly Expression<Func<Diagnostico, DiagnosticoDto>> Diagnostico = d => new DiagnosticoDto(
        d.DiagnosticoId,
        d.OrdenId,
        d.MecanicoId,
        d.Mecanico.NombreCompleto,
        d.FallaDetectada,
        d.ReparacionRequerida,
        d.Severidad,
        d.CostoManoObra,
        d.Observaciones,
        d.Fecha);

    /// <summary>Clasifica el stock de un repuesto: AGOTADO, BAJO o DISPONIBLE.</summary>
    public static string EstadoStock(int disponible, int stock, int stockMinimo) =>
        disponible <= 0 ? "AGOTADO" : stock <= stockMinimo ? "BAJO" : "DISPONIBLE";
}
