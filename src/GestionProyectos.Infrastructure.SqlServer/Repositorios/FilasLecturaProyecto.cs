using GestionProyectos.Application.Comun;
using GestionProyectos.Application.Proyectos;
using GestionProyectos.Infrastructure.SqlServer.Conexion;

namespace GestionProyectos.Infrastructure.SqlServer.Repositorios;

/// <summary>
/// Fila plana del listado de proyectos (ConsultasProyecto.Listado).
/// </summary>
/// <remarks>
/// Creado: 2026-10-09
/// Modificado: 2026-10-09 - Versión inicial (RF-001)
/// </remarks>
internal sealed class FilaResumenProyecto
{
    public int Id { get; init; }
    public string Codigo { get; init; } = string.Empty;
    public string Nombre { get; init; } = string.Empty;
    public DateTime FechaInicio { get; init; }
    public DateTime FechaObjetivo { get; init; }
    public DateTime? FechaArchivado { get; init; }
    public int ResponsableId { get; init; }
    public string ResponsableNombre { get; init; } = string.Empty;
    public bool ResponsableActivo { get; init; }
    public int EstadoId { get; init; }
    public string EstadoNombre { get; init; } = string.Empty;
    public bool EstadoEsFinal { get; init; }
    public bool EstadoActivo { get; init; }
    public bool EstadoEsSistema { get; init; }

    /// <summary>Convierte la fila en el DTO del listado.</summary>
    public ProyectoResumenDto ADto() => new(
        Id, Codigo, Nombre,
        new PersonaReferenciaDto(ResponsableId, ResponsableNombre, ResponsableActivo),
        SqlUtil.Fecha(FechaInicio), SqlUtil.Fecha(FechaObjetivo),
        new EstadoReferenciaDto(EstadoId, EstadoNombre, EstadoEsFinal, EstadoActivo, EstadoEsSistema),
        FechaArchivado is not null);
}

/// <summary>
/// Fila plana del detalle de proyecto (ConsultasProyecto.Detalle).
/// </summary>
/// <remarks>
/// Creado: 2026-10-09
/// Modificado: 2026-10-09 - Versión inicial (RF-001)
/// Modificado: 2026-10-09 - Agrega CantidadIncidencias (RF-002)
/// </remarks>
internal sealed class FilaDetalleProyecto
{
    public int Id { get; init; }
    public string Codigo { get; init; } = string.Empty;
    public string Nombre { get; init; } = string.Empty;
    public string Descripcion { get; init; } = string.Empty;
    public DateTime FechaInicio { get; init; }
    public DateTime FechaObjetivo { get; init; }
    public DateTime? FechaArchivado { get; init; }
    public DateTime FechaCreacion { get; init; }
    public DateTime? FechaModificacion { get; init; }
    public int ResponsableId { get; init; }
    public string ResponsableNombre { get; init; } = string.Empty;
    public bool ResponsableActivo { get; init; }
    public int EstadoId { get; init; }
    public string EstadoNombre { get; init; } = string.Empty;
    public bool EstadoEsFinal { get; init; }
    public bool EstadoActivo { get; init; }
    public bool EstadoEsSistema { get; init; }
    public int? ArchivadoPorId { get; init; }
    public string? ArchivadoPorNombre { get; init; }
    public bool? ArchivadoPorActivo { get; init; }
    public int? EstadoAlArchivarId { get; init; }
    public string? EstadoAlArchivarNombre { get; init; }
    public bool? EstadoAlArchivarEsFinal { get; init; }
    public bool? EstadoAlArchivarActivo { get; init; }
    public bool? EstadoAlArchivarEsSistema { get; init; }
    public int CantidadAgrupaciones { get; init; }
    public int CantidadIncidencias { get; init; }

    /// <summary>Convierte la fila en el DTO de detalle.</summary>
    public ProyectoDto ADto() => new(
        Id, Codigo, Nombre, Descripcion,
        new PersonaReferenciaDto(ResponsableId, ResponsableNombre, ResponsableActivo),
        SqlUtil.Fecha(FechaInicio), SqlUtil.Fecha(FechaObjetivo),
        new EstadoReferenciaDto(EstadoId, EstadoNombre, EstadoEsFinal, EstadoActivo, EstadoEsSistema),
        FechaArchivado is not null,
        SqlUtil.Utc(FechaArchivado),
        ArchivadoPor(),
        EstadoAlArchivar(),
        CantidadAgrupaciones,
        SqlUtil.Utc(FechaCreacion),
        SqlUtil.Utc(FechaModificacion),
        CantidadIncidencias);

    private PersonaReferenciaDto? ArchivadoPor() => ArchivadoPorId is { } id
        ? new PersonaReferenciaDto(id, ArchivadoPorNombre ?? string.Empty, ArchivadoPorActivo ?? false)
        : null;

    private EstadoReferenciaDto? EstadoAlArchivar() => EstadoAlArchivarId is { } id
        ? new EstadoReferenciaDto(id, EstadoAlArchivarNombre ?? string.Empty,
            EstadoAlArchivarEsFinal ?? false, EstadoAlArchivarActivo ?? false,
            EstadoAlArchivarEsSistema ?? false)
        : null;
}
