using GestionProyectos.Application.Comun;
using GestionProyectos.Application.Incidencias;

namespace GestionProyectos.Application.Seguimiento;

/// <summary>
/// Niveles y tipos efectivamente contabilizados en una respuesta de seguimiento (RN-06).
/// </summary>
/// <remarks>
/// Creado: 2026-10-10
/// Modificado: 2026-10-10 - Versión inicial (RF-005)
/// </remarks>
public sealed record AlcanceSeguimientoDto(
    bool TodosLosNiveles,
    IReadOnlyList<NivelReferenciaDto> Niveles,
    bool TodosLosTipos,
    IReadOnlyList<TipoReferenciaDto> Tipos);

/// <summary>
/// Estado en el conteo por estado; EsFinal = final en todos los flujos contabilizados (RN-15).
/// </summary>
/// <remarks>
/// Creado: 2026-10-10
/// Modificado: 2026-10-10 - Versión inicial (RF-005)
/// </remarks>
public sealed record EstadoSeguimientoDto(int Id, string Nombre, int Orden, bool Activo, string? Categoria,
    bool EsFinal);

/// <summary>
/// Cantidad de incidencias en un estado (fila de porEstado, RN-15).
/// </summary>
/// <remarks>
/// Creado: 2026-10-10
/// Modificado: 2026-10-10 - Versión inicial (RF-005)
/// </remarks>
public sealed record ConteoEstadoDto(EstadoSeguimientoDto Estado, int Cantidad);

/// <summary>
/// Indicadores de un ámbito: totales, alertas y conteo por estado (RN-09 a RN-16).
/// </summary>
/// <remarks>
/// Creado: 2026-10-10
/// Modificado: 2026-10-10 - Versión inicial (RF-005)
/// </remarks>
public sealed record IndicadoresDto(
    int Total,
    int Abiertas,
    int Finalizadas,
    int Vencidas,
    int Bloqueadas,
    int EnEspera,
    int SinResponsable,
    IReadOnlyList<ConteoEstadoDto> PorEstado);

/// <summary>
/// Proyecto de una fila del tablero general.
/// </summary>
/// <remarks>
/// Creado: 2026-10-10
/// Modificado: 2026-10-10 - Versión inicial (RF-005)
/// </remarks>
public sealed record ProyectoSeguimientoDto(
    int Id,
    string Codigo,
    string Nombre,
    bool Archivado,
    EstadoReferenciaDto Estado,
    PersonaReferenciaDto Responsable,
    DateOnly FechaObjetivo);

/// <summary>
/// Fila del tablero general: proyecto, cantidad de agrupaciones e indicadores (RN-02).
/// </summary>
/// <remarks>
/// Creado: 2026-10-10
/// Modificado: 2026-10-10 - Versión inicial (RF-005)
/// </remarks>
public sealed record FilaTableroProyectosDto(
    ProyectoSeguimientoDto Proyecto,
    int CantidadAgrupaciones,
    IndicadoresDto Indicadores);

/// <summary>
/// Tablero general: totales de todos los proyectos filtrados y página de proyectos (RN-27).
/// </summary>
/// <remarks>
/// Creado: 2026-10-10
/// Modificado: 2026-10-10 - Versión inicial (RF-005)
/// </remarks>
public sealed record TableroProyectosDto(
    DateOnly FechaReferencia,
    AlcanceSeguimientoDto Alcance,
    IndicadoresDto Totales,
    Paginado<FilaTableroProyectosDto> Proyectos);

/// <summary>
/// Agrupación del ámbito "agrupacion" con su descripción (DA-08).
/// </summary>
/// <remarks>
/// Creado: 2026-10-10
/// Modificado: 2026-10-10 - Versión inicial (RF-005)
/// </remarks>
public sealed record AgrupacionSeguimientoDto(int Id, string Nombre, string Descripcion);

/// <summary>
/// Fila de la tabla por agrupación; Agrupacion null = "Sin agrupación" (RN-26).
/// </summary>
/// <remarks>
/// Creado: 2026-10-10
/// Modificado: 2026-10-10 - Versión inicial (RF-005)
/// </remarks>
public sealed record FilaAgrupacionSeguimientoDto(AgrupacionReferenciaDto? Agrupacion, IndicadoresDto Indicadores);

/// <summary>
/// Resumen de seguimiento de un proyecto, una agrupación o las incidencias sin agrupación.
/// </summary>
/// <remarks>
/// Creado: 2026-10-10
/// Modificado: 2026-10-10 - Versión inicial (RF-005)
/// </remarks>
public sealed record SeguimientoDto(
    DateOnly FechaReferencia,
    ProyectoReferenciaIncidenciaDto Proyecto,
    string Ambito,
    AgrupacionSeguimientoDto? Agrupacion,
    AlcanceSeguimientoDto Alcance,
    IndicadoresDto Indicadores,
    IReadOnlyList<FilaAgrupacionSeguimientoDto> Agrupaciones);

/// <summary>
/// Estado de un ítem del tablero; Orden para ordenar en el cliente, EsFinal según el flujo del tipo.
/// </summary>
/// <remarks>
/// Creado: 2026-10-10
/// Modificado: 2026-10-10 - Versión inicial (RF-005)
/// </remarks>
public sealed record EstadoTableroDto(int Id, string Nombre, int Orden, bool Activo, bool EsFinal, string? Categoria);

/// <summary>
/// Fila de la grilla del tablero con los datos de las seis columnas (RN-29).
/// </summary>
/// <remarks>
/// Creado: 2026-10-10
/// Modificado: 2026-10-10 - Versión inicial (RF-005)
/// </remarks>
public sealed record ItemTableroDto(
    int Id,
    string Codigo,
    int Numero,
    string Titulo,
    TipoReferenciaDto Tipo,
    EstadoTableroDto Estado,
    decimal? Estimacion,
    bool Vencida,
    PersonaReferenciaDto? Responsable);

/// <summary>
/// Estado pedido en el filtro estadoId (eco para chips).
/// </summary>
/// <remarks>
/// Creado: 2026-10-10
/// Modificado: 2026-10-10 - Versión inicial (RF-005)
/// </remarks>
public sealed record EstadoFiltroTableroDto(int Id, string Nombre, bool Activo, string? Categoria);

/// <summary>
/// Eco de los filtros aplicados en el tablero (RN-21).
/// </summary>
/// <remarks>
/// Creado: 2026-10-10
/// Modificado: 2026-10-10 - Versión inicial (RF-005)
/// </remarks>
public sealed record FiltrosTableroDto(string? Indicador, EstadoFiltroTableroDto? Estado, string? Texto);

/// <summary>
/// Grilla de incidencias del ámbito, alcance y filtros, con la configuración de columnas (RN-20).
/// </summary>
/// <remarks>
/// Creado: 2026-10-10
/// Modificado: 2026-10-10 - Versión inicial (RF-005)
/// </remarks>
public sealed record TableroIncidenciasDto(
    DateOnly FechaReferencia,
    ProyectoReferenciaIncidenciaDto Proyecto,
    AlcanceSeguimientoDto Alcance,
    FiltrosTableroDto Filtros,
    ConfiguracionTableroDto Configuracion,
    int Total,
    IReadOnlyList<ItemTableroDto> Items);
