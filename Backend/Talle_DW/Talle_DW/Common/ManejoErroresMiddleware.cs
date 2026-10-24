using Microsoft.EntityFrameworkCore;

namespace Talle_DW.Common;

/// <summary>
/// Convierte las excepciones en respuestas JSON uniformes: { "mensaje": "..." }.
/// </summary>
public class ManejoErroresMiddleware(RequestDelegate next, ILogger<ManejoErroresMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            var (status, mensaje) = ex switch
            {
                ReglaNegocioException => (StatusCodes.Status400BadRequest, ex.Message),
                NoEncontradoException => (StatusCodes.Status404NotFound, ex.Message),
                AccesoDenegadoException => (StatusCodes.Status403Forbidden, ex.Message),
                NoAutorizadoException => (StatusCodes.Status401Unauthorized, ex.Message),
                DbUpdateException => (StatusCodes.Status409Conflict,
                    "No se pudo guardar la información porque entra en conflicto con datos existentes."),
                _ => (StatusCodes.Status500InternalServerError,
                    "Ocurrió un error inesperado. Intente de nuevo o contacte al administrador.")
            };

            if (status >= 500 || ex is DbUpdateException)
                logger.LogError(ex, "Error procesando {Metodo} {Ruta}", context.Request.Method, context.Request.Path);

            if (context.Response.HasStarted) throw;

            context.Response.Clear();
            context.Response.StatusCode = status;
            await context.Response.WriteAsJsonAsync(new RespuestaError(mensaje));
        }
    }
}

public record RespuestaError(string Mensaje, IDictionary<string, string[]>? Errores = null);
