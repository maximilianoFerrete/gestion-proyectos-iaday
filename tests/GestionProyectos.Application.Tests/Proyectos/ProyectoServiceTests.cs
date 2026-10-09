using FluentValidation;
using GestionProyectos.Application.Abstracciones;
using GestionProyectos.Application.Abstracciones.Persistencia;
using GestionProyectos.Application.Comun;
using GestionProyectos.Application.Proyectos;
using GestionProyectos.Application.Tests.Comun;
using GestionProyectos.Domain.Comun;
using GestionProyectos.Domain.Entidades;
using GestionProyectos.Domain.Excepciones;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace GestionProyectos.Application.Tests.Proyectos;

/// <summary>
/// Tests de ProyectoService (RN-14 a RN-25).
/// </summary>
/// <remarks>
/// Creado: 2026-10-09
/// Modificado: 2026-10-09 - Versión inicial (tests RF-001)
/// </remarks>
public class ProyectoServiceTests
{
    private const int IdCreado = 10;
    private readonly IProyectoRepository _proyectos = Substitute.For<IProyectoRepository>();
    private readonly IPersonaRepository _personas = Substitute.For<IPersonaRepository>();
    private readonly IEstadoProyectoRepository _estados = Substitute.For<IEstadoProyectoRepository>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();
    private readonly IUsuarioActual _usuario = Substitute.For<IUsuarioActual>();
    private readonly ProyectoService _sut;
    private Proyecto? _agregado;

    public ProyectoServiceTests()
    {
        var validadores = new ProyectoValidadores(new CrearProyectoRequestValidator(),
            new EditarProyectoRequestValidator(), new ListarProyectosQueryValidator());
        _sut = new ProyectoService(_proyectos, _personas, _estados, _uow, _usuario, validadores,
            DatosPrueba.Reloj(), NullLogger<ProyectoService>.Instance);

        _personas.ObtenerPorIdAsync(2, Arg.Any<CancellationToken>()).Returns(DatosPrueba.Persona(2));
        _estados.ObtenerInicialAsync(Arg.Any<CancellationToken>())
            .Returns(DatosPrueba.Estado(1, "En planificación", 10, esInicial: true));
        _estados.ObtenerPorCodigoAsync(CodigosEstado.Archivado, Arg.Any<CancellationToken>())
            .Returns(DatosPrueba.EstadoArchivado(7));
        _proyectos.ObtenerSiguienteNumeroAsync(Arg.Any<CancellationToken>()).Returns(1);
        _proyectos.AgregarAsync(Arg.Do<Proyecto>(p =>
        {
            p.AsignarId(IdCreado);
            _agregado = p;
        }), Arg.Any<CancellationToken>());
        _proyectos.ObtenerDetalleAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(ci => DatosPrueba.ProyectoDto(ci.Arg<int>()));
    }

    private static CrearProyectoRequest Alta(string? nombre = "Portal clientes", int? responsableId = 2,
        DateOnly? inicio = null, DateOnly? objetivo = null) =>
        new(nombre, "Nuevo portal de autogestión.", responsableId,
            inicio ?? DatosPrueba.FechaInicio, objetivo ?? DatosPrueba.FechaObjetivo);

    private static EditarProyectoRequest Edicion(int responsableId = 2, int? estadoId = 3) =>
        new("Portal clientes v2", "Descripción editada.", responsableId, DatosPrueba.FechaInicio,
            new DateOnly(2027, 1, 31), estadoId);

    private void ExisteProyecto(Proyecto proyecto) =>
        _proyectos.ObtenerPorIdAsync(proyecto.Id, Arg.Any<CancellationToken>()).Returns(proyecto);

    private void ConfigurarUsuario(int id, bool esAdmin)
    {
        _usuario.EstaIdentificado.Returns(true);
        _usuario.PersonaId.Returns(id);
        _usuario.EsAdmin.Returns(esAdmin);
    }

