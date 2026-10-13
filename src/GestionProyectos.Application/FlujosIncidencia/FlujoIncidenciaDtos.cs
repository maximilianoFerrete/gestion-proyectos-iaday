using FluentValidation;
using GestionProyectos.Application.Comun;
using GestionProyectos.Domain.Entidades;

namespace GestionProyectos.Application.FlujosIncidencia;

/// <summary>
/// Estado incluido en un flujo, con su posición, marcas y categoría de seguimiento.
/// </summary>
/// <remarks>
/// Creado: 2026-10-09
/// Modificado: 2026-10-09 - Versión inicial (RF-002)
/// Modificado: 2026-10-10 - Agrega Categoria (RF-005, RN-18)
/// </remarks>
public sealed record EstadoDeFlujoDto(int EstadoId, string Nombre, bool Activo, int Orden, bool EsInicial,
    bool EsFinal, string? Categoria = null);

/// <summary>
/// Tipo de incidencia que usa un flujo.
/// </summary>
/// <remarks>
/// Creado: 2026-10-09
/// Modificado: 2026-10-09 - Versión inicial (RF-002)
/// </remarks>
public sealed record TipoDeFlujoDto(int Id, string Nombre, bool Activo);

/// <summary>
/// Flujo de trabajo expuesto por la API, con sus estados ordenados y los tipos que lo usan.
/// </summary>
/// <remarks>
/// Creado: 2026-10-09
/// Modificado: 2026-10-09 - Versión inicial (RF-002)
/// </remarks>
public sealed record FlujoIncidenciaDto(
    int Id,
    string Nombre,
    string? Descripcion,
    IReadOnlyList<EstadoDeFlujoDto> Estados,
    IReadOnlyList<TipoDeFlujoDto> Tipos,
    DateTime FechaCreacion,
    DateTime? FechaModificacion);

/// <summary>
/// Estado de la composición de un flujo en el request; el orden es su posición.
/// </summary>
/// <remarks>
/// Creado: 2026-10-09
/// Modificado: 2026-10-09 - Versión inicial (RF-002)
/// </remarks>
public sealed record EstadoFlujoRequest(int? EstadoId, bool EsInicial, bool EsFinal);

/// <summary>
/// Datos para crear o reemplazar un flujo con su composición completa (RN-13, RN-14).
/// </summary>
/// <remarks>
/// Creado: 2026-10-09
/// Modificado: 2026-10-09 - Versión inicial (RF-002)
/// </remarks>
public sealed record FlujoIncidenciaRequest(
    string? Nombre,
    string? Descripcion,
    IReadOnlyList<EstadoFlujoRequest>? Estados)
{
    /// <summary>Devuelve una copia con los textos normalizados (RN-39).</summary>
    public FlujoIncidenciaRequest Normalizar() => this with
    {
        Nombre = TextoEntrada.Normalizar(Nombre),
        Descripcion = TextoEntrada.Normalizar(Descripcion),
    };

    /// <summary>Convierte los estados validados en la composición del dominio (orden desde 1).</summary>
    public IReadOnlyList<EstadoEnFlujo> AComposicion() =>
        (Estados ?? [])
            .Select((e, indice) => new EstadoEnFlujo(e.EstadoId!.Value, indice + 1, e.EsInicial, e.EsFinal))
            .ToList();
}

/// <summary>
/// Validación de formato de FlujoIncidenciaRequest; los errores de composición van en "estados".
/// </summary>
/// <remarks>
/// Creado: 2026-10-09
/// Modificado: 2026-10-09 - Versión inicial (RF-002, RN-13, RN-14)
/// </remarks>
public class FlujoIncidenciaRequestValidator : AbstractValidator<FlujoIncidenciaRequest>
{
    public const int LargoMaximoNombre = 100;
    public const int LargoMaximoDescripcion = 250;

    /// <summary>Define las reglas de nombre, descripción y composición.</summary>
    public FlujoIncidenciaRequestValidator()
    {
        RuleFor(x => x.Nombre).TextoObligatorio("El nombre", LargoMaximoNombre);
        RuleFor(x => x.Descripcion).TextoOpcional("La descripción", LargoMaximoDescripcion);

        RuleFor(x => x.Estados)
            .Must(e => e is { Count: > 0 })
            .WithMessage("El flujo debe incluir al menos un estado.");
        RuleFor(x => x.Estados)
            .Must(e => e!.All(item => item is { EstadoId: > 0 }))
            .When(x => x.Estados is { Count: > 0 })
            .WithMessage("Cada estado del flujo debe indicar un estadoId válido.");
        RuleFor(x => x.Estados)
            .Must(e => e!.Where(i => i?.EstadoId is not null).GroupBy(i => i.EstadoId).All(g => g.Count() == 1))
            .When(x => x.Estados is { Count: > 0 })
            .WithMessage("Un estado no puede repetirse en el flujo.");
        RuleFor(x => x.Estados)
            .Must(e => e!.Count(i => i is { EsInicial: true }) == 1)
            .When(x => x.Estados is { Count: > 0 })
            .WithMessage("El flujo debe tener exactamente un estado inicial.");
        RuleFor(x => x.Estados)
            .Must(e => !e!.Any(i => i is { EsInicial: true, EsFinal: true }))
            .When(x => x.Estados is { Count: > 0 })
            .WithMessage("El estado inicial no puede ser final.");
    }
}
