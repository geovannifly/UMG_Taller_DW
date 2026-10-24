using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Talle_DW.Entities;

namespace Talle_DW.Security;

public interface ITokenService
{
    (string Token, DateTime Expira) Generar(Usuario usuario);
}

public class TokenService(IOptions<JwtOptions> opciones) : ITokenService
{
    private readonly JwtOptions _jwt = opciones.Value;

    public (string Token, DateTime Expira) Generar(Usuario usuario)
    {
        var expira = DateTime.UtcNow.AddHours(_jwt.ExpiracionHoras);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, usuario.UsuarioId.ToString()),
            new(ClaimTypes.Name, usuario.NombreUsuario),
            new(ClaimTypes.Role, usuario.Rol.Nombre),
            new(ClaimsTaller.NombreCompleto, usuario.NombreCompleto),
            new(ClaimsTaller.RolId, usuario.RolId.ToString()),
            new(ClaimsTaller.EsAdministrador, usuario.Rol.EsSistema ? "true" : "false")
        };

        var credenciales = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwt.Clave)),
            SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _jwt.Emisor,
            audience: _jwt.Audiencia,
            claims: claims,
            expires: expira,
            signingCredentials: credenciales);

        return (new JwtSecurityTokenHandler().WriteToken(token), expira);
    }
}

public static class ClaimsTaller
{
    public const string NombreCompleto = "nombre_completo";
    public const string RolId = "rol_id";
    public const string EsAdministrador = "es_admin";
}