    // RN-14, RN-18, CA-15: estado inicial vigente, código desde la secuencia y fecha del reloj
    [Fact]
    public async Task CrearAsync_Valido_AsignaEstadoInicialCodigoYFechaDelReloj()
    {
        // Act
        await _sut.CrearAsync(Alta(), CancellationToken.None);

        // Assert
        Assert.NotNull(_agregado);
        Assert.Equal("PRJ-0001", _agregado.Codigo);
        Assert.Equal(1, _agregado.EstadoId);
        Assert.Equal(DatosPrueba.AhoraTruncado, _agregado.FechaCreacion);
    }

    // CA-15: el segundo proyecto recibe PRJ-0002
    [Fact]
    public async Task CrearAsync_SegundoNumeroDeSecuencia_GeneraPRJ0002()
    {
        // Arrange
        _proyectos.ObtenerSiguienteNumeroAsync(Arg.Any<CancellationToken>()).Returns(2);

        // Act
        await _sut.CrearAsync(Alta(), CancellationToken.None);

        // Assert
        Assert.Equal("PRJ-0002", _agregado?.Codigo);
    }

    // CA-10, RN-18: un proyecto nuevo se crea en el estado inicial vigente (el que esté marcado)
    [Fact]
    public async Task CrearAsync_OtroEstadoInicial_UsaElInicialVigente()
    {
        // Arrange
        _estados.ObtenerInicialAsync(Arg.Any<CancellationToken>())
            .Returns(DatosPrueba.Estado(2, "Planificado", 20, esInicial: true));

        // Act
        await _sut.CrearAsync(Alta(), CancellationToken.None);

        // Assert
        Assert.Equal(2, _agregado?.EstadoId);
    }

    // Contrato POST /api/proyectos: devuelve el detalle del proyecto creado
    [Fact]
    public async Task CrearAsync_Valido_DevuelveDetalleDelProyectoCreado()
    {
        // Act
        var dto = await _sut.CrearAsync(Alta(), CancellationToken.None);

        // Assert
        Assert.Equal(IdCreado, dto.Id);
    }

    // RN-31: textos recortados antes de guardar
    [Fact]
    public async Task CrearAsync_TextosConEspacios_GuardaTextosRecortados()
    {
        // Act
        await _sut.CrearAsync(Alta(nombre: "   Portal   "), CancellationToken.None);

        // Assert
        Assert.Equal("Portal", _agregado?.Nombre);
    }

    // RN-17, CA-05: responsable inexistente
    [Fact]
    public async Task CrearAsync_ResponsableInexistente_LanzaResponsableInvalido()
    {
        // Act
        var ex = await Assert.ThrowsAsync<ValidacionNegocioException>(
            () => _sut.CrearAsync(Alta(responsableId: 99), CancellationToken.None));

        // Assert
        Assert.Equal(CodigosError.ResponsableInvalido, ex.Codigo);
        await _proyectos.DidNotReceive().AgregarAsync(Arg.Any<Proyecto>(), Arg.Any<CancellationToken>());
    }

    // RN-17, CA-05: responsable inactivo
    [Fact]
    public async Task CrearAsync_ResponsableInactivo_LanzaResponsableInvalido()
    {
        // Arrange
        _personas.ObtenerPorIdAsync(6, Arg.Any<CancellationToken>()).Returns(DatosPrueba.Persona(6, activo: false));

        // Act
        var ex = await Assert.ThrowsAsync<ValidacionNegocioException>(
            () => _sut.CrearAsync(Alta(responsableId: 6), CancellationToken.None));

        // Assert
        Assert.Equal(CodigosError.ResponsableInvalido, ex.Codigo);
        await _proyectos.DidNotReceive().ObtenerSiguienteNumeroAsync(Arg.Any<CancellationToken>());
    }

