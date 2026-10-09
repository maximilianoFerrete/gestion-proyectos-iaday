using FluentValidation;
using GestionProyectos.Application.Comun;

namespace GestionProyectos.Application.TiposIncidencia;

/// <summary>
/// Tipo de incidencia expuesto por la API, con nivel, flujo y cantidad de incidencias.
/// </summary>
/// <remarks>
/// Creado: 2026-10-09
/// Modificado: 2026-10-09 - Versión inicial (RF-002)
/// </remarks>
public sealed record TipoIncidenciaDto(
    int Id,
    string Nombre,
    string? Descripcion,
    NivelReferenciaDto Nivel,
    FlujoReferenciaDto Flujo,
    bool Activo,
    int CantidadIncidencias,
    DateTime FechaCreacion,
    DateTime? FechaModificacion);

/// <summary>
/// Datos para crear un tipo de incidencia (RN-05, RN-06).
/// </summary>
/// <remarks>
/// Creado: 2026-10-09
/// Modificado: 2026-10-09 - Versión inicial (RF-002)
/// </remarks>
public sealed record CrearTipoIncidenciaRequest(string? Nombre, string? Descripcion, int? NivelId, int? FlujoId)
{
    /// <summary>Devuelve una copia con los textos normalizados (RN-39).</summary>
    public CrearTipoIncidenciaRequest Normalizar() => this with
    {
        Nombre = TextoEntrada.Normalizar(Nombre),
        Descripcion = TextoEntrada.Normalizar(Descripcion),
    };
}

/// <summary>
/// Datos para editar un tipo; el nivel no es editable (RN-06, DA-03).
/// </summary>
/// <remarks>
/// Creado: 2026-10-09
/// Modificado: 2026-10-09 - Versión inicial (RF-002)
/// </remarks>
public sealed record EditarTipoIncidenciaRequest(string? Nombre, string? Descripcion, int? FlujoId)
{
    /// <summary>Devuelve una copia con los textos normalizados (RN-39).</summary>
    public EditarTipoIncidenciaRequest Normalizar() => this with
    {
        Nombre = TextoEntrada.Normalizar(Nombre),
        Descripcion = TextoEntrada.Normalizar(Descripcion),
    };
}

/// <summary>
/// Límites de formato de los tipos de incidencia (RN-05).
/// </summary>
/// <remarks>
/// Creado: 2026-10-09
/// Modificado: 2026-10-09 - Versión inicial (RF-002)
/// </remarks>
public static class LimitesTipo
{
    public const int LargoMaximoNombre = 50;
    public const int LargoMaximoDescripcion = 250;
}

/// <summary>
/// Validación de formato del alta de tipo (RN-05).
/// </summary>
/// <remarks>
/// Creado: 2026-10-09
/// Modificado: 2026-10-09 - Versión inicial (RF-002)
/// </remarks>
public class CrearTipoIncidenciaRequestValidator : AbstractValidator<CrearTipoIncidenciaRequest>
{
    /// <summary>Define las reglas de nombre, descripción, nivel y flujo.</summary>
    public CrearTipoIncidenciaRequestValidator()
    {
        RuleFor(x => x.Nombre).TextoObligatorio("El nombre", LimitesTipo.LargoMaximoNombre);
        RuleFor(x => x.Descripcion).TextoOpcional("La descripción", LimitesTipo.LargoMaximoDescripcion);
        RuleFor(x => x.NivelId)
            .NotNull().WithMessage("El nivel es obligatorio.")
            .GreaterThan(0).WithMessage("El nivel es obligatorio.");
        RuleFor(x => x.FlujoId)
            .NotNull().WithMessage("El flujo es obligatorio.")
            .GreaterThan(0).WithMessage("El flujo es obligatorio.");
    }
}

/// <summary>
/// Validación de formato de la edición de tipo (RN-05).
/// </summary>
/// <remarks>
/// Creado: 2026-10-09
/// Modificado: 2026-10-09 - Versión inicial (RF-002)
/// </remarks>
public class EditarTipoIncidenciaRequestValidator : AbstractValidator<EditarTipoIncidenciaRequest>
{
    /// <summary>Define las reglas de nombre, descripción y flujo.</summary>
    public EditarTipoIncidenciaRequestValidator()
    {
        RuleFor(x => x.Nombre).TextoObligatorio("El nombre", LimitesTipo.LargoMaximoNombre);
        RuleFor(x => x.Descripcion).TextoOpcional("La descripción", LimitesTipo.LargoMaximoDescripcion);
        RuleFor(x => x.FlujoId)
            .NotNull().WithMessage("El flujo es obligatorio.")
            .GreaterThan(0).WithMessage("El flujo es obligatorio.");
    }
}
