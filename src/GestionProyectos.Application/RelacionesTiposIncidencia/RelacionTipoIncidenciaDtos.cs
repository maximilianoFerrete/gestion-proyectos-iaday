using FluentValidation;
using GestionProyectos.Application.Comun;

namespace GestionProyectos.Application.RelacionesTiposIncidencia;

/// <summary>
/// Relación permitida tipo padre → tipo hijo expuesta por la API.
/// </summary>
/// <remarks>
/// Creado: 2026-10-09
/// Modificado: 2026-10-09 - Versión inicial (RF-002)
/// </remarks>
public sealed record RelacionTipoIncidenciaDto(
    int Id,
    TipoReferenciaDto TipoPadre,
    TipoReferenciaDto TipoHijo,
    DateTime FechaCreacion);

/// <summary>
/// Datos para crear una relación permitida (RN-08).
/// </summary>
/// <remarks>
/// Creado: 2026-10-09
/// Modificado: 2026-10-09 - Versión inicial (RF-002)
/// </remarks>
public sealed record CrearRelacionTipoIncidenciaRequest(int? TipoPadreId, int? TipoHijoId);

/// <summary>
/// Validación de formato del alta de relación: ids presentes y distintos (RN-08).
/// </summary>
/// <remarks>
/// Creado: 2026-10-09
/// Modificado: 2026-10-09 - Versión inicial (RF-002)
/// </remarks>
public class CrearRelacionTipoIncidenciaRequestValidator : AbstractValidator<CrearRelacionTipoIncidenciaRequest>
{
    /// <summary>Define las reglas de tipo padre y tipo hijo.</summary>
    public CrearRelacionTipoIncidenciaRequestValidator()
    {
        RuleFor(x => x.TipoPadreId)
            .NotNull().WithMessage("El tipo padre es obligatorio.")
            .GreaterThan(0).WithMessage("El tipo padre es obligatorio.");
        RuleFor(x => x.TipoHijoId)
            .NotNull().WithMessage("El tipo hijo es obligatorio.")
            .GreaterThan(0).WithMessage("El tipo hijo es obligatorio.")
            .NotEqual(x => x.TipoPadreId)
            .When(x => x.TipoPadreId is not null, ApplyConditionTo.CurrentValidator)
            .WithMessage("El tipo hijo debe ser distinto del tipo padre.");
    }
}
