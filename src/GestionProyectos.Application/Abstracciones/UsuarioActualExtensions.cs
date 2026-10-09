using GestionProyectos.Domain.Excepciones;

namespace GestionProyectos.Application.Abstracciones;

/// <summary>
/// Verificaciones de permisos sobre el usuario actual (defensa en profundidad, spec 0.4).
/// </summary>
/// <remarks>
/// Creado: 2026-10-09
/// Modificado: 2026-10-09 - Versión inicial (RF-001)
/// Modificado: 2026-10-09 - Agrega AsegurarPermiso (RF-002, 0.1)
/// </remarks>
public static class UsuarioActualExtensions
{
    /// <summary>Exige un administrador; devuelve su PersonaId. Lanza 401/403 si no lo es.</summary>
    public static int AsegurarAdministrador(this IUsuarioActual usuarioActual)
    {
        var personaId = AsegurarIdentificado(usuarioActual);
        if (!usuarioActual.EsAdmin)
        {
            throw new PermisoDenegadoException();
        }

        return personaId;
    }

    /// <summary>Exige el permiso indicado; devuelve el PersonaId. Lanza 401/403 si no lo tiene.</summary>
    public static int AsegurarPermiso(this IUsuarioActual usuarioActual, string permiso)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(permiso);
        var personaId = AsegurarIdentificado(usuarioActual);
        if (!usuarioActual.TienePermiso(permiso))
        {
            throw new PermisoDenegadoException(permiso);
        }

        return personaId;
    }

    private static int AsegurarIdentificado(IUsuarioActual usuarioActual)
    {
        ArgumentNullException.ThrowIfNull(usuarioActual);
        if (!usuarioActual.EstaIdentificado || usuarioActual.PersonaId is null)
        {
            throw new UsuarioNoIdentificadoException();
        }

        return usuarioActual.PersonaId.Value;
    }
}
