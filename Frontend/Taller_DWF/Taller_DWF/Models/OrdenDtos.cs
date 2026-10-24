using System.ComponentModel.DataAnnotations;

namespace Taller_DWF.Models;

public record OrdenResumenDto(
    int OrdenId,
    string NumeroOrden,
    DateTime FechaIngreso,
    string Estado,
    string EstadoDescripcion,
    string Prioridad,
    int VehiculoId,
    string Placa,
    string Vehiculo,
    int ClienteId,
    string Cliente,
    int? MecanicoId,
    string? Mecanico,
    DateOnly? FechaEstimada,
    string MotivoIngreso);

public record HistorialOrdenDto(
    DateTime Fecha,
    string? EstadoAnterior,
    string EstadoNuevo,
    string EstadoDescripcion,
    string Usuario,
    string? Comentario);

public record OrdenDetalleDto(
    int OrdenId,
    string NumeroOrden,
    string Estado,
    string EstadoDescripcion,
    string Prioridad,
    DateTime FechaIngreso,
    DateOnly? FechaEstimada,
    int Kilometraje,
    string NivelCombustible,
    string MotivoIngreso,
    string? Observaciones,
    string Recepcionista,
    int? MecanicoId,
    string? Mecanico,
    DateTime? FechaSalida,
    string? UsuarioSalida,
    string? ObservacionSalida,
    VehiculoDto Vehiculo,
    ClienteDto Cliente,
    List<DiagnosticoDto> Diagnosticos,
    List<SolicitudDto> Solicitudes,
    List<HistorialOrdenDto> Historial,
    FacturaResumenDto? Factura,
    string[] EstadosSiguientes,
    decimal TotalManoObra,
    decimal TotalRepuestos);

public class OrdenCrearRequest
{
    [Range(1, int.MaxValue, ErrorMessage = "Seleccione el vehículo.")]
    public int VehiculoId { get; set; }

    [Range(0, 5_000_000, ErrorMessage = "Ingrese un kilometraje válido.")]
    public int Kilometraje { get; set; }

    [Required(ErrorMessage = "Indique el nivel de combustible.")]
    [StringLength(10)]
    public string NivelCombustible { get; set; } = "1/2";

    [Required(ErrorMessage = "Describa el motivo de ingreso.")]
    [StringLength(500)]
    public string MotivoIngreso { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Observaciones { get; set; }

    [Required]
    [RegularExpression("^(BAJA|NORMAL|ALTA|URGENTE)$", ErrorMessage = "Prioridad no válida.")]
    public string Prioridad { get; set; } = "NORMAL";

    public DateOnly? FechaEstimada { get; set; }

    /// <summary>Opcional: asigna el mecánico desde la recepción.</summary>
    public int? MecanicoId { get; set; }
}

public class OrdenEditarRequest
{
    [Range(0, 5_000_000, ErrorMessage = "Ingrese un kilometraje válido.")]
    public int Kilometraje { get; set; }

    [Required(ErrorMessage = "Indique el nivel de combustible.")]
    [StringLength(10)]
    public string NivelCombustible { get; set; } = string.Empty;

    [Required(ErrorMessage = "Describa el motivo de ingreso.")]
    [StringLength(500)]
    public string MotivoIngreso { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Observaciones { get; set; }

    [Required]
    [RegularExpression("^(BAJA|NORMAL|ALTA|URGENTE)$", ErrorMessage = "Prioridad no válida.")]
    public string Prioridad { get; set; } = "NORMAL";

    public DateOnly? FechaEstimada { get; set; }
}

public class AsignarMecanicoRequest
{
    [Range(1, int.MaxValue, ErrorMessage = "Seleccione un mecánico.")]
    public int MecanicoId { get; set; }
}

public class CambiarEstadoOrdenRequest
{
    [Required(ErrorMessage = "Seleccione el nuevo estado.")]
    public string Estado { get; set; } = string.Empty;

    [StringLength(300)]
    public string? Comentario { get; set; }
}

public class CancelarOrdenRequest
{
    [Required(ErrorMessage = "Indique el motivo de la cancelación.")]
    [StringLength(300)]
    public string Motivo { get; set; } = string.Empty;
}

public class RegistrarSalidaRequest
{
    [StringLength(500)]
    public string? Observacion { get; set; }
}

public record MecanicoDisponibleDto(int UsuarioId, string NombreCompleto, int TrabajosActivos);

public record ColumnaTableroDto(string Estado, string Descripcion, List<OrdenResumenDto> Ordenes);
