using GestionProyectos.Api.Errores;
using Microsoft.AspNetCore.Mvc;

namespace GestionProyectos.Api.Configuracion;

/// <summary>
/// Registro de MVC, ProblemDetails, CORS y Swagger de la API.
/// </summary>
/// <remarks>
/// Creado: 2026-10-09
/// Modificado: 2026-10-09 - Versión inicial (RF-001)
/// </remarks>
public static class ApiExtensions
{
    /// <summary>Nombre de la política CORS del frontend.</summary>
    public const string PoliticaCorsFrontend = "Frontend";

    /// <summary>Clave de configuración con los orígenes CORS permitidos.</summary>
    public const string ClaveOrigenesCors = "Cors:OrigenesPermitidos";

    /// <summary>Registra controllers, binding en español y la respuesta 400 VALIDACION.</summary>
    public static IServiceCollection AddApiControllers(this IServiceCollection services)
    {
        services
            .AddControllers(opciones =>
            {
                opciones.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true;
                ConfigurarMensajesBinding(opciones);
            })
            .ConfigureApiBehaviorOptions(opciones =>
                opciones.InvalidModelStateResponseFactory = RespuestaModeloInvalido.Crear);
        return services;
    }

    /// <summary>Registra ProblemDetails y el manejador global de excepciones.</summary>
    public static IServiceCollection AddManejoErrores(this IServiceCollection services)
    {
        services.AddProblemDetails();
        services.AddSingleton<FabricaProblemas>();
        services.AddSingleton<EscritorProblemas>();
        services.AddExceptionHandler<ManejadorExcepciones>();
        return services;
    }

    /// <summary>Registra la política CORS con los orígenes configurados (exponiendo Location).</summary>
    public static IServiceCollection AddCorsFrontend(this IServiceCollection services, IConfiguration configuration)
    {
        var origenes = configuration.GetSection(ClaveOrigenesCors).Get<string[]>() ?? [];
        services.AddCors(opciones => opciones.AddPolicy(PoliticaCorsFrontend, politica => politica
            .WithOrigins(origenes)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .WithExposedHeaders("Location")));
        return services;
    }

    /// <summary>Registra el generador de Swagger/OpenAPI.</summary>
    public static IServiceCollection AddDocumentacionApi(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(opciones => opciones.OperationFilter<HeaderUsuarioActualOperationFilter>());
        return services;
    }

    private static void ConfigurarMensajesBinding(MvcOptions opciones)
    {
        var mensajes = opciones.ModelBindingMessageProvider;
        mensajes.SetMissingRequestBodyRequiredValueAccessor(() => "El cuerpo de la solicitud es obligatorio.");
        mensajes.SetValueIsInvalidAccessor(valor => $"El valor '{valor}' no es válido.");
        mensajes.SetAttemptedValueIsInvalidAccessor((valor, _) => $"El valor '{valor}' no es válido.");
        mensajes.SetNonPropertyAttemptedValueIsInvalidAccessor(valor => $"El valor '{valor}' no es válido.");
        mensajes.SetValueMustBeANumberAccessor(_ => "El valor debe ser un número.");
        mensajes.SetValueMustNotBeNullAccessor(valor => $"El valor '{valor}' no es válido.");
        mensajes.SetMissingBindRequiredValueAccessor(campo => $"Falta el valor de {campo}.");
        mensajes.SetMissingKeyOrValueAccessor(() => "Falta un valor obligatorio.");
        mensajes.SetUnknownValueIsInvalidAccessor(campo => $"El valor indicado para {campo} no es válido.");
        mensajes.SetNonPropertyUnknownValueIsInvalidAccessor(() => "El valor indicado no es válido.");
        mensajes.SetNonPropertyValueMustBeANumberAccessor(() => "El valor debe ser un número.");
    }
}
