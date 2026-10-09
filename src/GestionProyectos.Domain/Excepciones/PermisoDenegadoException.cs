namespace GestionProyectos.Domain.Excepciones;

/// <summary>
/// El usuario actual no tiene permiso para la operación (se traduce a HTTP 403).
/// </summary>
/// <remarks>
/// Creado: 2026-10-09
/// Modificado: 2026-10-09 - Versión inicial (RF-001)
/// </remarks>
public class PermisoDenegadoException()
    : ExcepcionDominio(
        CodigosError.PermisoDenegado,
        "La operación requiere un usuario administrador.");
