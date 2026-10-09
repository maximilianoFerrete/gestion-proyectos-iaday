using FluentValidation;
using GestionProyectos.Application.Comun;

namespace GestionProyectos.Application.EstadosProyecto;

/// <summary>
/// Validación de formato de EstadoProyectoRequest (RN-07).
/// </summary>
/// <remarks>
/// Creado: 2026-10-09
/// Modificado: 2026-10-09 - Versión inicial (RF-001)
/// </remarks>
public class EstadoProyectoRequestValidator : AbstractValidator<EstadoProyectoRequest>
{
    public const int LargoMaximoNombre = 50;
    public const int LargoMaximoDescripcion = 250;
    public const int OrdenMinimo = 1;
    public const int OrdenMaximo = 9998;

    /// <summary>Define las reglas de nombre, descripción y orden.</summary>
    public EstadoProyectoRequestValidator()
    {
        RuleFor(x => x.Nombre).TextoObligatorio("El nombre", LargoMaximoNombre);
        RuleFor(x => x.Descripcion).TextoOpcional("La descripción", LargoMaximoDescripcion);

        RuleFor(x => x.Orden)
            .NotNull()
            .WithMessage("El orden es obligatorio.")
            .InclusiveBetween(OrdenMinimo, OrdenMaximo)
            .WithMessage($"El orden debe estar entre {OrdenMinimo} y {OrdenMaximo}.");
    }
}
