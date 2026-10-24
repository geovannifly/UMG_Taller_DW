using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Talle_DW.DTOs;
using Talle_DW.Services.Interfaces;

namespace Talle_DW.Controllers;

/// <summary>
/// API de consulta de existencias en bodega. La consumen el frontend y la lógica de
/// reparaciones para comprobar disponibilidad antes de asignar un repuesto.
/// </summary>
[ApiController]
[Route("api/bodega")]
[Authorize]
public class BodegaController(IBodegaService bodega) : ControllerBase
{
    /// <summary>Existencias de los repuestos activos: stock, reservado y disponible.</summary>
    [HttpGet("existencias")]
    public async Task<ActionResult<List<ExistenciaDto>>> Existencias(string? buscar, int? categoriaId, bool soloDisponibles = false) =>
        Ok(await bodega.ListarExistenciasAsync(buscar, categoriaId, soloDisponibles));

    /// <summary>Comprueba en tiempo real si hay existencia suficiente de un repuesto.</summary>
    [HttpGet("existencias/{repuestoId:int}")]
    public async Task<ActionResult<ConsultaExistenciaDto>> Consultar(int repuestoId, int cantidad = 1) =>
        Ok(await bodega.ConsultarExistenciaAsync(repuestoId, cantidad));
}
