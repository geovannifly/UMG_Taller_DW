namespace Taller_DWF.Models;

public class CambiarEstadoRequest
{
    public bool Activo { get; set; }
}

public record OpcionDto(int Id, string Nombre);

/// <summary>Formato de error que devuelve la API: { mensaje, errores }.</summary>
public record RespuestaError(string Mensaje, Dictionary<string, string[]>? Errores = null);
