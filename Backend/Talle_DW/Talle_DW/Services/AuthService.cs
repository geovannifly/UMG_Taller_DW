using Microsoft.EntityFrameworkCore;
using Talle_DW.Common;
using Talle_DW.Data;
using Talle_DW.DTOs;
using Talle_DW.Security;
using Talle_DW.Services.Interfaces;

namespace Talle_DW.Services;

public class AuthService(
    TallerDbContext db,
    ITokenService tokens,
    IBitacoraService bitacora,
    IUsuarioActual actual) : IAuthService
{
    public async Task<LoginResponse> LoginAsync(LoginRequest request)
    {
        var nombreUsuario = request.NombreUsuario.Trim();
        var usuario = await db.Usuarios.Include(u => u.Rol)
            .FirstOrDefaultAsync(u => u.NombreUsuario == nombreUsuario);

        if (usuario is null || !BCrypt.Net.BCrypt.Verify(request.Password, usuario.PasswordHash))
        {
            await bitacora.RegistrarAsync(Modulos.Auth, "LOGIN_FALLIDO",
                $"Intento de inicio de sesión fallido para '{nombreUsuario}'.", usuario?.UsuarioId, nombreUsuario);
            throw new NoAutorizadoException("Usuario o contraseña incorrectos.");
        }

        if (!usuario.Activo || !usuario.Rol.Activo)
            throw new NoAutorizadoException("Su usuario está desactivado. Contacte al administrador.");

        usuario.UltimoAcceso = DateTime.Now;
        await db.SaveChangesAsync();
        await bitacora.RegistrarAsync(Modulos.Auth, "LOGIN", "Inicio de sesión.", usuario.UsuarioId, usuario.NombreUsuario);

        var (token, expira) = tokens.Generar(usuario);
        return new LoginResponse(token, expira, await ObtenerPerfilAsync(usuario.UsuarioId));
    }

    public async Task<PerfilUsuarioDto> ObtenerPerfilAsync(int usuarioId)
    {
        var usuario = await db.Usuarios.AsNoTracking().Include(u => u.Rol)
            .FirstOrDefaultAsync(u => u.UsuarioId == usuarioId)
            ?? throw new NoEncontradoException("El usuario no existe.");

        var permisos = await db.RolPermisos.AsNoTracking()
            .Where(p => p.RolId == usuario.RolId && (p.PuedeVer || p.PuedeCrear || p.PuedeEditar || p.PuedeEliminar))
            .OrderBy(p => p.Modulo.Orden)
            .Select(p => new PermisoMenuDto(p.Modulo.Codigo, p.Modulo.Nombre, p.Modulo.Grupo, p.Modulo.Icono,
                p.Modulo.Orden, p.PuedeVer, p.PuedeCrear, p.PuedeEditar, p.PuedeEliminar))
            .ToListAsync();

        return new PerfilUsuarioDto(usuario.UsuarioId, usuario.NombreUsuario, usuario.NombreCompleto, usuario.Correo,
            usuario.Rol.Nombre, usuario.Rol.EsSistema, permisos);
    }

    public async Task CambiarPasswordAsync(CambiarPasswordRequest request)
    {
        var usuario = await db.Usuarios.FindAsync(actual.Id)
            ?? throw new NoEncontradoException("El usuario no existe.");

        if (!BCrypt.Net.BCrypt.Verify(request.PasswordActual, usuario.PasswordHash))
            throw new ReglaNegocioException("La contraseña actual no es correcta.");

        if (request.PasswordActual == request.PasswordNueva)
            throw new ReglaNegocioException("La nueva contraseña debe ser diferente a la actual.");

        usuario.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.PasswordNueva);
        await db.SaveChangesAsync();
        await bitacora.RegistrarAsync(Modulos.Auth, "PASSWORD", "Cambió su contraseña.");
    }
}