    // RN-15, CA-16: el nombre no es único; no se consulta unicidad y se crea igual
    [Fact]
    public async Task CrearAsync_NombreIgualAOtroProyecto_CreaSinConsultarUnicidad()
    {
        // Arrange
        _proyectos.ObtenerSiguienteNumeroAsync(Arg.Any<CancellationToken>()).Returns(5);

        // Act
        await _sut.CrearAsync(Alta(nombre: "Portal clientes"), CancellationToken.None);

        // Assert
        Assert.Equal("PRJ-0005", _agregado?.Codigo);
        Assert.DoesNotContain(_proyectos.ReceivedCalls(), c => c.GetMethodInfo().Name.Contains("Nombre"));
    }

    // RN-16, CA-17: fecha objetivo anterior a la de inicio
    [Fact]
    public async Task CrearAsync_FechaObjetivoAnteriorAInicio_LanzaValidationExceptionEnFechaObjetivo()
    {
        // Arrange
        var request = Alta(inicio: new DateOnly(2026, 10, 10), objetivo: new DateOnly(2026, 10, 9));

        // Act
        var ex = await Assert.ThrowsAsync<ValidationException>(
            () => _sut.CrearAsync(request, CancellationToken.None));

        // Assert
        Assert.Contains(ex.Errors, e => e.PropertyName == nameof(CrearProyectoRequest.FechaObjetivo));
        await _proyectos.DidNotReceive().AgregarAsync(Arg.Any<Proyecto>(), Arg.Any<CancellationToken>());
    }

    // RN-16, CA-17: fechas iguales se aceptan
    [Fact]
    public async Task CrearAsync_FechasIguales_Crea()
    {
        // Arrange
        var fecha = new DateOnly(2026, 10, 10);

        // Act
        await _sut.CrearAsync(Alta(inicio: fecha, objetivo: fecha), CancellationToken.None);

        // Assert
        Assert.Equal(fecha, _agregado?.FechaObjetivo);
    }

    // RN-22, CA-23: un proyecto archivado no se edita
    [Fact]
    public async Task EditarAsync_ProyectoArchivado_LanzaProyectoArchivadoYNoActualiza()
    {
        // Arrange
        ExisteProyecto(DatosPrueba.Proyecto(3, estadoId: 7, archivado: true));

        // Act
        var ex = await Assert.ThrowsAsync<ProyectoArchivadoException>(
            () => _sut.EditarAsync(3, Edicion(), CancellationToken.None));

        // Assert
        Assert.Equal(CodigosError.ProyectoArchivado, ex.Codigo);
        await _proyectos.DidNotReceive().ActualizarAsync(Arg.Any<Proyecto>(), Arg.Any<CancellationToken>());
    }

    // CA-19: edita nombre, descripción, fechas y estado; el código no cambia
    [Fact]
    public async Task EditarAsync_Valido_ActualizaDatosYConservaCodigo()
    {
        // Arrange
        var proyecto = DatosPrueba.Proyecto(3, estadoId: 3);
        ExisteProyecto(proyecto);
        _estados.ObtenerPorIdAsync(4, Arg.Any<CancellationToken>()).Returns(DatosPrueba.Estado(4, "Pausado", 40));

        // Act
        await _sut.EditarAsync(3, Edicion(estadoId: 4), CancellationToken.None);

        // Assert
        Assert.Equal("PRJ-0003", proyecto.Codigo);
        Assert.Equal("Portal clientes v2", proyecto.Nombre);
        Assert.Equal(new DateOnly(2027, 1, 31), proyecto.FechaObjetivo);
        Assert.Equal(4, proyecto.EstadoId);
        Assert.Equal(DatosPrueba.AhoraTruncado, proyecto.FechaModificacion);
        await _proyectos.Received(1).ActualizarAsync(proyecto, Arg.Any<CancellationToken>());
    }

    // RN-17: mantener el responsable actual aunque esté inactivo es válido
    [Fact]
    public async Task EditarAsync_ManteniendoResponsableActualInactivo_Actualiza()
    {
        // Arrange
        ExisteProyecto(DatosPrueba.Proyecto(3, responsableId: 6));
        _personas.ObtenerPorIdAsync(6, Arg.Any<CancellationToken>()).Returns(DatosPrueba.Persona(6, activo: false));

        // Act
        await _sut.EditarAsync(3, Edicion(responsableId: 6), CancellationToken.None);

        // Assert
        await _proyectos.Received(1).ActualizarAsync(Arg.Any<Proyecto>(), Arg.Any<CancellationToken>());
    }

