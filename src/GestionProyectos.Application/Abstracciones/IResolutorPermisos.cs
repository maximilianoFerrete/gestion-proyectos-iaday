using GestionProyectos.Application.UsuarioActual;

namespace GestionProyectos.Application.Abstracciones;

/// <summary>
/// Obtiene los permisos de una identidad; reemplazable cuando exista gestión de roles (0.1).
/// </summary>
/// <remarks>
/// Creado: 2026-10-09
/// Modificado: 2026-10-09 - Versión inicial (RF-002)
/// </remarks>
public interface IResolutorPermisos
{
    /// <summary>Devuelve los permisos de la identidad (vacío si no tiene ninguno).</summary>
    Task<IReadOnlyCollection<string>> ObtenerPermisosAsync(IdentidadUsuario identidad, CancellationToken ct);
}
