using Microsoft.AspNetCore.Mvc;
using Talle_DW.Common;
using Talle_DW.DTOs;
using Talle_DW.Security;
using Talle_DW.Services.Interfaces;

namespace Talle_DW.Controllers;

/// <summary>Bodega: atención de las solicitudes de repuestos.</summary>
[ApiController]
[Route("api/despachos")]
public class DespachosController(ISolicitudService solicitudes) : ControllerBase
{
    [HttpGet]
    [RequierePermiso(Modulos.Despachos)]
    public async Task<ActionResult<List<SolicitudDto>>> Listar(string? estado) =>
        Ok(await solicitudes.ListarAsync(null, estado, false));

    /// <summary>Despacha el repuesto: descuenta el stock y lo asigna a la orden.</summary>
    [HttpPost("{id:int}/despachar")]
    [RequierePermiso(Modulos.Despachos, Accion.Editar)]
    public async Task<ActionResult<SolicitudDto>> Despachar(int id, DespacharRequest request) =>
        Ok(await solicitudes.DespacharAsync(id, request));

    [HttpPost("{id:int}/rechazar")]
    [RequierePermiso(Modulos.Despachos, Accion.Editar)]
    public async Task<ActionResult<SolicitudDto>> Rechazar(int id, RechazarRequest request) =>
        Ok(await solicitudes.RechazarAsync(id, request));
}
