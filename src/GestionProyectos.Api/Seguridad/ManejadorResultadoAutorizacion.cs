using GestionProyectos.Api.Errores;
using GestionProyectos.Domain.Excepciones;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Microsoft.AspNetCore.Authorization.Policy;

namespace GestionProyectos.Api.Seguridad;

/// <summary>
/// Responde 401 USUARIO_NO_IDENTIFICADO o 403 PERMISO_DENEGADO cuando falla una política.
/// </summary>
/// <remarks>
/// Creado: 2026-10-09
/// Modificado: 2026-10-09 - Versión inicial (RF-001)
/// Modificado: 2026-10-09 - Mensaje del 403 según el permiso exigido por la política (RF-002, 0.1)
/// </remarks>
public sealed class ManejadorResultadoAutorizacion(EscritorProblemas escritor)
    : IAuthorizationMiddlewareResultHandler
{
    private readonly AuthorizationMiddlewareResultHandler _porDefecto = new();

    /// <inheritdoc />
    public Task HandleAsync(RequestDelegate next, HttpContext context, AuthorizationPolicy policy,
        PolicyAuthorizationResult authorizeResult)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(authorizeResult);
        var identificado = context.User.Identity?.IsAuthenticated == true;

        if (authorizeResult.Challenged || (authorizeResult.Forbidden && !identificado))
        {
            return escritor.EscribirAsync(context, StatusCodes.Status401Unauthorized,
                CodigosError.UsuarioNoIdentificado, "La operación requiere un usuario identificado.");
        }

        if (authorizeResult.Forbidden)
        {
            return escritor.EscribirAsync(context, StatusCodes.Status403Forbidden,
                CodigosError.PermisoDenegado, MensajePermisoDenegado(policy));
        }

        return _porDefecto.HandleAsync(next, context, policy, authorizeResult);
    }

    /// <summary>Mensaje del 403: menciona el permiso si la política exige el claim "permiso".</summary>
    public static string MensajePermisoDenegado(AuthorizationPolicy? policy)
    {
        var permiso = policy?.Requirements
            .OfType<ClaimsAuthorizationRequirement>()
            .Where(r => r.ClaimType == EsquemaProvisional.ClaimPermiso)
            .SelectMany(r => r.AllowedValues ?? [])
            .FirstOrDefault();
        return permiso is null
            ? new PermisoDenegadoException().Message
            : new PermisoDenegadoException(permiso).Message;
    }
}
