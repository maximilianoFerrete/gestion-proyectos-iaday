using GestionProyectos.Application.Abstracciones.Persistencia;
using GestionProyectos.Application.FlujosIncidencia;
using GestionProyectos.Domain.Entidades;
using GestionProyectos.Domain.Reglas;
using GestionProyectos.Infrastructure.SqlServer.Conexion;

namespace GestionProyectos.Infrastructure.SqlServer.Repositorios;

/// <summary>
/// Repositorio de flujos sobre dbo.FlujosIncidencia y dbo.FlujosIncidenciaEstados (Dapper).
/// </summary>
/// <remarks>
/// Creado: 2026-10-09
/// Modificado: 2026-10-09 - Versión inicial (RF-002)
/// Modificado: 2026-10-10 - Informa la categoría de cada estado del flujo (RF-005)
/// </remarks>
internal sealed class FlujoIncidenciaRepository(EjecutorSql sql) : IFlujoIncidenciaRepository
{
    private const string ColumnasFlujo = "Id, Nombre, Descripcion, FechaCreacion, FechaModificacion";

    private const string ConsultaEstados = """
        SELECT fe.FlujoId, fe.EstadoId, e.Nombre, e.Activo, fe.Orden, fe.EsInicial, fe.EsFinal, e.Categoria
        FROM dbo.FlujosIncidenciaEstados fe
        INNER JOIN dbo.EstadosIncidencia e ON e.Id = fe.EstadoId
        """;

    private const string ConsultaTipos = "SELECT t.FlujoId, t.Id, t.Nombre, t.Activo FROM dbo.TiposIncidencia t";

    /// <inheritdoc />
    public async Task<FlujoIncidencia?> ObtenerPorIdAsync(int id, CancellationToken ct)
    {
        var fila = await sql.ConsultarUnoAsync<FilaFlujo>(
            $"SELECT {ColumnasFlujo} FROM dbo.FlujosIncidencia WHERE Id = @Id;", new { Id = id }, ct);
        if (fila is null)
        {
            return null;
        }

        var estados = await sql.ConsultarAsync<FilaEstadoDeFlujo>(
            $"{ConsultaEstados} WHERE fe.FlujoId = @Id ORDER BY fe.Orden;", new { Id = id }, ct);
        return FlujoIncidencia.Reconstituir(fila.Id, fila.Nombre, fila.Descripcion,
            estados.Select(e => new EstadoEnFlujo(e.EstadoId, e.Orden, e.EsInicial, e.EsFinal)),
            SqlUtil.Utc(fila.FechaCreacion), SqlUtil.Utc(fila.FechaModificacion));
    }

