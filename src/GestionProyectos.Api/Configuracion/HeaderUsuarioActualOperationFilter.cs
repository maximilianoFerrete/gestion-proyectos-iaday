using GestionProyectos.Api.Seguridad;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace GestionProyectos.Api.Configuracion;

/// <summary>
/// Documenta en Swagger el header opcional X-Usuario-Id en todas las operaciones.
/// </summary>
/// <remarks>
/// Creado: 2026-10-09
/// Modificado: 2026-10-09 - Versión inicial (RF-001)
/// </remarks>
public sealed class HeaderUsuarioActualOperationFilter : IOperationFilter
{
    /// <inheritdoc />
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        ArgumentNullException.ThrowIfNull(operation);
        operation.Parameters ??= new List<IOpenApiParameter>();
        operation.Parameters.Add(new OpenApiParameter
        {
            Name = EsquemaProvisional.Header,
            In = ParameterLocation.Header,
            Required = false,
            Description = "Id de la persona que actúa como usuario actual (mecanismo provisional).",
            Schema = new OpenApiSchema { Type = JsonSchemaType.Integer },
        });
    }
}
