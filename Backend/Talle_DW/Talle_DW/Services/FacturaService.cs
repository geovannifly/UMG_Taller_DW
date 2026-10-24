using Microsoft.EntityFrameworkCore;
using Talle_DW.Common;
using Talle_DW.Data;
using Talle_DW.DTOs;
using Talle_DW.Entities;
using Talle_DW.Security;
using Talle_DW.Services.Interfaces;

namespace Talle_DW.Services;

/// <summary>
/// Facturación de órdenes finalizadas. Los precios incluyen IVA: el impuesto se desglosa del total.
/// </summary>
public class FacturaService(
    TallerDbContext db,
    IUsuarioActual actual,
    IParametroService parametros,
    IBitacoraService bitacora) : IFacturaService
{
    public async Task<List<FacturaResumenDto>> ListarAsync(string? buscar, string? estado, DateTime? desde, DateTime? hasta)
    {
        var query = db.Facturas.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(estado)) query = query.Where(f => f.Estado == estado);
        if (desde.HasValue) query = query.Where(f => f.FechaEmision >= desde.Value.Date);
        if (hasta.HasValue) query = query.Where(f => f.FechaEmision < hasta.Value.Date.AddDays(1));
        if (!string.IsNullOrWhiteSpace(buscar))
        {
            var texto = buscar.Trim();
            query = query.Where(f => f.NumeroFactura.Contains(texto) || f.NitCliente.Contains(texto)
                                  || f.NombreCliente.Contains(texto) || f.Orden.NumeroOrden.Contains(texto)
                                  || f.Orden.Vehiculo.Placa.Contains(texto));
        }

        return await query.OrderByDescending(f => f.FacturaId).Take(300).Select(Proyecciones.FacturaResumen).ToListAsync();
    }

    public async Task<FacturaDetalleDto> ObtenerAsync(int id)
    {
        var factura = await db.Facturas.AsNoTracking()
            .Include(f => f.Detalles)
            .Include(f => f.Usuario)
            .Include(f => f.Orden).ThenInclude(o => o.Vehiculo)
            .FirstOrDefaultAsync(f => f.FacturaId == id)
            ?? throw new NoEncontradoException("La factura no existe.");

        var taller = await parametros.ObtenerDatosTallerAsync();
        var vehiculo = factura.Orden.Vehiculo;

        return new FacturaDetalleDto(
            factura.FacturaId, factura.NumeroFactura, factura.FechaEmision, factura.OrdenId, factura.Orden.NumeroOrden,
            vehiculo.Placa, $"{vehiculo.Marca} {vehiculo.Modelo} {vehiculo.Anio}",
            factura.NitCliente, factura.NombreCliente, factura.DireccionCliente,
            factura.Detalles.OrderBy(d => d.Tipo == "SERVICIO" ? 0 : 1).ThenBy(d => d.DetalleId)
                .Select(d => new LineaFacturaDto(d.Tipo, d.Descripcion, d.Cantidad, d.PrecioUnitario, d.Subtotal)).ToList(),
            factura.SubtotalManoObra, factura.SubtotalRepuestos, factura.Descuento, factura.Total,
            factura.PorcentajeImpuesto, factura.BaseImponible, factura.Impuesto, factura.MetodoPago, factura.Estado,
            factura.Usuario.NombreCompleto, factura.MotivoAnulacion, factura.FechaAnulacion, taller);
    }

    public Task<List<OrdenResumenDto>> ListarOrdenesPorFacturarAsync() =>
        db.Ordenes.AsNoTracking()
            .Where(o => o.Estado == EstadosOrden.Finalizado && !o.Facturas.Any(f => f.Estado == EstadosFactura.Emitida))
            .OrderBy(o => o.FechaIngreso)
            .Select(Proyecciones.OrdenResumen)
            .ToListAsync();

    public async Task<FacturaPreliminarDto> ObtenerPreliminarAsync(int ordenId)
    {
        var orden = await CargarOrdenFacturableAsync(ordenId);
        var lineas = await GenerarLineasAsync(ordenId);
        var taller = await parametros.ObtenerDatosTallerAsync();

        var manoObra = lineas.Where(l => l.Tipo == "SERVICIO").Sum(l => l.Subtotal);
        var repuestos = lineas.Where(l => l.Tipo == "REPUESTO").Sum(l => l.Subtotal);

        return new FacturaPreliminarDto(
            orden.OrdenId, orden.NumeroOrden, orden.Vehiculo.Placa,
            $"{orden.Vehiculo.Marca} {orden.Vehiculo.Modelo} {orden.Vehiculo.Anio}",
            orden.Cliente.Nit, $"{orden.Cliente.Nombres} {orden.Cliente.Apellidos}", orden.Cliente.Direccion,
            lineas.Select(l => new LineaFacturaDto(l.Tipo, l.Descripcion, l.Cantidad, l.PrecioUnitario, l.Subtotal)).ToList(),
            manoObra, repuestos, manoObra + repuestos, taller.IvaPorcentaje, taller.DescuentoMaximo);
    }

    public async Task<FacturaDetalleDto> EmitirAsync(EmitirFacturaRequest request)
    {
        var orden = await CargarOrdenFacturableAsync(request.OrdenId);
        var lineas = await GenerarLineasAsync(request.OrdenId);
        var taller = await parametros.ObtenerDatosTallerAsync();

        if (lineas.Count == 0)
            throw new ReglaNegocioException("La orden no tiene mano de obra ni repuestos que facturar.");

        var manoObra = lineas.Where(l => l.Tipo == "SERVICIO").Sum(l => l.Subtotal);
        var repuestos = lineas.Where(l => l.Tipo == "REPUESTO").Sum(l => l.Subtotal);
        var bruto = manoObra + repuestos;
        var descuento = Math.Round(request.Descuento, 2);
        var descuentoMaximo = Math.Round(bruto * taller.DescuentoMaximo / 100m, 2);

        if (descuento > descuentoMaximo)
            throw new ReglaNegocioException(
                $"El descuento máximo permitido es {taller.DescuentoMaximo}% ({taller.Moneda} {descuentoMaximo:N2}).");

        var total = bruto - descuento;
        var baseImponible = Math.Round(total / (1 + taller.IvaPorcentaje / 100m), 2);

        var factura = new Factura
        {
            OrdenId = orden.OrdenId,
            ClienteId = orden.ClienteId,
            NitCliente = request.NitCliente.Trim().ToUpperInvariant(),
            NombreCliente = request.NombreCliente.Trim(),
            DireccionCliente = string.IsNullOrWhiteSpace(request.DireccionCliente) ? null : request.DireccionCliente.Trim(),
            FechaEmision = DateTime.Now,
            SubtotalManoObra = manoObra,
            SubtotalRepuestos = repuestos,
            Descuento = descuento,
            Total = total,
            PorcentajeImpuesto = taller.IvaPorcentaje,
            BaseImponible = baseImponible,
            Impuesto = total - baseImponible,
            MetodoPago = request.MetodoPago,
            Estado = EstadosFactura.Emitida,
            UsuarioId = actual.Id,
            Detalles = lineas
        };

        db.Facturas.Add(factura);
        await db.SaveChangesAsync();

        orden.RegistrarEvento(actual.Id, $"Factura {factura.NumeroFactura} emitida por {taller.Moneda} {total:N2}.");
        await db.SaveChangesAsync();
        await bitacora.RegistrarAsync(Modulos.Facturacion, "EMITIR",
            $"Emitió la factura {factura.NumeroFactura} de la orden {orden.NumeroOrden} por {taller.Moneda} {total:N2}.");

        return await ObtenerAsync(factura.FacturaId);
    }

    public async Task<FacturaDetalleDto> AnularAsync(int id, AnularFacturaRequest request)
    {
        var factura = await db.Facturas.Include(f => f.Orden).FirstOrDefaultAsync(f => f.FacturaId == id)
            ?? throw new NoEncontradoException("La factura no existe.");

        if (factura.Estado == EstadosFactura.Anulada)
            throw new ReglaNegocioException("La factura ya está anulada.");
        if (factura.Orden.Estado == EstadosOrden.Entregado)
            throw new ReglaNegocioException("No se puede anular la factura de un vehículo que ya fue entregado.");

        factura.Estado = EstadosFactura.Anulada;
        factura.MotivoAnulacion = request.Motivo.Trim();
        factura.FechaAnulacion = DateTime.Now;
        factura.Orden.RegistrarEvento(actual.Id, $"Factura {factura.NumeroFactura} anulada: {factura.MotivoAnulacion}");

        await db.SaveChangesAsync();
        await bitacora.RegistrarAsync(Modulos.Facturacion, "ANULAR",
            $"Anuló la factura {factura.NumeroFactura}: {factura.MotivoAnulacion}");

        return await ObtenerAsync(id);
    }

    private async Task<OrdenTrabajo> CargarOrdenFacturableAsync(int ordenId)
    {
        var orden = await db.Ordenes.Include(o => o.Vehiculo).Include(o => o.Cliente)
            .FirstOrDefaultAsync(o => o.OrdenId == ordenId)
            ?? throw new NoEncontradoException("La orden de trabajo no existe.");

        if (orden.Estado != EstadosOrden.Finalizado)
            throw new ReglaNegocioException("Solo se pueden facturar órdenes con la reparación finalizada.");
        if (await db.Facturas.AnyAsync(f => f.OrdenId == ordenId && f.Estado == EstadosFactura.Emitida))
            throw new ReglaNegocioException("La orden ya tiene una factura emitida.");

        return orden;
    }

    /// <summary>Mano de obra de cada diagnóstico + repuestos despachados agrupados por precio.</summary>
    private async Task<List<DetalleFactura>> GenerarLineasAsync(int ordenId)
    {
        var servicios = await db.Diagnosticos.AsNoTracking()
            .Where(d => d.OrdenId == ordenId && d.CostoManoObra > 0)
            .OrderBy(d => d.Fecha)
            .Select(d => new DetalleFactura
            {
                Tipo = "SERVICIO",
                Descripcion = "Mano de obra: " + d.ReparacionRequerida,
                Cantidad = 1,
                PrecioUnitario = d.CostoManoObra,
                Subtotal = d.CostoManoObra
            })
            .ToListAsync();

        var repuestos = await db.Solicitudes.AsNoTracking()
            .Where(s => s.OrdenId == ordenId && s.Estado == EstadosSolicitud.Despachado)
            .GroupBy(s => new { s.RepuestoId, s.Repuesto.Codigo, s.Repuesto.Nombre, Precio = s.PrecioUnitario ?? 0 })
            .Select(g => new DetalleFactura
            {
                Tipo = "REPUESTO",
                Descripcion = g.Key.Codigo + " - " + g.Key.Nombre,
                Cantidad = g.Sum(s => s.Cantidad),
                PrecioUnitario = g.Key.Precio,
                Subtotal = g.Key.Precio * g.Sum(s => s.Cantidad),
                RepuestoId = g.Key.RepuestoId
            })
            .ToListAsync();

        foreach (var linea in servicios.Where(l => l.Descripcion.Length > 300))
            linea.Descripcion = linea.Descripcion[..297] + "...";

        return [.. servicios, .. repuestos];
    }
}
