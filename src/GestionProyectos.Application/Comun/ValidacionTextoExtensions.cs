using FluentValidation;

namespace GestionProyectos.Application.Comun;

/// <summary>
/// Reglas de FluentValidation para textos que consideran el trim (RN-31).
/// </summary>
/// <remarks>
/// Creado: 2026-10-09
/// Modificado: 2026-10-09 - Versión inicial (RF-001)
/// </remarks>
public static class ValidacionTextoExtensions
{
    /// <summary>Texto obligatorio con largo máximo medido tras el trim; etiqueta: "El nombre".</summary>
    public static IRuleBuilderOptions<T, string?> TextoObligatorio<T>(
        this IRuleBuilder<T, string?> regla, string etiqueta, int largoMaximo, bool femenino = false) =>
        regla
            .Must(v => !string.IsNullOrWhiteSpace(v))
            .WithMessage($"{etiqueta} es {(femenino ? "obligatoria" : "obligatorio")}.")
            .TextoOpcional(etiqueta, largoMaximo);

    /// <summary>Texto opcional con largo máximo, medido después del trim.</summary>
    public static IRuleBuilderOptions<T, string?> TextoOpcional<T>(
        this IRuleBuilder<T, string?> regla, string etiqueta, int largoMaximo) =>
        regla
            .Must(v => v is null || v.Trim().Length <= largoMaximo)
            .WithMessage($"{etiqueta} no puede superar los {largoMaximo} caracteres.");
}
