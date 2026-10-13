using GestionProyectos.Application.Comun;
using GestionProyectos.Application.NivelesIncidencia;
using GestionProyectos.Application.Seguimiento;
using GestionProyectos.Application.TiposIncidencia;
using GestionProyectos.Domain.Comun;
using GestionProyectos.Domain.Entidades;

namespace GestionProyectos.Application.Tests.Comun;

/// <summary>
/// Catálogo semilla y constructores de filas/DTOs para los tests de seguimiento (RF-005).
/// Niveles 1–3; tipos Épica (N1), Historia de Usuario (N2), Subtarea, Bug y Viejo inactivo (N3);
/// estados Por hacer, En progreso, Bloqueada (BLOQUEADA), Esperando (EN_ESPERA), Hecho (final).
/// </summary>
/// <remarks>
/// Creado: 2026-10-11
/// Modificado: 2026-10-11 - Versión inicial (tests RF-005)
/// </remarks>
internal static class DatosSeguimiento
{
    public const int Nivel1 = 1;
    public const int Nivel2 = 2;
    public const int Nivel3 = 3;
    public const int Epica = 1;
    public const int Hu = 2;
    public const int Subtarea = 3;
    public const int Bug = 5;
    public const int TipoViejo = 6;
    public const int PorHacer = 1;
    public const int EnProgreso = 2;
    public const int Hecho = 3;
    public const int Bloqueada = 4;
    public const int Esperando = 5;
    public const int Obsoleto = 6;

    public static readonly DateOnly Hoy = new(2026, 10, 10);

    /// <summary>Niveles del catálogo (DTO del repositorio).</summary>
    public static IReadOnlyList<NivelIncidenciaDto> NivelesDto() =>
    [
        NivelDto(Nivel1, 1), NivelDto(Nivel2, 2), NivelDto(Nivel3, 3),
    ];

    /// <summary>Tipos del catálogo (DTO del repositorio), activos e inactivos.</summary>
    public static IReadOnlyList<TipoIncidenciaDto> TiposDto() =>
    [
        TipoDto(Epica, "Épica", Nivel1), TipoDto(Hu, "Historia de Usuario", Nivel2),
        TipoDto(Subtarea, "Subtarea", Nivel3), TipoDto(Bug, "Bug", Nivel3),
        TipoDto(TipoViejo, "Viejo", Nivel3, activo: false),
    ];

    /// <summary>Niveles como referencias para el resolutor de alcance.</summary>
    public static IReadOnlyList<NivelReferenciaDto> Niveles() =>
        NivelesDto().Select(n => new NivelReferenciaDto(n.Id, n.Numero, n.Nombre)).ToList();

    /// <summary>Tipos como entrada del resolutor de alcance.</summary>
    public static IReadOnlyList<TipoCatalogoAlcance> TiposAlcance() =>
        TiposDto().Select(t => new TipoCatalogoAlcance(t.Id, t.Nombre, t.Activo, t.Nivel.Id, t.Nivel.Numero)).ToList();

    /// <summary>Catálogo de estados con categorías (S004 + "Esperando" EN_ESPERA + "Obsoleto" inactivo).</summary>
    public static IReadOnlyList<EstadoIncidencia> Estados() =>
    [
        DatosIncidencias.Estado(PorHacer, "Por hacer", orden: 10),
        DatosIncidencias.Estado(EnProgreso, "En progreso", orden: 20),
        DatosIncidencias.Estado(Esperando, "Esperando cliente", orden: 22, categoria: CategoriasEstadoIncidencia.EnEspera),
        DatosIncidencias.Estado(Bloqueada, "Bloqueada", orden: 24, categoria: CategoriasEstadoIncidencia.Bloqueada),
        DatosIncidencias.Estado(Hecho, "Hecho", orden: 30),
        DatosIncidencias.Estado(Obsoleto, "Obsoleto", activo: false, orden: 15),
    ];

