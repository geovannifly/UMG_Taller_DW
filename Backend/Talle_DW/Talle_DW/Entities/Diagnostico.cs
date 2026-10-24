namespace Talle_DW.Entities;

public class Diagnostico
{
    public int DiagnosticoId { get; set; }
    public int OrdenId { get; set; }
    public int MecanicoId { get; set; }
    public string FallaDetectada { get; set; } = string.Empty;
    public string ReparacionRequerida { get; set; } = string.Empty;
    public string Severidad { get; set; } = "MEDIA";
    public decimal CostoManoObra { get; set; }
    public string? Observaciones { get; set; }
    public DateTime Fecha { get; set; } = DateTime.Now;

    public OrdenTrabajo Orden { get; set; } = null!;
    public Usuario Mecanico { get; set; } = null!;
}
