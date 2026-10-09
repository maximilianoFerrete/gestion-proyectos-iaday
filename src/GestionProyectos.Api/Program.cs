using GestionProyectos.Api.Configuracion;
using GestionProyectos.Api.Seguridad;
using GestionProyectos.Application;
using GestionProyectos.Infrastructure.SqlServer;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApiControllers();
builder.Services.AddManejoErrores();
builder.Services.AddAplicacion();
// Proveedor de datos: para cambiar de motor se reemplaza solo esta línea.
builder.Services.AddInfraestructuraSqlServer(builder.Configuration);
builder.Services.AddSeguridad(builder.Configuration);
builder.Services.AddCorsFrontend(builder.Configuration);
builder.Services.AddDocumentacionApi();

var app = builder.Build();

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    app.UseHttpsRedirection();
}

app.UseCors(ApiExtensions.PoliticaCorsFrontend);
app.UseAuthentication();
app.UseMiddleware<RechazoUsuarioInvalidoMiddleware>();
app.UseAuthorization();

app.MapControllers();

await app.RunAsync();

/// <summary>
/// Punto de entrada; parcial y público para pruebas con WebApplicationFactory.
/// </summary>
/// <remarks>
/// Creado: 2026-10-09
/// Modificado: 2026-10-09 - Versión inicial (RF-001)
/// </remarks>
public partial class Program;
