using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace Talle_DW.Common;

/// <summary>
/// Agrega la autenticación Bearer (JWT) al documento OpenAPI para probar la API desde /scalar/v1.
/// </summary>
public sealed class EsquemaBearerTransformer : IOpenApiDocumentTransformer
{
    public Task TransformAsync(OpenApiDocument document, OpenApiDocumentTransformerContext context, CancellationToken cancellationToken)
    {
        document.Info.Title = "Taller DW · API";
        document.Info.Description = "API REST para la gestión del taller: recepción, diagnósticos, reparaciones, bodega y facturación.";

        document.Components ??= new OpenApiComponents();
        document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
        document.Components.SecuritySchemes["Bearer"] = new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            In = ParameterLocation.Header,
            Description = "Token obtenido en POST /api/auth/login"
        };

        document.Security ??= [];
        document.Security.Add(new OpenApiSecurityRequirement
        {
            [new OpenApiSecuritySchemeReference("Bearer", document)] = []
        });

        return Task.CompletedTask;
    }
}
