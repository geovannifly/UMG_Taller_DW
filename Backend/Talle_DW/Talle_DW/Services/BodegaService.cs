using Microsoft.EntityFrameworkCore;
using Talle_DW.Common;
using Talle_DW.Data;
using Talle_DW.DTOs;
using Talle_DW.Entities;
using Talle_DW.Security;
using Talle_DW.Services.Interfaces;

namespace Talle_DW.Services;

public class BodegaService(TallerDbContext db, IUsuarioActual actual) : IBodegaService
{
    public async Task<List<ExistenciaDto>> ListarExistenciasAsync(string? buscar, int? categoriaId, bool soloDisponibles)
    {
        var query = db.Repuestos.AsNoTracking().Where(r => r.Activo);

        if (categoriaId.HasValue) query = query.Where(r => r.CategoriaId == categoriaId);
        if (!string.IsNullOrWhiteSpace(buscar))
        {
            var texto = buscar.Trim();
            query = query.Where(r => r.Codigo.Contains(texto) || r.Nombre.Contains(texto)
                                  || (r.Marca != null && r.Marca.Contains(texto)));
        }

        var existencias = await query
            .OrderBy(r => r.Nombre)
            .Select(r => new
            {
                r.RepuestoId, r.Codigo, r.Nombre, Categoria = r.Categoria.Nombre, r.Marca, r.PrecioVenta,
                r.Stock, r.StockMinimo, r.Ubicacion,
                Reservado = r.Solicitudes.Where(s => s.Estado == EstadosSolicitud.Pendiente).Sum(s => (int?)s.Cantidad) ?? 0
            })
            .ToListAsync();

        return existencias
            .Select(r => new ExistenciaDto(r.RepuestoId, r.Codigo, r.Nombre, r.Categoria, r.Marca, r.PrecioVenta,
                r.Stock, r.Reservado, r.Stock - r.Reservado, r.StockMinimo, r.Ubicacion,
                Proyecciones.EstadoStock(r.Stock - r.Reservado, r.Stock, r.StockMinimo)))
            .Where(r => !soloDisponibles || r.Disponible > 0)
            .ToList();
    }

    public async Task<ConsultaExistenciaDto> ConsultarExistenciaAsync(int repuestoId, int cantidad)
    {
        if (cantidad <= 0) throw new ReglaNegocioException("La cantidad a consultar debe ser mayor a cero.");

        var repuesto = await db.Repuestos.AsNoTracking()
            .Where(r => r.RepuestoId == repuestoId)
            .Select(r => new
            {
                r.RepuestoId, r.Codigo, r.Nombre, r.Stock, r.Activo,
                Reservado = r.Solicitudes.Where(s => s.Estado == EstadosSolicitud.Pendiente).Sum(s => (int?)s.Cantidad) ?? 0
            })
            .FirstOrDefaultAsync()
            ?? throw new NoEncontradoException("El repuesto no existe en bodega.");

        if (!repuesto.Activo) throw new ReglaNegocioException("El repuesto está dado de baja en bodega.");

        var disponible = Math.Max(repuesto.Stock - repuesto.Reservado, 0);
        var suficiente = disponible >= cantidad;
        var mensaje = suficiente
            ? $"Hay existencia suficiente: {disponible} disponible(s)."
            : disponible == 0
                ? $"{repuesto.Nombre} está agotado en bodega (stock {repuesto.Stock}, reservado {repuesto.Reservado})."
                : $"Existencia insuficiente de {repuesto.Nombre}: solicitó {cantidad} y solo hay {disponible} disponible(s).";

        return new ConsultaExistenciaDto(repuesto.RepuestoId, repuesto.Codigo, repuesto.Nombre, repuesto.Stock,
            repuesto.Reservado, disponible, cantidad, suficiente, mensaje, DateTime.Now);
    }

    public async Task<MovimientoInventario?> MoverStockAsync(int repuestoId, int delta, string tipo, string? referencia, string? observacion)
    {
        // UPDATE atómico: evita que dos despachos simultáneos dejen el stock en negativo
        var resultado = await db.Database.SqlQuery<CambioStock>($"""
            UPDATE dbo.Repuestos
               SET Stock = Stock + {delta}
            OUTPUT deleted.Stock AS StockAnterior, inserted.Stock AS StockNuevo
             WHERE RepuestoId = {repuestoId} AND Stock + {delta} >= 0
            """).ToListAsync();

        if (resultado.Count == 0) return null;

        var movimiento = new MovimientoInventario
        {
            RepuestoId = repuestoId,
            Tipo = tipo,
            Cantidad = delta,
            StockAnterior = resultado[0].StockAnterior,
            StockNuevo = resultado[0].StockNuevo,
            Referencia = referencia,
            Observacion = observacion,
            UsuarioId = actual.Id,
            Fecha = DateTime.Now
        };
        db.Movimientos.Add(movimiento);
        return movimiento;
    }

    private class CambioStock
    {
        public int StockAnterior { get; set; }
        public int StockNuevo { get; set; }
    }
}
