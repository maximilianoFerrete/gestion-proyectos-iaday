using GestionProyectos.Application.Comun;
using GestionProyectos.Application.Incidencias;
using GestionProyectos.Domain.Entidades;
using GestionProyectos.Domain.Reglas;

namespace GestionProyectos.Application.Abstracciones.Persistencia;

/// <summary>
/// Persistencia, bloqueo por proyecto y consultas jerárquicas de incidencias (spec 0.3, 0.4).
/// </summary>
/// <remarks>
/// Creado: 2026-10-09
/// Modificado: 2026-10-09 - Versión inicial (RF-002)
/// Modificado: 2026-10-10 - Árbol filtrado por ámbito (RF-005, C-RF002-04)
/// </remarks>
public interface IIncidenciaRepository
{
    /// <summary>Bloquea la fila del proyecto hasta el fin de la transacción; null si no existe.</summary>
    Task<ProyectoBloqueado?> BloquearProyectoAsync(int proyectoId, CancellationToken ct);

    /// <summary>Incrementa el contador del proyecto (lo bloquea); null si el proyecto no existe.</summary>
    Task<NumeroIncidenciaAsignado?> ObtenerSiguienteNumeroAsync(int proyectoId, CancellationToken ct);

    /// <summary>Obtiene la incidencia por id o null si no existe.</summary>
    Task<Incidencia?> ObtenerPorIdAsync(int id, CancellationToken ct);

    /// <summary>Obtiene el detalle de la incidencia (padre, hijas y tipos permitidos) o null.</summary>
    Task<IncidenciaDto?> ObtenerDetalleAsync(int id, CancellationToken ct);

    /// <summary>Lista incidencias del proyecto con filtros, orden y paginación.</summary>
    Task<Paginado<IncidenciaResumenDto>> ListarAsync(FiltroIncidencias filtro, CancellationToken ct);

    /// <summary>Lista plana de todas las incidencias del proyecto ordenadas por número.</summary>
    Task<IReadOnlyList<NodoArbolIncidenciaDto>> ListarArbolAsync(int proyectoId, CancellationToken ct);

    /// <summary>Lista plana del ámbito (agrupación o sin agrupación) ordenada por número.</summary>
    Task<IReadOnlyList<NodoArbolIncidenciaDto>> ListarArbolAsync(int proyectoId, int? agrupacionId,
        bool sinAgrupacion, CancellationToken ct);

    /// <summary>Ids de los ancestros de la incidencia (padre, abuelo, ...), sin incluirla.</summary>
    Task<IReadOnlyList<int>> ObtenerIdsAncestrosAsync(int id, CancellationToken ct);

    /// <summary>Hijas directas de la incidencia con tipo y vencimiento (RN-25, RN-32).</summary>
    Task<IReadOnlyList<HijaIncidencia>> ListarHijasAsync(int id, CancellationToken ct);

    /// <summary>Inserta la incidencia y le asigna el id generado.</summary>
    Task AgregarAsync(Incidencia incidencia, CancellationToken ct);

    /// <summary>Guarda los campos editables de la incidencia.</summary>
    Task ActualizarAsync(Incidencia incidencia, CancellationToken ct);

    /// <summary>Asigna la agrupación a todos los descendientes de la incidencia (RN-29).</summary>
    Task<int> ActualizarAgrupacionDescendientesAsync(int id, int? agrupacionId, DateTime ahora,
        CancellationToken ct);

    /// <summary>Quita la agrupación a las incidencias del proyecto que la tienen (RN-30).</summary>
    Task<int> DesasociarAgrupacionAsync(int proyectoId, int agrupacionId, DateTime ahora, CancellationToken ct);
}
