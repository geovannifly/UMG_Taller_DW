using System.ComponentModel.DataAnnotations;

namespace Taller_DWF.Models;

// ---------- Categorías ----------

public record CategoriaDto(int CategoriaId, string Nombre, string? Descripcion, bool Activo, int CantidadRepuestos);

public class CategoriaGuardarRequest
{
    [Required(ErrorMessage = "El nombre es obligatorio.")]
    [StringLength(60)]
    public string Nombre { get; set; } = string.Empty;

    [StringLength(200)]
    public string? Descripcion { get; set; }
}

// ---------- Repuestos ----------

public record RepuestoDto(
    int RepuestoId,
    string Codigo,
    string Nombre,
    string? Descripcion,
    int CategoriaId,
    string Categoria,
    string? Marca,
    decimal PrecioCosto,
    decimal PrecioVenta,
    int Stock,
    int StockMinimo,
    int Reservado,
    int Disponible,
    string? Ubicacion,
    bool Activo,
    string EstadoStock);

public record RepuestoDetalleDto(RepuestoDto Repuesto, List<MovimientoDto> UltimosMovimientos);

public class RepuestoGuardarRequest
{
    [Required(ErrorMessage = "El código es obligatorio.")]
    [StringLength(30)]
    public string Codigo { get; set; } = string.Empty;

    [Required(ErrorMessage = "El nombre es obligatorio.")]
    [StringLength(120)]
    public string Nombre { get; set; } = string.Empty;

    [StringLength(300)]
    public string? Descripcion { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Seleccione una categoría.")]
    public int CategoriaId { get; set; }

    [StringLength(50)]
    public string? Marca { get; set; }

    [Range(0, 1_000_000, ErrorMessage = "El precio de costo no es válido.")]
    public decimal PrecioCosto { get; set; }

    [Range(0, 1_000_000, ErrorMessage = "El precio de venta no es válido.")]
    public decimal PrecioVenta { get; set; }

    [Range(0, 100_000, ErrorMessage = "El stock mínimo no es válido.")]
    public int StockMinimo { get; set; }

    [StringLength(50)]
    public string? Ubicacion { get; set; }

    /// <summary>Solo se usa al crear: genera un movimiento de ENTRADA inicial.</summary>
    [Range(0, 100_000, ErrorMessage = "El stock inicial no es válido.")]
    public int StockInicial { get; set; }
}

// ---------- Movimientos ----------

public record MovimientoDto(
    int MovimientoId,
    DateTime Fecha,
    int RepuestoId,
    string CodigoRepuesto,
    string Repuesto,
    string Tipo,
    int Cantidad,
    int StockAnterior,
    int StockNuevo,
    string? Referencia,
    string? Observacion,
    string Usuario);

public class MovimientoCrearRequest
{
    [Required]
    [RegularExpression("^(ENTRADA|AJUSTE)$", ErrorMessage = "Solo se permiten movimientos de ENTRADA o AJUSTE.")]
    public string Tipo { get; set; } = "ENTRADA";

    /// <summary>ENTRADA: cantidad positiva. AJUSTE: positiva suma, negativa resta.</summary>
    [Range(-100_000, 100_000)]
    public int Cantidad { get; set; }

    [StringLength(50)]
    public string? Referencia { get; set; }

    [StringLength(250)]
    public string? Observacion { get; set; }
}

// ---------- API de Bodega (consulta de existencias) ----------

public record ExistenciaDto(
    int RepuestoId,
    string Codigo,
    string Nombre,
    string Categoria,
    string? Marca,
    decimal PrecioVenta,
    int Stock,
    int Reservado,
    int Disponible,
    int StockMinimo,
    string? Ubicacion,
    string EstadoStock);

public record ConsultaExistenciaDto(
    int RepuestoId,
    string Codigo,
    string Nombre,
    int Stock,
    int Reservado,
    int Disponible,
    int CantidadSolicitada,
    bool Suficiente,
    string Mensaje,
    DateTime FechaConsulta);

// ---------- Solicitudes y despachos ----------

public record SolicitudDto(
    int SolicitudId,
    int OrdenId,
    string NumeroOrden,
    string Placa,
    int RepuestoId,
    string CodigoRepuesto,
    string Repuesto,
    int Cantidad,
    string Estado,
    DateTime FechaSolicitud,
    string Solicitante,
    string? Bodeguero,
    DateTime? FechaRespuesta,
    decimal? PrecioUnitario,
    decimal? Subtotal,
    string? Observacion,
    string? MotivoRechazo,
    int StockActual);

public class SolicitudCrearRequest
{
    [Range(1, int.MaxValue, ErrorMessage = "Indique la orden de trabajo.")]
    public int OrdenId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Seleccione el repuesto.")]
    public int RepuestoId { get; set; }

    [Range(1, 1000, ErrorMessage = "La cantidad debe ser mayor a cero.")]
    public int Cantidad { get; set; } = 1;

    [StringLength(250)]
    public string? Observacion { get; set; }
}

public class DespacharRequest
{
    [StringLength(250)]
    public string? Observacion { get; set; }
}

public class RechazarRequest
{
    [Required(ErrorMessage = "Indique el motivo del rechazo.")]
    [StringLength(250)]
    public string Motivo { get; set; } = string.Empty;
}
