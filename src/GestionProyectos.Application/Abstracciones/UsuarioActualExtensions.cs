using GestionProyectos.Domain.Excepciones;

namespace GestionProyectos.Application.Abstracciones;

/// <summary>
/// Verificaciones de permisos sobre el usuario actual (defensa en profundidad, spec 0.4).
/// </summary>
/// <remarks>
/// Creado: 2026-10-09
/// Modificado: 2026-10-09 - Versión inicial (RF-001)
/// </remarks>
public static class UsuarioActualExtensions
{
    /// <summary>Exige un administrador; devuelve su PersonaId. Lanza 401/403 si no lo es.</summary>
    public static int AsegurarAdministrador(this IUsuarioActual usuarioActual)
    {
        ArgumentNullException.ThrowIfNull(usuarioActual);
        if (!usuarioActual.EstaIdentificado || usuarioActual.PersonaId is null)
        {
            throw new UsuarioNoIdentificadoException();
        }

        if (!usuarioActual.EsAdmin)
        {
            throw new PermisoDenegadoException();
        }

        return usuarioActual.PersonaId.Value;
    }
}
