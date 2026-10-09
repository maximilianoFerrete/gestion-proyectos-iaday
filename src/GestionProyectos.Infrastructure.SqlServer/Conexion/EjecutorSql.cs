using Dapper;
using Microsoft.Data.SqlClient;

namespace GestionProyectos.Infrastructure.SqlServer.Conexion;

/// <summary>
/// Ejecuta SQL parametrizado con Dapper sobre la sesión de la solicitud.
/// </summary>
/// <remarks>
/// Creado: 2026-10-09
/// Modificado: 2026-10-09 - Versión inicial (RF-001)
/// Modificado: 2026-10-09 - Traduce también violaciones de FK conocidas (RF-002)
/// </remarks>
internal sealed class EjecutorSql(ISesionBaseDatos sesion)
{
    /// <summary>Ejecuta una consulta y devuelve todas las filas.</summary>
    public async Task<IReadOnlyList<T>> ConsultarAsync<T>(string sql, object? parametros, CancellationToken ct)
    {
        var conexion = await sesion.ObtenerConexionAsync(ct);
        var filas = await conexion.QueryAsync<T>(Comando(sql, parametros, ct));
        return filas.AsList();
    }

    /// <summary>Ejecuta una consulta y devuelve la única fila o el valor por defecto.</summary>
    public async Task<T?> ConsultarUnoAsync<T>(string sql, object? parametros, CancellationToken ct)
    {
        var conexion = await sesion.ObtenerConexionAsync(ct);
        return await conexion.QuerySingleOrDefaultAsync<T>(Comando(sql, parametros, ct));
    }

    /// <summary>Ejecuta un lote con dos resultados: filas y un escalar (p. ej. página y total).</summary>
    public async Task<(IReadOnlyList<T> Filas, int Total)> ConsultarConTotalAsync<T>(string sql,
        object? parametros, CancellationToken ct)
    {
        var conexion = await sesion.ObtenerConexionAsync(ct);
        using var lector = await conexion.QueryMultipleAsync(Comando(sql, parametros, ct));
        var filas = (await lector.ReadAsync<T>()).AsList();
        var total = await lector.ReadSingleAsync<int>();
        return (filas, total);
    }

    /// <summary>Ejecuta un comando que devuelve un escalar, traduciendo errores conocidos.</summary>
    public async Task<T> EscalarAsync<T>(string sql, object? parametros, CancellationToken ct)
    {
        var conexion = await sesion.ObtenerConexionAsync(ct);
        try
        {
            return await conexion.ExecuteScalarAsync<T>(Comando(sql, parametros, ct))
                ?? throw new InvalidOperationException("El comando no devolvió un valor.");
        }
        catch (SqlException ex) when (TraductorErroresSql.EsConocido(ex))
        {
            throw TraductorErroresSql.Traducir(ex);
        }
    }

    /// <summary>Ejecuta un comando y devuelve las filas afectadas, traduciendo errores conocidos.</summary>
    public async Task<int> EjecutarAsync(string sql, object? parametros, CancellationToken ct)
    {
        var conexion = await sesion.ObtenerConexionAsync(ct);
        try
        {
            return await conexion.ExecuteAsync(Comando(sql, parametros, ct));
        }
        catch (SqlException ex) when (TraductorErroresSql.EsConocido(ex))
        {
            throw TraductorErroresSql.Traducir(ex);
        }
    }

    private CommandDefinition Comando(string sql, object? parametros, CancellationToken ct) =>
        new(sql, parametros, sesion.Transaccion, cancellationToken: ct);
}
