using Talle_DW.DTOs;

namespace Talle_DW.Services.Interfaces;

public interface IClienteService
{
    Task<List<ClienteDto>> ListarAsync(string? buscar, bool incluirInactivos);
    Task<ClienteDetalleDto> ObtenerAsync(int id);
    Task<ClienteDto> CrearAsync(ClienteGuardarRequest request);
    Task<ClienteDto> ActualizarAsync(int id, ClienteGuardarRequest request);
    Task CambiarEstadoAsync(int id, bool activo);
}

public interface IVehiculoService
{
    Task<List<VehiculoDto>> ListarAsync(string? buscar, int? clienteId, bool incluirInactivos);
    Task<VehiculoDetalleDto> ObtenerAsync(int id);
    Task<VehiculoDto> CrearAsync(VehiculoGuardarRequest request);
    Task<VehiculoDto> ActualizarAsync(int id, VehiculoGuardarRequest request);
    Task CambiarEstadoAsync(int id, bool activo);
}

public interface IOrdenService
{
    Task<List<OrdenResumenDto>> ListarAsync(string? estado, string? buscar, int? mecanicoId, DateTime? desde, DateTime? hasta);
    Task<OrdenDetalleDto> ObtenerAsync(int id);
    Task<OrdenDetalleDto> CrearAsync(OrdenCrearRequest request);
    Task<OrdenDetalleDto> ActualizarAsync(int id, OrdenEditarRequest request);
    Task<OrdenDetalleDto> AsignarMecanicoAsync(int id, AsignarMecanicoRequest request);
    Task<OrdenDetalleDto> CancelarAsync(int id, CancelarOrdenRequest request);
    Task<OrdenDetalleDto> RegistrarSalidaAsync(int id, RegistrarSalidaRequest request);
    Task<List<MecanicoDisponibleDto>> ListarMecanicosAsync();
}
