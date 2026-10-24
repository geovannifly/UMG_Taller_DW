using Microsoft.AspNetCore.Mvc;
using Talle_DW.Common;
using Talle_DW.DTOs;
using Talle_DW.Security;
using Talle_DW.Services.Interfaces;

namespace Talle_DW.Controllers;

[ApiController]
[Route("api/vehiculos")]
public class VehiculosController(IVehiculoService vehiculos) : ControllerBase
{
    [HttpGet]
    [RequierePermiso($"{Modulos.Vehiculos},{Modulos.Ordenes}")]
    public async Task<ActionResult<List<VehiculoDto>>> Listar(string? buscar, int? clienteId, bool incluirInactivos = false) =>
        Ok(await vehiculos.ListarAsync(buscar, clienteId, incluirInactivos));

    [HttpGet("{id:int}")]
    [RequierePermiso(Modulos.Vehiculos)]
    public async Task<ActionResult<VehiculoDetalleDto>> Obtener(int id) => Ok(await vehiculos.ObtenerAsync(id));

    [HttpPost]
    [RequierePermiso(Modulos.Vehiculos, Accion.Crear)]
    public async Task<ActionResult<VehiculoDto>> Crear(VehiculoGuardarRequest request)
    {
        var vehiculo = await vehiculos.CrearAsync(request);
        return CreatedAtAction(nameof(Obtener), new { id = vehiculo.VehiculoId }, vehiculo);
    }

    [HttpPut("{id:int}")]
    [RequierePermiso(Modulos.Vehiculos, Accion.Editar)]
    public async Task<ActionResult<VehiculoDto>> Actualizar(int id, VehiculoGuardarRequest request) =>
        Ok(await vehiculos.ActualizarAsync(id, request));

    [HttpPatch("{id:int}/estado")]
    [RequierePermiso(Modulos.Vehiculos, Accion.Eliminar)]
    public async Task<IActionResult> CambiarEstado(int id, CambiarEstadoRequest request)
    {
        await vehiculos.CambiarEstadoAsync(id, request.Activo);
        return NoContent();
    }
}
