using Microsoft.AspNetCore.Mvc;
using Talle_DW.Common;
using Talle_DW.DTOs;
using Talle_DW.Security;
using Talle_DW.Services.Interfaces;

namespace Talle_DW.Controllers;

[ApiController]
[Route("api/ordenes")]
public class OrdenesController(IOrdenService ordenes) : ControllerBase
{
    private const string ModulosConsulta =
        $"{Modulos.Ordenes},{Modulos.Reparaciones},{Modulos.Diagnosticos},{Modulos.Despachos},{Modulos.Facturacion}";

    /// <summary>Listado de órdenes. estado admite un estado puntual o "ACTIVAS".</summary>
    [HttpGet]
    [RequierePermiso(Modulos.Ordenes)]
    public async Task<ActionResult<List<OrdenResumenDto>>> Listar(string? estado, string? buscar, int? mecanicoId,
        DateTime? desde, DateTime? hasta) =>
        Ok(await ordenes.ListarAsync(estado, buscar, mecanicoId, desde, hasta));

    /// <summary>Detalle completo: vehículo, cliente, diagnósticos, repuestos, historial y factura.</summary>
    [HttpGet("{id:int}")]
    [RequierePermiso(ModulosConsulta)]
    public async Task<ActionResult<OrdenDetalleDto>> Obtener(int id) => Ok(await ordenes.ObtenerAsync(id));

    /// <summary>Recepción: registra el ingreso del vehículo.</summary>
    [HttpPost]
    [RequierePermiso(Modulos.Ordenes, Accion.Crear)]
    public async Task<ActionResult<OrdenDetalleDto>> Crear(OrdenCrearRequest request)
    {
        var orden = await ordenes.CrearAsync(request);
        return CreatedAtAction(nameof(Obtener), new { id = orden.OrdenId }, orden);
    }

    [HttpPut("{id:int}")]
    [RequierePermiso(Modulos.Ordenes, Accion.Editar)]
    public async Task<ActionResult<OrdenDetalleDto>> Actualizar(int id, OrdenEditarRequest request) =>
        Ok(await ordenes.ActualizarAsync(id, request));

    [HttpPost("{id:int}/asignar")]
    [RequierePermiso(Modulos.Ordenes, Accion.Editar)]
    public async Task<ActionResult<OrdenDetalleDto>> AsignarMecanico(int id, AsignarMecanicoRequest request) =>
        Ok(await ordenes.AsignarMecanicoAsync(id, request));

    [HttpPost("{id:int}/cancelar")]
    [RequierePermiso(Modulos.Ordenes, Accion.Editar)]
    public async Task<ActionResult<OrdenDetalleDto>> Cancelar(int id, CancelarOrdenRequest request) =>
        Ok(await ordenes.CancelarAsync(id, request));

    /// <summary>Recepción: registra la salida (entrega) del vehículo. Requiere factura emitida.</summary>
    [HttpPost("{id:int}/salida")]
    [RequierePermiso(Modulos.Ordenes, Accion.Editar)]
    public async Task<ActionResult<OrdenDetalleDto>> RegistrarSalida(int id, RegistrarSalidaRequest request) =>
        Ok(await ordenes.RegistrarSalidaAsync(id, request));

    /// <summary>Mecánicos disponibles con su carga de trabajo actual.</summary>
    [HttpGet("mecanicos")]
    [RequierePermiso(Modulos.Ordenes)]
    public async Task<ActionResult<List<MecanicoDisponibleDto>>> Mecanicos() => Ok(await ordenes.ListarMecanicosAsync());
}
