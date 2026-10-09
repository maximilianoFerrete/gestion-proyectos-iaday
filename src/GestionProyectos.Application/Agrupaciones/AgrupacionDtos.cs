using FluentValidation;
using GestionProyectos.Application.Comun;
using GestionProyectos.Domain.Entidades;

namespace GestionProyectos.Application.Agrupaciones;

/// <summary>
/// Agrupación expuesta por la API.
/// </summary>
/// <remarks>
/// Creado: 2026-10-09
/// Modificado: 2026-10-09 - Versión inicial (RF-001)
/// </remarks>
public sealed record AgrupacionDto(
    int Id,
    int ProyectoId,
    string Nombre,
    string Descripcion,
    DateTime FechaCreacion,
    DateTime? FechaModificacion)
{
    /// <summary>Crea el DTO a partir de la entidad.</summary>
    public static AgrupacionDto Desde(Agrupacion agrupacion)
    {
        ArgumentNullException.ThrowIfNull(agrupacion);
        return new AgrupacionDto(agrupacion.Id, agrupacion.ProyectoId, agrupacion.Nombre,
            agrupacion.Descripcion, agrupacion.FechaCreacion, agrupacion.FechaModificacion);
    }
}

/// <summary>
/// Datos para crear o editar una agrupación; el proyecto viene de la URL (RN-28).
/// </summary>
/// <remarks>
/// Creado: 2026-10-09
/// Modificado: 2026-10-09 - Versión inicial (RF-001)
/// </remarks>
public sealed record AgrupacionRequest(string? Nombre, string? Descripcion)
{
    /// <summary>Devuelve una copia con los textos normalizados (RN-31).</summary>
    public AgrupacionRequest Normalizar() => this with
    {
        Nombre = TextoEntrada.Normalizar(Nombre),
        Descripcion = TextoEntrada.Normalizar(Descripcion),
    };
}

/// <summary>
/// Validación de formato de AgrupacionRequest (RN-26).
/// </summary>
/// <remarks>
/// Creado: 2026-10-09
/// Modificado: 2026-10-09 - Versión inicial (RF-001)
/// </remarks>
public class AgrupacionRequestValidator : AbstractValidator<AgrupacionRequest>
{
    public const int LargoMaximoNombre = 100;
    public const int LargoMaximoDescripcion = 1000;

    /// <summary>Define las reglas de nombre y descripción.</summary>
    public AgrupacionRequestValidator()
    {
        RuleFor(x => x.Nombre).TextoObligatorio("El nombre", LargoMaximoNombre);
        RuleFor(x => x.Descripcion).TextoObligatorio("La descripción", LargoMaximoDescripcion, femenino: true);
    }
}
