using System.Security.Claims;

namespace Talle_DW.Security;

/// <summary>Datos del usuario autenticado en la petición actual.</summary>
public interface IUsuarioActual
{
    int Id { get; }
    string NombreUsuario { get; }
    string NombreCompleto { get; }
    bool EsAdministrador { get; }
    string? Ip { get; }
}

public class UsuarioActual(IHttpContextAccessor accessor) : IUsuarioActual
{
    private ClaimsPrincipal? Principal => accessor.HttpContext?.User;

    public int Id => int.TryParse(Principal?.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : 0;
    public string NombreUsuario => Principal?.FindFirstValue(ClaimTypes.Name) ?? string.Empty;
    public string NombreCompleto => Principal?.FindFirstValue(ClaimsTaller.NombreCompleto) ?? string.Empty;
    public bool EsAdministrador => Principal?.FindFirstValue(ClaimsTaller.EsAdministrador) == "true";
    public string? Ip => accessor.HttpContext?.Connection.RemoteIpAddress?.ToString();
}
