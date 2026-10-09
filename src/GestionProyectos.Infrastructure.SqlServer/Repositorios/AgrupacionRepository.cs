using GestionProyectos.Application.Abstracciones.Persistencia;
using GestionProyectos.Domain.Entidades;
using GestionProyectos.Infrastructure.SqlServer.Conexion;

namespace GestionProyectos.Infrastructure.SqlServer.Repositorios;

/// <summary>
/// Repositorio de agrupaciones sobre dbo.Agrupaciones (Dapper).
/// </summary>
/// <remarks>
/// Creado: 2026-10-09
/// Modificado: 2026-10-09 - Versión inicial (RF-001)
/// Modificado: 2026-10-09 - Conteo de incidencias por agrupación (RF-002)
/// </remarks>
internal sealed class AgrupacionRepository(EjecutorSql sql) : IAgrupacionRepository
{
    private const string Columnas =
        "Id, ProyectoId, Nombre, Descripcion, FechaCreacion, FechaModificacion";

    /// <inheritdoc />
    public async Task<Agrupacion?> ObtenerPorIdAsync(int id, CancellationToken ct)
    {
        var fila = await sql.ConsultarUnoAsync<FilaAgrupacion>(
            $"SELECT {Columnas} FROM dbo.Agrupaciones WHERE Id = @Id;", new { Id = id }, ct);
        return fila?.AEntidad();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Agrupacion>> ListarPorProyectoAsync(int proyectoId, CancellationToken ct)
    {
        var filas = await sql.ConsultarAsync<FilaAgrupacion>(
            $"SELECT {Columnas} FROM dbo.Agrupaciones WHERE ProyectoId = @ProyectoId ORDER BY Nombre, Id;",
            new { ProyectoId = proyectoId }, ct);
        return filas.Select(f => f.AEntidad()).ToList();
    }

    /// <inheritdoc />
    public Task<bool> ExisteNombreAsync(int proyectoId, string nombre, int? excluirId, CancellationToken ct)
    {
        const string consulta = """
            SELECT CASE WHEN EXISTS (
                SELECT 1 FROM dbo.Agrupaciones
                WHERE ProyectoId = @ProyectoId AND Nombre = @Nombre
                  AND (@ExcluirId IS NULL OR Id <> @ExcluirId)
            ) THEN 1 ELSE 0 END;
            """;
        return sql.ConsultarUnoAsync<bool>(consulta,
            new { ProyectoId = proyectoId, Nombre = nombre, ExcluirId = excluirId }, ct);
    }

    /// <inheritdoc />
    public Task<int> ContarIncidenciasAsync(int id, CancellationToken ct) =>
        sql.ConsultarUnoAsync<int>("SELECT COUNT(*) FROM dbo.Incidencias WHERE AgrupacionId = @Id;",
            new { Id = id }, ct);

    /// <inheritdoc />
    public async Task<IReadOnlyDictionary<int, int>> ContarIncidenciasPorAgrupacionAsync(int proyectoId,
        CancellationToken ct)
    {
        const string consulta = """
            SELECT AgrupacionId, COUNT(*) AS Cantidad
            FROM dbo.Incidencias
            WHERE ProyectoId = @ProyectoId AND AgrupacionId IS NOT NULL
            GROUP BY AgrupacionId;
            """;
        var filas = await sql.ConsultarAsync<(int AgrupacionId, int Cantidad)>(consulta,
            new { ProyectoId = proyectoId }, ct);
        return filas.ToDictionary(f => f.AgrupacionId, f => f.Cantidad);
    }

    /// <inheritdoc />
    public async Task AgregarAsync(Agrupacion agrupacion, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(agrupacion);
        const string comando = """
            INSERT INTO dbo.Agrupaciones (ProyectoId, Nombre, Descripcion, FechaCreacion, FechaModificacion)
            OUTPUT INSERTED.Id
            VALUES (@ProyectoId, @Nombre, @Descripcion, @FechaCreacion, @FechaModificacion);
            """;
        var id = await sql.EscalarAsync<int>(comando, Parametros(agrupacion), ct);
        agrupacion.AsignarId(id);
    }

    /// <inheritdoc />
    public async Task ActualizarAsync(Agrupacion agrupacion, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(agrupacion);
        const string comando = """
            UPDATE dbo.Agrupaciones
            SET Nombre = @Nombre, Descripcion = @Descripcion, FechaModificacion = @FechaModificacion
            WHERE Id = @Id;
            """;
        await sql.EjecutarAsync(comando, Parametros(agrupacion), ct);
    }

    /// <inheritdoc />
    public async Task EliminarAsync(int id, CancellationToken ct) =>
        await sql.EjecutarAsync("DELETE FROM dbo.Agrupaciones WHERE Id = @Id;", new { Id = id }, ct);

    private static object Parametros(Agrupacion a) => new
    {
        a.Id,
        a.ProyectoId,
        a.Nombre,
        a.Descripcion,
        a.FechaCreacion,
        a.FechaModificacion,
    };

    /// <summary>
    /// Fila de dbo.Agrupaciones tal como la devuelve Dapper.
    /// </summary>
    /// <remarks>
    /// Creado: 2026-10-09
    /// Modificado: 2026-10-09 - Versión inicial (RF-001)
    /// </remarks>
    private sealed record FilaAgrupacion(int Id, int ProyectoId, string Nombre, string Descripcion,
        DateTime FechaCreacion, DateTime? FechaModificacion)
    {
        public Agrupacion AEntidad() => Agrupacion.Reconstituir(Id, ProyectoId, Nombre, Descripcion,
            SqlUtil.Utc(FechaCreacion), SqlUtil.Utc(FechaModificacion));
    }
}
