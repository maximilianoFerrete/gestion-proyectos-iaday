using GestionProyectos.Application.Abstracciones.Persistencia;
using GestionProyectos.Infrastructure.SqlServer.Conexion;
using GestionProyectos.Infrastructure.SqlServer.Repositorios;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace GestionProyectos.Infrastructure.SqlServer;

/// <summary>
/// Registro en DI del proveedor de datos SQL Server (reemplazable por otro motor).
/// </summary>
/// <remarks>
/// Creado: 2026-10-09
/// Modificado: 2026-10-09 - Versión inicial (RF-001)
/// Modificado: 2026-10-09 - Repositorios de incidencias y su configuración (RF-002)
/// </remarks>
public static class DependencyInjection
{
    /// <summary>Registra conexión, unidad de trabajo y repositorios Dapper de SQL Server.</summary>
    public static IServiceCollection AddInfraestructuraSqlServer(this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddOptions<OpcionesSqlServer>()
            .Configure(o => o.CadenaConexion =
                configuration.GetConnectionString(OpcionesSqlServer.NombreCadenaConexion) ?? string.Empty)
            .Validate(o => !string.IsNullOrWhiteSpace(o.CadenaConexion),
                $"Falta la cadena de conexión ConnectionStrings:{OpcionesSqlServer.NombreCadenaConexion}.")
            .ValidateOnStart();

        services.AddSingleton<IDbConnectionFactory, SqlServerConnectionFactory>();
        services.AddScoped<SesionSqlServer>();
        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<SesionSqlServer>());
        services.AddScoped<ISesionBaseDatos>(sp => sp.GetRequiredService<SesionSqlServer>());
        services.AddScoped<EjecutorSql>();

        services.AddScoped<IPersonaRepository, PersonaRepository>();
        services.AddScoped<IEstadoProyectoRepository, EstadoProyectoRepository>();
        services.AddScoped<IProyectoRepository, ProyectoRepository>();
        services.AddScoped<IAgrupacionRepository, AgrupacionRepository>();
        services.AddScoped<INivelIncidenciaRepository, NivelIncidenciaRepository>();
        services.AddScoped<ITipoIncidenciaRepository, TipoIncidenciaRepository>();
        services.AddScoped<IRelacionTipoIncidenciaRepository, RelacionTipoIncidenciaRepository>();
        services.AddScoped<IEstadoIncidenciaRepository, EstadoIncidenciaRepository>();
        services.AddScoped<IFlujoIncidenciaRepository, FlujoIncidenciaRepository>();
        services.AddScoped<IPrioridadIncidenciaRepository, PrioridadIncidenciaRepository>();
        services.AddScoped<IIncidenciaRepository, IncidenciaRepository>();
        return services;
    }
}
