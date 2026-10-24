using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Talle_DW.Common;
using Talle_DW.DTOs;
using Talle_DW.Security;
using Talle_DW.Services.Interfaces;

namespace Talle_DW.Controllers;

[ApiController]
[Route("api/parametros")]
public class ParametrosController(IParametroService parametros) : ControllerBase
{
    [HttpGet]
    [RequierePermiso(Modulos.Parametros)]
    public async Task<ActionResult<List<ParametroDto>>> Listar() => Ok(await parametros.ListarAsync());

    [HttpPut("{id:int}")]
    [RequierePermiso(Modulos.Parametros, Accion.Editar)]
    public async Task<ActionResult<ParametroDto>> Actualizar(int id, ParametroActualizarRequest request) =>
        Ok(await parametros.ActualizarAsync(id, request));

    /// <summary>Datos generales del taller (nombre, moneda, IVA) para cualquier usuario autenticado.</summary>
    [HttpGet("taller")]
    [Authorize]
    public async Task<ActionResult<DatosTallerDto>> DatosTaller() => Ok(await parametros.ObtenerDatosTallerAsync());
}
