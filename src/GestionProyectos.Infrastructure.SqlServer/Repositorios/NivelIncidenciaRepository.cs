using GestionProyectos.Application.Abstracciones.Persistencia;
using GestionProyectos.Application.NivelesIncidencia;
using GestionProyectos.Domain.Entidades;
using GestionProyectos.Infrastructure.SqlServer.Conexion;

namespace GestionProyectos.Infrastructure.SqlServer.Repositorios;

/// <summary>
/// Repositorio de niveles sobre dbo.NivelesIncidencia (Dapper).
/// </summary>
/// <remarks>
/// Creado: 2026-10-09
/// Modificado: 2026-10-09 - Versión inicial (RF-002)
/// </remarks>
internal sealed class NivelIncidenciaRepository(EjecutorSql sql) : INivelIncidenciaRepository
{
    private const string ConsultaDtos = """
        SELECT n.Id, n.Numero, n.Nombre, n.Descripcion,
               (SELECT COUNT(*) FROM dbo.TiposIncidencia t WHERE t.NivelId = n.Id) AS CantidadTipos,
               n.FechaCreacion, n.FechaModificacion
        FROM dbo.NivelesIncidencia n
        """;

    /// <inheritdoc />
    public async Task<NivelIncidencia?> ObtenerPorIdAsync(int id, CancellationToken ct)
    {
        var fila = await sql.ConsultarUnoAsync<FilaNivel>(
            "SELECT Id, Numero, Nombre, Descripcion, FechaCreacion, FechaModificacion "
            + "FROM dbo.NivelesIncidencia WHERE Id = @Id;", new { Id = id }, ct);
        return fila?.AEntidad();
    }

    /// <inheritdoc />
    public async Task<NivelIncidenciaDto?> ObtenerDtoAsync(int id, CancellationToken ct)
    {
        var fila = await sql.ConsultarUnoAsync<FilaNivelDto>($"{ConsultaDtos} WHERE n.Id = @Id;", new { Id = id }, ct);
        return fila?.ADto();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<NivelIncidenciaDto>> ListarAsync(CancellationToken ct)
    {
        var filas = await sql.ConsultarAsync<FilaNivelDto>($"{ConsultaDtos} ORDER BY n.Numero;", null, ct);
        return filas.Select(f => f.ADto()).ToList();
    }

    /// <inheritdoc />
    public Task<bool> ExisteNumeroAsync(int numero, CancellationToken ct) =>
        sql.ConsultarUnoAsync<bool>(
            "SELECT CASE WHEN EXISTS (SELECT 1 FROM dbo.NivelesIncidencia WHERE Numero = @Numero) THEN 1 ELSE 0 END;",
            new { Numero = numero }, ct);

    /// <inheritdoc />
    public Task<bool> ExisteNombreAsync(string nombre, int? excluirId, CancellationToken ct)
    {
        const string consulta = """
            SELECT CASE WHEN EXISTS (
                SELECT 1 FROM dbo.NivelesIncidencia
                WHERE Nombre = @Nombre AND (@ExcluirId IS NULL OR Id <> @ExcluirId)
            ) THEN 1 ELSE 0 END;
            """;
        return sql.ConsultarUnoAsync<bool>(consulta, new { Nombre = nombre, ExcluirId = excluirId }, ct);
    }

    /// <inheritdoc />
    public async Task AgregarAsync(NivelIncidencia nivel, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(nivel);
        const string comando = """
            INSERT INTO dbo.NivelesIncidencia (Numero, Nombre, Descripcion, FechaCreacion, FechaModificacion)
            OUTPUT INSERTED.Id
            VALUES (@Numero, @Nombre, @Descripcion, @FechaCreacion, @FechaModificacion);
            """;
        var id = await sql.EscalarAsync<int>(comando, new
        {
            nivel.Numero,
            nivel.Nombre,
            nivel.Descripcion,
            nivel.FechaCreacion,
            nivel.FechaModificacion,
        }, ct);
        nivel.AsignarId(id);
    }

    /// <inheritdoc />
    public async Task ActualizarAsync(NivelIncidencia nivel, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(nivel);
        const string comando = """
            UPDATE dbo.NivelesIncidencia
            SET Nombre = @Nombre, Descripcion = @Descripcion, FechaModificacion = @FechaModificacion
            WHERE Id = @Id;
            """;
        await sql.EjecutarAsync(comando,
            new { nivel.Id, nivel.Nombre, nivel.Descripcion, nivel.FechaModificacion }, ct);
    }

    /// <summary>
    /// Fila de dbo.NivelesIncidencia tal como la devuelve Dapper.
    /// </summary>
    /// <remarks>
    /// Creado: 2026-10-09
    /// Modificado: 2026-10-09 - Versión inicial (RF-002)
    /// </remarks>
    private sealed record FilaNivel(int Id, int Numero, string Nombre, string? Descripcion,
        DateTime FechaCreacion, DateTime? FechaModificacion)
    {
        public NivelIncidencia AEntidad() => NivelIncidencia.Reconstituir(Id, Numero, Nombre, Descripcion,
            SqlUtil.Utc(FechaCreacion), SqlUtil.Utc(FechaModificacion));
    }

    /// <summary>
    /// Fila del DTO de nivel con su cantidad de tipos.
    /// </summary>
    /// <remarks>
    /// Creado: 2026-10-09
    /// Modificado: 2026-10-09 - Versión inicial (RF-002)
    /// </remarks>
    private sealed record FilaNivelDto(int Id, int Numero, string Nombre, string? Descripcion, int CantidadTipos,
        DateTime FechaCreacion, DateTime? FechaModificacion)
    {
        public NivelIncidenciaDto ADto() => new(Id, Numero, Nombre, Descripcion, CantidadTipos,
            SqlUtil.Utc(FechaCreacion), SqlUtil.Utc(FechaModificacion));
    }
}
