using Taller_DWF.Models;

namespace Taller_DWF.Services.Api;

public class FacturasApi(ApiClient api)
{
    public Task<List<FacturaResumenDto>> ListarAsync(string? buscar = null, string? estado = null, DateTime? desde = null, DateTime? hasta = null) =>
        api.GetAsync<List<FacturaResumenDto>>(ApiClient.Url("api/facturas", ("buscar", buscar), ("estado", estado),
            ("desde", desde), ("hasta", hasta)));
    public Task<FacturaDetalleDto> ObtenerAsync(int id) => api.GetAsync<FacturaDetalleDto>($"api/facturas/{id}");
    public Task<List<OrdenResumenDto>> PorFacturarAsync() => api.GetAsync<List<OrdenResumenDto>>("api/facturas/por-facturar");
    public Task<FacturaPreliminarDto> PreliminarAsync(int ordenId) => api.GetAsync<FacturaPreliminarDto>($"api/facturas/preliminar/{ordenId}");
    public Task<FacturaDetalleDto> EmitirAsync(EmitirFacturaRequest r) => api.PostAsync<FacturaDetalleDto>("api/facturas", r);
    public Task<FacturaDetalleDto> AnularAsync(int id, string motivo) =>
        api.PostAsync<FacturaDetalleDto>($"api/facturas/{id}/anular", new AnularFacturaRequest { Motivo = motivo });
}
