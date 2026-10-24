using System.ComponentModel.DataAnnotations;

namespace Taller_DWF.Models;

// ---------- Clientes ----------

public record ClienteDto(
    int ClienteId,
    string Nombres,
    string Apellidos,
    string NombreCompleto,
    string Nit,
    string? Dpi,
    string Telefono,
    string? Correo,
    string? Direccion,
    bool Activo,
    DateTime FechaRegistro,
    int CantidadVehiculos);

public record ClienteDetalleDto(ClienteDto Cliente, List<VehiculoDto> Vehiculos, List<OrdenResumenDto> Ordenes);

public class ClienteGuardarRequest
{
    [Required(ErrorMessage = "Los nombres son obligatorios.")]
    [StringLength(80)]
    public string Nombres { get; set; } = string.Empty;

    [Required(ErrorMessage = "Los apellidos son obligatorios.")]
    [StringLength(80)]
    public string Apellidos { get; set; } = string.Empty;

    [StringLength(20)]
    public string? Nit { get; set; }

    [StringLength(20)]
    [RegularExpression(@"^\d{13}$", ErrorMessage = "El DPI debe tener 13 dígitos.")]
    public string? Dpi { get; set; }

    [Required(ErrorMessage = "El teléfono es obligatorio.")]
    [StringLength(20)]
    public string Telefono { get; set; } = string.Empty;

    [EmailAddress(ErrorMessage = "El correo no es válido.")]
    [StringLength(120)]
    public string? Correo { get; set; }

    [StringLength(250)]
    public string? Direccion { get; set; }
}

// ---------- Vehículos ----------

public record VehiculoDto(
    int VehiculoId,
    int ClienteId,
    string Cliente,
    string Placa,
    string Marca,
    string Modelo,
    int Anio,
    string? Color,
    string? Vin,
    bool Activo,
    DateTime FechaRegistro,
    bool TieneOrdenActiva);

public record VehiculoDetalleDto(VehiculoDto Vehiculo, List<OrdenResumenDto> Ordenes);

public class VehiculoGuardarRequest
{
    [Range(1, int.MaxValue, ErrorMessage = "Seleccione el cliente propietario.")]
    public int ClienteId { get; set; }

    [Required(ErrorMessage = "La placa es obligatoria.")]
    [StringLength(15)]
    public string Placa { get; set; } = string.Empty;

    [Required(ErrorMessage = "La marca es obligatoria.")]
    [StringLength(50)]
    public string Marca { get; set; } = string.Empty;

    [Required(ErrorMessage = "El modelo es obligatorio.")]
    [StringLength(50)]
    public string Modelo { get; set; } = string.Empty;

    [Range(1950, 2100, ErrorMessage = "Ingrese un año válido.")]
    public int Anio { get; set; } = DateTime.Today.Year;

    [StringLength(30)]
    public string? Color { get; set; }

    [StringLength(30)]
    public string? Vin { get; set; }
}
