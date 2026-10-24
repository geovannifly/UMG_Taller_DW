using Microsoft.EntityFrameworkCore;
using Talle_DW.Common;
using Talle_DW.Data;
using Talle_DW.DTOs;
using Talle_DW.Entities;
using Talle_DW.Services.Interfaces;

namespace Talle_DW.Services;

public class InventarioService(TallerDbContext db, IBodegaService bodega, IBitacoraService bitacora) : IInventarioService
{
    // ------------------------- Repuestos -------------------------

    public async Task<List<RepuestoDto>> ListarRepuestosAsync(string? buscar, int? categoriaId, string? estadoStock, bool incluirInactivos)
    {
        var query = db.Repuestos.AsNoTracking();

        if (!incluirInactivos) query = query.Where(r => r.Activo);
        if (categoriaId.HasValue) query = query.Where(r => r.CategoriaId == categoriaId);
        if (!string.IsNullOrWhiteSpace(buscar))
        {
            var texto = buscar.Trim();
            query = query.Where(r => r.Codigo.Contains(texto) || r.Nombre.Contains(texto)
                                  || (r.Marca != null && r.Marca.Contains(texto))
                                  || (r.Ubicacion != null && r.Ubicacion.Contains(texto)));
        }

        var repuestos = await ConsultarAsync(query.OrderBy(r => r.Nombre));

        return string.IsNullOrWhiteSpace(estadoStock)
            ? repuestos
            : repuestos.Where(r => r.EstadoStock == estadoStock || (estadoStock == "BAJO" && r.EstadoStock == "AGOTADO")).ToList();
    }

    public async Task<RepuestoDetalleDto> ObtenerRepuestoAsync(int id)
    {
        var repuesto = (await ConsultarAsync(db.Repuestos.AsNoTracking().Where(r => r.RepuestoId == id))).FirstOrDefault()
            ?? throw new NoEncontradoException("El repuesto no existe.");

        var movimientos = await db.Movimientos.AsNoTracking()
            .Where(m => m.RepuestoId == id)
            .OrderByDescending(m => m.MovimientoId)
            .Take(25)
            .Select(Proyecciones.Movimiento)
            .ToListAsync();

        return new RepuestoDetalleDto(repuesto, movimientos);
    }

    public async Task<RepuestoDto> CrearRepuestoAsync(RepuestoGuardarRequest request)
    {
        var repuesto = new Repuesto { Stock = 0, FechaRegistro = DateTime.Now };
        await AplicarAsync(repuesto, request);

        await using var transaccion = await db.Database.BeginTransactionAsync();
        db.Repuestos.Add(repuesto);
        await db.SaveChangesAsync();

        if (request.StockInicial > 0)
        {
            await bodega.MoverStockAsync(repuesto.RepuestoId, request.StockInicial, TiposMovimiento.Entrada,
                "INV-INICIAL", "Existencia inicial al registrar el repuesto");
            await db.SaveChangesAsync();
        }

        await transaccion.CommitAsync();
        await bitacora.RegistrarAsync(Modulos.Inventario, "CREAR",
            $"Registró el repuesto {repuesto.Codigo} - {repuesto.Nombre} con stock inicial {request.StockInicial}.");

        return (await ObtenerRepuestoAsync(repuesto.RepuestoId)).Repuesto;
    }

    public async Task<RepuestoDto> ActualizarRepuestoAsync(int id, RepuestoGuardarRequest request)
    {
        var repuesto = await db.Repuestos.FindAsync(id) ?? throw new NoEncontradoException("El repuesto no existe.");
        await AplicarAsync(repuesto, request);

        await db.SaveChangesAsync();
        await bitacora.RegistrarAsync(Modulos.Inventario, "EDITAR", $"Actualizó el repuesto {repuesto.Codigo}.");

        return (await ObtenerRepuestoAsync(id)).Repuesto;
    }

    public async Task CambiarEstadoRepuestoAsync(int id, bool activo)
    {
        var repuesto = await db.Repuestos.FindAsync(id) ?? throw new NoEncontradoException("El repuesto no existe.");
        if (repuesto.Activo == activo) return;

        if (!activo && await db.Solicitudes.AnyAsync(s => s.RepuestoId == id && s.Estado == EstadosSolicitud.Pendiente))
            throw new ReglaNegocioException("El repuesto tiene solicitudes pendientes de despacho.");

        repuesto.Activo = activo;
        await db.SaveChangesAsync();
        await bitacora.RegistrarAsync(Modulos.Inventario, activo ? "ACTIVAR" : "DESACTIVAR",
            $"{(activo ? "Activó" : "Dio de baja")} el repuesto {repuesto.Codigo}.");
    }

