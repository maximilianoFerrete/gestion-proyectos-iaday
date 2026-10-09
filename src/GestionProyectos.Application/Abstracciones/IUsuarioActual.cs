namespace GestionProyectos.Application.Abstracciones;

/// <summary>
/// Usuario que ejecuta la operación; independiente del mecanismo de autenticación (spec 0.4).
/// </summary>
/// <remarks>
/// Creado: 2026-10-09
/// Modificado: 2026-10-09 - Versión inicial (RF-001)
/// Modificado: 2026-10-09 - Agrega Permisos y TienePermiso (RF-002, 0.1)
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

    /// <summary>Permisos del usuario actual (vacío si es anónimo).</summary>
    IReadOnlyCollection<string> Permisos { get; }

    /// <summary>Indica si el usuario actual tiene el permiso indicado.</summary>
    bool TienePermiso(string permiso);
}
