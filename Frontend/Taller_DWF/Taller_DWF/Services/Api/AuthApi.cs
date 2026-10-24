using Taller_DWF.Models;

namespace Taller_DWF.Services.Api;

public class AuthApi(ApiClient api)
{
    public Task<LoginResponse> LoginAsync(LoginRequest request) => api.PostAsync<LoginResponse>("api/auth/login", request);
    public Task CambiarPasswordAsync(CambiarPasswordRequest request) => api.PutAsync("api/auth/password", request);
    public Task<DashboardDto> DashboardAsync() => api.GetAsync<DashboardDto>("api/dashboard");
    public Task<DatosTallerDto> DatosTallerAsync() => api.GetAsync<DatosTallerDto>("api/parametros/taller");
}
