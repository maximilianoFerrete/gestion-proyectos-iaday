using System.Data.Common;
using GestionProyectos.Application.Abstracciones.Persistencia;

namespace GestionProyectos.Infrastructure.SqlServer.Conexion;

/// <summary>
/// Conexión y transacción compartidas por los repositorios de una misma solicitud (scoped).
/// </summary>
/// <remarks>
/// Creado: 2026-10-09
/// Modificado: 2026-10-09 - Versión inicial (RF-001)
/// </remarks>
internal interface ISesionBaseDatos
{
    /// <summary>Transacción en curso o null si no hay ninguna.</summary>
    DbTransaction? Transaccion { get; }

    /// <summary>Obtiene la conexión de la solicitud, abriéndola la primera vez.</summary>
    Task<DbConnection> ObtenerConexionAsync(CancellationToken ct);
}

/// <summary>
/// Implementa IUnitOfWork sobre una conexión SqlClient por solicitud.
/// </summary>
/// <remarks>
/// Creado: 2026-10-09
/// Modificado: 2026-10-09 - Versión inicial (RF-001)
/// </remarks>
internal sealed class SesionSqlServer(IDbConnectionFactory fabrica)
    : IUnitOfWork, ISesionBaseDatos, IAsyncDisposable
{
    private DbConnection? _conexion;
    private DbTransaction? _transaccion;

    /// <inheritdoc />
    public DbTransaction? Transaccion => _transaccion;

    /// <inheritdoc />
    public async Task<DbConnection> ObtenerConexionAsync(CancellationToken ct) =>
        _conexion ??= await fabrica.AbrirConexionAsync(ct);

    /// <inheritdoc />
    public async Task IniciarAsync(CancellationToken ct)
    {
        if (_transaccion is not null)
        {
            throw new InvalidOperationException("Ya hay una transacción en curso.");
        }

        var conexion = await ObtenerConexionAsync(ct);
        _transaccion = await conexion.BeginTransactionAsync(ct);
    }

    /// <inheritdoc />
    public async Task ConfirmarAsync(CancellationToken ct)
    {
        var transaccion = _transaccion
            ?? throw new InvalidOperationException("No hay una transacción en curso.");
        await transaccion.CommitAsync(ct);
        await LiberarTransaccionAsync();
    }

    /// <inheritdoc />
    public async Task RevertirAsync(CancellationToken ct)
    {
        if (_transaccion is null)
        {
            return;
        }

        await _transaccion.RollbackAsync(ct);
        await LiberarTransaccionAsync();
    }

    /// <summary>Libera la transacción pendiente (se revierte) y la conexión.</summary>
    public async ValueTask DisposeAsync()
    {
        await LiberarTransaccionAsync();
        if (_conexion is not null)
        {
            await _conexion.DisposeAsync();
            _conexion = null;
        }
    }

    private async Task LiberarTransaccionAsync()
    {
        if (_transaccion is not null)
        {
            await _transaccion.DisposeAsync();
            _transaccion = null;
        }
    }
}
