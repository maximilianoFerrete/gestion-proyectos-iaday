using GestionProyectos.Application.Abstracciones.Persistencia;
using GestionProyectos.Application.Comun;
using GestionProyectos.Application.Seguimiento;
using GestionProyectos.Application.Tests.Comun;
using NSubstitute;
using static GestionProyectos.Application.Tests.Comun.DatosSeguimiento;

namespace GestionProyectos.Application.Tests.Seguimiento;

/// <summary>
/// Escenario común de SeguimientoService: repositorios sustituidos con el catálogo de DatosSeguimiento
/// y CatalogoSeguimiento/AmbitoSeguimiento reales, para probar las reglas sin base de datos.
/// Proyecto 1 (PRJ-0001) con agrupaciones Envíos (9) y Pagos (8); agrupación 20 de otro proyecto.
/// </summary>
/// <remarks>
/// Creado: 2026-10-11
/// Modificado: 2026-10-11 - Versión inicial (tests RF-005)
/// </remarks>
public abstract class ContextoSeguimientoService
{
    protected const int ProyectoId = 1;
    protected const int OtroProyectoId = 4;
    protected const int AgrupacionPagos = 8;
    protected const int AgrupacionEnvios = 9;
    protected const int AgrupacionOtroProyecto = 20;

    protected readonly ISeguimientoRepository Seguimiento = Substitute.For<ISeguimientoRepository>();
    protected readonly INivelIncidenciaRepository Niveles = Substitute.For<INivelIncidenciaRepository>();
    protected readonly ITipoIncidenciaRepository Tipos = Substitute.For<ITipoIncidenciaRepository>();
    protected readonly IProyectoRepository Proyectos = Substitute.For<IProyectoRepository>();
    protected readonly IAgrupacionRepository Agrupaciones = Substitute.For<IAgrupacionRepository>();
    protected readonly IEstadoIncidenciaRepository EstadosRepo = Substitute.For<IEstadoIncidenciaRepository>();
    protected readonly IConfiguracionTableroRepository Configuraciones = Substitute.For<IConfiguracionTableroRepository>();
    protected readonly IFechaReferencia FechaReferencia = Substitute.For<IFechaReferencia>();

    protected ContextoSeguimientoService()
    {
        FechaReferencia.Hoy().Returns(Hoy);
        Niveles.ListarAsync(Arg.Any<CancellationToken>()).Returns(NivelesDto());
        Tipos.ListarAsync(false, null, Arg.Any<CancellationToken>()).Returns(TiposDto());
        Seguimiento.ListarEstadosAsync(Arg.Any<CancellationToken>()).Returns(Estados());
        Seguimiento.ListarEstadosDeFlujosAsync(Arg.Any<IReadOnlyList<int>?>(), Arg.Any<CancellationToken>())
            .Returns(EstadosFlujoGeneral());
        Seguimiento.ContarAsync(Arg.Any<FiltroConteoIncidencias>(), Arg.Any<AgrupacionConteo>(),
            Arg.Any<CancellationToken>()).Returns([]);
        Seguimiento.ListarProyectosAsync(Arg.Any<FiltroProyectosSeguimiento>(), Arg.Any<CancellationToken>())
            .Returns([]);
        Seguimiento.ListarIncidenciasTableroAsync(Arg.Any<FiltroTableroIncidencias>(), Arg.Any<CancellationToken>())
            .Returns([]);
        ExisteProyecto(archivado: false);
        Agrupaciones.ObtenerPorIdAsync(AgrupacionPagos, Arg.Any<CancellationToken>())
            .Returns(DatosPrueba.Agrupacion(AgrupacionPagos, ProyectoId, "Pagos"));
        Agrupaciones.ObtenerPorIdAsync(AgrupacionEnvios, Arg.Any<CancellationToken>())
            .Returns(DatosPrueba.Agrupacion(AgrupacionEnvios, ProyectoId, "Envíos"));
        Agrupaciones.ObtenerPorIdAsync(AgrupacionOtroProyecto, Arg.Any<CancellationToken>())
            .Returns(DatosPrueba.Agrupacion(AgrupacionOtroProyecto, OtroProyectoId));
        Agrupaciones.ListarPorProyectoAsync(ProyectoId, Arg.Any<CancellationToken>())
            .Returns([DatosPrueba.Agrupacion(AgrupacionEnvios, ProyectoId, "Envíos"),
                DatosPrueba.Agrupacion(AgrupacionPagos, ProyectoId, "Pagos")]);
        foreach (var estado in Estados())
        {
            EstadosRepo.ObtenerPorIdAsync(estado.Id, Arg.Any<CancellationToken>()).Returns(estado);
        }
    }

    /// <summary>Crea el servicio con catálogo y ámbito reales sobre los repositorios sustituidos.</summary>
    protected SeguimientoService Sut() =>
        new(Seguimiento, new CatalogoSeguimiento(Niveles, Tipos, Seguimiento),
            new AmbitoSeguimiento(Proyectos, Agrupaciones), EstadosRepo, Configuraciones, FechaReferencia,
            new SeguimientoValidadores(new ConsultaTableroProyectosQueryValidator(),
                new ConsultaSeguimientoQueryValidator(), new ConsultaTableroIncidenciasQueryValidator()));

    /// <summary>Registra el proyecto 1, archivado o no.</summary>
    protected void ExisteProyecto(bool archivado) =>
        Proyectos.ObtenerPorIdAsync(ProyectoId, Arg.Any<CancellationToken>())
            .Returns(DatosPrueba.Proyecto(ProyectoId, archivado: archivado, codigo: "PRJ-0001"));

    /// <summary>Configura los conteos crudos que devuelve el repositorio.</summary>
    protected void ConConteos(params ConteoIncidencias[] conteos) =>
        Seguimiento.ContarAsync(Arg.Any<FiltroConteoIncidencias>(), Arg.Any<AgrupacionConteo>(),
            Arg.Any<CancellationToken>()).Returns(conteos);
}
