using Microsoft.EntityFrameworkCore;
using Talle_DW.Data;
using Talle_DW.DTOs;
using Talle_DW.Entities;
using Talle_DW.Security;
using Talle_DW.Services.Interfaces;

namespace Talle_DW.Services;

public class BitacoraService(TallerDbContext db, IUsuarioActual actual) : IBitacoraService
{
    public async Task RegistrarAsync(string modulo, string accion, string detalle, int? usuarioId = null, string? nombreUsuario = null)
    {
        db.Bitacora.Add(new RegistroBitacora
        {
            UsuarioId = usuarioId ?? (actual.Id > 0 ? actual.Id : null),
            NombreUsuario = nombreUsuario ?? actual.NombreUsuario,
            Modulo = modulo,
            Accion = accion,
            Detalle = detalle.Length > 500 ? detalle[..497] + "..." : detalle,
            Ip = actual.Ip,
            Fecha = DateTime.Now
        });
        await db.SaveChangesAsync();
    }

    public async Task<List<BitacoraDto>> ConsultarAsync(DateTime? desde, DateTime? hasta, string? modulo, string? buscar)
    {
        var query = db.Bitacora.AsNoTracking();

        if (desde.HasValue) query = query.Where(b => b.Fecha >= desde.Value.Date);
        if (hasta.HasValue) query = query.Where(b => b.Fecha < hasta.Value.Date.AddDays(1));
        if (!string.IsNullOrWhiteSpace(modulo)) query = query.Where(b => b.Modulo == modulo);
        if (!string.IsNullOrWhiteSpace(buscar))
        {
            var texto = buscar.Trim();
            query = query.Where(b => (b.NombreUsuario != null && b.NombreUsuario.Contains(texto))
                                  || (b.Detalle != null && b.Detalle.Contains(texto))
                                  || b.Accion.Contains(texto));
        }

        return await query
            .OrderByDescending(b => b.BitacoraId)
            .Take(500)
            .Select(b => new BitacoraDto(b.BitacoraId, b.Fecha, b.NombreUsuario, b.Modulo, b.Accion, b.Detalle, b.Ip))
            .ToListAsync();
    }
}
