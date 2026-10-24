using Microsoft.AspNetCore.Mvc;
using Talle_DW.Common;
using Talle_DW.DTOs;
using Talle_DW.Security;
using Talle_DW.Services.Interfaces;

namespace Talle_DW.Controllers;

[ApiController]
[Route("api/bitacora")]
public class BitacoraController(IBitacoraService bitacora) : ControllerBase
{
    /// <summary>Historial de operaciones (máximo 500 registros por consulta).</summary>
    [HttpGet]
    [RequierePermiso(Modulos.Bitacora)]
    public async Task<ActionResult<List<BitacoraDto>>> Consultar(DateTime? desde, DateTime? hasta, string? modulo, string? buscar) =>
        Ok(await bitacora.ConsultarAsync(desde, hasta, modulo, buscar));
}
