namespace Talle_DW.Common;

/// <summary>Códigos de módulo registrados en la tabla Modulos.</summary>
public static class Modulos
{
    public const string Clientes = "CLIENTES";
    public const string Vehiculos = "VEHICULOS";
    public const string Ordenes = "ORDENES";
    public const string Diagnosticos = "DIAGNOSTICOS";
    public const string Reparaciones = "REPARACIONES";
    public const string Solicitudes = "SOLICITUDES";
    public const string Inventario = "INVENTARIO";
    public const string Despachos = "DESPACHOS";
    public const string Facturacion = "FACTURACION";
    public const string Usuarios = "USUARIOS";
    public const string Roles = "ROLES";
    public const string Parametros = "PARAMETROS";
    public const string Bitacora = "BITACORA";
    public const string Auth = "AUTENTICACION";
}

public enum Accion
{
    Ver,
    Crear,
    Editar,
    Eliminar
}

public static class EstadosOrden
{
    public const string Recibido = "RECIBIDO";
    public const string EnDiagnostico = "EN_DIAGNOSTICO";
    public const string EnReparacion = "EN_REPARACION";
    public const string EsperandoRepuestos = "ESPERANDO_REPUESTOS";
    public const string Finalizado = "FINALIZADO";
    public const string Entregado = "ENTREGADO";
    public const string Cancelado = "CANCELADO";

    /// <summary>Estados en los que la orden sigue abierta dentro del taller.</summary>
    public static readonly string[] Activos = [Recibido, EnDiagnostico, EnReparacion, EsperandoRepuestos, Finalizado];

    /// <summary>Estados en los que el mecánico puede trabajar sobre la orden.</summary>
    public static readonly string[] EnTaller = [EnDiagnostico, EnReparacion, EsperandoRepuestos];
}

public static class EstadosSolicitud
{
    public const string Pendiente = "PENDIENTE";
    public const string Despachado = "DESPACHADO";
    public const string Rechazado = "RECHAZADO";
    public const string Cancelado = "CANCELADO";
}

public static class EstadosFactura
{
    public const string Emitida = "EMITIDA";
    public const string Anulada = "ANULADA";
}

public static class TiposMovimiento
{
    public const string Entrada = "ENTRADA";
    public const string Salida = "SALIDA";
    public const string Ajuste = "AJUSTE";
}

public static class ClavesParametro
{
    public const string NombreTaller = "NOMBRE_TALLER";
    public const string NitTaller = "NIT_TALLER";
    public const string DireccionTaller = "DIRECCION_TALLER";
    public const string TelefonoTaller = "TELEFONO_TALLER";
    public const string CorreoTaller = "CORREO_TALLER";
    public const string Moneda = "MONEDA";
    public const string IvaPorcentaje = "IVA_PORCENTAJE";
    public const string DescuentoMaximo = "DESCUENTO_MAXIMO";
}
