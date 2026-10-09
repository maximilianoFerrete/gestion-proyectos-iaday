namespace GestionProyectos.Application.UsuarioActual;

/// <summary>
/// Datos del usuario actual expuestos por GET /api/usuario-actual.
/// </summary>
/// <remarks>
/// Creado: 2026-10-09
/// Modificado: 2026-10-09 - Versión inicial (RF-001)
/// Modificado: 2026-10-09 - Agrega Permisos con igualdad por valor (RF-002, 0.1)
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

    /// <summary>Permisos del usuario (vacío si es anónimo o no tiene ninguno).</summary>
    public IReadOnlyList<string> Permisos { get; init; } = [];

    /// <summary>Compara por valor, incluida la lista de permisos.</summary>
    public bool Equals(UsuarioActualDto? other) =>
        other is not null
        && Identificado == other.Identificado
        && PersonaId == other.PersonaId
        && Nombre == other.Nombre
        && Email == other.Email
        && EsAdmin == other.EsAdmin
        && Permisos.SequenceEqual(other.Permisos);

    /// <summary>Hash coherente con Equals.</summary>
    public override int GetHashCode() =>
        HashCode.Combine(Identificado, PersonaId, Nombre, Email, EsAdmin, Permisos.Count);
}

/// <summary>
/// Identidad de una persona activa usada por el mecanismo de autenticación.
/// </summary>
/// <remarks>
/// Creado: 2026-10-09
/// Modificado: 2026-10-09 - Versión inicial (RF-001)
/// </remarks>
public sealed record IdentidadUsuario(int PersonaId, string Nombre, string Email, bool EsAdmin);
