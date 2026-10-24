namespace Talle_DW.Entities;

public class Cliente
{
    public int ClienteId { get; set; }
    public string Nombres { get; set; } = string.Empty;
    public string Apellidos { get; set; } = string.Empty;
    public string Nit { get; set; } = "CF";
    public string? Dpi { get; set; }
    public string Telefono { get; set; } = string.Empty;
    public string? Correo { get; set; }
    public string? Direccion { get; set; }
    public bool Activo { get; set; } = true;
    public DateTime FechaRegistro { get; set; } = DateTime.Now;

    public ICollection<Vehiculo> Vehiculos { get; set; } = [];
}
