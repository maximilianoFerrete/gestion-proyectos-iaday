using GestionProyectos.Application.Abstracciones.Persistencia;
using GestionProyectos.Application.Comun;
using GestionProyectos.Application.RelacionesTiposIncidencia;
using GestionProyectos.Domain.Entidades;
using GestionProyectos.Infrastructure.SqlServer.Conexion;

namespace GestionProyectos.Infrastructure.SqlServer.Repositorios;

/// <summary>
/// Repositorio de relaciones permitidas sobre dbo.RelacionesTiposIncidencia (solo INSERT y SELECT).
/// </summary>
/// <remarks>
/// Creado: 2026-10-09
/// Modificado: 2026-10-09 - Versión inicial (RF-002)
/// </remarks>
internal sealed class RelacionTipoIncidenciaRepository(EjecutorSql sql) : IRelacionTipoIncidenciaRepository
{
    private const string ConsultaDtos = """
        SELECT r.Id, r.FechaCreacion,
               tp.Id AS PadreId, tp.Nombre AS PadreNombre, tp.Activo AS PadreActivo, np.Numero AS PadreNivelNumero,
               th.Id AS HijoId, th.Nombre AS HijoNombre, th.Activo AS HijoActivo, nh.Numero AS HijoNivelNumero
        FROM dbo.RelacionesTiposIncidencia r
        INNER JOIN dbo.TiposIncidencia tp ON tp.Id = r.TipoPadreId
        INNER JOIN dbo.NivelesIncidencia np ON np.Id = tp.NivelId
        INNER JOIN dbo.TiposIncidencia th ON th.Id = r.TipoHijoId
        INNER JOIN dbo.NivelesIncidencia nh ON nh.Id = th.NivelId
        """;

    /// <inheritdoc />
    public async Task<IReadOnlyList<RelacionTipoIncidenciaDto>> ListarAsync(int? tipoPadreId, int? tipoHijoId,
        CancellationToken ct)
    {
        const string consulta = $"""
            {ConsultaDtos}
            WHERE (@TipoPadreId IS NULL OR r.TipoPadreId = @TipoPadreId)
              AND (@TipoHijoId IS NULL OR r.TipoHijoId = @TipoHijoId)
            ORDER BY np.Numero, tp.Nombre, th.Nombre, r.Id;
            """;
        var filas = await sql.ConsultarAsync<FilaRelacion>(consulta,
            new { TipoPadreId = tipoPadreId, TipoHijoId = tipoHijoId }, ct);
        return filas.Select(f => f.ADto()).ToList();
    }

    /// <inheritdoc />
    public async Task<RelacionTipoIncidenciaDto?> ObtenerDtoAsync(int id, CancellationToken ct)
    {
        var fila = await sql.ConsultarUnoAsync<FilaRelacion>($"{ConsultaDtos} WHERE r.Id = @Id;", new { Id = id }, ct);
        return fila?.ADto();
    }

    /// <inheritdoc />
    public Task<bool> ExisteAsync(int tipoPadreId, int tipoHijoId, CancellationToken ct)
    {
        const string consulta = """
            SELECT CASE WHEN EXISTS (
                SELECT 1 FROM dbo.RelacionesTiposIncidencia
                WHERE TipoPadreId = @TipoPadreId AND TipoHijoId = @TipoHijoId
            ) THEN 1 ELSE 0 END;
            """;
        return sql.ConsultarUnoAsync<bool>(consulta, new { TipoPadreId = tipoPadreId, TipoHijoId = tipoHijoId }, ct);
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<int>> ListarTiposHijoAsync(int tipoPadreId, CancellationToken ct) =>
        sql.ConsultarAsync<int>(
            "SELECT TipoHijoId FROM dbo.RelacionesTiposIncidencia WHERE TipoPadreId = @TipoPadreId;",
            new { TipoPadreId = tipoPadreId }, ct);

    /// <inheritdoc />
    public async Task AgregarAsync(RelacionTipoIncidencia relacion, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(relacion);
        const string comando = """
            INSERT INTO dbo.RelacionesTiposIncidencia (TipoPadreId, TipoHijoId, FechaCreacion)
            OUTPUT INSERTED.Id
            VALUES (@TipoPadreId, @TipoHijoId, @FechaCreacion);
            """;
        var id = await sql.EscalarAsync<int>(comando,
            new { relacion.TipoPadreId, relacion.TipoHijoId, relacion.FechaCreacion }, ct);
        relacion.AsignarId(id);
    }

    /// <summary>
    /// Fila plana del DTO de relación con ambos tipos.
    /// </summary>
    /// <remarks>
    /// Creado: 2026-10-09
    /// Modificado: 2026-10-09 - Versión inicial (RF-002)
    /// </remarks>
    private sealed class FilaRelacion
    {
        public int Id { get; init; }
        public DateTime FechaCreacion { get; init; }
        public int PadreId { get; init; }
        public string PadreNombre { get; init; } = string.Empty;
        public bool PadreActivo { get; init; }
        public int PadreNivelNumero { get; init; }
        public int HijoId { get; init; }
        public string HijoNombre { get; init; } = string.Empty;
        public bool HijoActivo { get; init; }
        public int HijoNivelNumero { get; init; }

        public RelacionTipoIncidenciaDto ADto() => new(Id,
            new TipoReferenciaDto(PadreId, PadreNombre, PadreActivo, PadreNivelNumero),
            new TipoReferenciaDto(HijoId, HijoNombre, HijoActivo, HijoNivelNumero),
            SqlUtil.Utc(FechaCreacion));
    }
}
