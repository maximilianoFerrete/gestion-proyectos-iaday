using GestionProyectos.Api.Errores;
using GestionProyectos.Domain.Excepciones;
using Microsoft.AspNetCore.Authentication;

namespace GestionProyectos.Api.Seguridad;

/// <summary>
/// Corta con 401 USUARIO_INVALIDO cualquier request cuya autenticación falló (header inválido).
/// </summary>
/// <remarks>
/// Creado: 2026-10-09
/// Modificado: 2026-10-09 - Versión inicial (RF-001)
/// </remarks>
public sealed class RechazoUsuarioInvalidoMiddleware(RequestDelegate siguiente)
{
    /// <summary>Verifica el resultado de autenticación (cacheado por el handler) y continúa.</summary>
    public async Task InvokeAsync(HttpContext contexto, EscritorProblemas escritor)
    {
        ArgumentNullException.ThrowIfNull(contexto);
        var resultado = await contexto.AuthenticateAsync();
        if (resultado.Failure is not null)
        {
            await escritor.EscribirAsync(contexto, StatusCodes.Status401Unauthorized,
                CodigosError.UsuarioInvalido, resultado.Failure.Message);
            return;
        }

        await siguiente(contexto);
    }
}
