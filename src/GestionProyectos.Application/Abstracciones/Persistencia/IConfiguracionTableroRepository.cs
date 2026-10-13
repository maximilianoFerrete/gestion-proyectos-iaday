using GestionProyectos.Application.Seguimiento;

namespace GestionProyectos.Application.Abstracciones.Persistencia;

/// <summary>
/// Persistencia de la configuración de columnas del tablero por proyecto (RF-005 DA-15).
/// </summary>
/// <remarks>
/// Creado: 2026-10-10
/// Modificado: 2026-10-10 - Versión inicial (RF-005)
/// </remarks>
public interface IConfiguracionTableroRepository
{
    /// <summary>Obtiene la configuración guardada o null si el proyecto usa la de defecto.</summary>
    Task<ConfiguracionTablero?> ObtenerAsync(int proyectoId, CancellationToken ct);

    /// <summary>Crea o actualiza la configuración (llamar dentro de una transacción).</summary>
    Task GuardarAsync(ConfiguracionTablero configuracion, CancellationToken ct);
}
