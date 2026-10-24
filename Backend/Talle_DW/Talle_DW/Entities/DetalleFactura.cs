namespace Talle_DW.Entities;

public class DetalleFactura
{
    public int DetalleId { get; set; }
    public int FacturaId { get; set; }
    public string Tipo { get; set; } = string.Empty;
    public string Descripcion { get; set; } = string.Empty;
    public int Cantidad { get; set; }
    public decimal PrecioUnitario { get; set; }
    public decimal Subtotal { get; set; }
    public int? RepuestoId { get; set; }

    public Factura Factura { get; set; } = null!;
}
