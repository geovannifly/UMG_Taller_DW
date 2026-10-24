namespace Talle_DW.DTOs;

public class CambiarEstadoRequest
{
    public bool Activo { get; set; }
}

public record OpcionDto(int Id, string Nombre);
