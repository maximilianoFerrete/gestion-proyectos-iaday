namespace GestionProyectos.Domain.Excepciones;

/// <summary>
/// La operación choca con el estado actual de los datos (se traduce a HTTP 409).
/// </summary>
/// <remarks>
/// Creado: 2026-10-09
/// Modificado: 2026-10-09 - Versión inicial (RF-001)
/// </remarks>
public class ConflictoException(string codigo, string mensaje)
    : ExcepcionDominio(codigo, mensaje);
