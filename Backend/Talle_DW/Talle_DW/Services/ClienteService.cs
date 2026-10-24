using Microsoft.EntityFrameworkCore;
using Talle_DW.Common;
using Talle_DW.Data;
using Talle_DW.DTOs;
using Talle_DW.Entities;
using Talle_DW.Services.Interfaces;

namespace Talle_DW.Services;

public class ClienteService(TallerDbContext db, IBitacoraService bitacora) : IClienteService
{
    public async Task<List<ClienteDto>> ListarAsync(string? buscar, bool incluirInactivos)
    {
        var query = db.Clientes.AsNoTracking();

        if (!incluirInactivos) query = query.Where(c => c.Activo);
        if (!string.IsNullOrWhiteSpace(buscar))
        {
            var texto = buscar.Trim();
            query = query.Where(c => (c.Nombres + " " + c.Apellidos).Contains(texto)
                                  || c.Nit.Contains(texto)
                                  || (c.Dpi != null && c.Dpi.Contains(texto))
                                  || c.Telefono.Contains(texto)
                                  || c.Vehiculos.Any(v => v.Placa.Contains(texto)));
        }

        return await query.OrderBy(c => c.Nombres).ThenBy(c => c.Apellidos).Select(Proyecciones.Cliente).ToListAsync();
    }

    public async Task<ClienteDetalleDto> ObtenerAsync(int id)
    {
        var cliente = await db.Clientes.AsNoTracking().Where(c => c.ClienteId == id)
            .Select(Proyecciones.Cliente).FirstOrDefaultAsync()
            ?? throw new NoEncontradoException("El cliente no existe.");

        var vehiculos = await db.Vehiculos.AsNoTracking().Where(v => v.ClienteId == id)
            .OrderBy(v => v.Placa).Select(Proyecciones.Vehiculo).ToListAsync();

        var ordenes = await db.Ordenes.AsNoTracking().Where(o => o.ClienteId == id)
            .OrderByDescending(o => o.FechaIngreso).Take(20).Select(Proyecciones.OrdenResumen).ToListAsync();

        return new ClienteDetalleDto(cliente, vehiculos, ordenes);
    }

    public async Task<ClienteDto> CrearAsync(ClienteGuardarRequest request)
    {
        var cliente = new Cliente();
        await AplicarAsync(cliente, request);

        db.Clientes.Add(cliente);
        await db.SaveChangesAsync();
        await bitacora.RegistrarAsync(Modulos.Clientes, "CREAR", $"Registró al cliente {cliente.Nombres} {cliente.Apellidos}.");

        return (await ObtenerAsync(cliente.ClienteId)).Cliente;
    }

    public async Task<ClienteDto> ActualizarAsync(int id, ClienteGuardarRequest request)
    {
        var cliente = await db.Clientes.FindAsync(id) ?? throw new NoEncontradoException("El cliente no existe.");
        await AplicarAsync(cliente, request);

        await db.SaveChangesAsync();
        await bitacora.RegistrarAsync(Modulos.Clientes, "EDITAR", $"Actualizó al cliente {cliente.Nombres} {cliente.Apellidos}.");

        return (await ObtenerAsync(id)).Cliente;
    }

    public async Task CambiarEstadoAsync(int id, bool activo)
    {
        var cliente = await db.Clientes.FindAsync(id) ?? throw new NoEncontradoException("El cliente no existe.");
        if (cliente.Activo == activo) return;

        if (!activo && await db.Ordenes.AnyAsync(o => o.ClienteId == id && EstadosOrden.Activos.Contains(o.Estado)))
            throw new ReglaNegocioException("El cliente tiene órdenes de trabajo abiertas; no se puede desactivar.");

        cliente.Activo = activo;
        await db.SaveChangesAsync();
        await bitacora.RegistrarAsync(Modulos.Clientes, activo ? "ACTIVAR" : "DESACTIVAR",
            $"{(activo ? "Activó" : "Desactivó")} al cliente {cliente.Nombres} {cliente.Apellidos}.");
    }

    private async Task AplicarAsync(Cliente cliente, ClienteGuardarRequest request)
    {
        var dpi = string.IsNullOrWhiteSpace(request.Dpi) ? null : request.Dpi.Trim();
        if (dpi is not null && await db.Clientes.AnyAsync(c => c.Dpi == dpi && c.ClienteId != cliente.ClienteId))
            throw new ReglaNegocioException("Ya existe un cliente registrado con ese DPI.");

        cliente.Nombres = request.Nombres.Trim();
        cliente.Apellidos = request.Apellidos.Trim();
        cliente.Nit = string.IsNullOrWhiteSpace(request.Nit) ? "CF" : request.Nit.Trim().ToUpperInvariant();
        cliente.Dpi = dpi;
        cliente.Telefono = request.Telefono.Trim();
        cliente.Correo = string.IsNullOrWhiteSpace(request.Correo) ? null : request.Correo.Trim();
        cliente.Direccion = string.IsNullOrWhiteSpace(request.Direccion) ? null : request.Direccion.Trim();
    }
}
