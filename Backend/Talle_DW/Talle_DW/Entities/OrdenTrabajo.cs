namespace Talle_DW.Entities;

public class OrdenTrabajo
{
    public int OrdenId { get; set; }
    /// <summary>Columna calculada en SQL Server: OT-000001.</summary>
    public string NumeroOrden { get; set; } = string.Empty;
    public int VehiculoId { get; set; }
    public int ClienteId { get; set; }
    public int RecepcionistaId { get; set; }
    public int? MecanicoId { get; set; }
    public DateTime FechaIngreso { get; set; } = DateTime.Now;
    public DateOnly? FechaEstimada { get; set; }
    public int Kilometraje { get; set; }
    public string NivelCombustible { get; set; } = string.Empty;
    public string MotivoIngreso { get; set; } = string.Empty;
    public string? Observaciones { get; set; }
    public string Prioridad { get; set; } = "NORMAL";
    public string Estado { get; set; } = "RECIBIDO";
    public DateTime? FechaSalida { get; set; }
    public int? UsuarioSalidaId { get; set; }
    public string? ObservacionSalida { get; set; }

    public Vehiculo Vehiculo { get; set; } = null!;
    public Cliente Cliente { get; set; } = null!;
    public Usuario Recepcionista { get; set; } = null!;
    public Usuario? Mecanico { get; set; }
    public Usuario? UsuarioSalida { get; set; }

    public ICollection<HistorialOrden> Historial { get; set; } = [];
    public ICollection<Diagnostico> Diagnosticos { get; set; } = [];
    public ICollection<SolicitudRepuesto> Solicitudes { get; set; } = [];
    public ICollection<Factura> Facturas { get; set; } = [];

    /// <summary>Cambia el estado y deja constancia en el historial de la orden.</summary>
    public void CambiarEstado(string nuevoEstado, int usuarioId, string? comentario)
    {
        Historial.Add(new HistorialOrden
        {
            EstadoAnterior = Estado,
            EstadoNuevo = nuevoEstado,
            UsuarioId = usuarioId,
            Comentario = comentario
        });
        Estado = nuevoEstado;
    }

    /// <summary>Registra un evento en el historial sin cambiar el estado.</summary>
    public void RegistrarEvento(int usuarioId, string comentario)
    {
        Historial.Add(new HistorialOrden
        {
            EstadoAnterior = Estado,
            EstadoNuevo = Estado,
            UsuarioId = usuarioId,
            Comentario = comentario.Length > 300 ? comentario[..297] + "..." : comentario
        });
    }
}
