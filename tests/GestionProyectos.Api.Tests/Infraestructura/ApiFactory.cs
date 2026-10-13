using GestionProyectos.Application.Abstracciones.Persistencia;
using GestionProyectos.Application.Agrupaciones;
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
using GestionProyectos.Domain.Entidades;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace GestionProyectos.Api.Tests.Infraestructura;

/// <summary>
/// Host de pruebas de la API: servicios de negocio y repositorios reemplazados por sustitutos.
/// Ninguna dependencia abre conexiones a la base de datos.
/// </summary>
/// <remarks>
/// Creado: 2026-10-09
/// Modificado: 2026-10-09 - Versión inicial (tests RF-001)
/// Modificado: 2026-10-10 - Sustitutos de servicios y repositorios de incidencias (tests RF-002)
/// Modificado: 2026-10-11 - Sustitutos de seguimiento y configuración del tablero (tests RF-005)
/// </remarks>
public sealed class ApiFactory : WebApplicationFactory<Program>
{
    public const string Header = "X-Usuario-Id";
    public const int IdAdmin = 1;
    public const int IdNoAdmin = 2;
    public const int IdInactivo = 5;

    public IProyectoService Proyectos { get; } = Substitute.For<IProyectoService>();
    public IAgrupacionService Agrupaciones { get; } = Substitute.For<IAgrupacionService>();
    public IPersonaService Personas { get; } = Substitute.For<IPersonaService>();
    public IEstadoProyectoService Estados { get; } = Substitute.For<IEstadoProyectoService>();
    public IIncidenciaService Incidencias { get; } = Substitute.For<IIncidenciaService>();
    public IIncidenciaConsultaService ConsultasIncidencias { get; } = Substitute.For<IIncidenciaConsultaService>();
    public INivelIncidenciaService Niveles { get; } = Substitute.For<INivelIncidenciaService>();
    public ITipoIncidenciaService Tipos { get; } = Substitute.For<ITipoIncidenciaService>();
    public IRelacionTipoIncidenciaService Relaciones { get; } = Substitute.For<IRelacionTipoIncidenciaService>();
    public IEstadoIncidenciaService EstadosIncidencia { get; } = Substitute.For<IEstadoIncidenciaService>();
    public IFlujoIncidenciaService Flujos { get; } = Substitute.For<IFlujoIncidenciaService>();
    public IPrioridadIncidenciaService Prioridades { get; } = Substitute.For<IPrioridadIncidenciaService>();
    public ISeguimientoService Seguimiento { get; } = Substitute.For<ISeguimientoService>();
    public IConfiguracionTableroService ConfiguracionTablero { get; } = Substitute.For<IConfiguracionTableroService>();

    /// <summary>Repositorio de personas usado por la identificación del usuario actual.</summary>
    public IPersonaRepository RepoPersonas { get; } = Substitute.For<IPersonaRepository>();

    /// <summary>Configura las personas conocidas por el esquema de autenticación provisional.</summary>
    public ApiFactory()
    {
        var creada = new DateTime(2026, 10, 1, 10, 0, 0, DateTimeKind.Utc);
        RepoPersonas.ObtenerPorIdAsync(IdAdmin, Arg.Any<CancellationToken>()).Returns(
            Persona.Reconstituir(IdAdmin, "Administrador", "admin@gestionproyectos.local", true, true, creada, null));
        RepoPersonas.ObtenerPorIdAsync(IdNoAdmin, Arg.Any<CancellationToken>()).Returns(
            Persona.Reconstituir(IdNoAdmin, "Ana Gómez", "ana.gomez@empresa.com", false, true, creada, null));
        RepoPersonas.ObtenerPorIdAsync(IdInactivo, Arg.Any<CancellationToken>()).Returns(
            Persona.Reconstituir(IdInactivo, "Inactiva", "inactiva@empresa.com", false, false, creada, null));
    }

    /// <summary>Crea un cliente con el header X-Usuario-Id indicado (o sin header).</summary>
    public HttpClient Cliente(string? usuarioId = null)
    {
        var cliente = CreateClient();
        if (usuarioId is not null)
        {
            cliente.DefaultRequestHeaders.Add(Header, usuarioId);
        }

        return cliente;
    }

    /// <inheritdoc />
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.UseSetting("Seguridad:ModoProvisional", "true");
        builder.UseSetting("ConnectionStrings:Principal", "Server=sin-bd-en-tests;Database=NoUsar");
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<IDbConnectionFactory>();
            services.AddSingleton(SinBaseDeDatos());
            Reemplazar(services, RepoPersonas);
            Reemplazar(services, Substitute.For<IEstadoProyectoRepository>());
            Reemplazar(services, Substitute.For<IProyectoRepository>());
            Reemplazar(services, Substitute.For<IAgrupacionRepository>());
            Reemplazar(services, Substitute.For<IUnitOfWork>());
            Reemplazar(services, Proyectos);
            Reemplazar(services, Agrupaciones);
            Reemplazar(services, Personas);
            Reemplazar(services, Estados);
            Reemplazar(services, Substitute.For<IIncidenciaRepository>());
            Reemplazar(services, Substitute.For<INivelIncidenciaRepository>());
            Reemplazar(services, Substitute.For<ITipoIncidenciaRepository>());
            Reemplazar(services, Substitute.For<IRelacionTipoIncidenciaRepository>());
            Reemplazar(services, Substitute.For<IEstadoIncidenciaRepository>());
            Reemplazar(services, Substitute.For<IFlujoIncidenciaRepository>());
            Reemplazar(services, Substitute.For<IPrioridadIncidenciaRepository>());
            Reemplazar(services, Incidencias);
            Reemplazar(services, ConsultasIncidencias);
            Reemplazar(services, Niveles);
            Reemplazar(services, Tipos);
            Reemplazar(services, Relaciones);
            Reemplazar(services, EstadosIncidencia);
            Reemplazar(services, Flujos);
            Reemplazar(services, Prioridades);
            Reemplazar(services, Substitute.For<ISeguimientoRepository>());
            Reemplazar(services, Substitute.For<IConfiguracionTableroRepository>());
            Reemplazar(services, Seguimiento);
            Reemplazar(services, ConfiguracionTablero);
        });
    }

    private static void Reemplazar<T>(IServiceCollection services, T instancia) where T : class
    {
        services.RemoveAll<T>();
        services.AddSingleton(instancia);
    }

    private static IDbConnectionFactory SinBaseDeDatos()
    {
        var fabrica = Substitute.For<IDbConnectionFactory>();
        fabrica.AbrirConexionAsync(Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("Los tests unitarios no deben abrir la BD."));
        return fabrica;
    }
}
