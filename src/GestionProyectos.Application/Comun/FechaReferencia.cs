using Microsoft.Extensions.Options;

namespace GestionProyectos.Application.Comun;

/// <summary>
/// Fecha de referencia ("hoy") única del sistema, calculada en el servidor (RF-005 0.1, RN-10).
/// </summary>
/// <remarks>
/// Creado: 2026-10-10
/// Modificado: 2026-10-10 - Versión inicial (RF-005, DA-03)
/// </remarks>
public interface IFechaReferencia
{
    /// <summary>Fecha actual en la zona horaria configurada.</summary>
    DateOnly Hoy();
}

/// <summary>
/// Opciones de la fecha de referencia (sección "FechaReferencia" de la configuración).
/// </summary>
/// <remarks>
/// Creado: 2026-10-10
/// Modificado: 2026-10-10 - Versión inicial (RF-005, 0.1)
/// </remarks>
public sealed class OpcionesFechaReferencia
{
    /// <summary>Nombre de la sección de configuración.</summary>
    public const string Seccion = "FechaReferencia";

    /// <summary>Id IANA de la zona (p. ej. America/Argentina/Buenos_Aires); null = zona del servidor.</summary>
    public string? ZonaHoraria { get; set; }

    /// <summary>Resuelve la zona configurada o la local del reloj; null si el id es inválido.</summary>
    public TimeZoneInfo? ResolverZona(TimeProvider reloj)
    {
        ArgumentNullException.ThrowIfNull(reloj);
        if (string.IsNullOrWhiteSpace(ZonaHoraria))
        {
            return reloj.LocalTimeZone;
        }

        return TimeZoneInfo.TryFindSystemTimeZoneById(ZonaHoraria.Trim(), out var zona) ? zona : null;
    }
}

/// <summary>
/// Valida al arrancar que la zona horaria configurada exista (ValidateOnStart, RF-005 0.1).
/// </summary>
/// <remarks>
/// Creado: 2026-10-10
/// Modificado: 2026-10-10 - Versión inicial (RF-005, 0.1)
/// </remarks>
public sealed class ValidadorOpcionesFechaReferencia(TimeProvider reloj) : IValidateOptions<OpcionesFechaReferencia>
{
    /// <inheritdoc />
    public ValidateOptionsResult Validate(string? name, OpcionesFechaReferencia options)
    {
        ArgumentNullException.ThrowIfNull(options);
        return options.ResolverZona(reloj) is null
            ? ValidateOptionsResult.Fail(
                $"{OpcionesFechaReferencia.Seccion}:ZonaHoraria '{options.ZonaHoraria}' no es una zona horaria "
                + "válida. Use un id IANA (p. ej. America/Argentina/Buenos_Aires) o deje el valor vacío "
                + "para usar la zona del servidor.")
            : ValidateOptionsResult.Success;
    }
}

/// <summary>
/// Implementa IFechaReferencia con TimeProvider y la zona de OpcionesFechaReferencia.
/// </summary>
/// <remarks>
/// Creado: 2026-10-10
/// Modificado: 2026-10-10 - Versión inicial (RF-005, 0.1)
/// </remarks>
public sealed class FechaReferenciaServidor(TimeProvider reloj, IOptions<OpcionesFechaReferencia> opciones)
    : IFechaReferencia
{
    private readonly TimeZoneInfo _zona = opciones.Value.ResolverZona(reloj)
        ?? throw new InvalidOperationException(
            $"Zona horaria inválida en {OpcionesFechaReferencia.Seccion}:ZonaHoraria: '{opciones.Value.ZonaHoraria}'.");

    /// <inheritdoc />
    public DateOnly Hoy() => reloj.HoyEn(_zona);
}
