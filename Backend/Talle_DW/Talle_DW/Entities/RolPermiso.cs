namespace Talle_DW.Entities;

public class RolPermiso
{
    public int RolId { get; set; }
    public int ModuloId { get; set; }
    public bool PuedeVer { get; set; }
    public bool PuedeCrear { get; set; }
    public bool PuedeEditar { get; set; }
    public bool PuedeEliminar { get; set; }

    public Rol Rol { get; set; } = null!;
    public Modulo Modulo { get; set; } = null!;
}
