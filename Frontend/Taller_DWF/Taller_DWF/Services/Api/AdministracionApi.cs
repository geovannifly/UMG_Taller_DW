using Taller_DWF.Models;

namespace Taller_DWF.Services.Api;

public class UsuariosApi(ApiClient api)
{
    public Task<List<UsuarioDto>> ListarAsync(string? buscar = null, int? rolId = null, bool? activo = null) =>
        api.GetAsync<List<UsuarioDto>>(ApiClient.Url("api/usuarios", ("buscar", buscar), ("rolId", rolId), ("activo", activo)));
    public Task<UsuarioDto> CrearAsync(UsuarioGuardarRequest r) => api.PostAsync<UsuarioDto>("api/usuarios", r);
    public Task<UsuarioDto> ActualizarAsync(int id, UsuarioGuardarRequest r) => api.PutAsync<UsuarioDto>($"api/usuarios/{id}", r);
    public Task RestablecerPasswordAsync(int id, string password) =>
        api.PutAsync($"api/usuarios/{id}/password", new RestablecerPasswordRequest { PasswordNueva = password });
    public Task CambiarEstadoAsync(int id, bool activo) => api.PatchAsync($"api/usuarios/{id}/estado", new CambiarEstadoRequest { Activo = activo });
}

public class RolesApi(ApiClient api)
{
    public Task<List<RolDto>> ListarAsync() => api.GetAsync<List<RolDto>>("api/roles");
    public Task<RolDetalleDto> ObtenerAsync(int id) => api.GetAsync<RolDetalleDto>($"api/roles/{id}");
    public Task<RolDto> CrearAsync(RolGuardarRequest r) => api.PostAsync<RolDto>("api/roles", r);
    public Task<RolDto> ActualizarAsync(int id, RolGuardarRequest r) => api.PutAsync<RolDto>($"api/roles/{id}", r);
    public Task ActualizarPermisosAsync(int id, ActualizarPermisosRequest r) => api.PutAsync($"api/roles/{id}/permisos", r);
    public Task EliminarAsync(int id) => api.DeleteAsync($"api/roles/{id}");
}

public class ParametrosApi(ApiClient api)
{
    public Task<List<ParametroDto>> ListarAsync() => api.GetAsync<List<ParametroDto>>("api/parametros");
    public Task<ParametroDto> ActualizarAsync(int id, string valor) =>
        api.PutAsync<ParametroDto>($"api/parametros/{id}", new ParametroActualizarRequest { Valor = valor });
}

public class BitacoraApi(ApiClient api)
{
    public Task<List<BitacoraDto>> ConsultarAsync(DateTime? desde, DateTime? hasta, string? modulo, string? buscar) =>
        api.GetAsync<List<BitacoraDto>>(ApiClient.Url("api/bitacora", ("desde", desde), ("hasta", hasta), ("modulo", modulo), ("buscar", buscar)));
}
