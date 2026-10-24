namespace Talle_DW.DTOs;

/// <summary>
/// Indicadores de la pantalla de inicio. Cada sección es null si el usuario no tiene
/// permiso sobre el módulo que la alimenta.
/// </summary>
public record DashboardDto(
    int? OrdenesActivas,
    Dictionary<string, int>? OrdenesPorEstado,
    int? ListasParaEntrega,
    int? EntregadasHoy,
    int? MisTrabajosActivos,
    List<OrdenResumenDto>? MisTrabajos,
    int? SolicitudesPendientes,
    int? RepuestosStockBajo,
    List<ExistenciaDto>? AlertasStock,
    decimal? FacturadoMes,
    int? FacturasMes,
    int? OrdenesPorFacturar,
    List<OrdenResumenDto>? UltimasOrdenes);
