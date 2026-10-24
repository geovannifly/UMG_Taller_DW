using Talle_DW.DTOs;

namespace Talle_DW.Services.Interfaces;

public interface IUsuarioService
{
    Task<List<UsuarioDto>> ListarAsync(string? buscar, int? rolId, bool? activo);
    Task<UsuarioDto> ObtenerAsync(int id);
    Task<UsuarioDto> CrearAsync(UsuarioGuardarRequest request);
    Task<UsuarioDto> ActualizarAsync(int id, UsuarioGuardarRequest request);
    Task RestablecerPasswordAsync(int id, RestablecerPasswordRequest request);
    Task CambiarEstadoAsync(int id, bool activo);
}

public interface IRolService
{
    Task<List<RolDto>> ListarAsync();
    Task<RolDetalleDto> ObtenerAsync(int id);
    Task<RolDto> CrearAsync(RolGuardarRequest request);
    Task<RolDto> ActualizarAsync(int id, RolGuardarRequest request);
    Task ActualizarPermisosAsync(int id, ActualizarPermisosRequest request);
    Task EliminarAsync(int id);
}

public interface IParametroService
{
    Task<List<ParametroDto>> ListarAsync();
    Task<ParametroDto> ActualizarAsync(int id, ParametroActualizarRequest request);
    Task<DatosTallerDto> ObtenerDatosTallerAsync();
}

public interface IBitacoraService
{
    /// <summary>Guarda inmediatamente un registro en la bitácora con el usuario autenticado (o el indicado).</summary>
    Task RegistrarAsync(string modulo, string accion, string detalle, int? usuarioId = null, string? nombreUsuario = null);

    Task<List<BitacoraDto>> ConsultarAsync(DateTime? desde, DateTime? hasta, string? modulo, string? buscar);
}
