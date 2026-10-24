using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Talle_DW.Common;

namespace Talle_DW.Security;

/// <summary>
/// Exige que el rol del usuario tenga la acción indicada sobre el módulo.
/// Se pueden indicar varios módulos separados por coma: basta con tener permiso en uno.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public class RequierePermisoAttribute(string modulos, Accion accion = Accion.Ver)
    : AuthorizeAttribute, IAuthorizationRequirementData
{
    public IEnumerable<IAuthorizationRequirement> GetRequirements()
    {
        yield return new PermisoRequirement(
            modulos.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries), accion);
    }
}

public class PermisoRequirement(string[] modulos, Accion accion) : IAuthorizationRequirement
{
    public string[] Modulos { get; } = modulos;
    public Accion Accion { get; } = accion;
}

public class PermisoHandler(IPermisoService permisos) : AuthorizationHandler<PermisoRequirement>
{
    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, PermisoRequirement requirement)
    {
        if (!int.TryParse(context.User.FindFirstValue(ClaimTypes.NameIdentifier), out var usuarioId)) return;

        var perfil = await permisos.ObtenerPerfilAsync(usuarioId);
        if (perfil is not { Activo: true }) return;

        if (requirement.Modulos.Any(m => perfil.Tiene(m, requirement.Accion)))
            context.Succeed(requirement);
    }
}
