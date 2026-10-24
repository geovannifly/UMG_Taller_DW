namespace Talle_DW.Entities;

public class Repuesto
{
    public int RepuestoId { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public int CategoriaId { get; set; }
    public string? Marca { get; set; }
    public decimal PrecioCosto { get; set; }
    public decimal PrecioVenta { get; set; }
    public int Stock { get; set; }
    public int StockMinimo { get; set; }
    public string? Ubicacion { get; set; }
    public bool Activo { get; set; } = true;
    public DateTime FechaRegistro { get; set; } = DateTime.Now;

    public Categoria Categoria { get; set; } = null!;
    public ICollection<SolicitudRepuesto> Solicitudes { get; set; } = [];
}
