using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Components;
using Taller_DWF.Models;

namespace Taller_DWF.Services;

public class ApiException(string mensaje, int status) : Exception(mensaje)
{
    public int Status { get; } = status;
}

/// <summary>
/// Cliente HTTP de la API: agrega el token, traduce los errores a ApiException
/// y cierra la sesión cuando el token deja de ser válido.
/// </summary>
public class ApiClient(HttpClient http, SesionService sesion, NavigationManager navegacion)
{
    public async Task<T> GetAsync<T>(string url) => await LeerAsync<T>(await EnviarAsync(HttpMethod.Get, url));

    public async Task<T> PostAsync<T>(string url, object? cuerpo) => await LeerAsync<T>(await EnviarAsync(HttpMethod.Post, url, cuerpo));

    public async Task PostAsync(string url, object? cuerpo) => await EnviarAsync(HttpMethod.Post, url, cuerpo);

    public async Task<T> PutAsync<T>(string url, object? cuerpo) => await LeerAsync<T>(await EnviarAsync(HttpMethod.Put, url, cuerpo));

    public async Task PutAsync(string url, object? cuerpo) => await EnviarAsync(HttpMethod.Put, url, cuerpo);

    public async Task PatchAsync(string url, object? cuerpo) => await EnviarAsync(HttpMethod.Patch, url, cuerpo);

    public async Task DeleteAsync(string url) => await EnviarAsync(HttpMethod.Delete, url);

    /// <summary>Arma una URL con parámetros de consulta, omitiendo los vacíos.</summary>
    public static string Url(string ruta, params (string Clave, object? Valor)[] parametros)
    {
        var partes = parametros
            .Where(p => p.Valor is not null && p.Valor.ToString() != string.Empty)
            .Select(p => $"{p.Clave}={Uri.EscapeDataString(p.Valor switch
            {
                DateTime f => f.ToString("yyyy-MM-dd"),
                DateOnly f => f.ToString("yyyy-MM-dd"),
                bool b => b ? "true" : "false",
                _ => p.Valor!.ToString()!
            })}");
        var consulta = string.Join("&", partes);
        return consulta.Length == 0 ? ruta : $"{ruta}?{consulta}";
    }

    private async Task<HttpResponseMessage> EnviarAsync(HttpMethod metodo, string url, object? cuerpo = null)
    {
        using var solicitud = new HttpRequestMessage(metodo, url);
        if (cuerpo is not null) solicitud.Content = JsonContent.Create(cuerpo);
        if (sesion.Token is not null) solicitud.Headers.Authorization = new("Bearer", sesion.Token);

        HttpResponseMessage respuesta;
        try
        {
            respuesta = await http.SendAsync(solicitud);
        }
        catch (HttpRequestException)
        {
            throw new ApiException("No se pudo conectar con el servidor. Verifique que la API esté en ejecución.", 0);
        }

        if (respuesta.IsSuccessStatusCode) return respuesta;

        if (respuesta.StatusCode == HttpStatusCode.Unauthorized && sesion.Token is not null)
        {
            await sesion.CerrarAsync();
            navegacion.NavigateTo("login?expirada=1");
            throw new ApiException("Su sesión expiró. Inicie sesión nuevamente.", 401);
        }

        var mensaje = respuesta.StatusCode switch
        {
            HttpStatusCode.Forbidden => "No tiene permiso para realizar esta acción.",
            HttpStatusCode.NotFound => "El registro solicitado no existe.",
            _ => "Ocurrió un error al procesar la solicitud."
        };

        try
        {
            var error = await respuesta.Content.ReadFromJsonAsync<RespuestaError>();
            if (!string.IsNullOrWhiteSpace(error?.Mensaje)) mensaje = error.Mensaje;
        }
        catch
        {
            // La respuesta no traía cuerpo JSON (por ejemplo, un 403 del middleware de autorización)
        }

        throw new ApiException(mensaje, (int)respuesta.StatusCode);
    }

    private static async Task<T> LeerAsync<T>(HttpResponseMessage respuesta) =>
        (await respuesta.Content.ReadFromJsonAsync<T>())!;
}
