using GestionProyectos.Application.Abstracciones.Persistencia;
using GestionProyectos.Application.Comun;
using GestionProyectos.Application.Incidencias;
using GestionProyectos.Domain.Entidades;
using GestionProyectos.Domain.Reglas;
using GestionProyectos.Infrastructure.SqlServer.Conexion;

namespace GestionProyectos.Infrastructure.SqlServer.Repositorios;

/// <summary>
/// Repositorio de incidencias sobre dbo.Incidencias: contador y bloqueo por proyecto, CTE recursivas.
/// </summary>
/// <remarks>
/// Creado: 2026-10-09
/// Modificado: 2026-10-09 - Versión inicial (RF-002)
/// </remarks>
internal sealed class IncidenciaRepository(EjecutorSql sql) : IIncidenciaRepository
{
    private const string Columnas = """
        Id, ProyectoId, Numero, Codigo, Titulo, Descripcion, TipoId, EstadoId, PrioridadId, ResponsableId,
        FechaVencimiento, AgrupacionId, PadreId, FechaCreacion, FechaModificacion
        """;

    /// <inheritdoc />
    public async Task<ProyectoBloqueado?> BloquearProyectoAsync(int proyectoId, CancellationToken ct)
    {
        var fila = await sql.ConsultarUnoAsync<FilaProyectoBloqueado>(
            "SELECT Id, Codigo, FechaArchivado FROM dbo.Proyectos WITH (UPDLOCK, ROWLOCK) WHERE Id = @ProyectoId;",
            new { ProyectoId = proyectoId }, ct);
        return fila?.ABloqueado();
    }

    /// <inheritdoc />
    public async Task<NumeroIncidenciaAsignado?> ObtenerSiguienteNumeroAsync(int proyectoId, CancellationToken ct)
    {
        const string comando = """
            UPDATE dbo.Proyectos
            SET UltimoNumeroIncidencia = UltimoNumeroIncidencia + 1
            OUTPUT INSERTED.Id, INSERTED.Codigo, INSERTED.FechaArchivado, INSERTED.UltimoNumeroIncidencia AS Numero
            WHERE Id = @ProyectoId;
            """;
        var fila = await sql.ConsultarUnoAsync<FilaProyectoBloqueado>(comando, new { ProyectoId = proyectoId }, ct);
        return fila is null ? null : new NumeroIncidenciaAsignado(fila.Numero, fila.ABloqueado());
    }

    /// <inheritdoc />
    public async Task<Incidencia?> ObtenerPorIdAsync(int id, CancellationToken ct)
    {
        var fila = await sql.ConsultarUnoAsync<FilaEntidad>(
            $"SELECT {Columnas} FROM dbo.Incidencias WHERE Id = @Id;", new { Id = id }, ct);
        return fila?.AEntidad();
    }

    /// <inheritdoc />
    public async Task<IncidenciaDto?> ObtenerDetalleAsync(int id, CancellationToken ct)
    {
        var parametros = new { Id = id };
        var fila = await sql.ConsultarUnoAsync<FilaIncidencia>(ConsultasIncidencia.Detalle, parametros, ct);
        if (fila is null)
        {
            return null;
        }

        var padre = await sql.ConsultarUnoAsync<FilaIncidencia>(ConsultasIncidencia.Padre, parametros, ct);
        var hijas = await sql.ConsultarAsync<FilaIncidencia>(ConsultasIncidencia.Hijas, parametros, ct);
        var tipos = await sql.ConsultarAsync<FilaTipoHija>(ConsultasIncidencia.TiposHijaPermitidos, parametros, ct);
        return fila.ADetalle(padre?.AReferencia(), hijas.Select(h => h.AReferencia()).ToList(),
            tipos.Select(t => new TipoHijaPermitidoDto(t.Id, t.Nombre)).ToList());
    }

