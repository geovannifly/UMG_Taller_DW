using Talle_DW.DTOs;
using Talle_DW.Entities;

namespace Talle_DW.Services.Interfaces;

/// <summary>
/// API de bodega: consulta de existencias en tiempo real y control atómico del stock.
/// </summary>
public interface IBodegaService
{
    Task<List<ExistenciaDto>> ListarExistenciasAsync(string? buscar, int? categoriaId, bool soloDisponibles);
    Task<ConsultaExistenciaDto> ConsultarExistenciaAsync(int repuestoId, int cantidad);

    /// <summary>
    /// Suma (o resta) stock de forma atómica y agrega el movimiento al contexto.
    /// Devuelve null si el stock resultante sería negativo. Debe llamarse dentro de una transacción.
    /// </summary>
    Task<MovimientoInventario?> MoverStockAsync(int repuestoId, int delta, string tipo, string? referencia, string? observacion);
}

public interface IInventarioService
{
    Task<List<RepuestoDto>> ListarRepuestosAsync(string? buscar, int? categoriaId, string? estadoStock, bool incluirInactivos);
    Task<RepuestoDetalleDto> ObtenerRepuestoAsync(int id);
    Task<RepuestoDto> CrearRepuestoAsync(RepuestoGuardarRequest request);
    Task<RepuestoDto> ActualizarRepuestoAsync(int id, RepuestoGuardarRequest request);
    Task CambiarEstadoRepuestoAsync(int id, bool activo);
    Task<MovimientoDto> RegistrarMovimientoAsync(int repuestoId, MovimientoCrearRequest request);
    Task<List<MovimientoDto>> ListarMovimientosAsync(int? repuestoId, string? tipo, DateTime? desde, DateTime? hasta);

    Task<List<CategoriaDto>> ListarCategoriasAsync(bool incluirInactivas);
    Task<CategoriaDto> CrearCategoriaAsync(CategoriaGuardarRequest request);
    Task<CategoriaDto> ActualizarCategoriaAsync(int id, CategoriaGuardarRequest request);
    Task CambiarEstadoCategoriaAsync(int id, bool activo);
}
