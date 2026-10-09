using GestionProyectos.Application.Abstracciones;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;

namespace GestionProyectos.Api.Seguridad;

/// <summary>
/// Registro de autenticación, autorización y usuario actual. Para el login real (OIDC/JWT/cookies)
/// solo se reemplaza el esquema registrado aquí.
/// </summary>
/// <remarks>
/// Creado: 2026-10-09
/// Modificado: 2026-10-09 - Versión inicial (RF-001)
/// </remarks>
public static class SeguridadExtensions
{
    /// <summary>Registra el esquema (provisional o sin autenticación), la política y IUsuarioActual.</summary>
    public static IServiceCollection AddSeguridad(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        var opciones = configuration.GetSection(OpcionesSeguridad.Seccion).Get<OpcionesSeguridad>()
            ?? new OpcionesSeguridad();

        var autenticacion = services.AddAuthentication(EsquemaProvisional.Nombre);
        if (opciones.ModoProvisional)
        {
            autenticacion.AddScheme<AuthenticationSchemeOptions, ProvisionalAuthenticationHandler>(
                EsquemaProvisional.Nombre, null);
        }
        else
        {
            autenticacion.AddScheme<AuthenticationSchemeOptions, SinAutenticacionHandler>(
                EsquemaProvisional.Nombre, null);
        }

        services.AddAuthorizationBuilder()
            .AddPolicy(EsquemaProvisional.PoliticaSoloAdministradores, politica => politica
                .RequireAuthenticatedUser()
                .RequireRole(EsquemaProvisional.RolAdmin));

        services.AddSingleton<IAuthorizationMiddlewareResultHandler, ManejadorResultadoAutorizacion>();
        services.AddHttpContextAccessor();
        services.AddScoped<IUsuarioActual, UsuarioActualDesdeClaims>();
        return services;
    }
}
