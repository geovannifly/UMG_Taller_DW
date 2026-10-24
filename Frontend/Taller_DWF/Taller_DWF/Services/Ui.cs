using System.Globalization;

namespace Taller_DWF.Services;

/// <summary>Códigos de módulo (iguales a la tabla Modulos de la base de datos).</summary>
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

    /// <summary>Ruta de la pantalla principal de cada módulo (null = sin entrada propia en el menú).</summary>
    public static string? Ruta(string codigo) => codigo switch
    {
        Clientes => "clientes",
        Vehiculos => "vehiculos",
        Ordenes => "ordenes",
        Reparaciones => "reparaciones",
        Solicitudes => "solicitudes",
        Inventario => "inventario",
        Despachos => "despachos",
        Facturacion => "facturacion",
        Usuarios => "administracion/usuarios",
        Roles => "administracion/roles",
        Parametros => "administracion/parametros",
        Bitacora => "administracion/bitacora",
        _ => null
    };
}

/// <summary>Formatos y clases visuales compartidas por las pantallas.</summary>
public static class Ui
{
    /// <summary>Español de Guatemala con punto decimal y coma de miles (Q 1,250.00).</summary>
    public static readonly CultureInfo Cultura = CrearCultura();

    private static CultureInfo CrearCultura()
    {
        var cultura = (CultureInfo)new CultureInfo("es-GT").Clone();
        cultura.NumberFormat.NumberDecimalSeparator = ".";
        cultura.NumberFormat.NumberGroupSeparator = ",";
        cultura.NumberFormat.CurrencyDecimalSeparator = ".";
        cultura.NumberFormat.CurrencyGroupSeparator = ",";
        return cultura;
    }

    public static string Moneda(decimal? valor) => $"Q {(valor ?? 0).ToString("N2", Cultura)}";
    public static string Fecha(DateTime? fecha) => fecha?.ToString("dd/MM/yyyy", Cultura) ?? "—";
    public static string Fecha(DateOnly? fecha) => fecha?.ToString("dd/MM/yyyy", Cultura) ?? "—";
    public static string FechaHora(DateTime? fecha) => fecha?.ToString("dd/MM/yyyy HH:mm", Cultura) ?? "—";
    public static string Numero(int valor) => valor.ToString("N0", Cultura);

    public static string Iniciales(string? nombre)
    {
        var partes = (nombre ?? "").Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return partes.Length switch
        {
            0 => "?",
            1 => partes[0][..1].ToUpper(),
            _ => (partes[0][..1] + partes[1][..1]).ToUpper()
        };
    }

    public static string TiempoTranscurrido(DateTime fecha)
    {
        var lapso = DateTime.Now - fecha;
        if (lapso.TotalMinutes < 1) return "hace un momento";
        if (lapso.TotalMinutes < 60) return $"hace {(int)lapso.TotalMinutes} min";
        if (lapso.TotalHours < 24) return $"hace {(int)lapso.TotalHours} h";
        if (lapso.TotalDays < 30) return $"hace {(int)lapso.TotalDays} día(s)";
        return Fecha(fecha);
    }

    // ---------------- Órdenes ----------------

    public static readonly string[] FlujoPrincipal =
        ["RECIBIDO", "EN_DIAGNOSTICO", "EN_REPARACION", "FINALIZADO", "ENTREGADO"];

    public static string EstadoOrden(string estado) => estado switch
    {
        "RECIBIDO" => "Recibido",
        "EN_DIAGNOSTICO" => "En diagnóstico",
        "EN_REPARACION" => "En reparación",
        "ESPERANDO_REPUESTOS" => "Esperando repuestos",
        "FINALIZADO" => "Listo para entrega",
        "ENTREGADO" => "Entregado",
        "CANCELADO" => "Cancelado",
        _ => estado
    };

    public static string ClaseEstadoOrden(string estado) => estado switch
    {
        "RECIBIDO" => "est-recibido",
        "EN_DIAGNOSTICO" => "est-diagnostico",
        "EN_REPARACION" => "est-reparacion",
        "ESPERANDO_REPUESTOS" => "est-espera",
        "FINALIZADO" => "est-listo",
        "ENTREGADO" => "est-entregado",
        "CANCELADO" => "est-cancelado",
        _ => "est-recibido"
    };

    public static string IconoEstadoOrden(string estado) => estado switch
    {
        "RECIBIDO" => "bi-box-arrow-in-down",
        "EN_DIAGNOSTICO" => "bi-search",
        "EN_REPARACION" => "bi-tools",
        "ESPERANDO_REPUESTOS" => "bi-hourglass-split",
        "FINALIZADO" => "bi-check2-circle",
        "ENTREGADO" => "bi-flag",
        "CANCELADO" => "bi-x-circle",
        _ => "bi-circle"
    };

    public static string AccionEstado(string estado) => estado switch
    {
        "EN_REPARACION" => "Iniciar / continuar reparación",
        "ESPERANDO_REPUESTOS" => "Marcar esperando repuestos",
        "FINALIZADO" => "Finalizar reparación",
        _ => EstadoOrden(estado)
    };

    public static string ClasePrioridad(string prioridad) => prioridad switch
    {
        "URGENTE" => "pri-urgente",
        "ALTA" => "pri-alta",
        "BAJA" => "pri-baja",
        _ => "pri-normal"
    };

    public static string Prioridad(string prioridad) => prioridad switch
    {
        "URGENTE" => "Urgente",
        "ALTA" => "Alta",
        "BAJA" => "Baja",
        _ => "Normal"
    };

    public static string Severidad(string severidad) => severidad switch
    {
        "CRITICA" => "Crítica",
        "ALTA" => "Alta",
        "MEDIA" => "Media",
        _ => "Baja"
    };

    public static string ClaseSeveridad(string severidad) => severidad switch
    {
        "CRITICA" => "pri-urgente",
        "ALTA" => "pri-alta",
        "MEDIA" => "pri-normal",
        _ => "pri-baja"
    };

    // ---------------- Bodega ----------------

    public static string ClaseStock(string estado) => estado switch
    {
        "AGOTADO" => "stk-agotado",
        "BAJO" => "stk-bajo",
        _ => "stk-ok"
    };

    public static string EstadoStock(string estado) => estado switch
    {
        "AGOTADO" => "Agotado",
        "BAJO" => "Stock bajo",
        _ => "Disponible"
    };

    public static string ClaseSolicitud(string estado) => estado switch
    {
        "PENDIENTE" => "est-espera",
        "DESPACHADO" => "est-listo",
        "RECHAZADO" => "est-cancelado",
        _ => "est-entregado"
    };

    public static string Solicitud(string estado) => estado switch
    {
        "PENDIENTE" => "Pendiente",
        "DESPACHADO" => "Despachado",
        "RECHAZADO" => "Rechazado",
        "CANCELADO" => "Cancelado",
        _ => estado
    };

    public static string MetodoPago(string metodo) => metodo switch
    {
        "TARJETA" => "Tarjeta",
        "TRANSFERENCIA" => "Transferencia",
        _ => "Efectivo"
    };
}
