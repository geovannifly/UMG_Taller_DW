using Talle_DW.Common;
using Talle_DW.Entities;
using Talle_DW.Security;

namespace Talle_DW.Services;

/// <summary>Reglas compartidas por los servicios que trabajan sobre una orden en el taller.</summary>
public static class ReglasTaller
{
    /// <summary>Solo el mecánico asignado (o un administrador) puede trabajar sobre la orden.</summary>
    public static void ValidarResponsable(OrdenTrabajo orden, IUsuarioActual actual)
    {
        if (actual.EsAdministrador) return;
        if (orden.MecanicoId != actual.Id)
            throw new AccesoDenegadoException($"La orden {orden.NumeroOrden} no está asignada a usted.");
    }

    /// <summary>La orden debe estar dentro del taller (diagnóstico, reparación o esperando repuestos).</summary>
    public static void ValidarEnTaller(OrdenTrabajo orden)
    {
        if (!EstadosOrden.EnTaller.Contains(orden.Estado))
            throw new ReglaNegocioException(
                $"La orden {orden.NumeroOrden} está en estado '{FlujoOrden.Descripcion(orden.Estado)}' y no admite cambios del taller.");
    }
}
