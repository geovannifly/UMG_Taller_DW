using Microsoft.EntityFrameworkCore;
using Talle_DW.Common;
using Talle_DW.Data;
using Talle_DW.DTOs;
using Talle_DW.Entities;
using Talle_DW.Security;
using Talle_DW.Services.Interfaces;

namespace Talle_DW.Services;

public class UsuarioService(
    TallerDbContext db,
    IUsuarioActual actual,
    IPermisoService permisos,
    IBitacoraService bitacora) : IUsuarioService
{
    public async Task<List<UsuarioDto>> ListarAsync(string? buscar, int? rolId, bool? activo)
    {
        var query = db.Usuarios.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(buscar))
        {
            var texto = buscar.Trim();
            query = query.Where(u => u.NombreCompleto.Contains(texto) || u.NombreUsuario.Contains(texto)
                                  || (u.Correo != null && u.Correo.Contains(texto)));
        }
        if (rolId.HasValue) query = query.Where(u => u.RolId == rolId);
        if (activo.HasValue) query = query.Where(u => u.Activo == activo);

        return await query.OrderBy(u => u.NombreCompleto).Select(Proyeccion).ToListAsync();
    }

    public async Task<UsuarioDto> ObtenerAsync(int id) =>
        await db.Usuarios.AsNoTracking().Where(u => u.UsuarioId == id).Select(Proyeccion).FirstOrDefaultAsync()
        ?? throw new NoEncontradoException("El usuario no existe.");

    public async Task<UsuarioDto> CrearAsync(UsuarioGuardarRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Password))
            throw new ReglaNegocioException("La contraseña es obligatoria para un usuario nuevo.");

        var nombreUsuario = request.NombreUsuario.Trim().ToLowerInvariant();
        await ValidarAsync(nombreUsuario, request.RolId, null);

        var usuario = new Usuario
        {
            NombreCompleto = request.NombreCompleto.Trim(),
            NombreUsuario = nombreUsuario,
            Correo = Limpiar(request.Correo),
            Telefono = Limpiar(request.Telefono),
            RolId = request.RolId,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            Activo = true,
            FechaCreacion = DateTime.Now
        };

        db.Usuarios.Add(usuario);
        await db.SaveChangesAsync();
        await bitacora.RegistrarAsync(Modulos.Usuarios, "CREAR", $"Creó el usuario '{usuario.NombreUsuario}'.");

        return await ObtenerAsync(usuario.UsuarioId);
    }

    public async Task<UsuarioDto> ActualizarAsync(int id, UsuarioGuardarRequest request)
    {
        var usuario = await db.Usuarios.Include(u => u.Rol).FirstOrDefaultAsync(u => u.UsuarioId == id)
            ?? throw new NoEncontradoException("El usuario no existe.");

        var nombreUsuario = request.NombreUsuario.Trim().ToLowerInvariant();
        await ValidarAsync(nombreUsuario, request.RolId, id);

        if (usuario.RolId != request.RolId)
        {
            if (usuario.UsuarioId == actual.Id)
                throw new ReglaNegocioException("No puede cambiar su propio rol.");
            if (usuario.Rol.EsSistema && usuario.Activo)
                await ValidarQuedaAdministradorAsync(usuario.UsuarioId);
        }

        usuario.NombreCompleto = request.NombreCompleto.Trim();
        usuario.NombreUsuario = nombreUsuario;
        usuario.Correo = Limpiar(request.Correo);
        usuario.Telefono = Limpiar(request.Telefono);
        usuario.RolId = request.RolId;

        if (!string.IsNullOrWhiteSpace(request.Password))
            usuario.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);

        await db.SaveChangesAsync();
        permisos.Invalidar();
        await bitacora.RegistrarAsync(Modulos.Usuarios, "EDITAR", $"Actualizó el usuario '{usuario.NombreUsuario}'.");

        return await ObtenerAsync(id);
    }

    public async Task RestablecerPasswordAsync(int id, RestablecerPasswordRequest request)
    {
        var usuario = await db.Usuarios.FindAsync(id) ?? throw new NoEncontradoException("El usuario no existe.");
        usuario.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.PasswordNueva);
        await db.SaveChangesAsync();
        await bitacora.RegistrarAsync(Modulos.Usuarios, "PASSWORD", $"Restableció la contraseña de '{usuario.NombreUsuario}'.");
    }

    public async Task CambiarEstadoAsync(int id, bool activo)
    {
        var usuario = await db.Usuarios.Include(u => u.Rol).FirstOrDefaultAsync(u => u.UsuarioId == id)
            ?? throw new NoEncontradoException("El usuario no existe.");

        if (usuario.Activo == activo) return;

        if (!activo)
        {
            if (usuario.UsuarioId == actual.Id)
                throw new ReglaNegocioException("No puede desactivar su propio usuario.");
            if (usuario.Rol.EsSistema)
                await ValidarQuedaAdministradorAsync(usuario.UsuarioId);

            var trabajos = await db.Ordenes.CountAsync(o => o.MecanicoId == id && EstadosOrden.EnTaller.Contains(o.Estado));
            if (trabajos > 0)
                throw new ReglaNegocioException($"El usuario tiene {trabajos} trabajo(s) activo(s) asignado(s). Reasígnelos antes de desactivarlo.");
        }

        usuario.Activo = activo;
        await db.SaveChangesAsync();
        permisos.Invalidar();
        await bitacora.RegistrarAsync(Modulos.Usuarios, activo ? "ACTIVAR" : "DESACTIVAR",
            $"{(activo ? "Activó" : "Desactivó")} el usuario '{usuario.NombreUsuario}'.");
    }

    private async Task ValidarAsync(string nombreUsuario, int rolId, int? idActual)
    {
        if (await db.Usuarios.AnyAsync(u => u.NombreUsuario == nombreUsuario && u.UsuarioId != idActual))
            throw new ReglaNegocioException($"El nombre de usuario '{nombreUsuario}' ya está en uso.");

        var rol = await db.Roles.FindAsync(rolId) ?? throw new ReglaNegocioException("El rol seleccionado no existe.");
        if (!rol.Activo) throw new ReglaNegocioException("El rol seleccionado está inactivo.");
    }

    private async Task ValidarQuedaAdministradorAsync(int usuarioExcluido)
    {
        var otros = await db.Usuarios.CountAsync(u => u.Activo && u.Rol.EsSistema && u.UsuarioId != usuarioExcluido);
        if (otros == 0)
            throw new ReglaNegocioException("Debe existir al menos un administrador activo en el sistema.");
    }

    private static string? Limpiar(string? valor) => string.IsNullOrWhiteSpace(valor) ? null : valor.Trim();

    private static readonly System.Linq.Expressions.Expression<Func<Usuario, UsuarioDto>> Proyeccion = u => new UsuarioDto(
        u.UsuarioId, u.NombreCompleto, u.NombreUsuario, u.Correo, u.Telefono, u.RolId, u.Rol.Nombre,
        u.Activo, u.FechaCreacion, u.UltimoAcceso);
}
