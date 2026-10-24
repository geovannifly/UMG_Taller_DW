using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.JSInterop;
using Taller_DWF.Models;

namespace Taller_DWF.Services;

public enum Accion { Ver, Crear, Editar, Eliminar }

/// <summary>
/// Guarda el token y el perfil del usuario (con sus permisos) en memoria y en localStorage.
/// </summary>
public class SesionService(IJSRuntime js, HttpClient http)
{
    private const string ClaveToken = "tallerdw.token";
    private const string ClaveExpira = "tallerdw.expira";

    public string? Token { get; private set; }
    public DateTime? Expira { get; private set; }
    public PerfilUsuarioDto? Perfil { get; private set; }

    public bool Autenticado => Token is not null && Perfil is not null && Expira > DateTime.UtcNow;

    public event Action? Cambio;

    /// <summary>Recupera la sesión guardada y refresca los permisos desde la API.</summary>
    public async Task InicializarAsync()
    {
        try
        {
            var token = await js.InvokeAsync<string?>("localStorage.getItem", ClaveToken);
            var expira = await js.InvokeAsync<string?>("localStorage.getItem", ClaveExpira);
            if (token is null || !DateTime.TryParse(expira, null, System.Globalization.DateTimeStyles.RoundtripKind, out var fecha)
                || fecha <= DateTime.UtcNow)
            {
                await LimpiarAsync();
                return;
            }

            Token = token;
            Expira = fecha;
            await RefrescarPerfilAsync();
        }
        catch
        {
            await LimpiarAsync();
        }
    }

    public async Task IniciarAsync(LoginResponse respuesta)
    {
        Token = respuesta.Token;
        Expira = respuesta.Expira.ToUniversalTime();
        Perfil = respuesta.Usuario;
        await js.InvokeVoidAsync("localStorage.setItem", ClaveToken, Token);
        await js.InvokeVoidAsync("localStorage.setItem", ClaveExpira, Expira.Value.ToString("O"));
        Cambio?.Invoke();
    }

    /// <summary>Vuelve a pedir el perfil para reflejar cambios de permisos hechos por el administrador.</summary>
    public async Task RefrescarPerfilAsync()
    {
        if (Token is null) return;

        using var solicitud = new HttpRequestMessage(HttpMethod.Get, "api/auth/perfil");
        solicitud.Headers.Authorization = new("Bearer", Token);
        var respuesta = await http.SendAsync(solicitud);

        if (!respuesta.IsSuccessStatusCode)
        {
            await LimpiarAsync();
            return;
        }

        Perfil = await respuesta.Content.ReadFromJsonAsync<PerfilUsuarioDto>(new JsonSerializerOptions(JsonSerializerDefaults.Web));
        Cambio?.Invoke();
    }

    public async Task CerrarAsync()
    {
        await LimpiarAsync();
        Cambio?.Invoke();
    }

    public bool Puede(string modulo, Accion accion = Accion.Ver)
    {
        var permiso = Perfil?.Permisos.FirstOrDefault(p => p.Codigo == modulo);
        return permiso is not null && accion switch
        {
            Accion.Ver => permiso.Ver,
            Accion.Crear => permiso.Crear,
            Accion.Editar => permiso.Editar,
            Accion.Eliminar => permiso.Eliminar,
            _ => false
        };
    }

    public bool PuedeAlguno(params string[] modulos) => modulos.Any(m => Puede(m));

    private async Task LimpiarAsync()
    {
        Token = null;
        Expira = null;
        Perfil = null;
        try
        {
            await js.InvokeVoidAsync("localStorage.removeItem", ClaveToken);
            await js.InvokeVoidAsync("localStorage.removeItem", ClaveExpira);
        }
        catch
        {
            // localStorage puede no estar disponible (modo privado); la sesión queda solo en memoria
        }
    }
}
