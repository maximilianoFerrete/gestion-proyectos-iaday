using FluentValidation;
using GestionProyectos.Application.Agrupaciones;
using GestionProyectos.Application.EstadosProyecto;
using GestionProyectos.Application.Personas;
using GestionProyectos.Application.Proyectos;
using GestionProyectos.Application.UsuarioActual;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace GestionProyectos.Application;

/// <summary>
/// Registro en DI de los servicios y validadores de la capa de aplicación.
/// </summary>
/// <remarks>
/// Creado: 2026-10-09
/// Modificado: 2026-10-09 - Versión inicial (RF-001)
/// </remarks>
public static class DependencyInjection
{
    /// <summary>Registra servicios de aplicación, validadores y TimeProvider.</summary>
    public static IServiceCollection AddAplicacion(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.TryAddSingleton(TimeProvider.System);
        services.AddValidatorsFromAssemblyContaining<PersonaRequestValidator>(includeInternalTypes: false);

        services.AddScoped<ProyectoValidadores>();
        services.AddScoped<IUsuarioActualService, UsuarioActualService>();
        services.AddScoped<IIdentificacionUsuarioService, IdentificacionUsuarioService>();
        services.AddScoped<IPersonaService, PersonaService>();
        services.AddScoped<IEstadoProyectoService, EstadoProyectoService>();
        services.AddScoped<IProyectoService, ProyectoService>();
        services.AddScoped<IAgrupacionService, AgrupacionService>();
        return services;
    }
}
