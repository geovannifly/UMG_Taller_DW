using Microsoft.AspNetCore.Mvc;
using Talle_DW.Common;
using Talle_DW.DTOs;
using Talle_DW.Security;
using Talle_DW.Services.Interfaces;

namespace Talle_DW.Controllers;

[ApiController]
[Route("api/inventario")]
public class InventarioController(IInventarioService inventario) : ControllerBase
{
    // ---------- Repuestos ----------

    /// <summary>estadoStock: DISPONIBLE, BAJO (incluye agotados) o AGOTADO.</summary>
    [HttpGet("repuestos")]
    [RequierePermiso(Modulos.Inventario)]
    public async Task<ActionResult<List<RepuestoDto>>> ListarRepuestos(string? buscar, int? categoriaId, string? estadoStock,
        bool incluirInactivos = false) =>
        Ok(await inventario.ListarRepuestosAsync(buscar, categoriaId, estadoStock, incluirInactivos));

    [HttpGet("repuestos/{id:int}")]
    [RequierePermiso(Modulos.Inventario)]
    public async Task<ActionResult<RepuestoDetalleDto>> ObtenerRepuesto(int id) => Ok(await inventario.ObtenerRepuestoAsync(id));

    [HttpPost("repuestos")]
    [RequierePermiso(Modulos.Inventario, Accion.Crear)]
    public async Task<ActionResult<RepuestoDto>> CrearRepuesto(RepuestoGuardarRequest request) =>
        Ok(await inventario.CrearRepuestoAsync(request));

    [HttpPut("repuestos/{id:int}")]
    [RequierePermiso(Modulos.Inventario, Accion.Editar)]
    public async Task<ActionResult<RepuestoDto>> ActualizarRepuesto(int id, RepuestoGuardarRequest request) =>
        Ok(await inventario.ActualizarRepuestoAsync(id, request));

    [HttpPatch("repuestos/{id:int}/estado")]
    [RequierePermiso(Modulos.Inventario, Accion.Eliminar)]
    public async Task<IActionResult> CambiarEstadoRepuesto(int id, CambiarEstadoRequest request)
    {
        await inventario.CambiarEstadoRepuestoAsync(id, request.Activo);
        return NoContent();
    }

    /// <summary>Registra una entrada de mercadería o un ajuste de inventario.</summary>
    [HttpPost("repuestos/{id:int}/movimientos")]
    [RequierePermiso(Modulos.Inventario, Accion.Crear)]
    public async Task<ActionResult<MovimientoDto>> RegistrarMovimiento(int id, MovimientoCrearRequest request) =>
        Ok(await inventario.RegistrarMovimientoAsync(id, request));

    [HttpGet("movimientos")]
    [RequierePermiso(Modulos.Inventario)]
    public async Task<ActionResult<List<MovimientoDto>>> ListarMovimientos(int? repuestoId, string? tipo, DateTime? desde, DateTime? hasta) =>
        Ok(await inventario.ListarMovimientosAsync(repuestoId, tipo, desde, hasta));

    // ---------- Categorías ----------

    [HttpGet("categorias")]
    [RequierePermiso($"{Modulos.Inventario},{Modulos.Solicitudes}")]
    public async Task<ActionResult<List<CategoriaDto>>> ListarCategorias(bool incluirInactivas = false) =>
        Ok(await inventario.ListarCategoriasAsync(incluirInactivas));

    [HttpPost("categorias")]
    [RequierePermiso(Modulos.Inventario, Accion.Crear)]
    public async Task<ActionResult<CategoriaDto>> CrearCategoria(CategoriaGuardarRequest request) =>
        Ok(await inventario.CrearCategoriaAsync(request));

    [HttpPut("categorias/{id:int}")]
    [RequierePermiso(Modulos.Inventario, Accion.Editar)]
    public async Task<ActionResult<CategoriaDto>> ActualizarCategoria(int id, CategoriaGuardarRequest request) =>
        Ok(await inventario.ActualizarCategoriaAsync(id, request));

    [HttpPatch("categorias/{id:int}/estado")]
    [RequierePermiso(Modulos.Inventario, Accion.Eliminar)]
    public async Task<IActionResult> CambiarEstadoCategoria(int id, CambiarEstadoRequest request)
    {
        await inventario.CambiarEstadoCategoriaAsync(id, request.Activo);
        return NoContent();
    }
}