    /// <inheritdoc />
    public async Task<FlujoIncidenciaDto?> ObtenerDtoAsync(int id, CancellationToken ct)
    {
        var lista = await ListarDtosAsync("WHERE Id = @Id", "WHERE fe.FlujoId = @Id", "WHERE t.FlujoId = @Id",
            new { Id = id }, ct);
        return lista.Count == 0 ? null : lista[0];
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<FlujoIncidenciaDto>> ListarAsync(CancellationToken ct) =>
        ListarDtosAsync(string.Empty, string.Empty, string.Empty, null, ct);

    /// <inheritdoc />
    public Task<bool> ExisteNombreAsync(string nombre, int? excluirId, CancellationToken ct)
    {
        const string consulta = """
            SELECT CASE WHEN EXISTS (
                SELECT 1 FROM dbo.FlujosIncidencia
                WHERE Nombre = @Nombre AND (@ExcluirId IS NULL OR Id <> @ExcluirId)
            ) THEN 1 ELSE 0 END;
            """;
        return sql.ConsultarUnoAsync<bool>(consulta, new { Nombre = nombre, ExcluirId = excluirId }, ct);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<CantidadPorEstado>> ContarIncidenciasPorEstadoAsync(int flujoId,
        CancellationToken ct)
    {
        const string consulta = """
            SELECT i.EstadoId, e.Nombre AS EstadoNombre, COUNT(*) AS Cantidad
            FROM dbo.Incidencias i
            INNER JOIN dbo.TiposIncidencia t ON t.Id = i.TipoId
            INNER JOIN dbo.EstadosIncidencia e ON e.Id = i.EstadoId
            WHERE t.FlujoId = @FlujoId
            GROUP BY i.EstadoId, e.Nombre
            ORDER BY e.Nombre;
            """;
        return await sql.ConsultarAsync<CantidadPorEstado>(consulta, new { FlujoId = flujoId }, ct);
    }

    /// <inheritdoc />
    public async Task AgregarAsync(FlujoIncidencia flujo, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(flujo);
        const string comando = """
            INSERT INTO dbo.FlujosIncidencia (Nombre, Descripcion, FechaCreacion, FechaModificacion)
            OUTPUT INSERTED.Id
            VALUES (@Nombre, @Descripcion, @FechaCreacion, @FechaModificacion);
            """;
        var id = await sql.EscalarAsync<int>(comando,
            new { flujo.Nombre, flujo.Descripcion, flujo.FechaCreacion, flujo.FechaModificacion }, ct);
        flujo.AsignarId(id);
        await InsertarEstadosAsync(flujo, ct);
    }

    /// <inheritdoc />
    public async Task ActualizarAsync(FlujoIncidencia flujo, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(flujo);
        const string comando = """
            UPDATE dbo.FlujosIncidencia
            SET Nombre = @Nombre, Descripcion = @Descripcion, FechaModificacion = @FechaModificacion
            WHERE Id = @Id;

            DELETE FROM dbo.FlujosIncidenciaEstados WHERE FlujoId = @Id;
            """;
        await sql.EjecutarAsync(comando, new { flujo.Id, flujo.Nombre, flujo.Descripcion, flujo.FechaModificacion }, ct);
        await InsertarEstadosAsync(flujo, ct);
    }

    private async Task InsertarEstadosAsync(FlujoIncidencia flujo, CancellationToken ct)
    {
        const string comando = """
            INSERT INTO dbo.FlujosIncidenciaEstados (FlujoId, EstadoId, Orden, EsInicial, EsFinal)
            VALUES (@FlujoId, @EstadoId, @Orden, @EsInicial, @EsFinal);
            """;
        var filas = flujo.Estados
            .Select(e => new { FlujoId = flujo.Id, e.EstadoId, e.Orden, e.EsInicial, e.EsFinal })
            .ToList();
        await sql.EjecutarAsync(comando, filas, ct);
    }

    private async Task<IReadOnlyList<FlujoIncidenciaDto>> ListarDtosAsync(string filtroFlujo, string filtroEstados,
        string filtroTipos, object? parametros, CancellationToken ct)
    {
        var flujos = await sql.ConsultarAsync<FilaFlujo>(
            $"SELECT {ColumnasFlujo} FROM dbo.FlujosIncidencia {filtroFlujo} ORDER BY Nombre, Id;", parametros, ct);
        var estados = (await sql.ConsultarAsync<FilaEstadoDeFlujo>(
            $"{ConsultaEstados} {filtroEstados} ORDER BY fe.Orden;", parametros, ct)).ToLookup(e => e.FlujoId);
        var tipos = (await sql.ConsultarAsync<FilaTipoDeFlujo>(
            $"{ConsultaTipos} {filtroTipos} ORDER BY t.Nombre;", parametros, ct)).ToLookup(t => t.FlujoId);

        return flujos.Select(f => new FlujoIncidenciaDto(f.Id, f.Nombre, f.Descripcion,
                estados[f.Id].Select(e => new EstadoDeFlujoDto(e.EstadoId, e.Nombre, e.Activo, e.Orden, e.EsInicial,
                    e.EsFinal, e.Categoria)).ToList(),
                tipos[f.Id].Select(t => new TipoDeFlujoDto(t.Id, t.Nombre, t.Activo)).ToList(),
                SqlUtil.Utc(f.FechaCreacion), SqlUtil.Utc(f.FechaModificacion)))
            .ToList();
    }

    /// <summary>
    /// Fila de dbo.FlujosIncidencia tal como la devuelve Dapper.
    /// </summary>
    /// <remarks>
    /// Creado: 2026-10-09
    /// Modificado: 2026-10-09 - Versión inicial (RF-002)
    /// </remarks>
    private sealed record FilaFlujo(int Id, string Nombre, string? Descripcion, DateTime FechaCreacion,
        DateTime? FechaModificacion);

    /// <summary>
    /// Estado de un flujo con su nombre, actividad y categoría.
    /// </summary>
    /// <remarks>
    /// Creado: 2026-10-09
    /// Modificado: 2026-10-09 - Versión inicial (RF-002)
    /// Modificado: 2026-10-10 - Agrega Categoria (RF-005)
    /// </remarks>
    private sealed record FilaEstadoDeFlujo(int FlujoId, int EstadoId, string Nombre, bool Activo, int Orden,
        bool EsInicial, bool EsFinal, string? Categoria);

    /// <summary>
    /// Tipo que usa un flujo.
    /// </summary>
    /// <remarks>
    /// Creado: 2026-10-09
    /// Modificado: 2026-10-09 - Versión inicial (RF-002)
    /// </remarks>
    private sealed record FilaTipoDeFlujo(int FlujoId, int Id, string Nombre, bool Activo);
}