    /// <inheritdoc />
    public async Task<Paginado<IncidenciaResumenDto>> ListarAsync(FiltroIncidencias filtro, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(filtro);
        var parametros = new
        {
            filtro.ProyectoId,
            Patron = SqlUtil.PatronContiene(filtro.Texto),
            TipoIds = filtro.TipoIds.ToArray(),
            filtro.EstadoId,
            filtro.PrioridadId,
            filtro.ResponsableId,
            filtro.SinResponsable,
            filtro.AgrupacionId,
            filtro.SinAgrupacion,
            Desde = (filtro.Pagina - 1) * filtro.TamanioPagina,
            filtro.TamanioPagina,
        };
        var (filas, total) = await sql.ConsultarConTotalAsync<FilaIncidencia>(
            ConsultasIncidencia.Listado(filtro), parametros, ct);
        return new Paginado<IncidenciaResumenDto>(
            filas.Select(f => f.AResumen()).ToList(), filtro.Pagina, filtro.TamanioPagina, total);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<NodoArbolIncidenciaDto>> ListarArbolAsync(int proyectoId, CancellationToken ct)
    {
        var filas = await sql.ConsultarAsync<FilaIncidencia>(ConsultasIncidencia.Arbol,
            new { ProyectoId = proyectoId }, ct);
        return filas.Select(f => f.ANodo()).ToList();
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<int>> ObtenerIdsAncestrosAsync(int id, CancellationToken ct) =>
        sql.ConsultarAsync<int>(ConsultasIncidencia.Ancestros, new { Id = id }, ct);

    /// <inheritdoc />
    public async Task<IReadOnlyList<HijaIncidencia>> ListarHijasAsync(int id, CancellationToken ct)
    {
        var filas = await sql.ConsultarAsync<FilaHija>(
            "SELECT Id, Codigo, TipoId, FechaVencimiento FROM dbo.Incidencias WHERE PadreId = @Id ORDER BY Numero;",
            new { Id = id }, ct);
        return filas.Select(f => new HijaIncidencia(f.Id, f.Codigo, f.TipoId, SqlUtil.Fecha(f.FechaVencimiento)))
            .ToList();
    }

    /// <inheritdoc />
    public async Task AgregarAsync(Incidencia incidencia, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(incidencia);
        const string comando = """
            INSERT INTO dbo.Incidencias
                (ProyectoId, Numero, Codigo, Titulo, Descripcion, TipoId, EstadoId, PrioridadId, ResponsableId,
                 FechaVencimiento, AgrupacionId, PadreId, FechaCreacion, FechaModificacion)
            OUTPUT INSERTED.Id
            VALUES (@ProyectoId, @Numero, @Codigo, @Titulo, @Descripcion, @TipoId, @EstadoId, @PrioridadId,
                    @ResponsableId, @FechaVencimiento, @AgrupacionId, @PadreId, @FechaCreacion, @FechaModificacion);
            """;
        var id = await sql.EscalarAsync<int>(comando, Parametros(incidencia), ct);
        incidencia.AsignarId(id);
    }

    /// <inheritdoc />
    public async Task ActualizarAsync(Incidencia incidencia, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(incidencia);
        const string comando = """
            UPDATE dbo.Incidencias
            SET Titulo = @Titulo, Descripcion = @Descripcion, TipoId = @TipoId, EstadoId = @EstadoId,
                PrioridadId = @PrioridadId, ResponsableId = @ResponsableId, FechaVencimiento = @FechaVencimiento,
                AgrupacionId = @AgrupacionId, PadreId = @PadreId, FechaModificacion = @FechaModificacion
            WHERE Id = @Id;
            """;
        await sql.EjecutarAsync(comando, Parametros(incidencia), ct);
    }

    /// <inheritdoc />
    public Task<int> ActualizarAgrupacionDescendientesAsync(int id, int? agrupacionId, DateTime ahora,
        CancellationToken ct) =>
        sql.EjecutarAsync(ConsultasIncidencia.PropagarAgrupacion,
            new { Id = id, AgrupacionId = agrupacionId, Ahora = ahora }, ct);

    /// <inheritdoc />
    public Task<int> DesasociarAgrupacionAsync(int proyectoId, int agrupacionId, DateTime ahora,
        CancellationToken ct)
    {
        const string comando = """
            UPDATE dbo.Incidencias
            SET AgrupacionId = NULL, FechaModificacion = @Ahora
            WHERE ProyectoId = @ProyectoId AND AgrupacionId = @AgrupacionId;
            """;
        return sql.EjecutarAsync(comando,
            new { ProyectoId = proyectoId, AgrupacionId = agrupacionId, Ahora = ahora }, ct);
    }

    private static object Parametros(Incidencia i) => new
    {
        i.Id,
        i.ProyectoId,
        i.Numero,
        i.Codigo,
        i.Titulo,
        i.Descripcion,
        i.TipoId,
        i.EstadoId,
        i.PrioridadId,
        i.ResponsableId,
        FechaVencimiento = SqlUtil.Parametro(i.FechaVencimiento),
        i.AgrupacionId,
        i.PadreId,
        i.FechaCreacion,
        i.FechaModificacion,
    };

    /// <summary>
    /// Fila de dbo.Incidencias tal como la devuelve Dapper.
    /// </summary>
    /// <remarks>
    /// Creado: 2026-10-09
    /// Modificado: 2026-10-09 - Versión inicial (RF-002)
    /// </remarks>
    private sealed record FilaEntidad(int Id, int ProyectoId, int Numero, string Codigo, string Titulo,
        string Descripcion, int TipoId, int EstadoId, int PrioridadId, int? ResponsableId,
        DateTime? FechaVencimiento, int? AgrupacionId, int? PadreId, DateTime FechaCreacion,
        DateTime? FechaModificacion)
    {
        public Incidencia AEntidad() => Incidencia.Reconstituir(Id, ProyectoId, Numero, Codigo, TipoId, EstadoId,
            new DatosIncidencia(Titulo, Descripcion, PrioridadId, ResponsableId, SqlUtil.Fecha(FechaVencimiento)),
            new UbicacionIncidencia(PadreId, AgrupacionId),
            SqlUtil.Utc(FechaCreacion), SqlUtil.Utc(FechaModificacion));
    }

    /// <summary>
    /// Proyecto bloqueado (y número asignado, si la fila viene del contador).
    /// </summary>
    /// <remarks>
    /// Creado: 2026-10-09
    /// Modificado: 2026-10-09 - Versión inicial (RF-002)
    /// </remarks>
    private sealed class FilaProyectoBloqueado
    {
        public int Id { get; init; }
        public string Codigo { get; init; } = string.Empty;
        public DateTime? FechaArchivado { get; init; }
        public int Numero { get; init; }

        public ProyectoBloqueado ABloqueado() => new(Id, Codigo, FechaArchivado is not null);
    }

    /// <summary>
    /// Hija directa con los datos que usan las reglas de jerarquía.
    /// </summary>
    /// <remarks>
    /// Creado: 2026-10-09
    /// Modificado: 2026-10-09 - Versión inicial (RF-002)
    /// </remarks>
    private sealed record FilaHija(int Id, string Codigo, int TipoId, DateTime? FechaVencimiento);

    /// <summary>
    /// Tipo de hija permitido.
    /// </summary>
    /// <remarks>
    /// Creado: 2026-10-09
    /// Modificado: 2026-10-09 - Versión inicial (RF-002)
    /// </remarks>
    private sealed record FilaTipoHija(int Id, string Nombre);
}
