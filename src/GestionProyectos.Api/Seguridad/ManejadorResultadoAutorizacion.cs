using GestionProyectos.Api.Errores;
using GestionProyectos.Domain.Excepciones;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;

namespace GestionProyectos.Api.Seguridad;

/// <summary>
/// Responde 401 USUARIO_NO_IDENTIFICADO o 403 PERMISO_DENEGADO cuando falla una política.
/// </summary>
/// <remarks>
/// Creado: 2026-10-09
/// Modificado: 2026-10-09 - Versión inicial (RF-001)
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
                CodigosError.PermisoDenegado, "La operación requiere un usuario administrador.");
        }

        return _porDefecto.HandleAsync(next, context, policy, authorizeResult);
    }
}