    // RN-17: cambiar a un responsable inactivo no es válido
    [Fact]
    public async Task EditarAsync_CambiandoAResponsableInactivo_LanzaResponsableInvalido()
    {
        // Arrange
        ExisteProyecto(DatosPrueba.Proyecto(3, responsableId: 2));
        _personas.ObtenerPorIdAsync(6, Arg.Any<CancellationToken>()).Returns(DatosPrueba.Persona(6, activo: false));

        // Act
        var ex = await Assert.ThrowsAsync<ValidacionNegocioException>(
            () => _sut.EditarAsync(3, Edicion(responsableId: 6), CancellationToken.None));

        // Assert
        Assert.Equal(CodigosError.ResponsableInvalido, ex.Codigo);
        await _proyectos.DidNotReceive().ActualizarAsync(Arg.Any<Proyecto>(), Arg.Any<CancellationToken>());
    }

    // RN-13, RN-19, CA-13: mantener el estado actual aunque esté inactivo es válido
    [Fact]
    public async Task EditarAsync_ManteniendoEstadoActualInactivo_Actualiza()
    {
        // Arrange
        ExisteProyecto(DatosPrueba.Proyecto(3, estadoId: 4));
        _estados.ObtenerPorIdAsync(4, Arg.Any<CancellationToken>())
            .Returns(DatosPrueba.Estado(4, "Pausado", 40, activo: false));

        // Act
        await _sut.EditarAsync(3, Edicion(estadoId: 4), CancellationToken.None);

        // Assert
        await _proyectos.Received(1).ActualizarAsync(Arg.Any<Proyecto>(), Arg.Any<CancellationToken>());
    }

    // RN-09, RN-19, CA-13, CA-20: estado inactivo, "Archivado" o inexistente no se asignan
    [Theory]
    [MemberData(nameof(EstadosNoAsignables))]
    public async Task EditarAsync_EstadoNoAsignable_LanzaEstadoInvalido(EstadoProyecto? estado)
    {
        // Arrange
        ExisteProyecto(DatosPrueba.Proyecto(3, estadoId: 3));
        _estados.ObtenerPorIdAsync(8, Arg.Any<CancellationToken>()).Returns(estado);

        // Act
        var ex = await Assert.ThrowsAsync<ValidacionNegocioException>(
            () => _sut.EditarAsync(3, Edicion(estadoId: 8), CancellationToken.None));

        // Assert
        Assert.Equal(CodigosError.EstadoInvalido, ex.Codigo);
        await _proyectos.DidNotReceive().ActualizarAsync(Arg.Any<Proyecto>(), Arg.Any<CancellationToken>());
    }

    public static TheoryData<EstadoProyecto?> EstadosNoAsignables =>
    [
        DatosPrueba.Estado(8, "Pausado", 40, activo: false),
        DatosPrueba.EstadoArchivado(8),
        null,
    ];

    // Contrato PUT: 400 VALIDACION si falta el estado
    [Fact]
    public async Task EditarAsync_SinEstadoId_LanzaValidationExceptionEnEstadoId()
    {
        // Act
        var ex = await Assert.ThrowsAsync<ValidationException>(
            () => _sut.EditarAsync(3, Edicion(estadoId: null), CancellationToken.None));

        // Assert
        Assert.Contains(ex.Errors, e => e.PropertyName == nameof(EditarProyectoRequest.EstadoId));
    }

    // Contrato PUT: 404 PROYECTO_NO_ENCONTRADO
    [Fact]
    public async Task EditarAsync_IdInexistente_LanzaProyectoNoEncontrado()
    {
        // Act
        var ex = await Assert.ThrowsAsync<EntidadNoEncontradaException>(
            () => _sut.EditarAsync(99, Edicion(), CancellationToken.None));

        // Assert
        Assert.Equal(CodigosError.ProyectoNoEncontrado, ex.Codigo);
    }

