using GestionProyectos.Domain.Entidades;
using GestionProyectos.Domain.Excepciones;

namespace GestionProyectos.Domain.Reglas;

/// <summary>
/// Reglas puras de la categoría de seguimiento: un estado con categoría no es inicial ni final (RN-18).
/// </summary>
/// <remarks>
/// Creado: 2026-10-10
/// Modificado: 2026-10-10 - Versión inicial (RF-005, DA-06)
/// </remarks>
public static class ReglasCategoriaEstado
{
    /// <summary>Indica si la categoría es compatible con las marcas del estado en un flujo (RN-18).</summary>
    public static bool EsCompatible(string? categoria, bool esInicial, bool esFinal) =>
        categoria is null || (!esInicial && !esFinal);

    /// <summary>Rechaza asignar categoría a un estado inicial o final en algún flujo (RN-18 a).</summary>
    public static void AsegurarCategoriaAsignable(string nombreEstado, string? categoria,
        IReadOnlyCollection<string> flujosDondeEsInicialOFinal)
    {
        ArgumentNullException.ThrowIfNull(flujosDondeEsInicialOFinal);
        if (categoria is null || flujosDondeEsInicialOFinal.Count == 0)
        {
            return;
        }

        throw new ConflictoException(CodigosError.EstadoCategoriaIncompatible,
            $"El estado '{nombreEstado}' es inicial o final en los flujos: "
            + $"{string.Join(", ", flujosDondeEsInicialOFinal)}; un estado con categoría de seguimiento "
            + "no puede ser inicial ni final.");
    }

    /// <summary>Rechaza marcar inicial o final en el flujo a estados con categoría (RN-18 b).</summary>
    public static void AsegurarComposicionCompatible(string nombreFlujo, IEnumerable<EstadoEnFlujo> composicion,
        IReadOnlyCollection<EstadoIncidencia> estados)
    {
        ArgumentNullException.ThrowIfNull(composicion);
        ArgumentNullException.ThrowIfNull(estados);
        var incompatibles = composicion
            .Select(item => (Item: item, Estado: estados.FirstOrDefault(e => e.Id == item.EstadoId)))
            .Where(par => par.Estado is not null
                && !EsCompatible(par.Estado.Categoria, par.Item.EsInicial, par.Item.EsFinal))
            .Select(par => par.Estado!.Nombre)
            .ToList();
        if (incompatibles.Count == 0)
        {
            return;
        }

        var nombres = string.Join("', '", incompatibles);
        var sujeto = incompatibles.Count == 1
            ? $"El estado '{nombres}' tiene categoría de seguimiento y no puede"
            : $"Los estados '{nombres}' tienen categoría de seguimiento y no pueden";
        throw new ConflictoException(CodigosError.EstadoCategoriaIncompatible,
            $"{sujeto} ser inicial ni final en el flujo '{nombreFlujo}'.");
    }
}
