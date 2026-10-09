namespace GestionProyectos.Domain.Excepciones;

/// <summary>
/// El usuario actual informado no existe o está inactivo (se traduce a HTTP 401).
/// </summary>
/// <remarks>
/// Creado: 2026-10-09
/// Modificado: 2026-10-09 - Versión inicial (RF-001)
/// </remarks>
public class UsuarioInvalidoException()
    : ExcepcionDominio(
        CodigosError.UsuarioInvalido,
        "El usuario actual no es válido (inexistente o inactivo).");
