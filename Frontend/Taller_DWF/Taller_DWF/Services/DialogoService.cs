namespace Taller_DWF.Services;

public class SolicitudDialogo
{
    public string Titulo { get; init; } = string.Empty;
    public string Mensaje { get; init; } = string.Empty;
    public string TextoConfirmar { get; init; } = "Confirmar";
    public bool Peligro { get; init; }
    /// <summary>Si tiene valor, el diálogo pide un texto obligatorio (motivo).</summary>
    public string? EtiquetaTexto { get; init; }
    public TaskCompletionSource<(bool Confirmado, string? Texto)> Resultado { get; } = new();
}

/// <summary>Diálogos de confirmación reutilizables (el host vive en MainLayout).</summary>
public class DialogoService
{
    public SolicitudDialogo? Actual { get; private set; }
    public event Action? Cambio;

    public async Task<bool> ConfirmarAsync(string titulo, string mensaje, string textoConfirmar = "Confirmar", bool peligro = false)
    {
        var (confirmado, _) = await MostrarAsync(new SolicitudDialogo
        {
            Titulo = titulo, Mensaje = mensaje, TextoConfirmar = textoConfirmar, Peligro = peligro
        });
        return confirmado;
    }

    /// <summary>Pide un motivo; devuelve null si el usuario cancela.</summary>
    public async Task<string?> PedirMotivoAsync(string titulo, string mensaje, string etiqueta = "Motivo",
        string textoConfirmar = "Confirmar", bool peligro = true)
    {
        var (confirmado, texto) = await MostrarAsync(new SolicitudDialogo
        {
            Titulo = titulo, Mensaje = mensaje, TextoConfirmar = textoConfirmar, Peligro = peligro, EtiquetaTexto = etiqueta
        });
        return confirmado ? texto : null;
    }

    public void Responder(bool confirmado, string? texto = null)
    {
        var actual = Actual;
        Actual = null;
        Cambio?.Invoke();
        actual?.Resultado.TrySetResult((confirmado, texto));
    }

    private Task<(bool, string?)> MostrarAsync(SolicitudDialogo dialogo)
    {
        Actual?.Resultado.TrySetResult((false, null));
        Actual = dialogo;
        Cambio?.Invoke();
        return dialogo.Resultado.Task;
    }
}
