using FluentValidation;

namespace GestionProyectos.Application.Seguimiento;

/// <summary>
/// Configuración de columnas del tablero guardada para un proyecto (fila de la persistencia).
/// </summary>
/// <remarks>
/// Creado: 2026-10-10
/// Modificado: 2026-10-10 - Versión inicial (RF-005, DA-15)
/// </remarks>
public sealed record ConfiguracionTablero(
    int ProyectoId,
    bool MostrarEstado,
    bool MostrarEstimacion,
    bool MostrarResponsable,
    DateTime FechaModificacion);

/// <summary>
/// Columnas visibles del tablero de un proyecto; Personalizada = hay fila guardada (RN-30).
/// </summary>
/// <remarks>
/// Creado: 2026-10-10
/// Modificado: 2026-10-10 - Versión inicial (RF-005)
/// </remarks>
public sealed record ConfiguracionTableroDto(
    int ProyectoId,
    bool Personalizada,
    bool MostrarEstado,
    bool MostrarEstimacion,
    bool MostrarResponsable,
    DateTime? FechaModificacion)
{
    /// <summary>Configuración por defecto (todas visibles) o la guardada si existe (RN-30).</summary>
    public static ConfiguracionTableroDto Desde(int proyectoId, ConfiguracionTablero? guardada) =>
        guardada is null
            ? new ConfiguracionTableroDto(proyectoId, false, true, true, true, null)
            : new ConfiguracionTableroDto(guardada.ProyectoId, true, guardada.MostrarEstado,
                guardada.MostrarEstimacion, guardada.MostrarResponsable, guardada.FechaModificacion);
}

/// <summary>
/// Reemplazo completo de las columnas configurables; los tres valores son obligatorios (RN-31).
/// </summary>
/// <remarks>
/// Creado: 2026-10-10
/// Modificado: 2026-10-10 - Versión inicial (RF-005)
/// </remarks>
public sealed record ConfiguracionTableroRequest(bool? MostrarEstado, bool? MostrarEstimacion,
    bool? MostrarResponsable);

/// <summary>
/// Validación de ConfiguracionTableroRequest: ningún valor puede faltar ni ser null (RN-31, CA-33).
/// </summary>
/// <remarks>
/// Creado: 2026-10-10
/// Modificado: 2026-10-10 - Versión inicial (RF-005)
/// </remarks>
public class ConfiguracionTableroRequestValidator : AbstractValidator<ConfiguracionTableroRequest>
{
    /// <summary>Define la obligatoriedad de los tres valores.</summary>
    public ConfiguracionTableroRequestValidator()
    {
        RuleFor(x => x.MostrarEstado).NotNull().WithMessage("Indique si se muestra la columna Estado.");
        RuleFor(x => x.MostrarEstimacion).NotNull().WithMessage("Indique si se muestra la columna Estimación.");
        RuleFor(x => x.MostrarResponsable).NotNull().WithMessage("Indique si se muestra la columna Responsable.");
    }
}
