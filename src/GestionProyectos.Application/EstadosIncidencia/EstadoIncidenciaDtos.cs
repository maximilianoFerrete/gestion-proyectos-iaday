using FluentValidation;
using GestionProyectos.Application.Comun;
using GestionProyectos.Domain.Comun;

namespace GestionProyectos.Application.EstadosIncidencia;

/// <summary>
/// Flujo en el que participa un estado, con sus marcas dentro de ese flujo.
/// </summary>
/// <remarks>
/// Creado: 2026-10-09
/// Modificado: 2026-10-09 - Versión inicial (RF-002)
/// </remarks>
public sealed record FlujoDeEstadoDto(int Id, string Nombre, bool EsInicial, bool EsFinal);

/// <summary>
/// Estado de incidencia expuesto por la API, con cantidad de incidencias y flujos.
/// </summary>
/// <remarks>
/// Creado: 2026-10-09
/// Modificado: 2026-10-09 - Versión inicial (RF-002)
/// Modificado: 2026-10-10 - Agrega Categoria de seguimiento (RF-005, RN-17)
/// </remarks>
public sealed record EstadoIncidenciaDto(
    int Id,
    string Nombre,
    string? Descripcion,
    int Orden,
    bool Activo,
    int CantidadIncidencias,
    IReadOnlyList<FlujoDeEstadoDto> Flujos,
    string? Categoria = null);

/// <summary>
/// Datos para crear o editar un estado de incidencia (RN-10); la categoría es opcional (RF-005 RN-17).
/// </summary>
/// <remarks>
/// Creado: 2026-10-09
/// Modificado: 2026-10-09 - Versión inicial (RF-002)
/// Modificado: 2026-10-10 - Agrega Categoria (RF-005, RN-17)
/// </remarks>
public sealed record EstadoIncidenciaRequest(string? Nombre, string? Descripcion, int? Orden,
    string? Categoria = null)
{
    /// <summary>Devuelve una copia con textos normalizados y la categoría en mayúsculas (RN-39).</summary>
    public EstadoIncidenciaRequest Normalizar() => this with
    {
        Nombre = TextoEntrada.Normalizar(Nombre),
        Descripcion = TextoEntrada.Normalizar(Descripcion),
        Categoria = TextoEntrada.Normalizar(Categoria)?.ToUpperInvariant(),
    };
}

/// <summary>
/// Límites de formato de catálogos con nombre, descripción y orden (RN-10, RN-17).
/// </summary>
/// <remarks>
/// Creado: 2026-10-09
/// Modificado: 2026-10-09 - Versión inicial (RF-002)
/// </remarks>
public static class LimitesCatalogoIncidencia
{
    public const int LargoMaximoNombre = 50;
    public const int LargoMaximoDescripcion = 250;
    public const int OrdenMinimo = 1;
    public const int OrdenMaximo = 9999;
}

/// <summary>
/// Validación de formato de EstadoIncidenciaRequest (RN-10).
/// </summary>
/// <remarks>
/// Creado: 2026-10-09
/// Modificado: 2026-10-09 - Versión inicial (RF-002)
/// Modificado: 2026-10-10 - Valida la categoría (RF-005, RN-17)
/// </remarks>
public class EstadoIncidenciaRequestValidator : AbstractValidator<EstadoIncidenciaRequest>
{
    /// <summary>Define las reglas de nombre, descripción, orden y categoría.</summary>
    public EstadoIncidenciaRequestValidator()
    {
        RuleFor(x => x.Nombre).TextoObligatorio("El nombre", LimitesCatalogoIncidencia.LargoMaximoNombre);
        RuleFor(x => x.Descripcion)
            .TextoOpcional("La descripción", LimitesCatalogoIncidencia.LargoMaximoDescripcion);
        RuleFor(x => x.Orden)
            .NotNull().WithMessage("El orden es obligatorio.")
            .InclusiveBetween(LimitesCatalogoIncidencia.OrdenMinimo, LimitesCatalogoIncidencia.OrdenMaximo)
            .WithMessage($"El orden debe estar entre {LimitesCatalogoIncidencia.OrdenMinimo} y "
                + $"{LimitesCatalogoIncidencia.OrdenMaximo}.");
        RuleFor(x => x.Categoria)
            .Must(CategoriasEstadoIncidencia.EsValida)
            .WithMessage("Valores permitidos para categoria: BLOQUEADA, EN_ESPERA o null.");
    }
}
