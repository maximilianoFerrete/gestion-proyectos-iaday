using GestionProyectos.Application.Abstracciones.Persistencia;
using GestionProyectos.Application.EstadosIncidencia;
using GestionProyectos.Domain.Entidades;
using GestionProyectos.Infrastructure.SqlServer.Conexion;

namespace GestionProyectos.Infrastructure.SqlServer.Repositorios;

/// <summary>
/// Repositorio del catálogo de estados de incidencia sobre dbo.EstadosIncidencia (Dapper).
/// </summary>
/// <remarks>
/// Creado: 2026-10-09
/// Modificado: 2026-10-09 - Versión inicial (RF-002)
/// </remarks>
internal sealed class EstadoIncidenciaRepository(EjecutorSql sql) : IEstadoIncidenciaRepository
{
    private const string Columnas = "Id, Nombre, Descripcion, Orden, Activo, FechaCreacion, FechaModificacion";

    private const string ConsultaDtos = """
        SELECT e.Id, e.Nombre, e.Descripcion, e.Orden, e.Activo,
               (SELECT COUNT(*) FROM dbo.Incidencias i WHERE i.EstadoId = e.Id) AS CantidadIncidencias
        FROM dbo.EstadosIncidencia e
        """;

    private const string ConsultaFlujos = """
        SELECT fe.EstadoId, f.Id, f.Nombre, fe.EsInicial, fe.EsFinal
        FROM dbo.FlujosIncidenciaEstados fe
        INNER JOIN dbo.FlujosIncidencia f ON f.Id = fe.FlujoId
        """;

