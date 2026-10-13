using FluentValidation;
using GestionProyectos.Application.Abstracciones;
using GestionProyectos.Application.Agrupaciones;
using GestionProyectos.Application.Comun;
using GestionProyectos.Application.EstadosIncidencia;
using GestionProyectos.Application.EstadosProyecto;
using GestionProyectos.Application.FlujosIncidencia;
using GestionProyectos.Application.Incidencias;
using GestionProyectos.Application.NivelesIncidencia;
using GestionProyectos.Application.Personas;
using GestionProyectos.Application.PrioridadesIncidencia;
using GestionProyectos.Application.Proyectos;
using GestionProyectos.Application.RelacionesTiposIncidencia;
using GestionProyectos.Application.Seguimiento;
using GestionProyectos.Application.TiposIncidencia;
using GestionProyectos.Application.UsuarioActual;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace GestionProyectos.Application;

/// <summary>
/// Registro en DI de los servicios y validadores de la capa de aplicación.
/// </summary>
/// <remarks>
/// Creado: 2026-10-09
/// Modificado: 2026-10-09 - Versión inicial (RF-001)
/// Modificado: 2026-10-09 - Registra permisos, configuración e incidencias (RF-002)
/// Modificado: 2026-10-10 - Registra fecha de referencia y seguimiento (RF-005)
/// </remarks>
public static class DependencyInjection
{
    /// <summary>Registra servicios de aplicación, validadores, TimeProvider y fecha de referencia.</summary>
    public static IServiceCollection AddAplicacion(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.TryAddSingleton(TimeProvider.System);
        services.TryAddEnumerable(
            ServiceDescriptor.Singleton<IValidateOptions<OpcionesFechaReferencia>, ValidadorOpcionesFechaReferencia>());
        services.TryAddSingleton<IFechaReferencia, FechaReferenciaServidor>();
        services.AddValidatorsFromAssemblyContaining<PersonaRequestValidator>(includeInternalTypes: false);

        services.AddScoped<ProyectoValidadores>();
        services.TryAddSingleton<IResolutorPermisos, ResolutorPermisosPorAdministrador>();
        services.AddScoped<IUsuarioActualService, UsuarioActualService>();
        services.AddScoped<IIdentificacionUsuarioService, IdentificacionUsuarioService>();
        services.AddScoped<IPersonaService, PersonaService>();
        services.AddScoped<IEstadoProyectoService, EstadoProyectoService>();
        services.AddScoped<IProyectoService, ProyectoService>();
        services.AddScoped<IAgrupacionService, AgrupacionService>();

        services.AddScoped<TipoIncidenciaValidadores>();
        services.AddScoped<IncidenciaValidadores>();
        services.AddScoped<INivelIncidenciaService, NivelIncidenciaService>();
        services.AddScoped<ITipoIncidenciaService, TipoIncidenciaService>();
        services.AddScoped<IRelacionTipoIncidenciaService, RelacionTipoIncidenciaService>();
        services.AddScoped<IEstadoIncidenciaService, EstadoIncidenciaService>();
        services.AddScoped<IFlujoIncidenciaService, FlujoIncidenciaService>();
        services.AddScoped<IPrioridadIncidenciaService, PrioridadIncidenciaService>();
        services.AddScoped<IReferenciasIncidencia, ReferenciasIncidencia>();
        services.AddScoped<IIncidenciaConsultaService, IncidenciaConsultaService>();
        services.AddScoped<IIncidenciaService, IncidenciaService>();

        services.AddScoped<SeguimientoValidadores>();
        services.AddScoped<ICatalogoSeguimiento, CatalogoSeguimiento>();
        services.AddScoped<IAmbitoSeguimiento, AmbitoSeguimiento>();
        services.AddScoped<ISeguimientoService, SeguimientoService>();
        services.AddScoped<IConfiguracionTableroService, ConfiguracionTableroService>();
        return services;
    }
}
