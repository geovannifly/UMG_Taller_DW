using Microsoft.EntityFrameworkCore;
using Talle_DW.Common;
using Talle_DW.Data;
using Talle_DW.DTOs;
using Talle_DW.Entities;
using Talle_DW.Services.Interfaces;

namespace Talle_DW.Services;

public class VehiculoService(TallerDbContext db, IBitacoraService bitacora) : IVehiculoService
{
    public async Task<List<VehiculoDto>> ListarAsync(string? buscar, int? clienteId, bool incluirInactivos)
    {
        var query = db.Vehiculos.AsNoTracking();

        if (!incluirInactivos) query = query.Where(v => v.Activo);
        if (clienteId.HasValue) query = query.Where(v => v.ClienteId == clienteId);
        if (!string.IsNullOrWhiteSpace(buscar))
        {
            var texto = buscar.Trim();
            query = query.Where(v => v.Placa.Contains(texto) || v.Marca.Contains(texto) || v.Modelo.Contains(texto)
                                  || (v.Cliente.Nombres + " " + v.Cliente.Apellidos).Contains(texto));
        }

        return await query.OrderBy(v => v.Placa).Select(Proyecciones.Vehiculo).ToListAsync();
    }

    public async Task<VehiculoDetalleDto> ObtenerAsync(int id)
    {
        var vehiculo = await db.Vehiculos.AsNoTracking().Where(v => v.VehiculoId == id)
            .Select(Proyecciones.Vehiculo).FirstOrDefaultAsync()
            ?? throw new NoEncontradoException("El vehículo no existe.");

        var ordenes = await db.Ordenes.AsNoTracking().Where(o => o.VehiculoId == id)
            .OrderByDescending(o => o.FechaIngreso).Select(Proyecciones.OrdenResumen).ToListAsync();

        return new VehiculoDetalleDto(vehiculo, ordenes);
    }

    public async Task<VehiculoDto> CrearAsync(VehiculoGuardarRequest request)
    {
        var vehiculo = new Vehiculo();
        await AplicarAsync(vehiculo, request);

        db.Vehiculos.Add(vehiculo);
        await db.SaveChangesAsync();
        await bitacora.RegistrarAsync(Modulos.Vehiculos, "CREAR",
            $"Registró el vehículo {vehiculo.Placa} ({vehiculo.Marca} {vehiculo.Modelo}).");

        return (await ObtenerAsync(vehiculo.VehiculoId)).Vehiculo;
    }

    public async Task<VehiculoDto> ActualizarAsync(int id, VehiculoGuardarRequest request)
    {
        var vehiculo = await db.Vehiculos.FindAsync(id) ?? throw new NoEncontradoException("El vehículo no existe.");

        if (vehiculo.ClienteId != request.ClienteId
            && await db.Ordenes.AnyAsync(o => o.VehiculoId == id && EstadosOrden.Activos.Contains(o.Estado)))
            throw new ReglaNegocioException("No se puede cambiar el propietario mientras el vehículo tenga una orden abierta.");

        await AplicarAsync(vehiculo, request);
        await db.SaveChangesAsync();
        await bitacora.RegistrarAsync(Modulos.Vehiculos, "EDITAR", $"Actualizó el vehículo {vehiculo.Placa}.");

        return (await ObtenerAsync(id)).Vehiculo;
    }

    public async Task CambiarEstadoAsync(int id, bool activo)
    {
        var vehiculo = await db.Vehiculos.FindAsync(id) ?? throw new NoEncontradoException("El vehículo no existe.");
        if (vehiculo.Activo == activo) return;

        if (!activo && await db.Ordenes.AnyAsync(o => o.VehiculoId == id && EstadosOrden.Activos.Contains(o.Estado)))
            throw new ReglaNegocioException("El vehículo tiene una orden de trabajo abierta; no se puede desactivar.");

        vehiculo.Activo = activo;
        await db.SaveChangesAsync();
        await bitacora.RegistrarAsync(Modulos.Vehiculos, activo ? "ACTIVAR" : "DESACTIVAR",
            $"{(activo ? "Activó" : "Desactivó")} el vehículo {vehiculo.Placa}.");
    }

    private async Task AplicarAsync(Vehiculo vehiculo, VehiculoGuardarRequest request)
    {
        var placa = request.Placa.Trim().ToUpperInvariant().Replace(" ", "");
        if (await db.Vehiculos.AnyAsync(v => v.Placa == placa && v.VehiculoId != vehiculo.VehiculoId))
            throw new ReglaNegocioException($"Ya existe un vehículo con la placa {placa}.");

        var cliente = await db.Clientes.FindAsync(request.ClienteId)
            ?? throw new ReglaNegocioException("El cliente seleccionado no existe.");
        if (!cliente.Activo)
            throw new ReglaNegocioException("El cliente seleccionado está inactivo.");

        if (request.Anio > DateTime.Today.Year + 1)
            throw new ReglaNegocioException("El año del vehículo no puede ser mayor al próximo año.");

        vehiculo.ClienteId = request.ClienteId;
        vehiculo.Placa = placa;
        vehiculo.Marca = request.Marca.Trim();
        vehiculo.Modelo = request.Modelo.Trim();
        vehiculo.Anio = request.Anio;
        vehiculo.Color = string.IsNullOrWhiteSpace(request.Color) ? null : request.Color.Trim();
        vehiculo.Vin = string.IsNullOrWhiteSpace(request.Vin) ? null : request.Vin.Trim().ToUpperInvariant();
    }
}
