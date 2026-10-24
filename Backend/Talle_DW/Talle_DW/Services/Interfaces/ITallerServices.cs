using Talle_DW.DTOs;

namespace Talle_DW.Services.Interfaces;

public interface IDiagnosticoService
{
    Task<List<DiagnosticoDto>> ListarPorOrdenAsync(int ordenId);
    Task<DiagnosticoDto> CrearAsync(DiagnosticoGuardarRequest request);
    Task<DiagnosticoDto> ActualizarAsync(int id, DiagnosticoGuardarRequest request);
    Task EliminarAsync(int id);
}

public interface IReparacionService
{
    Task<List<ColumnaTableroDto>> ObtenerTableroAsync(bool soloMias);
    Task CambiarEstadoAsync(int ordenId, CambiarEstadoOrdenRequest request);
}

public interface ISolicitudService
{
    Task<List<SolicitudDto>> ListarAsync(int? ordenId, string? estado, bool soloMias);
    Task<SolicitudDto> CrearAsync(SolicitudCrearRequest request);
    Task CancelarAsync(int id);
    Task<SolicitudDto> DespacharAsync(int id, DespacharRequest request);
    Task<SolicitudDto> RechazarAsync(int id, RechazarRequest request);
}
