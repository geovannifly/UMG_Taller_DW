using Microsoft.EntityFrameworkCore;
using Talle_DW.Common;
using Talle_DW.Data;
using Talle_DW.DTOs;
using Talle_DW.Entities;
using Talle_DW.Security;
using Talle_DW.Services.Interfaces;

namespace Talle_DW.Services;

public class DiagnosticoService(TallerDbContext db, IUsuarioActual actual, IBitacoraService bitacora) : IDiagnosticoService
{
    public Task<List<DiagnosticoDto>> ListarPorOrdenAsync(int ordenId) =>
        db.Diagnosticos.AsNoTracking()
            .Where(d => d.OrdenId == ordenId)
            .OrderBy(d => d.Fecha)
            .Select(Proyecciones.Diagnostico)
            .ToListAsync();

    public async Task<DiagnosticoDto> CrearAsync(DiagnosticoGuardarRequest request)
    {
        var orden = await db.Ordenes.FirstOrDefaultAsync(o => o.OrdenId == request.OrdenId)
            ?? throw new NoEncontradoException("La orden de trabajo no existe.");

        ReglasTaller.ValidarEnTaller(orden);
        ReglasTaller.ValidarResponsable(orden, actual);

        var diagnostico = new Diagnostico
        {
            OrdenId = orden.OrdenId,
            MecanicoId = actual.Id,
            Fecha = DateTime.Now
        };
        Aplicar(diagnostico, request);

        db.Diagnosticos.Add(diagnostico);
        orden.RegistrarEvento(actual.Id, $"Diagnóstico registrado: {diagnostico.FallaDetectada}");

        await db.SaveChangesAsync();
        await bitacora.RegistrarAsync(Modulos.Diagnosticos, "CREAR", $"Registró un diagnóstico en la orden {orden.NumeroOrden}.");

        return await ObtenerAsync(diagnostico.DiagnosticoId);
    }

    public async Task<DiagnosticoDto> ActualizarAsync(int id, DiagnosticoGuardarRequest request)
    {
        var diagnostico = await db.Diagnosticos.Include(d => d.Orden).FirstOrDefaultAsync(d => d.DiagnosticoId == id)
            ?? throw new NoEncontradoException("El diagnóstico no existe.");

        ReglasTaller.ValidarEnTaller(diagnostico.Orden);
        if (!actual.EsAdministrador && diagnostico.MecanicoId != actual.Id)
            throw new AccesoDenegadoException("Solo el mecánico que registró el diagnóstico puede modificarlo.");

        Aplicar(diagnostico, request);
        await db.SaveChangesAsync();
        await bitacora.RegistrarAsync(Modulos.Diagnosticos, "EDITAR",
            $"Actualizó un diagnóstico de la orden {diagnostico.Orden.NumeroOrden}.");

        return await ObtenerAsync(id);
    }

    public async Task EliminarAsync(int id)
    {
        var diagnostico = await db.Diagnosticos.Include(d => d.Orden).FirstOrDefaultAsync(d => d.DiagnosticoId == id)
            ?? throw new NoEncontradoException("El diagnóstico no existe.");

        var orden = diagnostico.Orden;
        ReglasTaller.ValidarEnTaller(orden);

        var total = await db.Diagnosticos.CountAsync(d => d.OrdenId == orden.OrdenId);
        if (total == 1 && orden.Estado != EstadosOrden.EnDiagnostico)
            throw new ReglaNegocioException("La orden ya avanzó de etapa y debe conservar al menos un diagnóstico.");

        db.Diagnosticos.Remove(diagnostico);
        orden.RegistrarEvento(actual.Id, $"Diagnóstico eliminado: {diagnostico.FallaDetectada}");
        await db.SaveChangesAsync();
        await bitacora.RegistrarAsync(Modulos.Diagnosticos, "ELIMINAR", $"Eliminó un diagnóstico de la orden {orden.NumeroOrden}.");
    }

    private async Task<DiagnosticoDto> ObtenerAsync(int id) =>
        await db.Diagnosticos.AsNoTracking().Where(d => d.DiagnosticoId == id).Select(Proyecciones.Diagnostico).FirstAsync();

    private static void Aplicar(Diagnostico diagnostico, DiagnosticoGuardarRequest request)
    {
        diagnostico.FallaDetectada = request.FallaDetectada.Trim();
        diagnostico.ReparacionRequerida = request.ReparacionRequerida.Trim();
        diagnostico.Severidad = request.Severidad;
        diagnostico.CostoManoObra = Math.Round(request.CostoManoObra, 2);
        diagnostico.Observaciones = string.IsNullOrWhiteSpace(request.Observaciones) ? null : request.Observaciones.Trim();
    }
}
