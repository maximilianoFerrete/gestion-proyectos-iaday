using System.Globalization;
using GestionProyectos.Domain.Entidades;
using GestionProyectos.Domain.Excepciones;

namespace GestionProyectos.Domain.Reglas;

/// <summary>
/// Reglas puras de la jerarquía de incidencias: ciclos, relaciones, agrupación y vencimientos.
/// </summary>
/// <remarks>
/// Creado: 2026-10-09
/// Modificado: 2026-10-09 - Versión inicial (RF-002, RN-25 a RN-32)
/// </remarks>
public static class ReglasJerarquia
{
    /// <summary>Clave de la extensión de ProblemDetails con los códigos afectados (spec 0.2).</summary>
    public const string ExtensionIncidencias = "incidencias";

    /// <summary>Rechaza un padre que sea la incidencia o uno de sus descendientes (RN-26).</summary>
    public static void AsegurarSinCiclo(int incidenciaId, int padreId, IReadOnlyCollection<int> idsAncestrosDelPadre)
    {
        ArgumentNullException.ThrowIfNull(idsAncestrosDelPadre);
        if (padreId == incidenciaId || idsAncestrosDelPadre.Contains(incidenciaId))
        {
            throw new ConflictoException(CodigosError.RelacionCircular,
                "El padre elegido es la propia incidencia o una de sus descendientes.");
        }
    }

    /// <summary>Exige la relación tipo del padre → tipo de la hija (RN-25).</summary>
    public static void AsegurarRelacionPermitida(TipoIncidencia tipoPadre, TipoIncidencia tipoHijo,
        IReadOnlyCollection<int> tiposHijoPermitidosDelPadre)
    {
        ArgumentNullException.ThrowIfNull(tipoPadre);
        ArgumentNullException.ThrowIfNull(tipoHijo);
        ArgumentNullException.ThrowIfNull(tiposHijoPermitidosDelPadre);
        if (!tiposHijoPermitidosDelPadre.Contains(tipoHijo.Id))
        {
            throw new ValidacionNegocioException(CodigosError.RelacionNoPermitida,
                $"No se permite una incidencia de tipo '{tipoHijo.Nombre}' como hija de una de tipo "
                + $"'{tipoPadre.Nombre}'.");
        }
    }

    /// <summary>Con padre, la agrupación es la del padre; otro valor es un error (RN-28).</summary>
    public static int? ResolverAgrupacionConPadre(int? agrupacionSolicitada, Incidencia padre)
    {
        ArgumentNullException.ThrowIfNull(padre);
        if (agrupacionSolicitada is not null && agrupacionSolicitada != padre.AgrupacionId)
        {
            throw new ValidacionNegocioException(CodigosError.AgrupacionHeredada,
                $"La agrupación se hereda del padre {padre.Codigo}; envíe agrupacionId nulo o igual al del padre.");
        }

        return padre.AgrupacionId;
    }

    /// <summary>Sin padre: al quitar el padre sin enviar agrupación se conserva la actual (RN-29).</summary>
    public static int? ResolverAgrupacionSinPadre(int? agrupacionSolicitada, Incidencia? actual)
    {
        if (agrupacionSolicitada is null && actual is { PadreId: not null })
        {
            return actual.AgrupacionId;
        }

        return agrupacionSolicitada;
    }

    /// <summary>Indica si hay que propagar la agrupación a los descendientes (RN-29).</summary>
    public static bool RequierePropagacion(int? agrupacionAnterior, int? padreAnterior, UbicacionIncidencia nueva)
    {
        ArgumentNullException.ThrowIfNull(nueva);
        var cambioAOtroPadre = nueva.PadreId is not null && nueva.PadreId != padreAnterior;
        return agrupacionAnterior != nueva.AgrupacionId || cambioAOtroPadre;
    }

    /// <summary>El vencimiento de la hija no puede superar el del padre directo (RN-31).</summary>
    public static void AsegurarVencimientoNoPosteriorAlPadre(DateOnly? vencimiento, Incidencia padre)
    {
        ArgumentNullException.ThrowIfNull(padre);
        if (vencimiento is { } hija && padre.FechaVencimiento is { } delPadre && hija > delPadre)
        {
            throw new ValidacionNegocioException(CodigosError.VencimientoPosteriorAlPadre,
                $"El vencimiento {Formato(hija)} es posterior al del padre {padre.Codigo} ({Formato(delPadre)}).");
        }
    }

    /// <summary>Rechaza un vencimiento anterior al de alguna hija directa (RN-32).</summary>
    public static void AsegurarVencimientoNoAnteriorAHijas(DateOnly? nuevoVencimiento,
        IEnumerable<HijaIncidencia> hijas)
    {
        ArgumentNullException.ThrowIfNull(hijas);
        if (nuevoVencimiento is not { } nuevo)
        {
            return;
        }

        var posteriores = hijas
            .Where(h => h.FechaVencimiento is { } fecha && fecha > nuevo)
            .ToList();
        if (posteriores.Count == 0)
        {
            return;
        }

        var detalle = string.Join(", ", posteriores.Select(h => $"{h.Codigo} ({Formato(h.FechaVencimiento!.Value)})"));
        throw new ConflictoException(CodigosError.VencimientoAnteriorAHijas,
            $"El vencimiento {Formato(nuevo)} es anterior al de las hijas {detalle}.",
            ExtensionCodigos(posteriores));
    }

    /// <summary>Exige que cada hija directa sea compatible con el nuevo tipo (RN-25).</summary>
    public static void AsegurarHijasCompatibles(TipoIncidencia nuevoTipo, IEnumerable<HijaIncidencia> hijas,
        IReadOnlyCollection<int> tiposHijoPermitidos)
    {
        ArgumentNullException.ThrowIfNull(nuevoTipo);
        ArgumentNullException.ThrowIfNull(hijas);
        ArgumentNullException.ThrowIfNull(tiposHijoPermitidos);
        var incompatibles = hijas.Where(h => !tiposHijoPermitidos.Contains(h.TipoId)).ToList();
        if (incompatibles.Count == 0)
        {
            return;
        }

        throw new ConflictoException(CodigosError.TipoIncompatibleConHijas,
            $"El tipo '{nuevoTipo.Nombre}' no admite como hijas a: "
            + $"{string.Join(", ", incompatibles.Select(h => h.Codigo))}.",
            ExtensionCodigos(incompatibles));
    }

    private static Dictionary<string, object?> ExtensionCodigos(IEnumerable<HijaIncidencia> hijas) =>
        new() { [ExtensionIncidencias] = hijas.Select(h => h.Codigo).ToArray() };

    private static string Formato(DateOnly fecha) => fecha.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
}
