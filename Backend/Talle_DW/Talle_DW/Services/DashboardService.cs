using Microsoft.EntityFrameworkCore;
using Talle_DW.Common;
using Talle_DW.Data;
using Talle_DW.DTOs;
using Talle_DW.Security;
using Talle_DW.Services.Interfaces;

namespace Talle_DW.Services;

public class DashboardService(
    TallerDbContext db,
    IUsuarioActual actual,
    IPermisoService permisos,
    IBodegaService bodega) : IDashboardService
{
    public async Task<DashboardDto> ObtenerAsync()
    {
        var perfil = await permisos.ObtenerPerfilAsync(actual.Id)
            ?? throw new NoAutorizadoException("Sesión no válida.");

        bool Ve(string modulo) => perfil.Tiene(modulo, Accion.Ver);

        int? activas = null, listas = null, entregadasHoy = null, misActivos = null,
             solicitudesPendientes = null, stockBajo = null, facturasMes = null, porFacturar = null;
        Dictionary<string, int>? porEstado = null;
        List<OrdenResumenDto>? ultimas = null, misTrabajos = null;
        List<ExistenciaDto>? alertas = null;
        decimal? facturadoMes = null;

        if (Ve(Modulos.Ordenes))
        {
            porEstado = await db.Ordenes.AsNoTracking()
                .Where(o => EstadosOrden.Activos.Contains(o.Estado))
                .GroupBy(o => o.Estado)
                .Select(g => new { g.Key, Total = g.Count() })
                .ToDictionaryAsync(x => x.Key, x => x.Total);

            activas = porEstado.Values.Sum();
            listas = porEstado.GetValueOrDefault(EstadosOrden.Finalizado);

            var hoy = DateTime.Today;
            entregadasHoy = await db.Ordenes.CountAsync(o => o.Estado == EstadosOrden.Entregado && o.FechaSalida >= hoy);

            ultimas = await db.Ordenes.AsNoTracking()
                .OrderByDescending(o => o.OrdenId).Take(6)
                .Select(Proyecciones.OrdenResumen).ToListAsync();
        }

        if (Ve(Modulos.Reparaciones))
        {
            misTrabajos = await db.Ordenes.AsNoTracking()
                .Where(o => o.MecanicoId == actual.Id && EstadosOrden.EnTaller.Contains(o.Estado))
                .OrderBy(o => o.FechaIngreso)
                .Select(Proyecciones.OrdenResumen).ToListAsync();
            misActivos = misTrabajos.Count;
        }

        var veDespachos = Ve(Modulos.Despachos);
        if (veDespachos || Ve(Modulos.Solicitudes))
        {
            var usuarioId = actual.Id;
            solicitudesPendientes = await db.Solicitudes.CountAsync(s => s.Estado == EstadosSolicitud.Pendiente
                && (veDespachos || s.SolicitanteId == usuarioId));
        }

        if (Ve(Modulos.Inventario))
        {
            var existencias = await bodega.ListarExistenciasAsync(null, null, false);
            alertas = existencias.Where(e => e.EstadoStock != "DISPONIBLE")
                .OrderBy(e => e.Disponible).Take(8).ToList();
            stockBajo = existencias.Count(e => e.EstadoStock != "DISPONIBLE");
        }

        if (Ve(Modulos.Facturacion))
        {
            var inicioMes = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
            var facturas = db.Facturas.Where(f => f.Estado == EstadosFactura.Emitida && f.FechaEmision >= inicioMes);
            facturadoMes = await facturas.SumAsync(f => (decimal?)f.Total) ?? 0;
            facturasMes = await facturas.CountAsync();
            porFacturar = await db.Ordenes.CountAsync(o => o.Estado == EstadosOrden.Finalizado
                && !o.Facturas.Any(f => f.Estado == EstadosFactura.Emitida));
        }

        return new DashboardDto(activas, porEstado, listas, entregadasHoy, misActivos, misTrabajos,
            solicitudesPendientes, stockBajo, alertas, facturadoMes, facturasMes, porFacturar, ultimas);
    }
}
