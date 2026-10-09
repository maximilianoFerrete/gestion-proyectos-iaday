using System.Data.Common;
using GestionProyectos.Application.Abstracciones.Persistencia;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;

namespace GestionProyectos.Infrastructure.SqlServer.Conexion;

/// <summary>
/// Crea conexiones SqlClient abiertas a partir de OpcionesSqlServer.
/// </summary>
/// <remarks>
/// Creado: 2026-10-09
/// Modificado: 2026-10-09 - Versión inicial (RF-001)
/// </remarks>
internal sealed class SqlServerConnectionFactory(IOptions<OpcionesSqlServer> opciones) : IDbConnectionFactory
{
    /// <inheritdoc />
    public async Task<DbConnection> AbrirConexionAsync(CancellationToken ct)
    {
        var conexion = new SqlConnection(opciones.Value.CadenaConexion);
        try
        {
            await conexion.OpenAsync(ct);
            return conexion;
        }
        catch
        {
            await conexion.DisposeAsync();
            throw;
        }
    }
}
