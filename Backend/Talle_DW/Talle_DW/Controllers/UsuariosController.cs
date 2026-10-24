using Microsoft.AspNetCore.Mvc;
using Talle_DW.Common;
using Talle_DW.DTOs;
using Talle_DW.Security;
using Talle_DW.Services.Interfaces;

namespace Talle_DW.Controllers;

[ApiController]
[Route("api/usuarios")]
public class UsuariosController(IUsuarioService usuarios) : ControllerBase
{
    [HttpGet]
    [RequierePermiso(Modulos.Usuarios)]
    public async Task<ActionResult<List<UsuarioDto>>> Listar(string? buscar, int? rolId, bool? activo) =>
        Ok(await usuarios.ListarAsync(buscar, rolId, activo));

    [HttpGet("{id:int}")]
    [RequierePermiso(Modulos.Usuarios)]
    public async Task<ActionResult<UsuarioDto>> Obtener(int id) => Ok(await usuarios.ObtenerAsync(id));

    [HttpPost]
    [RequierePermiso(Modulos.Usuarios, Accion.Crear)]
    public async Task<ActionResult<UsuarioDto>> Crear(UsuarioGuardarRequest request)
    {
        var usuario = await usuarios.CrearAsync(request);
        return CreatedAtAction(nameof(Obtener), new { id = usuario.UsuarioId }, usuario);
    }

    [HttpPut("{id:int}")]
    [RequierePermiso(Modulos.Usuarios, Accion.Editar)]
    public async Task<ActionResult<UsuarioDto>> Actualizar(int id, UsuarioGuardarRequest request) =>
        Ok(await usuarios.ActualizarAsync(id, request));

    [HttpPut("{id:int}/password")]
    [RequierePermiso(Modulos.Usuarios, Accion.Editar)]
    public async Task<IActionResult> RestablecerPassword(int id, RestablecerPasswordRequest request)
    {
        await usuarios.RestablecerPasswordAsync(id, request);
        return NoContent();
    }

    [HttpPatch("{id:int}/estado")]
    [RequierePermiso(Modulos.Usuarios, Accion.Eliminar)]
    public async Task<IActionResult> CambiarEstado(int id, CambiarEstadoRequest request)
    {
        await usuarios.CambiarEstadoAsync(id, request.Activo);
        return NoContent();
    }
}
