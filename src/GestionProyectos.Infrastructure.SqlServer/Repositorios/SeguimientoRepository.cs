using GestionProyectos.Application.Abstracciones.Persistencia;
using GestionProyectos.Application.Comun;
using GestionProyectos.Application.Seguimiento;
using GestionProyectos.Domain.Comun;
using GestionProyectos.Domain.Entidades;
using GestionProyectos.Infrastructure.SqlServer.Conexion;

namespace GestionProyectos.Infrastructure.SqlServer.Repositorios;

/// <summary>
/// Repositorio de solo lectura del seguimiento sobre dbo.Incidencias y catálogos (Dapper).
/// </summary>
/// <remarks>
/// Creado: 2026-10-10
/// Modificado: 2026-10-10 - Versión inicial (RF-005)
/// </remarks>
internal sealed class SeguimientoRepository(EjecutorSql sql) : ISeguimientoRepository
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<ProyectoSeguimientoLeido>> ListarProyectosAsync(
        FiltroProyectosSeguimiento filtro, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(filtro);
        var filas = await sql.ConsultarAsync<FilaProyectoSeguimiento>(ConsultasSeguimiento.Proyectos(filtro),
            new { PatronProyecto = SqlUtil.PatronContiene(filtro.Texto) }, ct);
        return filas.Select(f => f.ALeido()).ToList();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ConteoIncidencias>> ContarAsync(FiltroConteoIncidencias filtro,
        AgrupacionConteo agruparPor, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(filtro);
        var parametros = new
        {
            filtro.ProyectoId,
            PatronProyecto = SqlUtil.PatronContiene(filtro.Proyectos?.Texto),
            filtro.Ambito.AgrupacionId,
            filtro.Ambito.SinAgrupacion,
            TipoIds = filtro.TipoIds?.ToArray(),
            FechaReferencia = SqlUtil.Parametro(filtro.FechaReferencia),
        };
        var filas = await sql.ConsultarAsync<FilaConteo>(ConsultasSeguimiento.Conteo(filtro, agruparPor),
            parametros, ct);
        return filas.Select(f => new ConteoIncidencias(f.Grupo, f.EstadoId, f.EsFinal, f.Cantidad,
            f.ConVencimientoAnterior, f.SinResponsable)).ToList();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<EstadoIncidencia>> ListarEstadosAsync(CancellationToken ct)
    {
        var filas = await sql.ConsultarAsync<FilaEstado>(ConsultasSeguimiento.Estados, null, ct);
        return filas.Select(f => EstadoIncidencia.Reconstituir(f.Id, f.Nombre, f.Descripcion, f.Orden, f.Activo,
            SqlUtil.Utc(f.FechaCreacion), SqlUtil.Utc(f.FechaModificacion), f.Categoria)).ToList();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<EstadoEnFlujoSeguimiento>> ListarEstadosDeFlujosAsync(
        IReadOnlyList<int>? tipoIds, CancellationToken ct)
    {
        var filas = await sql.ConsultarAsync<FilaEstadoDeFlujo>(
            ConsultasSeguimiento.EstadosDeFlujos(tipoIds is not null), new { TipoIds = tipoIds?.ToArray() }, ct);
        return filas.Select(f => new EstadoEnFlujoSeguimiento(f.FlujoId, f.EstadoId, f.EsFinal)).ToList();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<IncidenciaTableroLeida>> ListarIncidenciasTableroAsync(
        FiltroTableroIncidencias filtro, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(filtro);
        var parametros = new
        {
            filtro.ProyectoId,
            filtro.Ambito.AgrupacionId,
            filtro.Ambito.SinAgrupacion,
            TipoIds = filtro.TipoIds?.ToArray(),
            filtro.EstadoId,
            Patron = SqlUtil.PatronContiene(filtro.Texto),
            FechaReferencia = SqlUtil.Parametro(filtro.FechaReferencia),
            CategoriaIndicador = filtro.Indicador switch
            {
                IndicadorSeguimiento.Bloqueadas => CategoriasEstadoIncidencia.Bloqueada,
                IndicadorSeguimiento.EnEspera => CategoriasEstadoIncidencia.EnEspera,
                _ => null,
            },
        };
        var filas = await sql.ConsultarAsync<FilaTablero>(ConsultasSeguimiento.Tablero(filtro), parametros, ct);
        return filas.Select(f => f.ALeida()).ToList();
    }

    /// <summary>
    /// Fila del listado de proyectos del tablero general.
    /// </summary>
    /// <remarks>
    /// Creado: 2026-10-10
    /// Modificado: 2026-10-10 - Versión inicial (RF-005)
    /// </remarks>
    private sealed class FilaProyectoSeguimiento
    {
        public int Id { get; init; }
        public string Codigo { get; init; } = string.Empty;
        public string Nombre { get; init; } = string.Empty;
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
        public int CantidadAgrupaciones { get; init; }

        public ProyectoSeguimientoLeido ALeido() => new(
            new ProyectoSeguimientoDto(Id, Codigo, Nombre, FechaArchivado is not null,
                new EstadoReferenciaDto(EstadoId, EstadoNombre, EstadoEsFinal, EstadoActivo, EstadoEsSistema),
                new PersonaReferenciaDto(ResponsableId, ResponsableNombre, ResponsableActivo),
                SqlUtil.Fecha(FechaObjetivo)),
            CantidadAgrupaciones);
    }

    /// <summary>
    /// Conteo crudo por (grupo, estado, EsFinal).
    /// </summary>
    /// <remarks>
    /// Creado: 2026-10-10
    /// Modificado: 2026-10-10 - Versión inicial (RF-005)
    /// </remarks>
    private sealed class FilaConteo
    {
        public int? Grupo { get; init; }
        public int EstadoId { get; init; }
        public bool EsFinal { get; init; }
        public int Cantidad { get; init; }
        public int ConVencimientoAnterior { get; init; }
        public int SinResponsable { get; init; }
    }

    /// <summary>
    /// Fila de dbo.EstadosIncidencia.
    /// </summary>
    /// <remarks>
    /// Creado: 2026-10-10
    /// Modificado: 2026-10-10 - Versión inicial (RF-005)
    /// </remarks>
    private sealed class FilaEstado
    {
        public int Id { get; init; }
        public string Nombre { get; init; } = string.Empty;
        public string? Descripcion { get; init; }
        public int Orden { get; init; }
        public bool Activo { get; init; }
        public string? Categoria { get; init; }
        public DateTime FechaCreacion { get; init; }
        public DateTime? FechaModificacion { get; init; }
    }

    /// <summary>
    /// Estado de un flujo con su marca final.
    /// </summary>
    /// <remarks>
    /// Creado: 2026-10-10
    /// Modificado: 2026-10-10 - Versión inicial (RF-005)
    /// </remarks>
    private sealed class FilaEstadoDeFlujo
    {
        public int FlujoId { get; init; }
        public int EstadoId { get; init; }
        public bool EsFinal { get; init; }
    }

    /// <summary>
    /// Fila plana de la grilla del tablero.
    /// </summary>
    /// <remarks>
    /// Creado: 2026-10-10
    /// Modificado: 2026-10-10 - Versión inicial (RF-005)
    /// </remarks>
    private sealed class FilaTablero
    {
        public int Id { get; init; }
        public string Codigo { get; init; } = string.Empty;
        public int Numero { get; init; }
        public string Titulo { get; init; } = string.Empty;
        public decimal? Estimacion { get; init; }
        public DateTime? FechaVencimiento { get; init; }
        public int TipoId { get; init; }
        public string TipoNombre { get; init; } = string.Empty;
        public bool TipoActivo { get; init; }
        public int TipoNivelNumero { get; init; }
        public int EstadoId { get; init; }
        public string EstadoNombre { get; init; } = string.Empty;
        public int EstadoOrden { get; init; }
        public bool EstadoActivo { get; init; }
        public bool EstadoEsFinal { get; init; }
        public string? EstadoCategoria { get; init; }
        public int? ResponsableId { get; init; }
        public string? ResponsableNombre { get; init; }
        public bool? ResponsableActivo { get; init; }

        public IncidenciaTableroLeida ALeida() => new(Id, Codigo, Numero, Titulo,
            new TipoReferenciaDto(TipoId, TipoNombre, TipoActivo, TipoNivelNumero),
            new EstadoTableroDto(EstadoId, EstadoNombre, EstadoOrden, EstadoActivo, EstadoEsFinal, EstadoCategoria),
            SqlUtil.SinCerosFinales(Estimacion), SqlUtil.Fecha(FechaVencimiento),
            ResponsableId is { } id
                ? new PersonaReferenciaDto(id, ResponsableNombre ?? string.Empty, ResponsableActivo ?? false)
                : null);
    }
}
