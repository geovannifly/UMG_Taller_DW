using System.ComponentModel.DataAnnotations;

namespace Taller_DWF.Models;

// ---------- Usuarios ----------

public record UsuarioDto(
    int UsuarioId,
    string NombreCompleto,
    string NombreUsuario,
    string? Correo,
    string? Telefono,
    int RolId,
    string Rol,
    bool Activo,
    DateTime FechaCreacion,
    DateTime? UltimoAcceso);

public class UsuarioGuardarRequest
{
    [Required(ErrorMessage = "El nombre completo es obligatorio.")]
    [StringLength(120)]
    public string NombreCompleto { get; set; } = string.Empty;

    [Required(ErrorMessage = "El nombre de usuario es obligatorio.")]
    [StringLength(50, MinimumLength = 4, ErrorMessage = "El usuario debe tener entre 4 y 50 caracteres.")]
    [RegularExpression(@"^[a-zA-Z0-9._-]+$", ErrorMessage = "El usuario solo puede tener letras, números, punto, guion o guion bajo.")]
    public string NombreUsuario { get; set; } = string.Empty;

    [EmailAddress(ErrorMessage = "El correo no es válido.")]
    [StringLength(120)]
    public string? Correo { get; set; }

    [StringLength(20)]
    public string? Telefono { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Seleccione un rol.")]
    public int RolId { get; set; }

    /// <summary>Obligatoria solo al crear.</summary>
    [MinLength(8, ErrorMessage = "La contraseña debe tener al menos 8 caracteres.")]
    public string? Password { get; set; }
}

public class RestablecerPasswordRequest
{
    [Required(ErrorMessage = "Ingrese la nueva contraseña.")]
    [MinLength(8, ErrorMessage = "La contraseña debe tener al menos 8 caracteres.")]
    public string PasswordNueva { get; set; } = string.Empty;
}

// ---------- Roles y permisos ----------

public record RolDto(int RolId, string Nombre, string? Descripcion, bool EsSistema, bool Activo, int CantidadUsuarios);

public record PermisoRolDto(
    int ModuloId,
    string Codigo,
    string Nombre,
    string Grupo,
    string Icono,
    bool Ver,
    bool Crear,
    bool Editar,
    bool Eliminar);

public record RolDetalleDto(RolDto Rol, List<PermisoRolDto> Permisos);

public class RolGuardarRequest
{
    [Required(ErrorMessage = "El nombre del rol es obligatorio.")]
    [StringLength(50)]
    public string Nombre { get; set; } = string.Empty;

    [StringLength(200)]
    public string? Descripcion { get; set; }

    public bool Activo { get; set; } = true;
}

public class PermisoRolItem
{
    public int ModuloId { get; set; }
    public bool Ver { get; set; }
    public bool Crear { get; set; }
    public bool Editar { get; set; }
    public bool Eliminar { get; set; }
}

public class ActualizarPermisosRequest
{
    [Required]
    public List<PermisoRolItem> Permisos { get; set; } = [];
}

// ---------- Parámetros ----------

public record ParametroDto(int ParametroId, string Clave, string Valor, string? Descripcion, string Tipo);

public class ParametroActualizarRequest
{
    [Required(ErrorMessage = "El valor es obligatorio.")]
    [StringLength(250)]
    public string Valor { get; set; } = string.Empty;
}

public record DatosTallerDto(
    string Nombre,
    string Nit,
    string Direccion,
    string Telefono,
    string Correo,
    string Moneda,
    decimal IvaPorcentaje,
    decimal DescuentoMaximo);

// ---------- Bitácora ----------

public record BitacoraDto(long BitacoraId, DateTime Fecha, string? NombreUsuario, string Modulo, string Accion, string? Detalle, string? Ip);
