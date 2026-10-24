namespace Talle_DW.Entities;

public class SolicitudRepuesto
{
    public int SolicitudId { get; set; }
    public int OrdenId { get; set; }
    public int RepuestoId { get; set; }
    public int Cantidad { get; set; }
    public int SolicitanteId { get; set; }
    public DateTime FechaSolicitud { get; set; } = DateTime.Now;
    public string Estado { get; set; } = "PENDIENTE";
    public int? BodegueroId { get; set; }
    public DateTime? FechaRespuesta { get; set; }
    public decimal? PrecioUnitario { get; set; }
    public string? Observacion { get; set; }
    public string? MotivoRechazo { get; set; }

    public OrdenTrabajo Orden { get; set; } = null!;
    public Repuesto Repuesto { get; set; } = null!;
    public Usuario Solicitante { get; set; } = null!;
    public Usuario? Bodeguero { get; set; }
}
