using FluentValidation;
using GestionProyectos.Application.Comun;
using GestionProyectos.Application.EstadosIncidencia;
using GestionProyectos.Domain.Entidades;

namespace GestionProyectos.Application.PrioridadesIncidencia;

/// <summary>
/// Prioridad de incidencia expuesta por la API, con la cantidad de incidencias que la usan.
/// </summary>
/// <remarks>
/// Creado: 2026-10-09
/// Modificado: 2026-10-09 - Versión inicial (RF-002)
/// </remarks>
public sealed record PrioridadIncidenciaDto(
    int Id,
    string Nombre,
    string? Descripcion,
    int Orden,
    bool EsPredeterminada,
    bool Activo,
    int CantidadIncidencias)
{
    /// <summary>Crea el DTO a partir de la entidad y su cantidad de incidencias.</summary>
    public static PrioridadIncidenciaDto Desde(PrioridadIncidencia prioridad, int cantidadIncidencias)
    {
        ArgumentNullException.ThrowIfNull(prioridad);
        return new PrioridadIncidenciaDto(prioridad.Id, prioridad.Nombre, prioridad.Descripcion,
            prioridad.Orden, prioridad.EsPredeterminada, prioridad.Activo, cantidadIncidencias);
    }
}

/// <summary>
/// Datos para crear o editar una prioridad (RN-17).
/// </summary>
/// <remarks>
/// Creado: 2026-10-09
/// Modificado: 2026-10-09 - Versión inicial (RF-002)
/// </remarks>
public sealed record PrioridadIncidenciaRequest(string? Nombre, string? Descripcion, int? Orden)
{
    /// <summary>Devuelve una copia con los textos normalizados (RN-39).</summary>
    public PrioridadIncidenciaRequest Normalizar() => this with
    {
        Nombre = TextoEntrada.Normalizar(Nombre),
        Descripcion = TextoEntrada.Normalizar(Descripcion),
    };
}

/// <summary>
/// Validación de formato de PrioridadIncidenciaRequest (RN-17).
/// </summary>
/// <remarks>
/// Creado: 2026-10-09
/// Modificado: 2026-10-09 - Versión inicial (RF-002)
/// </remarks>
public class PrioridadIncidenciaRequestValidator : AbstractValidator<PrioridadIncidenciaRequest>
{
    /// <summary>Define las reglas de nombre, descripción y orden.</summary>
    public PrioridadIncidenciaRequestValidator()
    {
        RuleFor(x => x.Nombre).TextoObligatorio("El nombre", LimitesCatalogoIncidencia.LargoMaximoNombre);
        RuleFor(x => x.Descripcion)
            .TextoOpcional("La descripción", LimitesCatalogoIncidencia.LargoMaximoDescripcion);
        RuleFor(x => x.Orden)
            .NotNull().WithMessage("El orden es obligatorio.")
            .InclusiveBetween(LimitesCatalogoIncidencia.OrdenMinimo, LimitesCatalogoIncidencia.OrdenMaximo)
            .WithMessage($"El orden debe estar entre {LimitesCatalogoIncidencia.OrdenMinimo} y "
                + $"{LimitesCatalogoIncidencia.OrdenMaximo}.");
    }
}
