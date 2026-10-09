using GestionProyectos.Application.Comun;
using GestionProyectos.Domain.Entidades;

namespace GestionProyectos.Application.Personas;

/// <summary>
/// Persona expuesta por la API.
/// </summary>
/// <remarks>
/// Creado: 2026-10-09
/// Modificado: 2026-10-09 - Versión inicial (RF-001)
/// </remarks>
public sealed record PersonaDto(
    int Id,
    string Nombre,
    string Email,
    bool EsAdmin,
    bool Activo,
    DateTime FechaCreacion,
    DateTime? FechaModificacion)
{
    /// <summary>Crea el DTO a partir de la entidad.</summary>
    public static PersonaDto Desde(Persona persona)
    {
        ArgumentNullException.ThrowIfNull(persona);
        return new PersonaDto(persona.Id, persona.Nombre, persona.Email, persona.EsAdmin,
            persona.Activo, persona.FechaCreacion, persona.FechaModificacion);
    }
}

/// <summary>
/// Datos para crear o editar una persona.
/// </summary>
/// <remarks>
/// Creado: 2026-10-09
/// Modificado: 2026-10-09 - Versión inicial (RF-001)
/// </remarks>
public sealed record PersonaRequest(string? Nombre, string? Email, bool EsAdmin)
{
    /// <summary>Devuelve una copia con trim y email en minúsculas (RN-02, RN-31).</summary>
    public PersonaRequest Normalizar() => this with
    {
        Nombre = TextoEntrada.Normalizar(Nombre),
        Email = TextoEntrada.NormalizarMinusculas(Email),
    };
}
