using Microsoft.AspNetCore.Mvc;
using Talle_DW.Common;
using Talle_DW.DTOs;
using Talle_DW.Security;
using Talle_DW.Services.Interfaces;

namespace Talle_DW.Controllers;

/// <summary>Solicitudes de repuestos que realiza el mecánico para una orden.</summary>
[ApiController]
[Route("api/solicitudes")]
public class SolicitudesController(ISolicitudService solicitudes) : ControllerBase
{
    [HttpGet]
    [RequierePermiso($"{Modulos.Solicitudes},{Modulos.Despachos}")]
    public async Task<ActionResult<List<SolicitudDto>>> Listar(int? ordenId, string? estado, bool soloMias = false) =>
        Ok(await solicitudes.ListarAsync(ordenId, estado, soloMias));

    /// <summary>Solicita un repuesto. Antes de registrarla se consulta la existencia en bodega.</summary>
    [HttpPost]
    [RequierePermiso(Modulos.Solicitudes, Accion.Crear)]
    public async Task<ActionResult<SolicitudDto>> Crear(SolicitudCrearRequest request) =>
        Ok(await solicitudes.CrearAsync(request));

    [HttpDelete("{id:int}")]
    [RequierePermiso(Modulos.Solicitudes, Accion.Eliminar)]
    public async Task<IActionResult> Cancelar(int id)
    {
        await solicitudes.CancelarAsync(id);
        return NoContent();
    }
}