    /// <inheritdoc />
    public async Task<EstadoIncidencia?> ObtenerPorIdAsync(int id, CancellationToken ct)
    {
        var fila = await sql.ConsultarUnoAsync<FilaEstado>(
            $"SELECT {Columnas} FROM dbo.EstadosIncidencia WHERE Id = @Id;", new { Id = id }, ct);
        return fila?.AEntidad();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<EstadoIncidencia>> ListarPorIdsAsync(IReadOnlyCollection<int> ids,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(ids);
        if (ids.Count == 0)
        {
            return [];
        }

        var filas = await sql.ConsultarAsync<FilaEstado>(
            $"SELECT {Columnas} FROM dbo.EstadosIncidencia WHERE Id IN @Ids;", new { Ids = ids.Distinct().ToArray() }, ct);
        return filas.Select(f => f.AEntidad()).ToList();
    }

    /// <inheritdoc />
    public async Task<EstadoIncidenciaDto?> ObtenerDtoAsync(int id, CancellationToken ct)
    {
        var fila = await sql.ConsultarUnoAsync<FilaEstadoDto>($"{ConsultaDtos} WHERE e.Id = @Id;", new { Id = id }, ct);
        if (fila is null)
        {
            return null;
        }

        var flujos = await sql.ConsultarAsync<FilaFlujoDeEstado>(
            $"{ConsultaFlujos} WHERE fe.EstadoId = @Id ORDER BY f.Nombre;", new { Id = id }, ct);
        return fila.ADto(flujos);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<EstadoIncidenciaDto>> ListarAsync(bool soloActivos, CancellationToken ct)
    {
        var filas = await sql.ConsultarAsync<FilaEstadoDto>(
            $"{ConsultaDtos} WHERE (@SoloActivos = 0 OR e.Activo = 1) ORDER BY e.Orden, e.Nombre, e.Id;",
            new { SoloActivos = soloActivos }, ct);
        var flujos = await sql.ConsultarAsync<FilaFlujoDeEstado>($"{ConsultaFlujos} ORDER BY f.Nombre;", null, ct);
        var porEstado = flujos.ToLookup(f => f.EstadoId);
        return filas.Select(f => f.ADto(porEstado[f.Id])).ToList();
    }

    /// <inheritdoc />
    public Task<bool> ExisteNombreAsync(string nombre, int? excluirId, CancellationToken ct)
    {
        const string consulta = """
            SELECT CASE WHEN EXISTS (
                SELECT 1 FROM dbo.EstadosIncidencia
                WHERE Nombre = @Nombre AND (@ExcluirId IS NULL OR Id <> @ExcluirId)
            ) THEN 1 ELSE 0 END;
            """;
        return sql.ConsultarUnoAsync<bool>(consulta, new { Nombre = nombre, ExcluirId = excluirId }, ct);
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<string>> ListarFlujosDondeEsInicialAsync(int estadoId, CancellationToken ct)
    {
        const string consulta = """
            SELECT f.Nombre
            FROM dbo.FlujosIncidenciaEstados fe
            INNER JOIN dbo.FlujosIncidencia f ON f.Id = fe.FlujoId
            WHERE fe.EstadoId = @EstadoId AND fe.EsInicial = 1
            ORDER BY f.Nombre;
            """;
        return sql.ConsultarAsync<string>(consulta, new { EstadoId = estadoId }, ct);
    }

    /// <inheritdoc />
    public async Task AgregarAsync(EstadoIncidencia estado, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(estado);
        const string comando = """
            INSERT INTO dbo.EstadosIncidencia (Nombre, Descripcion, Orden, Activo, FechaCreacion, FechaModificacion)
            OUTPUT INSERTED.Id
            VALUES (@Nombre, @Descripcion, @Orden, @Activo, @FechaCreacion, @FechaModificacion);
            """;
        var id = await sql.EscalarAsync<int>(comando, Parametros(estado), ct);
        estado.AsignarId(id);
    }

    /// <inheritdoc />
    public async Task ActualizarAsync(EstadoIncidencia estado, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(estado);
        const string comando = """
            UPDATE dbo.EstadosIncidencia
            SET Nombre = @Nombre, Descripcion = @Descripcion, Orden = @Orden, Activo = @Activo,
                FechaModificacion = @FechaModificacion
            WHERE Id = @Id;
            """;
        await sql.EjecutarAsync(comando, Parametros(estado), ct);
    }

    private static object Parametros(EstadoIncidencia e) => new
    {
        e.Id,
        e.Nombre,
        e.Descripcion,
        e.Orden,
        e.Activo,
        e.FechaCreacion,
        e.FechaModificacion,
    };

    /// <summary>
    /// Fila de dbo.EstadosIncidencia tal como la devuelve Dapper.
    /// </summary>
    /// <remarks>
    /// Creado: 2026-10-09
    /// Modificado: 2026-10-09 - Versión inicial (RF-002)
    /// </remarks>
    private sealed record FilaEstado(int Id, string Nombre, string? Descripcion, int Orden, bool Activo,
        DateTime FechaCreacion, DateTime? FechaModificacion)
    {
        public EstadoIncidencia AEntidad() => EstadoIncidencia.Reconstituir(Id, Nombre, Descripcion, Orden, Activo,
            SqlUtil.Utc(FechaCreacion), SqlUtil.Utc(FechaModificacion));
    }

    /// <summary>
    /// Fila del DTO de estado con su cantidad de incidencias.
    /// </summary>
    /// <remarks>
    /// Creado: 2026-10-09
    /// Modificado: 2026-10-09 - Versión inicial (RF-002)
    /// </remarks>
    private sealed record FilaEstadoDto(int Id, string Nombre, string? Descripcion, int Orden, bool Activo,
        int CantidadIncidencias)
    {
        public EstadoIncidenciaDto ADto(IEnumerable<FilaFlujoDeEstado> flujos) => new(Id, Nombre, Descripcion,
            Orden, Activo, CantidadIncidencias,
            flujos.Select(f => new FlujoDeEstadoDto(f.Id, f.Nombre, f.EsInicial, f.EsFinal)).ToList());
    }

    /// <summary>
    /// Participación de un estado en un flujo.
    /// </summary>
    /// <remarks>
    /// Creado: 2026-10-09
    /// Modificado: 2026-10-09 - Versión inicial (RF-002)
    /// </remarks>
    private sealed record FilaFlujoDeEstado(int EstadoId, int Id, string Nombre, bool EsInicial, bool EsFinal);
}
