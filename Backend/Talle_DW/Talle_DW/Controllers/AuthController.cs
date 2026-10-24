using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Talle_DW.DTOs;
using Talle_DW.Security;
using Talle_DW.Services.Interfaces;

namespace Talle_DW.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController(IAuthService auth, IUsuarioActual actual) : ControllerBase
{
    /// <summary>Inicia sesión y devuelve el token JWT con los permisos del usuario.</summary>
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<LoginResponse>> Login(LoginRequest request) => Ok(await auth.LoginAsync(request));

    /// <summary>Datos del usuario autenticado y sus permisos por módulo.</summary>
    [HttpGet("perfil")]
    [Authorize]
    public async Task<ActionResult<PerfilUsuarioDto>> Perfil() => Ok(await auth.ObtenerPerfilAsync(actual.Id));

    [HttpPut("password")]
    [Authorize]
    public async Task<IActionResult> CambiarPassword(CambiarPasswordRequest request)
    {
        await auth.CambiarPasswordAsync(request);
        return NoContent();
    }
}
