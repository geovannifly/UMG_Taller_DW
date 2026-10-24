using Microsoft.EntityFrameworkCore;
using Talle_DW.Common;
using Talle_DW.Data;
using Talle_DW.DTOs;
using Talle_DW.Entities;
using Talle_DW.Security;
using Talle_DW.Services.Interfaces;

namespace Talle_DW.Services;

public class RolService(TallerDbContext db, IPermisoService permisos, IBitacoraService bitacora) : IRolService
{
    public Task<List<RolDto>> ListarAsync() =>
        db.Roles.AsNoTracking()
            .OrderBy(r => r.RolId)
            .Select(r => new RolDto(r.RolId, r.Nombre, r.Descripcion, r.EsSistema, r.Activo, r.Usuarios.Count()))
            .ToListAsync();

    public async Task<RolDetalleDto> ObtenerAsync(int id)
    {
        var rol = await db.Roles.AsNoTracking()
            .Where(r => r.RolId == id)
            .Select(r => new RolDto(r.RolId, r.Nombre, r.Descripcion, r.EsSistema, r.Activo, r.Usuarios.Count()))
            .FirstOrDefaultAsync()
            ?? throw new NoEncontradoException("El rol no existe.");

        var asignados = await db.RolPermisos.AsNoTracking().Where(p => p.RolId == id).ToDictionaryAsync(p => p.ModuloId);

        var modulos = await db.Modulos.AsNoTracking().OrderBy(m => m.Orden).ToListAsync();
        var matriz = modulos.Select(m =>
        {
            asignados.TryGetValue(m.ModuloId, out var p);
            return new PermisoRolDto(m.ModuloId, m.Codigo, m.Nombre, m.Grupo, m.Icono,
                p?.PuedeVer ?? false, p?.PuedeCrear ?? false, p?.PuedeEditar ?? false, p?.PuedeEliminar ?? false);
        }).ToList();

        return new RolDetalleDto(rol, matriz);
    }

    public async Task<RolDto> CrearAsync(RolGuardarRequest request)
    {
        var nombre = request.Nombre.Trim();
        if (await db.Roles.AnyAsync(r => r.Nombre == nombre))
            throw new ReglaNegocioException($"Ya existe un rol llamado '{nombre}'.");

        var rol = new Rol { Nombre = nombre, Descripcion = request.Descripcion?.Trim(), Activo = request.Activo };
        foreach (var moduloId in await db.Modulos.Select(m => m.ModuloId).ToListAsync())
            rol.Permisos.Add(new RolPermiso { ModuloId = moduloId });

        db.Roles.Add(rol);
        await db.SaveChangesAsync();
        await bitacora.RegistrarAsync(Modulos.Roles, "CREAR", $"Creó el rol '{rol.Nombre}'.");

        return new RolDto(rol.RolId, rol.Nombre, rol.Descripcion, rol.EsSistema, rol.Activo, 0);
    }

    public async Task<RolDto> ActualizarAsync(int id, RolGuardarRequest request)
    {
        var rol = await db.Roles.FindAsync(id) ?? throw new NoEncontradoException("El rol no existe.");
        var nombre = request.Nombre.Trim();

        if (await db.Roles.AnyAsync(r => r.Nombre == nombre && r.RolId != id))
            throw new ReglaNegocioException($"Ya existe un rol llamado '{nombre}'.");

        if (rol.EsSistema && (!request.Activo || nombre != rol.Nombre))
            throw new ReglaNegocioException("El rol Administrador es del sistema: no se puede renombrar ni desactivar.");

        if (rol.Activo && !request.Activo && await db.Usuarios.AnyAsync(u => u.RolId == id && u.Activo))
            throw new ReglaNegocioException("No se puede desactivar un rol que tiene usuarios activos.");

        rol.Nombre = nombre;
        rol.Descripcion = request.Descripcion?.Trim();
        rol.Activo = request.Activo;
        await db.SaveChangesAsync();
        permisos.Invalidar();
        await bitacora.RegistrarAsync(Modulos.Roles, "EDITAR", $"Actualizó el rol '{rol.Nombre}'.");

        var usuarios = await db.Usuarios.CountAsync(u => u.RolId == id);
        return new RolDto(rol.RolId, rol.Nombre, rol.Descripcion, rol.EsSistema, rol.Activo, usuarios);
    }

    public async Task ActualizarPermisosAsync(int id, ActualizarPermisosRequest request)
    {
        var rol = await db.Roles.Include(r => r.Permisos).FirstOrDefaultAsync(r => r.RolId == id)
            ?? throw new NoEncontradoException("El rol no existe.");

        if (rol.EsSistema)
            throw new ReglaNegocioException("El rol Administrador siempre tiene acceso total; sus permisos no se modifican.");

        var modulosValidos = await db.Modulos.Select(m => m.ModuloId).ToListAsync();

        foreach (var item in request.Permisos.Where(p => modulosValidos.Contains(p.ModuloId)))
        {
            var permiso = rol.Permisos.FirstOrDefault(p => p.ModuloId == item.ModuloId);
            if (permiso is null)
            {
                permiso = new RolPermiso { ModuloId = item.ModuloId };
                rol.Permisos.Add(permiso);
            }

            permiso.PuedeCrear = item.Crear;
            permiso.PuedeEditar = item.Editar;
            permiso.PuedeEliminar = item.Eliminar;
            // Cualquier acción sobre un módulo implica poder verlo
            permiso.PuedeVer = item.Ver || item.Crear || item.Editar || item.Eliminar;
        }

        await db.SaveChangesAsync();
        permisos.Invalidar();
        await bitacora.RegistrarAsync(Modulos.Roles, "PERMISOS", $"Actualizó los permisos del rol '{rol.Nombre}'.");
    }

    public async Task EliminarAsync(int id)
    {
        var rol = await db.Roles.FindAsync(id) ?? throw new NoEncontradoException("El rol no existe.");

        if (rol.EsSistema)
            throw new ReglaNegocioException("El rol Administrador no se puede eliminar.");
        if (await db.Usuarios.AnyAsync(u => u.RolId == id))
            throw new ReglaNegocioException("No se puede eliminar un rol que tiene usuarios asignados. Desactívelo o reasigne los usuarios.");

        db.Roles.Remove(rol);
        await db.SaveChangesAsync();
        permisos.Invalidar();
        await bitacora.RegistrarAsync(Modulos.Roles, "ELIMINAR", $"Eliminó el rol '{rol.Nombre}'.");
    }
}
