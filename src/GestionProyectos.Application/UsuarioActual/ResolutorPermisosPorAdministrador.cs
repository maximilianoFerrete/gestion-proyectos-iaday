using GestionProyectos.Application.Abstracciones;

namespace GestionProyectos.Application.UsuarioActual;

/// <summary>
/// Resolutor provisional: un administrador tiene todos los permisos y el resto ninguno (RN-03).
/// </summary>
/// <remarks>
/// Creado: 2026-10-09
/// Modificado: 2026-10-09 - Versión inicial (RF-002, DA-07)
/// </remarks>
public sealed class ResolutorPermisosPorAdministrador : IResolutorPermisos
{
    /// <inheritdoc />
    public Task<IReadOnlyCollection<string>> ObtenerPermisosAsync(IdentidadUsuario identidad, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(identidad);
        IReadOnlyCollection<string> permisos = identidad.EsAdmin ? Permisos.Todos : [];
        return Task.FromResult(permisos);
    }
}
