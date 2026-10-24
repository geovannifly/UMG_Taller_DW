using Microsoft.AspNetCore.Mvc;
using Talle_DW.Common;
using Talle_DW.DTOs;
using Talle_DW.Security;
using Talle_DW.Services.Interfaces;

namespace Talle_DW.Controllers;

[ApiController]
[Route("api/diagnosticos")]
public class DiagnosticosController(IDiagnosticoService diagnosticos) : ControllerBase
{
    [HttpGet]
    [RequierePermiso(Modulos.Diagnosticos)]
    public async Task<ActionResult<List<DiagnosticoDto>>> ListarPorOrden(int ordenId) =>
        Ok(await diagnosticos.ListarPorOrdenAsync(ordenId));

    [HttpPost]
    [RequierePermiso(Modulos.Diagnosticos, Accion.Crear)]
    public async Task<ActionResult<DiagnosticoDto>> Crear(DiagnosticoGuardarRequest request) =>
        Ok(await diagnosticos.CrearAsync(request));

    [HttpPut("{id:int}")]
    [RequierePermiso(Modulos.Diagnosticos, Accion.Editar)]
    public async Task<ActionResult<DiagnosticoDto>> Actualizar(int id, DiagnosticoGuardarRequest request) =>
        Ok(await diagnosticos.ActualizarAsync(id, request));

    [HttpDelete("{id:int}")]
    [RequierePermiso(Modulos.Diagnosticos, Accion.Eliminar)]
    public async Task<IActionResult> Eliminar(int id)
    {
        await diagnosticos.EliminarAsync(id);
        return NoContent();
    }
}
