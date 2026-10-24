using Taller_DWF.Models;

namespace Taller_DWF.Services.Api;

public class ClientesApi(ApiClient api)
{
    public Task<List<ClienteDto>> ListarAsync(string? buscar = null, bool incluirInactivos = false) =>
        api.GetAsync<List<ClienteDto>>(ApiClient.Url("api/clientes", ("buscar", buscar), ("incluirInactivos", incluirInactivos)));
    public Task<ClienteDetalleDto> ObtenerAsync(int id) => api.GetAsync<ClienteDetalleDto>($"api/clientes/{id}");
    public Task<ClienteDto> CrearAsync(ClienteGuardarRequest r) => api.PostAsync<ClienteDto>("api/clientes", r);
    public Task<ClienteDto> ActualizarAsync(int id, ClienteGuardarRequest r) => api.PutAsync<ClienteDto>($"api/clientes/{id}", r);
    public Task CambiarEstadoAsync(int id, bool activo) => api.PatchAsync($"api/clientes/{id}/estado", new CambiarEstadoRequest { Activo = activo });
}

public class VehiculosApi(ApiClient api)
{
    public Task<List<VehiculoDto>> ListarAsync(string? buscar = null, int? clienteId = null, bool incluirInactivos = false) =>
        api.GetAsync<List<VehiculoDto>>(ApiClient.Url("api/vehiculos", ("buscar", buscar), ("clienteId", clienteId),
            ("incluirInactivos", incluirInactivos)));
    public Task<VehiculoDetalleDto> ObtenerAsync(int id) => api.GetAsync<VehiculoDetalleDto>($"api/vehiculos/{id}");
    public Task<VehiculoDto> CrearAsync(VehiculoGuardarRequest r) => api.PostAsync<VehiculoDto>("api/vehiculos", r);
    public Task<VehiculoDto> ActualizarAsync(int id, VehiculoGuardarRequest r) => api.PutAsync<VehiculoDto>($"api/vehiculos/{id}", r);
    public Task CambiarEstadoAsync(int id, bool activo) => api.PatchAsync($"api/vehiculos/{id}/estado", new CambiarEstadoRequest { Activo = activo });
}

public class OrdenesApi(ApiClient api)
{
    public Task<List<OrdenResumenDto>> ListarAsync(string? estado = null, string? buscar = null, int? mecanicoId = null,
        DateTime? desde = null, DateTime? hasta = null) =>
        api.GetAsync<List<OrdenResumenDto>>(ApiClient.Url("api/ordenes", ("estado", estado), ("buscar", buscar),
            ("mecanicoId", mecanicoId), ("desde", desde), ("hasta", hasta)));
    public Task<OrdenDetalleDto> ObtenerAsync(int id) => api.GetAsync<OrdenDetalleDto>($"api/ordenes/{id}");
    public Task<OrdenDetalleDto> CrearAsync(OrdenCrearRequest r) => api.PostAsync<OrdenDetalleDto>("api/ordenes", r);
    public Task<OrdenDetalleDto> ActualizarAsync(int id, OrdenEditarRequest r) => api.PutAsync<OrdenDetalleDto>($"api/ordenes/{id}", r);
    public Task<OrdenDetalleDto> AsignarAsync(int id, int mecanicoId) =>
        api.PostAsync<OrdenDetalleDto>($"api/ordenes/{id}/asignar", new AsignarMecanicoRequest { MecanicoId = mecanicoId });
    public Task<OrdenDetalleDto> CancelarAsync(int id, string motivo) =>
        api.PostAsync<OrdenDetalleDto>($"api/ordenes/{id}/cancelar", new CancelarOrdenRequest { Motivo = motivo });
    public Task<OrdenDetalleDto> RegistrarSalidaAsync(int id, string? observacion) =>
        api.PostAsync<OrdenDetalleDto>($"api/ordenes/{id}/salida", new RegistrarSalidaRequest { Observacion = observacion });
    public Task<List<MecanicoDisponibleDto>> MecanicosAsync() => api.GetAsync<List<MecanicoDisponibleDto>>("api/ordenes/mecanicos");
}
