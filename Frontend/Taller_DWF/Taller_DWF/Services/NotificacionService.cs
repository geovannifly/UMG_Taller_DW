namespace Taller_DWF.Services;

public record Notificacion(Guid Id, string Tipo, string Mensaje);

/// <summary>Mensajes emergentes (toasts) en la esquina de la pantalla.</summary>
public class NotificacionService
{
    private readonly List<Notificacion> _activas = [];

    public IReadOnlyList<Notificacion> Activas => _activas;
    public event Action? Cambio;

    public void Exito(string mensaje) => Agregar("exito", mensaje);
    public void Error(string mensaje) => Agregar("error", mensaje);
    public void Info(string mensaje) => Agregar("info", mensaje);

    public void Cerrar(Guid id)
    {
        _activas.RemoveAll(n => n.Id == id);
        Cambio?.Invoke();
    }

    private void Agregar(string tipo, string mensaje)
    {
        var notificacion = new Notificacion(Guid.NewGuid(), tipo, mensaje);
        _activas.Add(notificacion);
        Cambio?.Invoke();
        _ = CerrarDespuesAsync(notificacion.Id, tipo == "error" ? 6000 : 3500);
    }

    private async Task CerrarDespuesAsync(Guid id, int milisegundos)
    {
        await Task.Delay(milisegundos);
        Cerrar(id);
    }
}
