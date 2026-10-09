namespace GestionProyectos.Application.UsuarioActual;

/// <summary>
/// Consulta de los datos del usuario actual.
/// </summary>
/// <remarks>
/// Creado: 2026-10-09
/// Modificado: 2026-10-09 - Versión inicial (RF-001)
/// </remarks>
public interface IUsuarioActualService
{
    /// <summary>Devuelve el usuario actual o el anónimo si no está identificado.</summary>
    Task<UsuarioActualDto> ObtenerAsync(CancellationToken ct);
}

/// <summary>
/// Búsqueda de la persona que se identifica, usada por los esquemas de autenticación.
/// </summary>
/// <remarks>
/// Creado: 2026-10-09
/// Modificado: 2026-10-09 - Versión inicial (RF-001)
/// </remarks>
public interface IIdentificacionUsuarioService
{
    /// <summary>Devuelve la identidad de la persona si existe y está activa; si no, null.</summary>
    Task<IdentidadUsuario?> BuscarActivaAsync(int personaId, CancellationToken ct);
}
