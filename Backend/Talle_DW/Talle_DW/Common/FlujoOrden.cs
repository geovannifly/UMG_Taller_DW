namespace Talle_DW.Common;

/// <summary>
/// Máquina de estados de la orden de trabajo para los cambios que realiza el mecánico.
/// Los cambios de Recepción (asignar, cancelar, entregar) se validan en OrdenService.
/// </summary>
public static class FlujoOrden
{
    private static readonly Dictionary<string, string[]> Transiciones = new()
    {
        [EstadosOrden.EnDiagnostico] = [EstadosOrden.EnReparacion, EstadosOrden.EsperandoRepuestos],
        [EstadosOrden.EnReparacion] = [EstadosOrden.EsperandoRepuestos, EstadosOrden.Finalizado],
        [EstadosOrden.EsperandoRepuestos] = [EstadosOrden.EnReparacion],
        [EstadosOrden.Finalizado] = [EstadosOrden.EnReparacion]
    };

    public static string[] Siguientes(string estadoActual) =>
        Transiciones.TryGetValue(estadoActual, out var siguientes) ? siguientes : [];

    public static bool EsPermitido(string desde, string hacia) => Siguientes(desde).Contains(hacia);

    public static string Descripcion(string estado) => estado switch
    {
        EstadosOrden.Recibido => "Recibido",
        EstadosOrden.EnDiagnostico => "En diagnóstico",
        EstadosOrden.EnReparacion => "En reparación",
        EstadosOrden.EsperandoRepuestos => "Esperando repuestos",
        EstadosOrden.Finalizado => "Listo para entrega",
        EstadosOrden.Entregado => "Entregado",
        EstadosOrden.Cancelado => "Cancelado",
        _ => estado
    };
}
