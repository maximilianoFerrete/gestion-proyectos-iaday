namespace GestionProyectos.Application.Abstracciones;

/// <summary>
/// Usuario que ejecuta la operación; independiente del mecanismo de autenticación (spec 0.4).
/// </summary>
/// <remarks>
/// Creado: 2026-10-09
/// Modificado: 2026-10-09 - Versión inicial (RF-001)
/// </remarks>
public interface IUsuarioActual
{
    /// <summary>Indica si hay un usuario identificado.</summary>
    bool EstaIdentificado { get; }

    /// <summary>Id de la persona del usuario actual, o null si es anónimo.</summary>
    int? PersonaId { get; }

    /// <summary>Nombre del usuario actual, o null si es anónimo.</summary>
    string? Nombre { get; }

    /// <summary>Indica si el usuario actual es administrador.</summary>
    bool EsAdmin { get; }
}
