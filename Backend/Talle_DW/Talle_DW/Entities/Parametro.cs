namespace Talle_DW.Entities;

public class Parametro
{
    public int ParametroId { get; set; }
    public string Clave { get; set; } = string.Empty;
    public string Valor { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public string Tipo { get; set; } = "TEXTO";
}
