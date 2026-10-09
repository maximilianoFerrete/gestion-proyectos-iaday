using System.Data.Common;

namespace GestionProyectos.Application.Abstracciones.Persistencia;

/// <summary>
/// Crea conexiones abiertas al motor de base de datos configurado.
/// </summary>
/// <remarks>
/// Creado: 2026-10-09
/// Modificado: 2026-10-09 - Versión inicial (RF-001)
/// </remarks>
public interface IDbConnectionFactory
{
    /// <summary>Crea y abre una conexión nueva; el llamador la libera.</summary>
    Task<DbConnection> AbrirConexionAsync(CancellationToken ct);
}