    public async Task<MovimientoDto> RegistrarMovimientoAsync(int repuestoId, MovimientoCrearRequest request)
    {
        var repuesto = await db.Repuestos.AsNoTracking()
            .Where(r => r.RepuestoId == repuestoId)
            .Select(r => new
            {
                r.Codigo, r.Activo,
                Reservado = r.Solicitudes.Where(s => s.Estado == EstadosSolicitud.Pendiente).Sum(s => (int?)s.Cantidad) ?? 0
            })
            .FirstOrDefaultAsync()
            ?? throw new NoEncontradoException("El repuesto no existe.");

        if (!repuesto.Activo) throw new ReglaNegocioException("El repuesto está dado de baja.");

        if (request.Tipo == TiposMovimiento.Entrada && request.Cantidad <= 0)
            throw new ReglaNegocioException("La cantidad de una entrada debe ser mayor a cero.");
        if (request.Tipo == TiposMovimiento.Ajuste)
        {
            if (request.Cantidad == 0)
                throw new ReglaNegocioException("El ajuste debe sumar o restar al menos una unidad.");
            if (string.IsNullOrWhiteSpace(request.Observacion))
                throw new ReglaNegocioException("Indique el motivo del ajuste de inventario.");
        }

        await using var transaccion = await db.Database.BeginTransactionAsync();

        var movimiento = await bodega.MoverStockAsync(repuestoId, request.Cantidad, request.Tipo,
            string.IsNullOrWhiteSpace(request.Referencia) ? null : request.Referencia.Trim(),
            string.IsNullOrWhiteSpace(request.Observacion) ? null : request.Observacion.Trim())
            ?? throw new ReglaNegocioException("El ajuste dejaría el stock en negativo.");

        if (movimiento.StockNuevo < repuesto.Reservado)
            throw new ReglaNegocioException(
                $"El stock no puede quedar por debajo de lo reservado en solicitudes pendientes ({repuesto.Reservado}).");

        await db.SaveChangesAsync();
        await transaccion.CommitAsync();

        await bitacora.RegistrarAsync(Modulos.Inventario, request.Tipo,
            $"{request.Tipo} de {request.Cantidad} unidad(es) en {repuesto.Codigo}: {movimiento.StockAnterior} → {movimiento.StockNuevo}.");

        return await db.Movimientos.AsNoTracking().Where(m => m.MovimientoId == movimiento.MovimientoId)
            .Select(Proyecciones.Movimiento).FirstAsync();
    }

    public async Task<List<MovimientoDto>> ListarMovimientosAsync(int? repuestoId, string? tipo, DateTime? desde, DateTime? hasta)
    {
        var query = db.Movimientos.AsNoTracking();

        if (repuestoId.HasValue) query = query.Where(m => m.RepuestoId == repuestoId);
        if (!string.IsNullOrWhiteSpace(tipo)) query = query.Where(m => m.Tipo == tipo);
        if (desde.HasValue) query = query.Where(m => m.Fecha >= desde.Value.Date);
        if (hasta.HasValue) query = query.Where(m => m.Fecha < hasta.Value.Date.AddDays(1));

        return await query.OrderByDescending(m => m.MovimientoId).Take(500).Select(Proyecciones.Movimiento).ToListAsync();
    }

    // ------------------------- Categorías -------------------------

    public Task<List<CategoriaDto>> ListarCategoriasAsync(bool incluirInactivas) =>
        db.Categorias.AsNoTracking()
            .Where(c => incluirInactivas || c.Activo)
            .OrderBy(c => c.Nombre)
            .Select(c => new CategoriaDto(c.CategoriaId, c.Nombre, c.Descripcion, c.Activo, c.Repuestos.Count(r => r.Activo)))
            .ToListAsync();

    public async Task<CategoriaDto> CrearCategoriaAsync(CategoriaGuardarRequest request)
    {
        var nombre = request.Nombre.Trim();
        if (await db.Categorias.AnyAsync(c => c.Nombre == nombre))
            throw new ReglaNegocioException($"Ya existe la categoría '{nombre}'.");

        var categoria = new Categoria { Nombre = nombre, Descripcion = request.Descripcion?.Trim() };
        db.Categorias.Add(categoria);
        await db.SaveChangesAsync();
        await bitacora.RegistrarAsync(Modulos.Inventario, "CREAR", $"Creó la categoría '{nombre}'.");

        return new CategoriaDto(categoria.CategoriaId, categoria.Nombre, categoria.Descripcion, true, 0);
    }

