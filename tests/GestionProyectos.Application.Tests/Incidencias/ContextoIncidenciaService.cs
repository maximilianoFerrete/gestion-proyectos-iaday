using GestionProyectos.Application.Abstracciones;
using GestionProyectos.Application.Abstracciones.Persistencia;
using GestionProyectos.Application.Incidencias;
using GestionProyectos.Application.Tests.Comun;
using GestionProyectos.Domain.Entidades;
using GestionProyectos.Domain.Reglas;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using static GestionProyectos.Application.Tests.Comun.DatosIncidencias;

namespace GestionProyectos.Application.Tests.Incidencias;

/// <summary>
/// Escenario común de IncidenciaService: repositorios sustituidos con la configuración semilla (S003)
/// y ReferenciasIncidencia real, para probar las reglas de punta a punta sin base de datos.
/// </summary>
/// <remarks>
/// Creado: 2026-10-10
/// Modificado: 2026-10-10 - Versión inicial (tests RF-002)
/// </remarks>
public abstract class ContextoIncidenciaService
{
    protected const int ProyectoId = 1;
    protected const int OtroProyectoId = 4;
    protected const int Epica = 1;
    protected const int Hu = 2;
    protected const int Subtarea = 3;
    protected const int CasoPrueba = 4;
    protected const int Bug = 5;
    protected const int TipoInactivo = 6;
    protected const int Defecto = 7;
    protected const int HuAlternativa = 10;
    protected const int Media = 2;
    protected const int Alta = 3;
    protected const int PrioridadInactiva = 9;
    protected const int PersonaActiva = 2;
    protected const int PersonaInactiva = 5;
    protected const int AgrupacionPagos = 8;
    protected const int AgrupacionEnvios = 9;
    protected const int AgrupacionOtroProyecto = 20;
    protected const int NuevoId = 16;

    protected readonly IIncidenciaRepository Incidencias = Substitute.For<IIncidenciaRepository>();
    protected readonly ITipoIncidenciaRepository Tipos = Substitute.For<ITipoIncidenciaRepository>();
    protected readonly IFlujoIncidenciaRepository Flujos = Substitute.For<IFlujoIncidenciaRepository>();
    protected readonly IEstadoIncidenciaRepository Estados = Substitute.For<IEstadoIncidenciaRepository>();
    protected readonly IPrioridadIncidenciaRepository Prioridades = Substitute.For<IPrioridadIncidenciaRepository>();
    protected readonly IPersonaRepository Personas = Substitute.For<IPersonaRepository>();
    protected readonly IAgrupacionRepository Agrupaciones = Substitute.For<IAgrupacionRepository>();
    protected readonly IRelacionTipoIncidenciaRepository Relaciones = Substitute.For<IRelacionTipoIncidenciaRepository>();
    protected readonly IIncidenciaConsultaService Consultas = Substitute.For<IIncidenciaConsultaService>();
    protected readonly IUnitOfWork Uow = Substitute.For<IUnitOfWork>();

    /// <summary>Incidencia recibida por AgregarAsync (alta).</summary>
    protected Incidencia? Agregada { get; private set; }

