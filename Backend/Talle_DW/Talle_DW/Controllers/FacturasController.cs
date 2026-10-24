using Microsoft.AspNetCore.Mvc;
using Talle_DW.Common;
using Talle_DW.DTOs;
using Talle_DW.Security;
using Talle_DW.Services.Interfaces;

namespace Talle_DW.Controllers;

[ApiController]
[Route("api/facturas")]
public class FacturasController(IFacturaService facturas) : ControllerBase
{
    [HttpGet]
    [RequierePermiso(Modulos.Facturacion)]
    public async Task<ActionResult<List<FacturaResumenDto>>> Listar(string? buscar, string? estado, DateTime? desde, DateTime? hasta) =>
        Ok(await facturas.ListarAsync(buscar, estado, desde, hasta));

    [HttpGet("{id:int}")]
    [RequierePermiso(Modulos.Facturacion)]
    public async Task<ActionResult<FacturaDetalleDto>> Obtener(int id) => Ok(await facturas.ObtenerAsync(id));

    /// <summary>Órdenes finalizadas que aún no tienen factura.</summary>
    [HttpGet("por-facturar")]
    [RequierePermiso(Modulos.Facturacion)]
    public async Task<ActionResult<List<OrdenResumenDto>>> PorFacturar() => Ok(await facturas.ListarOrdenesPorFacturarAsync());

    /// <summary>Vista previa de la factura de una orden (líneas y totales).</summary>
    [HttpGet("preliminar/{ordenId:int}")]
    [RequierePermiso(Modulos.Facturacion, Accion.Crear)]
    public async Task<ActionResult<FacturaPreliminarDto>> Preliminar(int ordenId) => Ok(await facturas.ObtenerPreliminarAsync(ordenId));

    [HttpPost]
    [RequierePermiso(Modulos.Facturacion, Accion.Crear)]
    public async Task<ActionResult<FacturaDetalleDto>> Emitir(EmitirFacturaRequest request)
    {
        var factura = await facturas.EmitirAsync(request);
        return CreatedAtAction(nameof(Obtener), new { id = factura.FacturaId }, factura);
    }

    [HttpPost("{id:int}/anular")]
    [RequierePermiso(Modulos.Facturacion, Accion.Eliminar)]
    public async Task<ActionResult<FacturaDetalleDto>> Anular(int id, AnularFacturaRequest request) =>
        Ok(await facturas.AnularAsync(id, request));
}