    public async Task<CategoriaDto> ActualizarCategoriaAsync(int id, CategoriaGuardarRequest request)
    {
        var categoria = await db.Categorias.FindAsync(id) ?? throw new NoEncontradoException("La categoría no existe.");
        var nombre = request.Nombre.Trim();

        if (await db.Categorias.AnyAsync(c => c.Nombre == nombre && c.CategoriaId != id))
            throw new ReglaNegocioException($"Ya existe la categoría '{nombre}'.");

        categoria.Nombre = nombre;
        categoria.Descripcion = request.Descripcion?.Trim();
        await db.SaveChangesAsync();
        await bitacora.RegistrarAsync(Modulos.Inventario, "EDITAR", $"Actualizó la categoría '{nombre}'.");

        var cantidad = await db.Repuestos.CountAsync(r => r.CategoriaId == id && r.Activo);
        return new CategoriaDto(categoria.CategoriaId, categoria.Nombre, categoria.Descripcion, categoria.Activo, cantidad);
    }

    public async Task CambiarEstadoCategoriaAsync(int id, bool activo)
    {
        var categoria = await db.Categorias.FindAsync(id) ?? throw new NoEncontradoException("La categoría no existe.");
        if (categoria.Activo == activo) return;

        if (!activo && await db.Repuestos.AnyAsync(r => r.CategoriaId == id && r.Activo))
            throw new ReglaNegocioException("La categoría tiene repuestos activos; muévalos o desactívelos primero.");

        categoria.Activo = activo;
        await db.SaveChangesAsync();
        await bitacora.RegistrarAsync(Modulos.Inventario, activo ? "ACTIVAR" : "DESACTIVAR",
            $"{(activo ? "Activó" : "Desactivó")} la categoría '{categoria.Nombre}'.");
    }

    // ------------------------- Auxiliares -------------------------

    private static async Task<List<RepuestoDto>> ConsultarAsync(IQueryable<Repuesto> query)
    {
        var filas = await query.Select(r => new
        {
            r.RepuestoId, r.Codigo, r.Nombre, r.Descripcion, r.CategoriaId, Categoria = r.Categoria.Nombre, r.Marca,
            r.PrecioCosto, r.PrecioVenta, r.Stock, r.StockMinimo, r.Ubicacion, r.Activo,
            Reservado = r.Solicitudes.Where(s => s.Estado == EstadosSolicitud.Pendiente).Sum(s => (int?)s.Cantidad) ?? 0
        }).ToListAsync();

        return filas.Select(r => new RepuestoDto(r.RepuestoId, r.Codigo, r.Nombre, r.Descripcion, r.CategoriaId, r.Categoria,
                r.Marca, r.PrecioCosto, r.PrecioVenta, r.Stock, r.StockMinimo, r.Reservado, r.Stock - r.Reservado,
                r.Ubicacion, r.Activo, Proyecciones.EstadoStock(r.Stock - r.Reservado, r.Stock, r.StockMinimo)))
            .ToList();
    }

    private async Task AplicarAsync(Repuesto repuesto, RepuestoGuardarRequest request)
    {
        var codigo = request.Codigo.Trim().ToUpperInvariant();
        if (await db.Repuestos.AnyAsync(r => r.Codigo == codigo && r.RepuestoId != repuesto.RepuestoId))
            throw new ReglaNegocioException($"Ya existe un repuesto con el código {codigo}.");

        var categoria = await db.Categorias.FindAsync(request.CategoriaId)
            ?? throw new ReglaNegocioException("La categoría seleccionada no existe.");
        if (!categoria.Activo) throw new ReglaNegocioException("La categoría seleccionada está inactiva.");

        if (request.PrecioVenta < request.PrecioCosto)
            throw new ReglaNegocioException("El precio de venta no puede ser menor al precio de costo.");

        repuesto.Codigo = codigo;
        repuesto.Nombre = request.Nombre.Trim();
        repuesto.Descripcion = string.IsNullOrWhiteSpace(request.Descripcion) ? null : request.Descripcion.Trim();
        repuesto.CategoriaId = request.CategoriaId;
        repuesto.Marca = string.IsNullOrWhiteSpace(request.Marca) ? null : request.Marca.Trim();
        repuesto.PrecioCosto = Math.Round(request.PrecioCosto, 2);
        repuesto.PrecioVenta = Math.Round(request.PrecioVenta, 2);
        repuesto.StockMinimo = request.StockMinimo;
        repuesto.Ubicacion = string.IsNullOrWhiteSpace(request.Ubicacion) ? null : request.Ubicacion.Trim().ToUpperInvariant();
    }
}
