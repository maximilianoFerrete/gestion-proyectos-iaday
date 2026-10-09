using GestionProyectos.Application.Abstracciones.Persistencia;
using GestionProyectos.Domain.Entidades;
using GestionProyectos.Infrastructure.SqlServer.Conexion;

namespace GestionProyectos.Infrastructure.SqlServer.Repositorios;

/// <summary>
/// Repositorio de estados de proyecto sobre dbo.EstadosProyecto (Dapper).
/// </summary>
/// <remarks>
/// Creado: 2026-10-09
/// Modificado: 2026-10-09 - Versión inicial (RF-001)
/// </remarks>
internal sealed class EstadoProyectoRepository(EjecutorSql sql) : IEstadoProyectoRepository
{
    private const string Columnas =
        "Id, Nombre, Descripcion, Orden, EsInicial, EsFinal, Activo, Codigo, FechaCreacion, FechaModificacion";

    /// <inheritdoc />
    public Task<EstadoProyecto?> ObtenerPorIdAsync(int id, CancellationToken ct) =>
        ObtenerUnoAsync($"SELECT {Columnas} FROM dbo.EstadosProyecto WHERE Id = @Id;", new { Id = id }, ct);

    /// <inheritdoc />
    public Task<EstadoProyecto?> ObtenerInicialAsync(CancellationToken ct) =>
        ObtenerUnoAsync($"SELECT {Columnas} FROM dbo.EstadosProyecto WHERE EsInicial = 1;", null, ct);

    /// <inheritdoc />
    public Task<EstadoProyecto?> ObtenerPorCodigoAsync(string codigo, CancellationToken ct) =>
        ObtenerUnoAsync($"SELECT {Columnas} FROM dbo.EstadosProyecto WHERE Codigo = @Codigo;",
            new { Codigo = codigo }, ct);

    /// <inheritdoc />
    public async Task<IReadOnlyList<EstadoProyecto>> ListarAsync(bool soloActivos, CancellationToken ct)
    {
        const string consulta = $"""
            SELECT {Columnas}
            FROM dbo.EstadosProyecto
            WHERE (@SoloActivos = 0 OR Activo = 1)
            ORDER BY Orden, Nombre, Id;
            """;
        var filas = await sql.ConsultarAsync<FilaEstado>(consulta, new { SoloActivos = soloActivos }, ct);
        return filas.Select(f => f.AEntidad()).ToList();
    }

    /// <inheritdoc />
    public Task<bool> ExisteNombreAsync(string nombre, int? excluirId, CancellationToken ct)
    {
        const string consulta = """
            SELECT CASE WHEN EXISTS (
                SELECT 1 FROM dbo.EstadosProyecto
                WHERE Nombre = @Nombre AND (@ExcluirId IS NULL OR Id <> @ExcluirId)
            ) THEN 1 ELSE 0 END;
            """;
        return sql.ConsultarUnoAsync<bool>(consulta, new { Nombre = nombre, ExcluirId = excluirId }, ct);
    }

    /// <inheritdoc />
    public Task<int> ContarProyectosAsync(int estadoId, CancellationToken ct) =>
        sql.ConsultarUnoAsync<int>("SELECT COUNT(*) FROM dbo.Proyectos WHERE EstadoId = @EstadoId;",
            new { EstadoId = estadoId }, ct);

    /// <inheritdoc />
    public async Task<IReadOnlyDictionary<int, int>> ContarProyectosPorEstadoAsync(CancellationToken ct)
    {
        var filas = await sql.ConsultarAsync<(int EstadoId, int Cantidad)>(
            "SELECT EstadoId, COUNT(*) AS Cantidad FROM dbo.Proyectos GROUP BY EstadoId;", null, ct);
        return filas.ToDictionary(f => f.EstadoId, f => f.Cantidad);
    }

    /// <inheritdoc />
    public async Task AgregarAsync(EstadoProyecto estado, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(estado);
        const string comando = """
            INSERT INTO dbo.EstadosProyecto
                (Nombre, Descripcion, Orden, EsInicial, EsFinal, Activo, Codigo, FechaCreacion, FechaModificacion)
            OUTPUT INSERTED.Id
            VALUES (@Nombre, @Descripcion, @Orden, @EsInicial, @EsFinal, @Activo, @Codigo,
                    @FechaCreacion, @FechaModificacion);
            """;
        var id = await sql.EscalarAsync<int>(comando, Parametros(estado), ct);
        estado.AsignarId(id);
    }

    /// <inheritdoc />
    public async Task ActualizarAsync(EstadoProyecto estado, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(estado);
        const string comando = """
            UPDATE dbo.EstadosProyecto
            SET Nombre = @Nombre, Descripcion = @Descripcion, Orden = @Orden, EsInicial = @EsInicial,
                EsFinal = @EsFinal, Activo = @Activo, FechaModificacion = @FechaModificacion
            WHERE Id = @Id;
            """;
        await sql.EjecutarAsync(comando, Parametros(estado), ct);
    }

    private async Task<EstadoProyecto?> ObtenerUnoAsync(string consulta, object? parametros,
        CancellationToken ct)
    {
        var fila = await sql.ConsultarUnoAsync<FilaEstado>(consulta, parametros, ct);
        return fila?.AEntidad();
    }

    private static object Parametros(EstadoProyecto e) => new
    {
        e.Id,
        e.Nombre,
        e.Descripcion,
        e.Orden,
        e.EsInicial,
        e.EsFinal,
        e.Activo,
        e.Codigo,
        e.FechaCreacion,
        e.FechaModificacion,
    };

    /// <summary>
    /// Fila de dbo.EstadosProyecto tal como la devuelve Dapper.
    /// </summary>
    /// <remarks>
    /// Creado: 2026-10-09
    /// Modificado: 2026-10-09 - Versión inicial (RF-001)
    /// </remarks>
    private sealed record FilaEstado(int Id, string Nombre, string? Descripcion, int Orden, bool EsInicial,
        bool EsFinal, bool Activo, string? Codigo, DateTime FechaCreacion, DateTime? FechaModificacion)
    {
        public EstadoProyecto AEntidad() => EstadoProyecto.Reconstituir(Id, Nombre, Descripcion, Orden,
            EsInicial, EsFinal, Activo, Codigo, SqlUtil.Utc(FechaCreacion), SqlUtil.Utc(FechaModificacion));
    }
}
