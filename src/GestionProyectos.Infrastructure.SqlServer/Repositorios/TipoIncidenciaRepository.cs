using GestionProyectos.Application.Abstracciones.Persistencia;
using GestionProyectos.Application.Comun;
using GestionProyectos.Application.TiposIncidencia;
using GestionProyectos.Domain.Entidades;
using GestionProyectos.Domain.Reglas;
using GestionProyectos.Infrastructure.SqlServer.Conexion;

namespace GestionProyectos.Infrastructure.SqlServer.Repositorios;

/// <summary>
/// Repositorio de tipos de incidencia sobre dbo.TiposIncidencia (Dapper).
/// </summary>
/// <remarks>
/// Creado: 2026-10-09
/// Modificado: 2026-10-09 - Versión inicial (RF-002)
/// </remarks>
internal sealed class TipoIncidenciaRepository(EjecutorSql sql) : ITipoIncidenciaRepository
{
    private const string ConsultaDtos = """
        SELECT t.Id, t.Nombre, t.Descripcion, t.Activo, t.FechaCreacion, t.FechaModificacion,
               n.Id AS NivelId, n.Numero AS NivelNumero, n.Nombre AS NivelNombre,
               f.Id AS FlujoId, f.Nombre AS FlujoNombre,
               (SELECT COUNT(*) FROM dbo.Incidencias i WHERE i.TipoId = t.Id) AS CantidadIncidencias
        FROM dbo.TiposIncidencia t
        INNER JOIN dbo.NivelesIncidencia n ON n.Id = t.NivelId
        INNER JOIN dbo.FlujosIncidencia f ON f.Id = t.FlujoId
        """;

    /// <inheritdoc />
    public async Task<TipoIncidencia?> ObtenerPorIdAsync(int id, CancellationToken ct)
    {
        var fila = await sql.ConsultarUnoAsync<FilaTipo>(
            "SELECT Id, Nombre, Descripcion, NivelId, FlujoId, Activo, FechaCreacion, FechaModificacion "
            + "FROM dbo.TiposIncidencia WHERE Id = @Id;", new { Id = id }, ct);
        return fila?.AEntidad();
    }

