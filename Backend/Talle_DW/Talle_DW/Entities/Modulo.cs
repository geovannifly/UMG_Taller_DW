namespace Talle_DW.Entities;

public class Modulo
{
    public int ModuloId { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public string Grupo { get; set; } = string.Empty;
    public string Icono { get; set; } = string.Empty;
    public int Orden { get; set; }
}
