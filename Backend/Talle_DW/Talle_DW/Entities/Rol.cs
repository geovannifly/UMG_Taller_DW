namespace Talle_DW.Entities;

public class Rol
{
    public int RolId { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public bool EsSistema { get; set; }
    public bool Activo { get; set; } = true;

    public ICollection<RolPermiso> Permisos { get; set; } = [];
    public ICollection<Usuario> Usuarios { get; set; } = [];
}