    // RN-20, CA-22: archivar sin usuario → 401
    [Fact]
    public async Task ArchivarAsync_SinUsuario_LanzaUsuarioNoIdentificado()
    {
        // Act
        var accion = () => _sut.ArchivarAsync(3, CancellationToken.None);

        // Assert
        await Assert.ThrowsAsync<UsuarioNoIdentificadoException>(accion);
        await _proyectos.DidNotReceive().ActualizarAsync(Arg.Any<Proyecto>(), Arg.Any<CancellationToken>());
    }

    // RN-20, CA-22: archivar con usuario no admin → 403
    [Fact]
    public async Task ArchivarAsync_UsuarioNoAdmin_LanzaPermisoDenegado()
    {
        // Arrange
        ConfigurarUsuario(2, esAdmin: false);
        ExisteProyecto(DatosPrueba.Proyecto(3));

        // Act
        var accion = () => _sut.ArchivarAsync(3, CancellationToken.None);

        // Assert
        await Assert.ThrowsAsync<PermisoDenegadoException>(accion);
        await _proyectos.DidNotReceive().ActualizarAsync(Arg.Any<Proyecto>(), Arg.Any<CancellationToken>());
    }

    // RN-21, CA-21: guarda estado al archivar, estado Archivado, fecha y usuario
    [Fact]
    public async Task ArchivarAsync_AdminProyectoFinalizado_GuardaDatosDeArchivado()
    {
        // Arrange
        ConfigurarUsuario(1, esAdmin: true);
        var proyecto = DatosPrueba.Proyecto(3, estadoId: 5);
        ExisteProyecto(proyecto);

        // Act
        await _sut.ArchivarAsync(3, CancellationToken.None);

        // Assert
        Assert.True(proyecto.EstaArchivado);
        Assert.Equal(7, proyecto.EstadoId);
        Assert.Equal(5, proyecto.EstadoAlArchivarId);
        Assert.Equal(DatosPrueba.AhoraTruncado, proyecto.FechaArchivado);
        Assert.Equal(1, proyecto.ArchivadoPorId);
    }

    // RN-21: el archivado se guarda en una transacción
    [Fact]
    public async Task ArchivarAsync_Admin_ActualizaDentroDeTransaccion()
    {
        // Arrange
        ConfigurarUsuario(1, esAdmin: true);
        var proyecto = DatosPrueba.Proyecto(3, estadoId: 5);
        ExisteProyecto(proyecto);

        // Act
        await _sut.ArchivarAsync(3, CancellationToken.None);

        // Assert
        Received.InOrder(() =>
        {
            _uow.IniciarAsync(Arg.Any<CancellationToken>());
            _proyectos.ActualizarAsync(proyecto, Arg.Any<CancellationToken>());
            _uow.ConfirmarAsync(Arg.Any<CancellationToken>());
        });
    }

    // RN-22, CA-23: re-archivar un proyecto archivado → 409
    [Fact]
    public async Task ArchivarAsync_ProyectoYaArchivado_LanzaProyectoArchivado()
    {
        // Arrange
        ConfigurarUsuario(1, esAdmin: true);
        ExisteProyecto(DatosPrueba.Proyecto(3, estadoId: 7, archivado: true));

        // Act
        var accion = () => _sut.ArchivarAsync(3, CancellationToken.None);

        // Assert
        await Assert.ThrowsAsync<ProyectoArchivadoException>(accion);
        await _uow.DidNotReceive().IniciarAsync(Arg.Any<CancellationToken>());
        await _proyectos.DidNotReceive().ActualizarAsync(Arg.Any<Proyecto>(), Arg.Any<CancellationToken>());
    }

