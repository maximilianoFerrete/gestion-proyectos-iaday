using GestionProyectos.Application.Abstracciones.Persistencia;
using GestionProyectos.Application.Comun;
using GestionProyectos.Application.Proyectos;
using GestionProyectos.Domain.Entidades;
using GestionProyectos.Infrastructure.SqlServer.Conexion;

namespace GestionProyectos.Infrastructure.SqlServer.Repositorios;

/// <summary>
/// Repositorio de proyectos sobre dbo.Proyectos y la secuencia SQ_Proyectos_Numero (Dapper).
/// </summary>
/// <remarks>
/// Creado: 2026-10-09
/// Modificado: 2026-10-09 - Versión inicial (RF-001)
/// </remarks>
internal sealed class ProyectoRepository(EjecutorSql sql) : IProyectoRepository
{
    private const string Columnas = """
        Id, Codigo, Nombre, Descripcion, ResponsableId, FechaInicio, FechaObjetivo, EstadoId,
        FechaArchivado, ArchivadoPorId, EstadoAlArchivarId, FechaCreacion, FechaModificacion
        """;

    /// <inheritdoc />
    public async Task<Proyecto?> ObtenerPorIdAsync(int id, CancellationToken ct)
    {
        var fila = await sql.ConsultarUnoAsync<FilaProyecto>(
            $"SELECT {Columnas} FROM dbo.Proyectos WHERE Id = @Id;", new { Id = id }, ct);
        return fila?.AEntidad();
    }

    /// <inheritdoc />
    public async Task<ProyectoDto?> ObtenerDetalleAsync(int id, CancellationToken ct)
    {
        var fila = await sql.ConsultarUnoAsync<FilaDetalleProyecto>(ConsultasProyecto.Detalle, new { Id = id }, ct);
        return fila?.ADto();
    }

    /// <inheritdoc />
    public async Task<Paginado<ProyectoResumenDto>> ListarAsync(FiltroProyectos filtro, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(filtro);
        var parametros = new
        {
            Patron = SqlUtil.PatronContiene(filtro.Texto),
            filtro.EstadoId,
            filtro.ResponsableId,
            Desde = (filtro.Pagina - 1) * filtro.TamanioPagina,
            filtro.TamanioPagina,
        };
        var (filas, total) = await sql.ConsultarConTotalAsync<FilaResumenProyecto>(
            ConsultasProyecto.Listado(filtro), parametros, ct);
        return new Paginado<ProyectoResumenDto>(
            filas.Select(f => f.ADto()).ToList(), filtro.Pagina, filtro.TamanioPagina, total);
    }

    /// <inheritdoc />
    public Task<int> ObtenerSiguienteNumeroAsync(CancellationToken ct) =>
        sql.EscalarAsync<int>("SELECT NEXT VALUE FOR dbo.SQ_Proyectos_Numero;", null, ct);

    /// <inheritdoc />
    public async Task AgregarAsync(Proyecto proyecto, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(proyecto);
        const string comando = """
            INSERT INTO dbo.Proyectos
                (Codigo, Nombre, Descripcion, ResponsableId, FechaInicio, FechaObjetivo, EstadoId,
                 FechaArchivado, ArchivadoPorId, EstadoAlArchivarId, FechaCreacion, FechaModificacion)
            OUTPUT INSERTED.Id
            VALUES (@Codigo, @Nombre, @Descripcion, @ResponsableId, @FechaInicio, @FechaObjetivo, @EstadoId,
                    @FechaArchivado, @ArchivadoPorId, @EstadoAlArchivarId, @FechaCreacion, @FechaModificacion);
            """;
        var id = await sql.EscalarAsync<int>(comando, Parametros(proyecto), ct);
        proyecto.AsignarId(id);
    }

    /// <inheritdoc />
    public async Task ActualizarAsync(Proyecto proyecto, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(proyecto);
        const string comando = """
            UPDATE dbo.Proyectos
            SET Nombre = @Nombre, Descripcion = @Descripcion, ResponsableId = @ResponsableId,
                FechaInicio = @FechaInicio, FechaObjetivo = @FechaObjetivo, EstadoId = @EstadoId,
                FechaArchivado = @FechaArchivado, ArchivadoPorId = @ArchivadoPorId,
                EstadoAlArchivarId = @EstadoAlArchivarId, FechaModificacion = @FechaModificacion
            WHERE Id = @Id;
            """;
        await sql.EjecutarAsync(comando, Parametros(proyecto), ct);
    }

    private static object Parametros(Proyecto p) => new
    {
        p.Id,
        p.Codigo,
        p.Nombre,
        p.Descripcion,
        p.ResponsableId,
        FechaInicio = SqlUtil.Parametro(p.FechaInicio),
        FechaObjetivo = SqlUtil.Parametro(p.FechaObjetivo),
        p.EstadoId,
        p.FechaArchivado,
        p.ArchivadoPorId,
        p.EstadoAlArchivarId,
        p.FechaCreacion,
        p.FechaModificacion,
    };

    /// <summary>
    /// Fila de dbo.Proyectos tal como la devuelve Dapper.
    /// </summary>
    /// <remarks>
    /// Creado: 2026-10-09
    /// Modificado: 2026-10-09 - Versión inicial (RF-001)
    /// </remarks>
    private sealed record FilaProyecto(int Id, string Codigo, string Nombre, string Descripcion,
        int ResponsableId, DateTime FechaInicio, DateTime FechaObjetivo, int EstadoId,
        DateTime? FechaArchivado, int? ArchivadoPorId, int? EstadoAlArchivarId,
        DateTime FechaCreacion, DateTime? FechaModificacion)
    {
        public Proyecto AEntidad()
        {
            var datos = new DatosProyecto(Nombre, Descripcion, ResponsableId,
                SqlUtil.Fecha(FechaInicio), SqlUtil.Fecha(FechaObjetivo));
            var archivado = FechaArchivado is { } fecha && ArchivadoPorId is { } por
                && EstadoAlArchivarId is { } estadoPrevio
                ? new DatosArchivado(SqlUtil.Utc(fecha), por, estadoPrevio)
                : null;
            return Proyecto.Reconstituir(Id, Codigo, datos, EstadoId, archivado,
                SqlUtil.Utc(FechaCreacion), SqlUtil.Utc(FechaModificacion));
        }
    }
}
