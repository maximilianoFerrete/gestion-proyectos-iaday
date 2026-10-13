namespace GestionProyectos.Application.Abstracciones;

/// <summary>
/// Único lugar de definición de los permisos; también son los nombres de las políticas (0.1).
/// </summary>
/// <remarks>
/// Creado: 2026-10-09
/// Modificado: 2026-10-09 - Versión inicial (RF-002)
/// Modificado: 2026-10-10 - Agrega Proyectos.ConfigurarTablero (RF-005, RN-31)
/// </remarks>
public static class Permisos
{
    /// <summary>Crear o editar la configuración de incidencias (RN-01).</summary>
    public const string ConfiguracionIncidencias = "Configuracion.Incidencias";

    /// <summary>Cambiar el tipo de una incidencia (RN-02).</summary>
    public const string IncidenciasCambiarTipo = "Incidencias.CambiarTipo";

    /// <summary>Modificar la configuración de columnas del tablero de un proyecto (RF-005 RN-31).</summary>
    public const string ProyectosConfigurarTablero = "Proyectos.ConfigurarTablero";

    /// <summary>Todos los permisos conocidos.</summary>
    public static IReadOnlyList<string> Todos { get; } =
        [ConfiguracionIncidencias, IncidenciasCambiarTipo, ProyectosConfigurarTablero];
}
