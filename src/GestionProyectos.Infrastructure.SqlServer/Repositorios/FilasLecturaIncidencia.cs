using GestionProyectos.Application.Comun;
using GestionProyectos.Application.Incidencias;
using GestionProyectos.Infrastructure.SqlServer.Conexion;

namespace GestionProyectos.Infrastructure.SqlServer.Repositorios;

/// <summary>
/// Fila plana de una incidencia con sus referencias (ConsultasIncidencia).
/// </summary>
/// <remarks>
/// Creado: 2026-10-09
/// Modificado: 2026-10-09 - Versión inicial (RF-002)
/// </remarks>
internal sealed class FilaIncidencia
{
    public int Id { get; init; }
    public int ProyectoId { get; init; }
    public int Numero { get; init; }
    public string Codigo { get; init; } = string.Empty;
    public string Titulo { get; init; } = string.Empty;
    public string Descripcion { get; init; } = string.Empty;
    public int? PadreId { get; init; }
    public string? PadreCodigo { get; init; }
    public DateTime? FechaVencimiento { get; init; }
    public DateTime FechaCreacion { get; init; }
    public DateTime? FechaModificacion { get; init; }
    public int TipoId { get; init; }
    public string TipoNombre { get; init; } = string.Empty;
    public bool TipoActivo { get; init; }
    public int TipoNivelNumero { get; init; }
    public int EstadoId { get; init; }
    public string EstadoNombre { get; init; } = string.Empty;
    public bool EstadoActivo { get; init; }
    public bool EstadoEsFinal { get; init; }
    public int PrioridadId { get; init; }
    public string PrioridadNombre { get; init; } = string.Empty;
    public int PrioridadOrden { get; init; }
    public bool PrioridadActivo { get; init; }
    public int? ResponsableId { get; init; }
    public string? ResponsableNombre { get; init; }
    public bool? ResponsableActivo { get; init; }
    public int? AgrupacionId { get; init; }
    public string? AgrupacionNombre { get; init; }
    public int CantidadHijas { get; init; }
    public string ProyectoCodigo { get; init; } = string.Empty;
    public string ProyectoNombre { get; init; } = string.Empty;
    public DateTime? ProyectoFechaArchivado { get; init; }

    /// <summary>Convierte la fila en la fila del listado.</summary>
    public IncidenciaResumenDto AResumen() => new(Id, Codigo, Numero, Titulo, Tipo(), Estado(), Prioridad(),
        Responsable(), SqlUtil.Fecha(FechaVencimiento), Agrupacion(),
        PadreId is { } padreId ? new PadreResumenDto(padreId, PadreCodigo ?? string.Empty) : null,
        CantidadHijas, SqlUtil.Utc(FechaCreacion));

    /// <summary>Convierte la fila en un nodo del árbol.</summary>
    public NodoArbolIncidenciaDto ANodo() => new(Id, Codigo, Titulo, PadreId, Tipo(), Estado(), Prioridad(),
        Responsable(), SqlUtil.Fecha(FechaVencimiento), Agrupacion());

    /// <summary>Convierte la fila en una referencia (padre o hija).</summary>
    public IncidenciaReferenciaDto AReferencia() => new(Id, Codigo, Titulo, Tipo(), Estado(), Responsable(),
        SqlUtil.Fecha(FechaVencimiento));

    /// <summary>Convierte la fila de detalle en el DTO completo.</summary>
    public IncidenciaDto ADetalle(IncidenciaReferenciaDto? padre, IReadOnlyList<IncidenciaReferenciaDto> hijas,
        IReadOnlyList<TipoHijaPermitidoDto> tiposHijaPermitidos) => new(
        Id, Codigo, Numero,
        new ProyectoReferenciaIncidenciaDto(ProyectoId, ProyectoCodigo, ProyectoNombre, ProyectoFechaArchivado is not null),
        Titulo, Descripcion, Tipo(), Estado(), Prioridad(), Responsable(), SqlUtil.Fecha(FechaVencimiento),
        Agrupacion(), PadreId is not null, padre, hijas, tiposHijaPermitidos,
        SqlUtil.Utc(FechaCreacion), SqlUtil.Utc(FechaModificacion));

    private TipoReferenciaDto Tipo() => new(TipoId, TipoNombre, TipoActivo, TipoNivelNumero);

    private EstadoIncidenciaReferenciaDto Estado() => new(EstadoId, EstadoNombre, EstadoActivo, EstadoEsFinal);

    private PrioridadReferenciaDto Prioridad() => new(PrioridadId, PrioridadNombre, PrioridadOrden, PrioridadActivo);

    private PersonaReferenciaDto? Responsable() => ResponsableId is { } id
        ? new PersonaReferenciaDto(id, ResponsableNombre ?? string.Empty, ResponsableActivo ?? false)
        : null;

    private AgrupacionReferenciaDto? Agrupacion() => AgrupacionId is { } id
        ? new AgrupacionReferenciaDto(id, AgrupacionNombre ?? string.Empty)
        : null;
}
