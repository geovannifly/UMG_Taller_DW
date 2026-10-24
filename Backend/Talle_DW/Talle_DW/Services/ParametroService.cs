using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Talle_DW.Common;
using Talle_DW.Data;
using Talle_DW.DTOs;
using Talle_DW.Services.Interfaces;

namespace Talle_DW.Services;

public class ParametroService(TallerDbContext db, IBitacoraService bitacora) : IParametroService
{
    public Task<List<ParametroDto>> ListarAsync() =>
        db.Parametros.AsNoTracking()
            .OrderBy(p => p.ParametroId)
            .Select(p => new ParametroDto(p.ParametroId, p.Clave, p.Valor, p.Descripcion, p.Tipo))
            .ToListAsync();

    public async Task<ParametroDto> ActualizarAsync(int id, ParametroActualizarRequest request)
    {
        var parametro = await db.Parametros.FindAsync(id)
            ?? throw new NoEncontradoException("El parámetro no existe.");

        var valor = request.Valor.Trim();

        if (parametro.Tipo == "NUMERO")
        {
            if (!decimal.TryParse(valor, NumberStyles.Number, CultureInfo.InvariantCulture, out var numero) || numero < 0)
                throw new ReglaNegocioException("El valor debe ser un número mayor o igual a cero (use punto como separador decimal).");

            if (parametro.Clave is ClavesParametro.IvaPorcentaje or ClavesParametro.DescuentoMaximo && numero > 100)
                throw new ReglaNegocioException("El porcentaje no puede ser mayor a 100.");
        }

        var anterior = parametro.Valor;
        parametro.Valor = valor;
        await db.SaveChangesAsync();
        await bitacora.RegistrarAsync(Modulos.Parametros, "EDITAR", $"Parámetro {parametro.Clave}: '{anterior}' → '{valor}'.");

        return new ParametroDto(parametro.ParametroId, parametro.Clave, parametro.Valor, parametro.Descripcion, parametro.Tipo);
    }

    public async Task<DatosTallerDto> ObtenerDatosTallerAsync()
    {
        var valores = await db.Parametros.AsNoTracking().ToDictionaryAsync(p => p.Clave, p => p.Valor);

        string Texto(string clave, string defecto = "") => valores.TryGetValue(clave, out var v) ? v : defecto;
        decimal Numero(string clave, decimal defecto) =>
            decimal.TryParse(Texto(clave), NumberStyles.Number, CultureInfo.InvariantCulture, out var n) ? n : defecto;

        return new DatosTallerDto(
            Texto(ClavesParametro.NombreTaller, "Taller DW"),
            Texto(ClavesParametro.NitTaller),
            Texto(ClavesParametro.DireccionTaller),
            Texto(ClavesParametro.TelefonoTaller),
            Texto(ClavesParametro.CorreoTaller),
            Texto(ClavesParametro.Moneda, "Q"),
            Numero(ClavesParametro.IvaPorcentaje, 12),
            Numero(ClavesParametro.DescuentoMaximo, 0));
    }
}
