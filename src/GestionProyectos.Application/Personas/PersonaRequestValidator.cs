using System.Text.RegularExpressions;
using FluentValidation;
using GestionProyectos.Application.Comun;

namespace GestionProyectos.Application.Personas;

/// <summary>
/// Validación de formato de PersonaRequest (RN-01, RN-02).
/// </summary>
/// <remarks>
/// Creado: 2026-10-09
/// Modificado: 2026-10-09 - Versión inicial (RF-001)
/// </remarks>
public partial class PersonaRequestValidator : AbstractValidator<PersonaRequest>
{
    public const int LargoMaximoNombre = 150;
    public const int LargoMaximoEmail = 254;

    /// <summary>Define las reglas de nombre y email.</summary>
    public PersonaRequestValidator()
    {
        RuleFor(x => x.Nombre).TextoObligatorio("El nombre", LargoMaximoNombre);

        RuleFor(x => x.Email)
            .TextoObligatorio("El email", LargoMaximoEmail)
            .DependentRules(() =>
                RuleFor(x => x.Email)
                    .Must(v => v is null || PatronEmail().IsMatch(v.Trim()))
                    .WithMessage("El email no tiene un formato válido."));
    }

    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.None, matchTimeoutMilliseconds: 250)]
    private static partial Regex PatronEmail();
}
