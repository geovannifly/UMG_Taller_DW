using Taller_DWF.Models;

namespace Taller_DWF.Services.Api;

/// <summary>Consumo de la API de existencias en bodega.</summary>
public class BodegaApi(ApiClient api)
{
    public Task<List<ExistenciaDto>> ExistenciasAsync(string? buscar = null, int? categoriaId = null, bool soloDisponibles = false) =>
        api.GetAsync<List<ExistenciaDto>>(ApiClient.Url("api/bodega/existencias", ("buscar", buscar), ("categoriaId", categoriaId),
            ("soloDisponibles", soloDisponibles)));
    public Task<ConsultaExistenciaDto> ConsultarAsync(int repuestoId, int cantidad) =>
        api.GetAsync<ConsultaExistenciaDto>(ApiClient.Url($"api/bodega/existencias/{repuestoId}", ("cantidad", cantidad)));
}

public class InventarioApi(ApiClient api)
{
    public Task<List<RepuestoDto>> RepuestosAsync(string? buscar = null, int? categoriaId = null, string? estadoStock = null,
        bool incluirInactivos = false) =>
        api.GetAsync<List<RepuestoDto>>(ApiClient.Url("api/inventario/repuestos", ("buscar", buscar), ("categoriaId", categoriaId),
            ("estadoStock", estadoStock), ("incluirInactivos", incluirInactivos)));
    public Task<RepuestoDetalleDto> RepuestoAsync(int id) => api.GetAsync<RepuestoDetalleDto>($"api/inventario/repuestos/{id}");
    public Task<RepuestoDto> CrearRepuestoAsync(RepuestoGuardarRequest r) => api.PostAsync<RepuestoDto>("api/inventario/repuestos", r);
    public Task<RepuestoDto> ActualizarRepuestoAsync(int id, RepuestoGuardarRequest r) => api.PutAsync<RepuestoDto>($"api/inventario/repuestos/{id}", r);
    public Task EstadoRepuestoAsync(int id, bool activo) =>
        api.PatchAsync($"api/inventario/repuestos/{id}/estado", new CambiarEstadoRequest { Activo = activo });
    public Task<MovimientoDto> MovimientoAsync(int id, MovimientoCrearRequest r) =>
        api.PostAsync<MovimientoDto>($"api/inventario/repuestos/{id}/movimientos", r);
    public Task<List<MovimientoDto>> MovimientosAsync(int? repuestoId = null, string? tipo = null, DateTime? desde = null, DateTime? hasta = null) =>
        api.GetAsync<List<MovimientoDto>>(ApiClient.Url("api/inventario/movimientos", ("repuestoId", repuestoId), ("tipo", tipo),
            ("desde", desde), ("hasta", hasta)));

    public Task<List<CategoriaDto>> CategoriasAsync(bool incluirInactivas = false) =>
        api.GetAsync<List<CategoriaDto>>(ApiClient.Url("api/inventario/categorias", ("incluirInactivas", incluirInactivas)));
    public Task<CategoriaDto> CrearCategoriaAsync(CategoriaGuardarRequest r) => api.PostAsync<CategoriaDto>("api/inventario/categorias", r);
    public Task<CategoriaDto> ActualizarCategoriaAsync(int id, CategoriaGuardarRequest r) =>
        api.PutAsync<CategoriaDto>($"api/inventario/categorias/{id}", r);
    public Task EstadoCategoriaAsync(int id, bool activo) =>
        api.PatchAsync($"api/inventario/categorias/{id}/estado", new CambiarEstadoRequest { Activo = activo });
}

public class DespachosApi(ApiClient api)
{
    public Task<List<SolicitudDto>> ListarAsync(string? estado = null) =>
        api.GetAsync<List<SolicitudDto>>(ApiClient.Url("api/despachos", ("estado", estado)));
    public Task<SolicitudDto> DespacharAsync(int id, string? observacion) =>
        api.PostAsync<SolicitudDto>($"api/despachos/{id}/despachar", new DespacharRequest { Observacion = observacion });
    public Task<SolicitudDto> RechazarAsync(int id, string motivo) =>
        api.PostAsync<SolicitudDto>($"api/despachos/{id}/rechazar", new RechazarRequest { Motivo = motivo });
}
