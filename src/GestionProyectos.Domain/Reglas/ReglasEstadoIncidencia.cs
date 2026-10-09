using GestionProyectos.Domain.Entidades;
using GestionProyectos.Domain.Excepciones;

namespace GestionProyectos.Domain.Reglas;

/// <summary>
/// Reglas puras del estado de una incidencia dentro del flujo de su tipo (RN-11, RN-23, RN-24).
/// </summary>
/// <remarks>
/// Creado: 2026-10-09
/// Modificado: 2026-10-09 - Versión inicial (RF-002)
/// </remarks>
public static class ReglasEstadoIncidencia
{
    /// <summary>Exige un estado del flujo y activo, salvo que sea el actual (RN-11, RN-23).</summary>
    public static void AsegurarEstadoAsignable(int estadoId, int estadoActualId, FlujoIncidencia flujo,
        EstadoIncidencia? estado)
    {
        ArgumentNullException.ThrowIfNull(flujo);
        if (estado is null || estado.Id != estadoId)
        {
            throw Invalido($"No existe el estado de incidencia con id {estadoId}.");
        }

        if (!flujo.Incluye(estadoId))
        {
            throw Invalido($"El estado '{estado.Nombre}' no pertenece al flujo '{flujo.Nombre}'.");
        }

        if (!estado.Activo && estadoId != estadoActualId)
        {
            throw Invalido($"El estado '{estado.Nombre}' está inactivo.");
        }
    }

    /// <summary>Resuelve el estado al cambiar de tipo: elegido, actual o inicial del flujo (RN-23).</summary>
    public static int ResolverEstadoAlCambiarTipo(int estadoActualId, int? estadoSolicitadoId,
        FlujoIncidencia flujoNuevo, EstadoIncidencia? estadoSolicitado)
    {
        ArgumentNullException.ThrowIfNull(flujoNuevo);
        if (estadoSolicitadoId is not { } solicitado)
        {
            return flujoNuevo.Incluye(estadoActualId) ? estadoActualId : flujoNuevo.EstadoInicialId;
        }

        AsegurarEstadoAsignable(solicitado, estadoActualId, flujoNuevo, estadoSolicitado);
        return solicitado;
    }

    private static ValidacionNegocioException Invalido(string mensaje) =>
        new(CodigosError.EstadoIncidenciaInvalido, mensaje);
}
