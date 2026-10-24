using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;
using Taller_DWF.Services;

namespace Taller_DWF.Auth;

/// <summary>Expone la sesión actual al sistema de autorización de Blazor ([Authorize], AuthorizeView).</summary>
public class TallerAuthStateProvider : AuthenticationStateProvider, IDisposable
{
    private readonly SesionService _sesion;

    public TallerAuthStateProvider(SesionService sesion)
    {
        _sesion = sesion;
        _sesion.Cambio += Notificar;
    }

    public override Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        if (!_sesion.Autenticado || _sesion.Perfil is null)
            return Task.FromResult(new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity())));

        var perfil = _sesion.Perfil;
        var identidad = new ClaimsIdentity(
        [
            new Claim(ClaimTypes.NameIdentifier, perfil.UsuarioId.ToString()),
            new Claim(ClaimTypes.Name, perfil.NombreUsuario),
            new Claim(ClaimTypes.Role, perfil.Rol)
        ], "jwt");

        return Task.FromResult(new AuthenticationState(new ClaimsPrincipal(identidad)));
    }

    private void Notificar() => NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());

    public void Dispose() => _sesion.Cambio -= Notificar;
}
