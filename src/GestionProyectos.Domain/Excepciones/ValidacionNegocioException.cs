namespace GestionProyectos.Domain.Excepciones;

/// <summary>
/// Dato de entrada inválido según una regla de negocio (se traduce a HTTP 400).
/// </summary>
/// <remarks>
/// Creado: 2026-10-09
/// Modificado: 2026-10-09 - Versión inicial (RF-001)
/// </remarks>
public class ValidacionNegocioException(string codigo, string mensaje)
    : ExcepcionDominio(codigo, mensaje);
