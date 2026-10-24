namespace Talle_DW.Entities;

public class MovimientoInventario
{
    public int MovimientoId { get; set; }
    public int RepuestoId { get; set; }
    public string Tipo { get; set; } = string.Empty;
    public int Cantidad { get; set; }
    public int StockAnterior { get; set; }
    public int StockNuevo { get; set; }
    public string? Referencia { get; set; }
    public string? Observacion { get; set; }
    public int UsuarioId { get; set; }
    public DateTime Fecha { get; set; } = DateTime.Now;

    public Repuesto Repuesto { get; set; } = null!;
    public Usuario Usuario { get; set; } = null!;
}
