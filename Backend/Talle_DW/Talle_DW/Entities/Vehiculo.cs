namespace Talle_DW.Entities;

public class Vehiculo
{
    public int VehiculoId { get; set; }
    public int ClienteId { get; set; }
    public string Placa { get; set; } = string.Empty;
    public string Marca { get; set; } = string.Empty;
    public string Modelo { get; set; } = string.Empty;
    public int Anio { get; set; }
    public string? Color { get; set; }
    public string? Vin { get; set; }
    public bool Activo { get; set; } = true;
    public DateTime FechaRegistro { get; set; } = DateTime.Now;

    public Cliente Cliente { get; set; } = null!;
    public ICollection<OrdenTrabajo> Ordenes { get; set; } = [];
}
