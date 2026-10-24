using System.ComponentModel.DataAnnotations;

namespace Taller_DWF.Models;

public record FacturaResumenDto(
    int FacturaId,
    string NumeroFactura,
    DateTime FechaEmision,
    int OrdenId,
    string NumeroOrden,
    string NitCliente,
    string NombreCliente,
    decimal Total,
    string MetodoPago,
    string Estado);

public record LineaFacturaDto(string Tipo, string Descripcion, int Cantidad, decimal PrecioUnitario, decimal Subtotal);

public record FacturaDetalleDto(
    int FacturaId,
    string NumeroFactura,
    DateTime FechaEmision,
    int OrdenId,
    string NumeroOrden,
    string Placa,
    string Vehiculo,
    string NitCliente,
    string NombreCliente,
    string? DireccionCliente,
    List<LineaFacturaDto> Lineas,
    decimal SubtotalManoObra,
    decimal SubtotalRepuestos,
    decimal Descuento,
    decimal Total,
    decimal PorcentajeImpuesto,
    decimal BaseImponible,
    decimal Impuesto,
    string MetodoPago,
    string Estado,
    string EmitidaPor,
    string? MotivoAnulacion,
    DateTime? FechaAnulacion,
    DatosTallerDto Taller);

public record FacturaPreliminarDto(
    int OrdenId,
    string NumeroOrden,
    string Placa,
    string Vehiculo,
    string NitCliente,
    string NombreCliente,
    string? DireccionCliente,
    List<LineaFacturaDto> Lineas,
    decimal SubtotalManoObra,
    decimal SubtotalRepuestos,
    decimal TotalBruto,
    decimal PorcentajeImpuesto,
    decimal DescuentoMaximoPorcentaje);

public class EmitirFacturaRequest
{
    [Range(1, int.MaxValue, ErrorMessage = "Indique la orden de trabajo.")]
    public int OrdenId { get; set; }

    [Required(ErrorMessage = "El NIT es obligatorio (use CF para consumidor final).")]
    [StringLength(20)]
    public string NitCliente { get; set; } = "CF";

    [Required(ErrorMessage = "El nombre para la factura es obligatorio.")]
    [StringLength(170)]
    public string NombreCliente { get; set; } = string.Empty;

    [StringLength(250)]
    public string? DireccionCliente { get; set; }

    [Range(0, 1_000_000, ErrorMessage = "El descuento no es válido.")]
    public decimal Descuento { get; set; }

    [Required]
    [RegularExpression("^(EFECTIVO|TARJETA|TRANSFERENCIA)$", ErrorMessage = "Método de pago no válido.")]
    public string MetodoPago { get; set; } = "EFECTIVO";
}

public class AnularFacturaRequest
{
    [Required(ErrorMessage = "Indique el motivo de anulación.")]
    [StringLength(250)]
    public string Motivo { get; set; } = string.Empty;
}
