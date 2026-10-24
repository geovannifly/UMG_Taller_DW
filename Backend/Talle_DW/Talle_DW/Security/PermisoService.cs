using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Talle_DW.Common;
using Talle_DW.Data;

namespace Talle_DW.Security;

public record PermisoModulo(bool Ver, bool Crear, bool Editar, bool Eliminar)
{
    public bool Permite(Accion accion) => accion switch
    {
        Accion.Ver => Ver,
        Accion.Crear => Crear,
        Accion.Editar => Editar,
        Accion.Eliminar => Eliminar,
        _ => false
    };
}

public record PerfilAcceso(int UsuarioId, bool Activo, int RolId, bool EsAdministrador,
    IReadOnlyDictionary<string, PermisoModulo> Permisos)
{
    public bool Tiene(string modulo, Accion accion) =>
        Permisos.TryGetValue(modulo, out var p) && p.Permite(accion);
}

public interface IPermisoService
{
    Task<PerfilAcceso?> ObtenerPerfilAsync(int usuarioId);
    Task<bool> TieneAsync(int usuarioId, string modulo, Accion accion);

    /// <summary>Descarta los permisos en caché (llamar al cambiar roles, permisos o usuarios).</summary>
    void Invalidar();
}

/// <summary>Versión global de la caché de permisos; al incrementarla todas las entradas quedan obsoletas.</summary>
public class VersionCachePermisos
{
    private int _version;
    public int Actual => _version;
    public void Incrementar() => Interlocked.Increment(ref _version);
}

public class PermisoService(TallerDbContext db, IMemoryCache cache, VersionCachePermisos version) : IPermisoService
{
    public async Task<PerfilAcceso?> ObtenerPerfilAsync(int usuarioId)
    {
        var clave = $"perfil:{version.Actual}:{usuarioId}";
        if (cache.TryGetValue(clave, out PerfilAcceso? perfil)) return perfil;

        var usuario = await db.Usuarios.AsNoTracking()
            .Where(u => u.UsuarioId == usuarioId)
            .Select(u => new { u.UsuarioId, u.Activo, u.RolId, RolActivo = u.Rol.Activo, u.Rol.EsSistema })
            .FirstOrDefaultAsync();

        if (usuario is null) return null;

        var permisos = await db.RolPermisos.AsNoTracking()
            .Where(p => p.RolId == usuario.RolId)
            .Select(p => new { p.Modulo.Codigo, p.PuedeVer, p.PuedeCrear, p.PuedeEditar, p.PuedeEliminar })
            .ToDictionaryAsync(p => p.Codigo, p => new PermisoModulo(p.PuedeVer, p.PuedeCrear, p.PuedeEditar, p.PuedeEliminar));

        perfil = new PerfilAcceso(usuario.UsuarioId, usuario.Activo && usuario.RolActivo, usuario.RolId,
            usuario.EsSistema, permisos);

        cache.Set(clave, perfil, TimeSpan.FromMinutes(5));
        return perfil;
    }

    public async Task<bool> TieneAsync(int usuarioId, string modulo, Accion accion)
    {
        var perfil = await ObtenerPerfilAsync(usuarioId);
        return perfil is { Activo: true } && perfil.Tiene(modulo, accion);
    }

    public void Invalidar() => version.Incrementar();
}
