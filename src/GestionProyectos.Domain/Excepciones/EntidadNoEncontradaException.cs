namespace GestionProyectos.Domain.Excepciones;

/// <summary>
/// El recurso solicitado no existe (se traduce a HTTP 404).
/// </summary>
/// <remarks>
/// Creado: 2026-10-09
/// Modificado: 2026-10-09 - Versión inicial (RF-001)
/// </remarks>
public class EntidadNoEncontradaException(string codigo, string mensaje)
    : ExcepcionDominio(codigo, mensaje);
