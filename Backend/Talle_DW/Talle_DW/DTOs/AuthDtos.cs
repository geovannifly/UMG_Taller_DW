using System.ComponentModel.DataAnnotations;

namespace Talle_DW.DTOs;

public class LoginRequest
{
    [Required(ErrorMessage = "Ingrese su usuario.")]
    public string NombreUsuario { get; set; } = string.Empty;

    [Required(ErrorMessage = "Ingrese su contraseña.")]
    public string Password { get; set; } = string.Empty;
}

public record LoginResponse(string Token, DateTime Expira, PerfilUsuarioDto Usuario);

public record PerfilUsuarioDto(
    int UsuarioId,
    string NombreUsuario,
    string NombreCompleto,
    string? Correo,
    string Rol,
    bool EsAdministrador,
    List<PermisoMenuDto> Permisos);

public record PermisoMenuDto(
    string Codigo,
    string Nombre,
    string Grupo,
    string Icono,
    int Orden,
    bool Ver,
    bool Crear,
    bool Editar,
    bool Eliminar);

public class CambiarPasswordRequest
{
    [Required(ErrorMessage = "Ingrese su contraseña actual.")]
    public string PasswordActual { get; set; } = string.Empty;

    [Required(ErrorMessage = "Ingrese la nueva contraseña.")]
    [MinLength(8, ErrorMessage = "La contraseña debe tener al menos 8 caracteres.")]
    public string PasswordNueva { get; set; } = string.Empty;
}
