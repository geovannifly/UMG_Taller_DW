using Microsoft.EntityFrameworkCore;
using Talle_DW.Common;
using Talle_DW.Data;
using Talle_DW.DTOs;
using Talle_DW.Security;
using Talle_DW.Services.Interfaces;

namespace Talle_DW.Services;

public class ReparacionService(TallerDbContext db, IUsuarioActual actual, IBitacoraService bitacora) : IReparacionService
{
    public async Task<List<ColumnaTableroDto>> ObtenerTableroAsync(bool soloMias)
    {
        string[] columnas = soloMias
            ? [EstadosOrden.EnDiagnostico, EstadosOrden.EnReparacion, EstadosOrden.EsperandoRepuestos, EstadosOrden.Finalizado]
            : [EstadosOrden.Recibido, EstadosOrden.EnDiagnostico, EstadosOrden.EnReparacion, EstadosOrden.EsperandoRepuestos, EstadosOrden.Finalizado];

        var query = db.Ordenes.AsNoTracking().Where(o => columnas.Contains(o.Estado));
        if (soloMias) query = query.Where(o => o.MecanicoId == actual.Id);

        var ordenes = await query
            .OrderBy(o => o.Prioridad == "URGENTE" ? 0 : o.Prioridad == "ALTA" ? 1 : o.Prioridad == "NORMAL" ? 2 : 3)
            .ThenBy(o => o.FechaIngreso)
            .Select(Proyecciones.OrdenResumen)
            .ToListAsync();

        return columnas
            .Select(e => new ColumnaTableroDto(e, FlujoOrden.Descripcion(e), ordenes.Where(o => o.Estado == e).ToList()))
            .ToList();
    }

    public async Task CambiarEstadoAsync(int ordenId, CambiarEstadoOrdenRequest request)
    {
        var orden = await db.Ordenes.FirstOrDefaultAsync(o => o.OrdenId == ordenId)
            ?? throw new NoEncontradoException("La orden de trabajo no existe.");

        ReglasTaller.ValidarResponsable(orden, actual);

        if (!FlujoOrden.EsPermitido(orden.Estado, request.Estado))
            throw new ReglaNegocioException(
                $"No se puede pasar de '{FlujoOrden.Descripcion(orden.Estado)}' a '{FlujoOrden.Descripcion(request.Estado)}'.");

        if (!await db.Diagnosticos.AnyAsync(d => d.OrdenId == ordenId))
            throw new ReglaNegocioException("Registre al menos un diagnóstico antes de avanzar la reparación.");

        if (request.Estado == EstadosOrden.Finalizado
            && await db.Solicitudes.AnyAsync(s => s.OrdenId == ordenId && s.Estado == EstadosSolicitud.Pendiente))
            throw new ReglaNegocioException("Hay solicitudes de repuestos pendientes en bodega. Espere el despacho o cancélelas.");

        if (orden.Estado == EstadosOrden.Finalizado
            && await db.Facturas.AnyAsync(f => f.OrdenId == ordenId && f.Estado == EstadosFactura.Emitida))
            throw new ReglaNegocioException("La orden ya fue facturada; anule la factura antes de reabrir la reparación.");

        var anterior = orden.Estado;
        var comentario = string.IsNullOrWhiteSpace(request.Comentario) ? null : request.Comentario.Trim();
        orden.CambiarEstado(request.Estado, actual.Id, comentario);

        await db.SaveChangesAsync();
        await bitacora.RegistrarAsync(Modulos.Reparaciones, "ESTADO",
            $"Orden {orden.NumeroOrden}: {FlujoOrden.Descripcion(anterior)} → {FlujoOrden.Descripcion(request.Estado)}.");
    }
}
