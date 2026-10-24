namespace Talle_DW.Security;

public class JwtOptions
{
    public const string Seccion = "Jwt";

    public string Clave { get; set; } = string.Empty;
    public string Emisor { get; set; } = string.Empty;
    public string Audiencia { get; set; } = string.Empty;
    public int ExpiracionHoras { get; set; } = 8;
}