    protected ContextoIncidenciaService()
    {
        ConfigurarProyecto(archivado: false);
        ConfigurarTipos();
        Flujos.ObtenerPorIdAsync(FlujoGeneral, Arg.Any<CancellationToken>()).Returns(_ => FlujoGeneralConEstados());
        Flujos.ObtenerPorIdAsync(FlujoBug, Arg.Any<CancellationToken>()).Returns(_ => FlujoBugConEstados());
        ConfigurarEstado(Estado(PorHacer, "Por hacer"));
        ConfigurarEstado(Estado(EnProgreso, "En progreso"));
        ConfigurarEstado(Estado(Hecho, "Hecho"));
        ConfigurarEstado(Estado(Nuevo, "Nuevo"));
        ConfigurarPrioridades(predeterminada: Media);
        Personas.ObtenerPorIdAsync(PersonaActiva, Arg.Any<CancellationToken>()).Returns(DatosPrueba.Persona(PersonaActiva));
        Personas.ObtenerPorIdAsync(PersonaInactiva, Arg.Any<CancellationToken>())
            .Returns(DatosPrueba.Persona(PersonaInactiva, activo: false));
        Agrupaciones.ObtenerPorIdAsync(AgrupacionPagos, Arg.Any<CancellationToken>())
            .Returns(DatosPrueba.Agrupacion(AgrupacionPagos, ProyectoId));
        Agrupaciones.ObtenerPorIdAsync(AgrupacionEnvios, Arg.Any<CancellationToken>())
            .Returns(DatosPrueba.Agrupacion(AgrupacionEnvios, ProyectoId, "Envíos"));
        Agrupaciones.ObtenerPorIdAsync(AgrupacionOtroProyecto, Arg.Any<CancellationToken>())
            .Returns(DatosPrueba.Agrupacion(AgrupacionOtroProyecto, OtroProyectoId));
        Incidencias.ListarHijasAsync(Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns([]);
        Incidencias.ObtenerIdsAncestrosAsync(Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns([]);
        Incidencias.When(r => r.AgregarAsync(Arg.Any<Incidencia>(), Arg.Any<CancellationToken>()))
            .Do(c =>
            {
                Agregada = c.Arg<Incidencia>();
                Agregada.AsignarId(NuevoId);
            });
        Consultas.ObtenerAsync(ProyectoId, Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(c => Dto(c.ArgAt<int>(1), ProyectoId));
    }

    /// <summary>Crea el servicio con el usuario indicado (por defecto con todos los permisos).</summary>
    protected IncidenciaService Sut(IUsuarioActual? usuario = null) =>
        new(Incidencias,
            new ReferenciasIncidencia(Tipos, Flujos, Estados, Prioridades, Personas, Agrupaciones, Relaciones,
                Incidencias),
            Consultas, Uow, usuario ?? Usuario(Permisos.Todos.ToArray()),
            new IncidenciaValidadores(new CrearIncidenciaRequestValidator(), new EditarIncidenciaRequestValidator(),
                new CambiarTipoIncidenciaRequestValidator()),
            DatosPrueba.Reloj(), NullLogger<IncidenciaService>.Instance);

    /// <summary>Configura el contador y el bloqueo del proyecto (spec 0.3).</summary>
    protected void ConfigurarProyecto(bool archivado)
    {
        var bloqueado = new ProyectoBloqueado(ProyectoId, "PRJ-0001", archivado);
        Incidencias.ObtenerSiguienteNumeroAsync(ProyectoId, Arg.Any<CancellationToken>())
            .Returns(new NumeroIncidenciaAsignado(15, bloqueado));
        Incidencias.BloquearProyectoAsync(ProyectoId, Arg.Any<CancellationToken>()).Returns(bloqueado);
    }

    /// <summary>Configura la prioridad predeterminada (RN-21).</summary>
    protected void ConfigurarPrioridades(int predeterminada)
    {
        Prioridades.ObtenerPorIdAsync(Media, Arg.Any<CancellationToken>())
            .Returns(Prioridad(Media, "Media", predeterminada == Media));
        Prioridades.ObtenerPorIdAsync(Alta, Arg.Any<CancellationToken>())
            .Returns(Prioridad(Alta, "Alta", predeterminada == Alta));
        Prioridades.ObtenerPorIdAsync(PrioridadInactiva, Arg.Any<CancellationToken>())
            .Returns(Prioridad(PrioridadInactiva, "Vieja", activo: false));
        Prioridades.ObtenerPredeterminadaAsync(Arg.Any<CancellationToken>())
            .Returns(Prioridad(predeterminada, "Predeterminada", true));
    }

    /// <summary>Registra un estado del catálogo.</summary>
    protected void ConfigurarEstado(EstadoIncidencia estado) =>
        Estados.ObtenerPorIdAsync(estado.Id, Arg.Any<CancellationToken>()).Returns(estado);

    /// <summary>Registra una incidencia existente.</summary>
    protected Incidencia Existe(Incidencia incidencia)
    {
        Incidencias.ObtenerPorIdAsync(incidencia.Id, Arg.Any<CancellationToken>()).Returns(incidencia);
        return incidencia;
    }

    /// <summary>Registra las hijas directas de una incidencia.</summary>
    protected void ConHijas(int id, params HijaIncidencia[] hijas) =>
        Incidencias.ListarHijasAsync(id, Arg.Any<CancellationToken>()).Returns(hijas);

    private void ConfigurarTipos()
    {
        TipoIncidencia[] tipos =
        [
            Tipo(Epica, "Épica", nivelId: 1), Tipo(Hu, "Historia de Usuario", nivelId: 2),
            Tipo(Subtarea, "Subtarea", nivelId: 3), Tipo(CasoPrueba, "Caso de Prueba", nivelId: 3),
            Tipo(Bug, "Bug", nivelId: 3), Tipo(TipoInactivo, "Viejo", nivelId: 3, activo: false),
            Tipo(Defecto, "Defecto", nivelId: 3, flujoId: FlujoBug), Tipo(HuAlternativa, "HU técnica", nivelId: 2),
        ];
        foreach (var tipo in tipos)
        {
            Tipos.ObtenerPorIdAsync(tipo.Id, Arg.Any<CancellationToken>()).Returns(tipo);
        }

        Relaciones.ListarTiposHijoAsync(Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns([]);
        Relaciones.ListarTiposHijoAsync(Epica, Arg.Any<CancellationToken>()).Returns([Hu, HuAlternativa]);
        Relaciones.ListarTiposHijoAsync(Hu, Arg.Any<CancellationToken>()).Returns([Subtarea, CasoPrueba, Bug, Defecto]);
        Relaciones.ListarTiposHijoAsync(HuAlternativa, Arg.Any<CancellationToken>()).Returns([Subtarea]);
    }
}