    // Contrato archivar: 404 PROYECTO_NO_ENCONTRADO
    [Fact]
    public async Task ArchivarAsync_IdInexistente_LanzaProyectoNoEncontrado()
    {
        // Arrange
        ConfigurarUsuario(1, esAdmin: true);

        // Act
        var ex = await Assert.ThrowsAsync<EntidadNoEncontradaException>(
            () => _sut.ArchivarAsync(99, CancellationToken.None));

        // Assert
        Assert.Equal(CodigosError.ProyectoNoEncontrado, ex.Codigo);
    }

    // CA-28: proyecto inexistente → 404 PROYECTO_NO_ENCONTRADO
    [Fact]
    public async Task ObtenerAsync_IdInexistente_LanzaProyectoNoEncontrado()
    {
        // Arrange
        _proyectos.ObtenerDetalleAsync(99, Arg.Any<CancellationToken>()).Returns((ProyectoDto?)null);

        // Act
        var ex = await Assert.ThrowsAsync<EntidadNoEncontradaException>(
            () => _sut.ObtenerAsync(99, CancellationToken.None));

        // Assert
        Assert.Equal(CodigosError.ProyectoNoEncontrado, ex.Codigo);
    }

    // RN-24, CA-25: defaults archivados=excluir, página 1, tamaño 20, codigo desc
    [Fact]
    public async Task ListarAsync_SinParametros_PasaAlRepoLosDefaults()
    {
        // Act
        await _sut.ListarAsync(new ListarProyectosQuery(), CancellationToken.None);

        // Assert
        var esperado = new FiltroProyectos(null, null, null, FiltroArchivados.Excluir, 1, 20,
            OrdenProyectos.Codigo, true);
        await _proyectos.Received(1).ListarAsync(esperado, Arg.Any<CancellationToken>());
    }

    // CA-25, CA-26: filtros normalizados (trim y opciones sin distinguir mayúsculas)
    [Fact]
    public async Task ListarAsync_ParametrosConMayusculasYEspacios_NormalizaFiltro()
    {
        // Arrange
        var query = new ListarProyectosQuery(" prj-0003 ", 3, 2, " SOLO ", 2, 5, "fechaInicio", "ASC");

        // Act
        await _sut.ListarAsync(query, CancellationToken.None);

        // Assert
        var esperado = new FiltroProyectos("prj-0003", 3, 2, FiltroArchivados.Solo, 2, 5,
            OrdenProyectos.FechaInicio, false);
        await _proyectos.Received(1).ListarAsync(esperado, Arg.Any<CancellationToken>());
    }

    // CA-27: parámetros inválidos → 400 VALIDACION
    [Theory]
    [MemberData(nameof(QueriesInvalidas))]
    public async Task ListarAsync_ParametroInvalido_LanzaValidationException(ListarProyectosQuery query)
    {
        // Act
        var accion = () => _sut.ListarAsync(query, CancellationToken.None);

        // Assert
        await Assert.ThrowsAsync<ValidationException>(accion);
        await _proyectos.DidNotReceive().ListarAsync(Arg.Any<FiltroProyectos>(), Arg.Any<CancellationToken>());
    }

    public static TheoryData<ListarProyectosQuery> QueriesInvalidas =>
    [
        new ListarProyectosQuery(TamanioPagina: 500),
        new ListarProyectosQuery(OrdenarPor: "foo"),
        new ListarProyectosQuery(Archivados: "x"),
    ];

    // CA-26: el resultado del repo se devuelve tal cual (items, total y paginación)
    [Fact]
    public async Task ListarAsync_Valido_DevuelvePaginaDelRepo()
    {
        // Arrange
        var pagina = new Paginado<ProyectoResumenDto>([], 1, 20, 0);
        _proyectos.ListarAsync(Arg.Any<FiltroProyectos>(), Arg.Any<CancellationToken>()).Returns(pagina);

        // Act
        var resultado = await _sut.ListarAsync(new ListarProyectosQuery(), CancellationToken.None);

        // Assert
        Assert.Same(pagina, resultado);
    }
}
