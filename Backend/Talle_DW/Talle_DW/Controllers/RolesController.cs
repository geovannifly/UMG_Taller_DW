using Microsoft.AspNetCore.Mvc;
using Talle_DW.Common;
using Talle_DW.DTOs;
using Talle_DW.Security;
using Talle_DW.Services.Interfaces;

namespace Talle_DW.Controllers;

[ApiController]
[Route("api/roles")]
public class RolesController(IRolService roles) : ControllerBase
{
    /// <summary>Listado de roles (también lo usa el formulario de usuarios).</summary>
    [HttpGet]
    [RequierePermiso($"{Modulos.Roles},{Modulos.Usuarios}")]
    public async Task<ActionResult<List<RolDto>>> Listar() => Ok(await roles.ListarAsync());

    /// <summary>Rol con su matriz de permisos por módulo.</summary>
    [HttpGet("{id:int}")]
    [RequierePermiso(Modulos.Roles)]
    public async Task<ActionResult<RolDetalleDto>> Obtener(int id) => Ok(await roles.ObtenerAsync(id));

    [HttpPost]
    [RequierePermiso(Modulos.Roles, Accion.Crear)]
    public async Task<ActionResult<RolDto>> Crear(RolGuardarRequest request)
    {
        var rol = await roles.CrearAsync(request);
        return CreatedAtAction(nameof(Obtener), new { id = rol.RolId }, rol);
    }

    [HttpPut("{id:int}")]
    [RequierePermiso(Modulos.Roles, Accion.Editar)]
    public async Task<ActionResult<RolDto>> Actualizar(int id, RolGuardarRequest request) =>
        Ok(await roles.ActualizarAsync(id, request));

    [HttpPut("{id:int}/permisos")]
    [RequierePermiso(Modulos.Roles, Accion.Editar)]
    public async Task<IActionResult> ActualizarPermisos(int id, ActualizarPermisosRequest request)
    {
        await roles.ActualizarPermisosAsync(id, request);
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    [RequierePermiso(Modulos.Roles, Accion.Eliminar)]
    public async Task<IActionResult> Eliminar(int id)
    {
        await roles.EliminarAsync(id);
        return NoContent();
    }
}
