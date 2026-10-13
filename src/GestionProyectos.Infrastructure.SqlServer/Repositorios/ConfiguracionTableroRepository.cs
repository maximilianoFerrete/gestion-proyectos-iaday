using GestionProyectos.Application.Abstracciones.Persistencia;
using GestionProyectos.Application.Seguimiento;
using GestionProyectos.Infrastructure.SqlServer.Conexion;

namespace GestionProyectos.Infrastructure.SqlServer.Repositorios;

/// <summary>
/// Repositorio de dbo.ConfiguracionesTableroProyecto; upsert con UPDATE y luego INSERT, sin MERGE (0.3).
/// </summary>
/// <remarks>
/// Creado: 2026-10-10
/// Modificado: 2026-10-10 - Versión inicial (RF-005)
/// </remarks>
internal sealed class ConfiguracionTableroRepository(EjecutorSql sql) : IConfiguracionTableroRepository
{
    /// <inheritdoc />
    public async Task<ConfiguracionTablero?> ObtenerAsync(int proyectoId, CancellationToken ct)
    {
        const string consulta = """
            SELECT ProyectoId, MostrarEstado, MostrarEstimacion, MostrarResponsable, FechaModificacion
            FROM dbo.ConfiguracionesTableroProyecto
            WHERE ProyectoId = @ProyectoId;
            """;
        var fila = await sql.ConsultarUnoAsync<FilaConfiguracion>(consulta, new { ProyectoId = proyectoId }, ct);
        return fila is null
            ? null
            : new ConfiguracionTablero(fila.ProyectoId, fila.MostrarEstado, fila.MostrarEstimacion,
                fila.MostrarResponsable, SqlUtil.Utc(fila.FechaModificacion));
    }

    /// <inheritdoc />
    public async Task GuardarAsync(ConfiguracionTablero configuracion, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(configuracion);
        // UPDLOCK + HOLDLOCK retiene el rango de la clave hasta el fin de la transacción: dos guardados
        // simultáneos del mismo proyecto no chocan en el INSERT (último en guardar gana).
        const string actualizar = """
            UPDATE dbo.ConfiguracionesTableroProyecto WITH (UPDLOCK, HOLDLOCK)
            SET MostrarEstado = @MostrarEstado, MostrarEstimacion = @MostrarEstimacion,
                MostrarResponsable = @MostrarResponsable, FechaModificacion = @FechaModificacion
            WHERE ProyectoId = @ProyectoId;
            """;
        const string insertar = """
            INSERT INTO dbo.ConfiguracionesTableroProyecto
                (ProyectoId, MostrarEstado, MostrarEstimacion, MostrarResponsable, FechaModificacion)
            VALUES (@ProyectoId, @MostrarEstado, @MostrarEstimacion, @MostrarResponsable, @FechaModificacion);
            """;
        var parametros = new
        {
            configuracion.ProyectoId,
            configuracion.MostrarEstado,
            configuracion.MostrarEstimacion,
            configuracion.MostrarResponsable,
            configuracion.FechaModificacion,
        };
        if (await sql.EjecutarAsync(actualizar, parametros, ct) == 0)
        {
            await sql.EjecutarAsync(insertar, parametros, ct);
        }
    }

    /// <summary>
    /// Fila de dbo.ConfiguracionesTableroProyecto.
    /// </summary>
    /// <remarks>
    /// Creado: 2026-10-10
    /// Modificado: 2026-10-10 - Versión inicial (RF-005)
    /// </remarks>
    private sealed class FilaConfiguracion
    {
        public int ProyectoId { get; init; }
        public bool MostrarEstado { get; init; }
        public bool MostrarEstimacion { get; init; }
        public bool MostrarResponsable { get; init; }
        public DateTime FechaModificacion { get; init; }
    }
}
