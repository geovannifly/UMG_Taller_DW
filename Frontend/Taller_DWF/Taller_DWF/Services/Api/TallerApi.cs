using Taller_DWF.Models;

namespace Taller_DWF.Services.Api;

public class ReparacionesApi(ApiClient api)
{
    public Task<List<ColumnaTableroDto>> TableroAsync(bool soloMias) =>
        api.GetAsync<List<ColumnaTableroDto>>(ApiClient.Url("api/reparaciones/tablero", ("soloMias", soloMias)));
    public Task CambiarEstadoAsync(int ordenId, string estado, string? comentario) =>
        api.PostAsync($"api/reparaciones/{ordenId}/estado", new CambiarEstadoOrdenRequest { Estado = estado, Comentario = comentario });
}

public class DiagnosticosApi(ApiClient api)
{
    public Task<DiagnosticoDto> CrearAsync(DiagnosticoGuardarRequest r) => api.PostAsync<DiagnosticoDto>("api/diagnosticos", r);
    public Task<DiagnosticoDto> ActualizarAsync(int id, DiagnosticoGuardarRequest r) => api.PutAsync<DiagnosticoDto>($"api/diagnosticos/{id}", r);
    public Task EliminarAsync(int id) => api.DeleteAsync($"api/diagnosticos/{id}");
}

public class SolicitudesApi(ApiClient api)
{
    public Task<List<SolicitudDto>> ListarAsync(int? ordenId = null, string? estado = null, bool soloMias = false) =>
        api.GetAsync<List<SolicitudDto>>(ApiClient.Url("api/solicitudes", ("ordenId", ordenId), ("estado", estado), ("soloMias", soloMias)));
    public Task<SolicitudDto> CrearAsync(SolicitudCrearRequest r) => api.PostAsync<SolicitudDto>("api/solicitudes", r);
    public Task CancelarAsync(int id) => api.DeleteAsync($"api/solicitudes/{id}");
}