    /// <inheritdoc />
    public async Task<TipoIncidenciaDto?> ObtenerDtoAsync(int id, CancellationToken ct)
    {
        var fila = await sql.ConsultarUnoAsync<FilaTipoDto>($"{ConsultaDtos} WHERE t.Id = @Id;", new { Id = id }, ct);
        return fila?.ADto();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<TipoIncidenciaDto>> ListarAsync(bool soloActivos, int? nivelId,
        CancellationToken ct)
    {
        const string consulta = $"""
            {ConsultaDtos}
            WHERE (@SoloActivos = 0 OR t.Activo = 1) AND (@NivelId IS NULL OR t.NivelId = @NivelId)
            ORDER BY n.Numero, t.Nombre, t.Id;
            """;
        var filas = await sql.ConsultarAsync<FilaTipoDto>(consulta,
            new { SoloActivos = soloActivos, NivelId = nivelId }, ct);
        return filas.Select(f => f.ADto()).ToList();
    }

    /// <inheritdoc />
    public Task<bool> ExisteNombreAsync(string nombre, int? excluirId, CancellationToken ct)
    {
        const string consulta = """
            SELECT CASE WHEN EXISTS (
                SELECT 1 FROM dbo.TiposIncidencia
                WHERE Nombre = @Nombre AND (@ExcluirId IS NULL OR Id <> @ExcluirId)
            ) THEN 1 ELSE 0 END;
            """;
        return sql.ConsultarUnoAsync<bool>(consulta, new { Nombre = nombre, ExcluirId = excluirId }, ct);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<CantidadPorEstado>> ContarIncidenciasPorEstadoAsync(int tipoId,
        CancellationToken ct)
    {
        const string consulta = """
            SELECT i.EstadoId, e.Nombre AS EstadoNombre, COUNT(*) AS Cantidad
            FROM dbo.Incidencias i
            INNER JOIN dbo.EstadosIncidencia e ON e.Id = i.EstadoId
            WHERE i.TipoId = @TipoId
            GROUP BY i.EstadoId, e.Nombre
            ORDER BY e.Nombre;
            """;
        return await sql.ConsultarAsync<CantidadPorEstado>(consulta, new { TipoId = tipoId }, ct);
    }

    /// <inheritdoc />
    public async Task AgregarAsync(TipoIncidencia tipo, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(tipo);
        const string comando = """
            INSERT INTO dbo.TiposIncidencia
                (Nombre, Descripcion, NivelId, FlujoId, Activo, FechaCreacion, FechaModificacion)
            OUTPUT INSERTED.Id
            VALUES (@Nombre, @Descripcion, @NivelId, @FlujoId, @Activo, @FechaCreacion, @FechaModificacion);
            """;
        var id = await sql.EscalarAsync<int>(comando, Parametros(tipo), ct);
        tipo.AsignarId(id);
    }

    /// <inheritdoc />
    public async Task ActualizarAsync(TipoIncidencia tipo, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(tipo);
        const string comando = """
            UPDATE dbo.TiposIncidencia
            SET Nombre = @Nombre, Descripcion = @Descripcion, FlujoId = @FlujoId, Activo = @Activo,
                FechaModificacion = @FechaModificacion
            WHERE Id = @Id;
            """;
        await sql.EjecutarAsync(comando, Parametros(tipo), ct);
    }

    private static object Parametros(TipoIncidencia t) => new
    {
        t.Id,
        t.Nombre,
        t.Descripcion,
        t.NivelId,
        t.FlujoId,
        t.Activo,
        t.FechaCreacion,
        t.FechaModificacion,
    };

    /// <summary>
    /// Fila de dbo.TiposIncidencia tal como la devuelve Dapper.
    /// </summary>
    /// <remarks>
    /// Creado: 2026-10-09
    /// Modificado: 2026-10-09 - Versión inicial (RF-002)
    /// </remarks>
    private sealed record FilaTipo(int Id, string Nombre, string? Descripcion, int NivelId, int FlujoId,
        bool Activo, DateTime FechaCreacion, DateTime? FechaModificacion)
    {
        public TipoIncidencia AEntidad() => TipoIncidencia.Reconstituir(Id, Nombre, Descripcion, NivelId, FlujoId,
            Activo, SqlUtil.Utc(FechaCreacion), SqlUtil.Utc(FechaModificacion));
    }

    /// <summary>
    /// Fila plana del DTO de tipo (con nivel, flujo y cantidad de incidencias).
    /// </summary>
    /// <remarks>
    /// Creado: 2026-10-09
    /// Modificado: 2026-10-09 - Versión inicial (RF-002)
    /// </remarks>
    private sealed class FilaTipoDto
    {
        public int Id { get; init; }
        public string Nombre { get; init; } = string.Empty;
        public string? Descripcion { get; init; }
        public bool Activo { get; init; }
        public DateTime FechaCreacion { get; init; }
        public DateTime? FechaModificacion { get; init; }
        public int NivelId { get; init; }
        public int NivelNumero { get; init; }
        public string NivelNombre { get; init; } = string.Empty;
        public int FlujoId { get; init; }
        public string FlujoNombre { get; init; } = string.Empty;
        public int CantidadIncidencias { get; init; }

        public TipoIncidenciaDto ADto() => new(Id, Nombre, Descripcion,
            new NivelReferenciaDto(NivelId, NivelNumero, NivelNombre), new FlujoReferenciaDto(FlujoId, FlujoNombre),
            Activo, CantidadIncidencias, SqlUtil.Utc(FechaCreacion), SqlUtil.Utc(FechaModificacion));
    }
}
