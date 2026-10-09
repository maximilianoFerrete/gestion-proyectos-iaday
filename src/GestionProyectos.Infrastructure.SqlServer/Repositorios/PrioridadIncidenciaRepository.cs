using GestionProyectos.Application.Abstracciones.Persistencia;
using GestionProyectos.Domain.Entidades;
using GestionProyectos.Infrastructure.SqlServer.Conexion;

namespace GestionProyectos.Infrastructure.SqlServer.Repositorios;

/// <summary>
/// Repositorio de prioridades sobre dbo.PrioridadesIncidencia (Dapper).
/// </summary>
/// <remarks>
/// Creado: 2026-10-09
/// Modificado: 2026-10-09 - Versión inicial (RF-002)
/// </remarks>
internal sealed class PrioridadIncidenciaRepository(EjecutorSql sql) : IPrioridadIncidenciaRepository
{
    private const string Columnas =
        "Id, Nombre, Descripcion, Orden, EsPredeterminada, Activo, FechaCreacion, FechaModificacion";

    /// <inheritdoc />
    public Task<PrioridadIncidencia?> ObtenerPorIdAsync(int id, CancellationToken ct) =>
        ObtenerUnoAsync($"SELECT {Columnas} FROM dbo.PrioridadesIncidencia WHERE Id = @Id;", new { Id = id }, ct);

    /// <inheritdoc />
    public Task<PrioridadIncidencia?> ObtenerPredeterminadaAsync(CancellationToken ct) =>
        ObtenerUnoAsync($"SELECT {Columnas} FROM dbo.PrioridadesIncidencia WHERE EsPredeterminada = 1;", null, ct);

    /// <inheritdoc />
    public async Task<IReadOnlyList<PrioridadIncidencia>> ListarAsync(bool soloActivas, CancellationToken ct)
    {
        const string consulta = $"""
            SELECT {Columnas}
            FROM dbo.PrioridadesIncidencia
            WHERE (@SoloActivas = 0 OR Activo = 1)
            ORDER BY Orden, Nombre, Id;
            """;
        var filas = await sql.ConsultarAsync<FilaPrioridad>(consulta, new { SoloActivas = soloActivas }, ct);
        return filas.Select(f => f.AEntidad()).ToList();
    }

    /// <inheritdoc />
    public Task<bool> ExisteNombreAsync(string nombre, int? excluirId, CancellationToken ct)
    {
        const string consulta = """
            SELECT CASE WHEN EXISTS (
                SELECT 1 FROM dbo.PrioridadesIncidencia
                WHERE Nombre = @Nombre AND (@ExcluirId IS NULL OR Id <> @ExcluirId)
            ) THEN 1 ELSE 0 END;
            """;
        return sql.ConsultarUnoAsync<bool>(consulta, new { Nombre = nombre, ExcluirId = excluirId }, ct);
    }

    /// <inheritdoc />
    public Task<int> ContarIncidenciasAsync(int prioridadId, CancellationToken ct) =>
        sql.ConsultarUnoAsync<int>("SELECT COUNT(*) FROM dbo.Incidencias WHERE PrioridadId = @PrioridadId;",
            new { PrioridadId = prioridadId }, ct);

    /// <inheritdoc />
    public async Task<IReadOnlyDictionary<int, int>> ContarIncidenciasPorPrioridadAsync(CancellationToken ct)
    {
        var filas = await sql.ConsultarAsync<(int PrioridadId, int Cantidad)>(
            "SELECT PrioridadId, COUNT(*) AS Cantidad FROM dbo.Incidencias GROUP BY PrioridadId;", null, ct);
        return filas.ToDictionary(f => f.PrioridadId, f => f.Cantidad);
    }

    /// <inheritdoc />
    public async Task AgregarAsync(PrioridadIncidencia prioridad, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(prioridad);
        const string comando = """
            INSERT INTO dbo.PrioridadesIncidencia
                (Nombre, Descripcion, Orden, EsPredeterminada, Activo, FechaCreacion, FechaModificacion)
            OUTPUT INSERTED.Id
            VALUES (@Nombre, @Descripcion, @Orden, @EsPredeterminada, @Activo, @FechaCreacion, @FechaModificacion);
            """;
        var id = await sql.EscalarAsync<int>(comando, Parametros(prioridad), ct);
        prioridad.AsignarId(id);
    }

    /// <inheritdoc />
    public async Task ActualizarAsync(PrioridadIncidencia prioridad, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(prioridad);
        const string comando = """
            UPDATE dbo.PrioridadesIncidencia
            SET Nombre = @Nombre, Descripcion = @Descripcion, Orden = @Orden, EsPredeterminada = @EsPredeterminada,
                Activo = @Activo, FechaModificacion = @FechaModificacion
            WHERE Id = @Id;
            """;
        await sql.EjecutarAsync(comando, Parametros(prioridad), ct);
    }

    private async Task<PrioridadIncidencia?> ObtenerUnoAsync(string consulta, object? parametros,
        CancellationToken ct)
    {
        var fila = await sql.ConsultarUnoAsync<FilaPrioridad>(consulta, parametros, ct);
        return fila?.AEntidad();
    }

    private static object Parametros(PrioridadIncidencia p) => new
    {
        p.Id,
        p.Nombre,
        p.Descripcion,
        p.Orden,
        p.EsPredeterminada,
        p.Activo,
        p.FechaCreacion,
        p.FechaModificacion,
    };

    /// <summary>
    /// Fila de dbo.PrioridadesIncidencia tal como la devuelve Dapper.
    /// </summary>
    /// <remarks>
    /// Creado: 2026-10-09
    /// Modificado: 2026-10-09 - Versión inicial (RF-002)
    /// </remarks>
    private sealed record FilaPrioridad(int Id, string Nombre, string? Descripcion, int Orden, bool EsPredeterminada,
        bool Activo, DateTime FechaCreacion, DateTime? FechaModificacion)
    {
        public PrioridadIncidencia AEntidad() => PrioridadIncidencia.Reconstituir(Id, Nombre, Descripcion, Orden,
            EsPredeterminada, Activo, SqlUtil.Utc(FechaCreacion), SqlUtil.Utc(FechaModificacion));
    }
}
