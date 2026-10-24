namespace Talle_DW.Entities;

public class HistorialOrden
{
    public int HistorialId { get; set; }
    public int OrdenId { get; set; }
    public string? EstadoAnterior { get; set; }
    public string EstadoNuevo { get; set; } = string.Empty;
    public int UsuarioId { get; set; }
    public string? Comentario { get; set; }
    public DateTime Fecha { get; set; } = DateTime.Now;

    public OrdenTrabajo Orden { get; set; } = null!;
    public Usuario Usuario { get; set; } = null!;
}
