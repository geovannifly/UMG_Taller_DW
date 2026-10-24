using Microsoft.AspNetCore.Mvc;
using Talle_DW.Common;
using Talle_DW.DTOs;
using Talle_DW.Security;
using Talle_DW.Services.Interfaces;

namespace Talle_DW.Controllers;

[ApiController]
[Route("api/clientes")]
public class ClientesController(IClienteService clientes) : ControllerBase
{
    [HttpGet]
    [RequierePermiso($"{Modulos.Clientes},{Modulos.Vehiculos}")]
    public async Task<ActionResult<List<ClienteDto>>> Listar(string? buscar, bool incluirInactivos = false) =>
        Ok(await clientes.ListarAsync(buscar, incluirInactivos));

    [HttpGet("{id:int}")]
    [RequierePermiso(Modulos.Clientes)]
    public async Task<ActionResult<ClienteDetalleDto>> Obtener(int id) => Ok(await clientes.ObtenerAsync(id));

    [HttpPost]
    [RequierePermiso(Modulos.Clientes, Accion.Crear)]
    public async Task<ActionResult<ClienteDto>> Crear(ClienteGuardarRequest request)
    {
        var cliente = await clientes.CrearAsync(request);
        return CreatedAtAction(nameof(Obtener), new { id = cliente.ClienteId }, cliente);
    }

    [HttpPut("{id:int}")]
    [RequierePermiso(Modulos.Clientes, Accion.Editar)]
    public async Task<ActionResult<ClienteDto>> Actualizar(int id, ClienteGuardarRequest request) =>
        Ok(await clientes.ActualizarAsync(id, request));

    [HttpPatch("{id:int}/estado")]
    [RequierePermiso(Modulos.Clientes, Accion.Eliminar)]
    public async Task<IActionResult> CambiarEstado(int id, CambiarEstadoRequest request)
    {
        await clientes.CambiarEstadoAsync(id, request.Activo);
        return NoContent();
    }
}
