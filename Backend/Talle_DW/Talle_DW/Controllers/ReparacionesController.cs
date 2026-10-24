using Microsoft.AspNetCore.Mvc;
using Talle_DW.Common;
using Talle_DW.DTOs;
using Talle_DW.Security;
using Talle_DW.Services.Interfaces;

namespace Talle_DW.Controllers;

[ApiController]
[Route("api/reparaciones")]
public class ReparacionesController(IReparacionService reparaciones) : ControllerBase
{
    /// <summary>Tablero de trabajos agrupado por estado. soloMias=true muestra lo asignado al usuario.</summary>
    [HttpGet("tablero")]
    [RequierePermiso(Modulos.Reparaciones)]
    public async Task<ActionResult<List<ColumnaTableroDto>>> Tablero(bool soloMias = true) =>
        Ok(await reparaciones.ObtenerTableroAsync(soloMias));

    /// <summary>Mecánico: actualiza el estado de la reparación.</summary>
    [HttpPost("{ordenId:int}/estado")]
    [RequierePermiso(Modulos.Reparaciones, Accion.Editar)]
    public async Task<IActionResult> CambiarEstado(int ordenId, CambiarEstadoOrdenRequest request)
    {
        await reparaciones.CambiarEstadoAsync(ordenId, request);
        return NoContent();
    }
}
