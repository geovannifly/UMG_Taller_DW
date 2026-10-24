using Talle_DW.DTOs;

namespace Talle_DW.Services.Interfaces;

public interface IFacturaService
{
    Task<List<FacturaResumenDto>> ListarAsync(string? buscar, string? estado, DateTime? desde, DateTime? hasta);
    Task<FacturaDetalleDto> ObtenerAsync(int id);
    Task<List<OrdenResumenDto>> ListarOrdenesPorFacturarAsync();
    Task<FacturaPreliminarDto> ObtenerPreliminarAsync(int ordenId);
    Task<FacturaDetalleDto> EmitirAsync(EmitirFacturaRequest request);
    Task<FacturaDetalleDto> AnularAsync(int id, AnularFacturaRequest request);
}

public interface IDashboardService
{
    Task<DashboardDto> ObtenerAsync();
}
