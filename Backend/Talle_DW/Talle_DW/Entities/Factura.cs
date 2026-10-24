namespace Talle_DW.Entities;

public class Factura
{
    public int FacturaId { get; set; }
    /// <summary>Columna calculada en SQL Server: FAC-000001.</summary>
    public string NumeroFactura { get; set; } = string.Empty;
    public int OrdenId { get; set; }
    public int ClienteId { get; set; }
    public string NitCliente { get; set; } = string.Empty;
    public string NombreCliente { get; set; } = string.Empty;
    public string? DireccionCliente { get; set; }
    public DateTime FechaEmision { get; set; } = DateTime.Now;
    public decimal SubtotalManoObra { get; set; }
    public decimal SubtotalRepuestos { get; set; }
    public decimal Descuento { get; set; }
    public decimal Total { get; set; }
    public decimal PorcentajeImpuesto { get; set; }
    public decimal BaseImponible { get; set; }
    public decimal Impuesto { get; set; }
    public string MetodoPago { get; set; } = "EFECTIVO";
    public string Estado { get; set; } = "EMITIDA";
    public int UsuarioId { get; set; }
    public string? MotivoAnulacion { get; set; }
    public DateTime? FechaAnulacion { get; set; }

    public OrdenTrabajo Orden { get; set; } = null!;
    public Cliente Cliente { get; set; } = null!;
    public Usuario Usuario { get; set; } = null!;
    public ICollection<DetalleFactura> Detalles { get; set; } = [];
}