    /// <summary>Estados del Flujo general (1): Por hacer, En progreso, Esperando, Bloqueada y Hecho (final).</summary>
    public static IReadOnlyList<EstadoEnFlujoSeguimiento> EstadosFlujoGeneral() =>
    [
        new(1, PorHacer, false), new(1, EnProgreso, false), new(1, Esperando, false), new(1, Bloqueada, false),
        new(1, Hecho, true),
    ];

    /// <summary>Calculadora con el catálogo y el flujo general.</summary>
    public static CalculadoraIndicadores Calculadora() => new(Estados(), EstadosFlujoGeneral());

    /// <summary>Fila de conteo crudo.</summary>
    public static ConteoIncidencias Conteo(int estadoId, int cantidad, bool esFinal = false, int vencidas = 0,
        int sinResponsable = 0, int? grupo = null) =>
        new(grupo, estadoId, esFinal, cantidad, vencidas, sinResponsable);

    /// <summary>Indicadores con los valores indicados y porEstado vacío.</summary>
    public static IndicadoresDto Indicadores(int total = 0, int abiertas = 0, int vencidas = 0, int bloqueadas = 0,
        int enEspera = 0, int sinResponsable = 0) =>
        new(total, abiertas, total - abiertas, vencidas, bloqueadas, enEspera, sinResponsable, []);

    /// <summary>Proyecto de una fila del tablero general.</summary>
    public static ProyectoSeguimientoDto ProyectoSeguimiento(int id, string? codigo = null, string? nombre = null) =>
        new(id, codigo ?? $"PRJ-{id:D4}", nombre ?? $"Proyecto {id}", false,
            new EstadoReferenciaDto(3, "En curso", false, true, false), new PersonaReferenciaDto(2, "Ana Gómez", true),
            new DateOnly(2026, 12, 15));

    /// <summary>Fila del tablero general.</summary>
    public static FilaTableroProyectosDto Fila(int id, IndicadoresDto? indicadores = null, string? codigo = null,
        string? nombre = null) =>
        new(ProyectoSeguimiento(id, codigo, nombre), 0, indicadores ?? Indicadores());

    /// <summary>Tipo de referencia de un ítem del tablero.</summary>
    public static TipoReferenciaDto TipoRef(int tipoId) =>
        TiposDto().Where(t => t.Id == tipoId)
            .Select(t => new TipoReferenciaDto(t.Id, t.Nombre, t.Activo, t.Nivel.Numero)).Single();

    /// <summary>Estado de un ítem del tablero.</summary>
    public static EstadoTableroDto EstadoTablero(int estadoId, bool esFinal = false) =>
        Estados().Where(e => e.Id == estadoId)
            .Select(e => new EstadoTableroDto(e.Id, e.Nombre, e.Orden, e.Activo, esFinal, e.Categoria)).Single();

    /// <summary>Ítem del tablero (DTO) para el orden por defecto.</summary>
    public static ItemTableroDto Item(int numero, int tipoId, int? id = null) =>
        new(id ?? numero, $"PRJ-0001-{numero}", numero, $"Incidencia {numero}", TipoRef(tipoId),
            EstadoTablero(PorHacer), null, false, null);

    /// <summary>Incidencia leída por el repositorio para la grilla del tablero.</summary>
    public static IncidenciaTableroLeida Leida(int numero, int tipoId, DateOnly? vencimiento = null,
        bool esFinal = false, decimal? estimacion = null, int estadoId = PorHacer) =>
        new(numero, $"PRJ-0001-{numero}", numero, $"Incidencia {numero}", TipoRef(tipoId),
            EstadoTablero(estadoId, esFinal), estimacion, vencimiento, null);

    private static NivelIncidenciaDto NivelDto(int id, int numero) =>
        new(id, numero, $"Nivel {numero}", null, 1, DatosPrueba.FechaAnterior, null);

    private static TipoIncidenciaDto TipoDto(int id, string nombre, int nivel, bool activo = true) =>
        new(id, nombre, null, new NivelReferenciaDto(nivel, nivel, $"Nivel {nivel}"),
            new FlujoReferenciaDto(1, "Flujo general"), activo, 0, DatosPrueba.FechaAnterior, null);
}
