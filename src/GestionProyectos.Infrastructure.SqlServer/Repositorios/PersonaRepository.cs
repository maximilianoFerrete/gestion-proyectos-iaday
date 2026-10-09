using GestionProyectos.Application.Abstracciones.Persistencia;
using GestionProyectos.Domain.Entidades;
using GestionProyectos.Infrastructure.SqlServer.Conexion;

namespace GestionProyectos.Infrastructure.SqlServer.Repositorios;

/// <summary>
/// Repositorio de personas sobre dbo.Personas (Dapper).
/// </summary>
/// <remarks>
/// Creado: 2026-10-09
/// Modificado: 2026-10-09 - Versión inicial (RF-001)
/// </remarks>
internal sealed class PersonaRepository(EjecutorSql sql) : IPersonaRepository
{
    private const string Columnas =
        "Id, Nombre, Email, EsAdmin, Activo, FechaCreacion, FechaModificacion";

    /// <inheritdoc />
    public async Task<Persona?> ObtenerPorIdAsync(int id, CancellationToken ct)
    {
        var fila = await sql.ConsultarUnoAsync<FilaPersona>(
            $"SELECT {Columnas} FROM dbo.Personas WHERE Id = @Id;", new { Id = id }, ct);
        return fila?.AEntidad();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Persona>> ListarAsync(string? texto, bool incluirInactivas,
        CancellationToken ct)
    {
        const string consulta = $"""
            SELECT {Columnas}
            FROM dbo.Personas
            WHERE (@IncluirInactivas = 1 OR Activo = 1)
              AND (@Patron IS NULL OR Nombre LIKE @Patron ESCAPE '\' OR Email LIKE @Patron ESCAPE '\')
            ORDER BY Nombre, Id;
            """;
        var filas = await sql.ConsultarAsync<FilaPersona>(consulta,
            new { IncluirInactivas = incluirInactivas, Patron = SqlUtil.PatronContiene(texto) }, ct);
        return filas.Select(f => f.AEntidad()).ToList();
    }

    /// <inheritdoc />
    public async Task<bool> ExisteEmailAsync(string email, int? excluirId, CancellationToken ct)
    {
        const string consulta = """
            SELECT CASE WHEN EXISTS (
                SELECT 1 FROM dbo.Personas
                WHERE Email = @Email AND (@ExcluirId IS NULL OR Id <> @ExcluirId)
            ) THEN 1 ELSE 0 END;
            """;
        return await sql.ConsultarUnoAsync<bool>(consulta, new { Email = email, ExcluirId = excluirId }, ct);
    }

    /// <inheritdoc />
    public Task<int> ContarAdministradoresActivosAsync(CancellationToken ct) =>
        sql.ConsultarUnoAsync<int>(
            "SELECT COUNT(*) FROM dbo.Personas WHERE EsAdmin = 1 AND Activo = 1;", null, ct);

    /// <inheritdoc />
    public async Task AgregarAsync(Persona persona, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(persona);
        const string comando = """
            INSERT INTO dbo.Personas (Nombre, Email, EsAdmin, Activo, FechaCreacion, FechaModificacion)
            OUTPUT INSERTED.Id
            VALUES (@Nombre, @Email, @EsAdmin, @Activo, @FechaCreacion, @FechaModificacion);
            """;
        var id = await sql.EscalarAsync<int>(comando, Parametros(persona), ct);
        persona.AsignarId(id);
    }

    /// <inheritdoc />
    public async Task ActualizarAsync(Persona persona, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(persona);
        const string comando = """
            UPDATE dbo.Personas
            SET Nombre = @Nombre, Email = @Email, EsAdmin = @EsAdmin, Activo = @Activo,
                FechaModificacion = @FechaModificacion
            WHERE Id = @Id;
            """;
        await sql.EjecutarAsync(comando, Parametros(persona), ct);
    }

    private static object Parametros(Persona p) => new
    {
        p.Id,
        p.Nombre,
        p.Email,
        p.EsAdmin,
        p.Activo,
        p.FechaCreacion,
        p.FechaModificacion,
    };

    /// <summary>
    /// Fila de dbo.Personas tal como la devuelve Dapper.
    /// </summary>
    /// <remarks>
    /// Creado: 2026-10-09
    /// Modificado: 2026-10-09 - Versión inicial (RF-001)
    /// </remarks>
    private sealed record FilaPersona(int Id, string Nombre, string Email, bool EsAdmin, bool Activo,
        DateTime FechaCreacion, DateTime? FechaModificacion)
    {
        public Persona AEntidad() => Persona.Reconstituir(Id, Nombre, Email, EsAdmin, Activo,
            SqlUtil.Utc(FechaCreacion), SqlUtil.Utc(FechaModificacion));
    }
}
