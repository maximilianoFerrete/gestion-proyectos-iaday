namespace GestionProyectos.Infrastructure.SqlServer.Conexion;

/// <summary>
/// Opciones del proveedor SQL Server (cadena ConnectionStrings:Principal).
/// </summary>
/// <remarks>
/// Creado: 2026-10-09
/// Modificado: 2026-10-09 - Versión inicial (RF-001)
/// </remarks>
public sealed class OpcionesSqlServer
{
    /// <summary>Nombre de la cadena de conexión en la configuración.</summary>
    public const string NombreCadenaConexion = "Principal";

    /// <summary>Cadena de conexión a SQL Server.</summary>
    public string CadenaConexion { get; set; } = string.Empty;
}
