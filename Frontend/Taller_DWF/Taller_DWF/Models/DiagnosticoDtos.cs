using System.ComponentModel.DataAnnotations;

namespace Taller_DWF.Models;

public record DiagnosticoDto(
    int DiagnosticoId,
    int OrdenId,
    int MecanicoId,
    string Mecanico,
    string FallaDetectada,
    string ReparacionRequerida,
    string Severidad,
    decimal CostoManoObra,
    string? Observaciones,
    DateTime Fecha);

public class DiagnosticoGuardarRequest
{
    [Range(1, int.MaxValue, ErrorMessage = "Indique la orden de trabajo.")]
    public int OrdenId { get; set; }

    [Required(ErrorMessage = "Describa la falla detectada.")]
    [StringLength(500)]
    public string FallaDetectada { get; set; } = string.Empty;

    [Required(ErrorMessage = "Describa la reparación requerida.")]
    [StringLength(500)]
    public string ReparacionRequerida { get; set; } = string.Empty;

    [Required]
    [RegularExpression("^(BAJA|MEDIA|ALTA|CRITICA)$", ErrorMessage = "Severidad no válida.")]
    public string Severidad { get; set; } = "MEDIA";

    [Range(0, 1_000_000, ErrorMessage = "El costo de mano de obra no es válido.")]
    public decimal CostoManoObra { get; set; }

    [StringLength(500)]
    public string? Observaciones { get; set; }
}
