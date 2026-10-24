namespace Talle_DW.Common;

/// <summary>Violación de una regla de negocio (HTTP 400).</summary>
public class ReglaNegocioException(string mensaje) : Exception(mensaje);

/// <summary>El recurso solicitado no existe (HTTP 404).</summary>
public class NoEncontradoException(string mensaje) : Exception(mensaje);

/// <summary>El usuario no puede ejecutar la operación sobre este recurso (HTTP 403).</summary>
public class AccesoDenegadoException(string mensaje) : Exception(mensaje);

/// <summary>Credenciales inválidas o sesión no permitida (HTTP 401).</summary>
public class NoAutorizadoException(string mensaje) : Exception(mensaje);
