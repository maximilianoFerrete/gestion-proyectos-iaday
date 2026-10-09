namespace GestionProyectos.Application.UsuarioActual;

/// <summary>
/// Datos del usuario actual expuestos por GET /api/usuario-actual.
/// </summary>
/// <remarks>
/// Creado: 2026-10-09
/// Modificado: 2026-10-09 - Versión inicial (RF-001)
/// </remarks>
public sealed record UsuarioActualDto(
    bool Identificado,
    int? PersonaId,
    string? Nombre,
    string? Email,
    bool EsAdmin)
{
    /// <summary>Usuario no identificado (sin header ni credenciales).</summary>
    public static UsuarioActualDto Anonimo { get; } = new(false, null, null, null, false);
}

/// <summary>
/// Identidad de una persona activa usada por el mecanismo de autenticación.
/// </summary>
/// <remarks>
/// Creado: 2026-10-09
/// Modificado: 2026-10-09 - Versión inicial (RF-001)
/// </remarks>
public sealed record IdentidadUsuario(int PersonaId, string Nombre, string Email, bool EsAdmin);
