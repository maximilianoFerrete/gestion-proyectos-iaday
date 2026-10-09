using GestionProyectos.Domain.Entidades;
using GestionProyectos.Domain.Excepciones;

namespace GestionProyectos.Domain.Reglas;

/// <summary>
/// Reglas puras de la configuración de incidencias: relaciones, flujos y cambio de flujo.
/// </summary>
/// <remarks>
/// Creado: 2026-10-09
/// Modificado: 2026-10-09 - Versión inicial (RF-002, RN-08, RN-11 a RN-15)
/// </remarks>
public static class ReglasConfiguracionIncidencias
{
    /// <summary>El nivel del tipo padre debe ser menor que el del tipo hijo (RN-08).</summary>
    public static void AsegurarNivelesDeRelacion(NivelIncidencia nivelPadre, NivelIncidencia nivelHijo)
    {
        ArgumentNullException.ThrowIfNull(nivelPadre);
        ArgumentNullException.ThrowIfNull(nivelHijo);
        if (nivelPadre.Numero >= nivelHijo.Numero)
        {
            throw new ValidacionNegocioException(CodigosError.RelacionNivelesInvalidos,
                $"El nivel del tipo padre ({nivelPadre.Numero}) debe ser menor que el del tipo hijo "
                + $"({nivelHijo.Numero}).");
        }
    }

    /// <summary>Valida estados existentes, y activos si se agregan o son inicial (RN-11).</summary>
    public static void AsegurarEstadosAsignables(IReadOnlyList<EstadoEnFlujo> composicion,
        IReadOnlyCollection<EstadoIncidencia> estados, FlujoIncidencia? flujoActual)
    {
        ArgumentNullException.ThrowIfNull(composicion);
        ArgumentNullException.ThrowIfNull(estados);
        foreach (var item in composicion)
        {
            var estado = estados.FirstOrDefault(e => e.Id == item.EstadoId)
                ?? throw Invalido($"No existe el estado de incidencia con id {item.EstadoId}.");
            if (estado.Activo)
            {
                continue;
            }

            if (item.EsInicial)
            {
                throw Invalido($"El estado '{estado.Nombre}' está inactivo y no puede ser el inicial.");
            }

            if (flujoActual is null || !flujoActual.Incluye(item.EstadoId))
            {
                throw Invalido($"El estado '{estado.Nombre}' está inactivo y no puede agregarse al flujo.");
            }
        }
    }

    /// <summary>Rechaza quitar estados con incidencias de tipos que usan el flujo (RN-15).</summary>
    public static void AsegurarEstadosQuitadosSinUso(FlujoIncidencia flujoActual,
        IReadOnlyList<EstadoEnFlujo> composicionNueva, IEnumerable<CantidadPorEstado> incidenciasPorEstado)
    {
        ArgumentNullException.ThrowIfNull(flujoActual);
        ArgumentNullException.ThrowIfNull(composicionNueva);
        ArgumentNullException.ThrowIfNull(incidenciasPorEstado);
        var enUso = incidenciasPorEstado
            .Where(c => c.Cantidad > 0 && flujoActual.Incluye(c.EstadoId)
                && !composicionNueva.Any(e => e.EstadoId == c.EstadoId))
            .ToList();
        if (enUso.Count > 0)
        {
            throw new ConflictoException(CodigosError.FlujoEstadoEnUso,
                $"No se pueden quitar del flujo '{flujoActual.Nombre}' estados con incidencias: {Detalle(enUso)}.");
        }
    }

    /// <summary>Un tipo cambia de flujo solo si sus incidencias están en estados del nuevo (RN-12).</summary>
    public static void AsegurarFlujoCompatible(FlujoIncidencia flujoNuevo,
        IEnumerable<CantidadPorEstado> incidenciasDelTipoPorEstado)
    {
        ArgumentNullException.ThrowIfNull(flujoNuevo);
        ArgumentNullException.ThrowIfNull(incidenciasDelTipoPorEstado);
        var fuera = incidenciasDelTipoPorEstado
            .Where(c => c.Cantidad > 0 && !flujoNuevo.Incluye(c.EstadoId))
            .ToList();
        if (fuera.Count > 0)
        {
            throw new ConflictoException(CodigosError.TipoFlujoIncompatible,
                $"El flujo '{flujoNuevo.Nombre}' no incluye estados con incidencias del tipo: {Detalle(fuera)}.");
        }
    }

    private static string Detalle(IEnumerable<CantidadPorEstado> cantidades) =>
        string.Join(", ", cantidades.Select(c => $"{c.EstadoNombre} ({c.Cantidad})"));

    private static ValidacionNegocioException Invalido(string mensaje) =>
        new(CodigosError.EstadoIncidenciaInvalido, mensaje);
}
