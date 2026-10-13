using GestionProyectos.Application.Comun;

namespace GestionProyectos.Api.Configuracion;

/// <summary>
/// Enlaza la sección FechaReferencia y valida la zona horaria al arrancar (RF-005 0.1).
/// </summary>
/// <remarks>
/// Creado: 2026-10-10
/// Modificado: 2026-10-10 - Versión inicial (RF-005)
/// </remarks>
public static class FechaReferenciaExtensions
{
    /// <summary>Registra OpcionesFechaReferencia con ValidateOnStart (zona inválida = no arranca).</summary>
    public static IServiceCollection AddFechaReferencia(this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        services.AddOptions<OpcionesFechaReferencia>()
            .Bind(configuration.GetSection(OpcionesFechaReferencia.Seccion))
            .ValidateOnStart();
        return services;
    }
}
