namespace Talle_DW.Entities;

public class RegistroBitacora
{
    public long BitacoraId { get; set; }
    public int? UsuarioId { get; set; }
    public string? NombreUsuario { get; set; }
    public string Modulo { get; set; } = string.Empty;
    public string Accion { get; set; } = string.Empty;
    public string? Detalle { get; set; }
    public string? Ip { get; set; }
    public DateTime Fecha { get; set; } = DateTime.Now;
}
