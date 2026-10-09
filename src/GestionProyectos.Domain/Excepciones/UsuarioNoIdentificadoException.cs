namespace GestionProyectos.Domain.Excepciones;

/// <summary>
/// La operación requiere un usuario actual identificado (se traduce a HTTP 401).
/// </summary>
/// <remarks>
/// Creado: 2026-10-09
/// Modificado: 2026-10-09 - Versión inicial (RF-001)
/// </remarks>
public class UsuarioNoIdentificadoException()
    : ExcepcionDominio(
        CodigosError.UsuarioNoIdentificado,
        "La operación requiere un usuario identificado.");
