using GestionProyectos.Application.Seguimiento;
using GestionProyectos.Domain.Entidades;

namespace GestionProyectos.Application.Abstracciones.Persistencia;

/// <summary>
/// Consultas de solo lectura del seguimiento: proyectos, conteos crudos y grilla (RF-005 0.2).
/// </summary>
/// <remarks>
/// Creado: 2026-10-10
/// Modificado: 2026-10-10 - Versión inicial (RF-005)
/// </remarks>
public interface ISeguimientoRepository
{
    /// <summary>Todos los proyectos que cumplen el filtro (sin paginar, DA-11).</summary>
    Task<IReadOnlyList<ProyectoSeguimientoLeido>> ListarProyectosAsync(FiltroProyectosSeguimiento filtro,
        CancellationToken ct);

    /// <summary>Conteos crudos por (grupo, estado, EsFinal) según el filtro (0.2).</summary>
    Task<IReadOnlyList<ConteoIncidencias>> ContarAsync(FiltroConteoIncidencias filtro, AgrupacionConteo agruparPor,
        CancellationToken ct);

    /// <summary>Catálogo completo de estados de incidencia (activos e inactivos).</summary>
    Task<IReadOnlyList<EstadoIncidencia>> ListarEstadosAsync(CancellationToken ct);

    /// <summary>Estados de los flujos que usan los tipos indicados (null = todos los tipos).</summary>
    Task<IReadOnlyList<EstadoEnFlujoSeguimiento>> ListarEstadosDeFlujosAsync(IReadOnlyList<int>? tipoIds,
        CancellationToken ct);

    /// <summary>Incidencias del ámbito, alcance y filtros de la grilla del tablero (RN-20, RN-21).</summary>
    Task<IReadOnlyList<IncidenciaTableroLeida>> ListarIncidenciasTableroAsync(FiltroTableroIncidencias filtro,
        CancellationToken ct);
}
