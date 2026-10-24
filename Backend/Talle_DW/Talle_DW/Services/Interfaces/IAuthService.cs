using Talle_DW.DTOs;

namespace Talle_DW.Services.Interfaces;

public interface IAuthService
{
    Task<LoginResponse> LoginAsync(LoginRequest request);
    Task<PerfilUsuarioDto> ObtenerPerfilAsync(int usuarioId);
    Task CambiarPasswordAsync(CambiarPasswordRequest request);
}
