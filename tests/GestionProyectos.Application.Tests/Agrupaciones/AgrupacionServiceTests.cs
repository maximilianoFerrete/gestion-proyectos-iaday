using FluentValidation;
using GestionProyectos.Application.Abstracciones.Persistencia;
using GestionProyectos.Application.Agrupaciones;
using GestionProyectos.Application.Incidencias;
using GestionProyectos.Application.Tests.Comun;
using GestionProyectos.Domain.Entidades;
using GestionProyectos.Domain.Excepciones;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace GestionProyectos.Application.Tests.Agrupaciones;

/// <summary>
/// Tests de AgrupacionService (RN-22, RN-26 a RN-30).
/// </summary>
/// <remarks>
/// Creado: 2026-10-09
/// Modificado: 2026-10-09 - Versión inicial (tests RF-001)
/// Modificado: 2026-10-09 - Inyecta IIncidenciaRepository y el bloqueo del proyecto (RF-002, RN-30)
/// </remarks>
public class AgrupacionServiceTests
{
    private const int ProyectoId = 3;
    private const int AgrupacionId = 8;
    private readonly IAgrupacionRepository _agrupaciones = Substitute.For<IAgrupacionRepository>();
    private readonly IProyectoRepository _proyectos = Substitute.For<IProyectoRepository>();
    private readonly IIncidenciaRepository _incidencias = Substitute.For<IIncidenciaRepository>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();
    private readonly AgrupacionService _sut;
    private readonly AgrupacionRequest _request = new("Módulo pagos", "Incidencias del circuito de pagos.");

    public AgrupacionServiceTests()
    {
        _sut = new AgrupacionService(_agrupaciones, _proyectos, _incidencias, _uow, new AgrupacionRequestValidator(),
            DatosPrueba.Reloj(), NullLogger<AgrupacionService>.Instance);
    }

    private void ExisteProyecto(bool archivado = false)
    {
        _proyectos.ObtenerPorIdAsync(ProyectoId, Arg.Any<CancellationToken>())
            .Returns(DatosPrueba.Proyecto(ProyectoId, archivado: archivado));
        _incidencias.BloquearProyectoAsync(ProyectoId, Arg.Any<CancellationToken>())
            .Returns(new ProyectoBloqueado(ProyectoId, "PRJ-0003", archivado));
    }

    private Agrupacion ExisteAgrupacion(int proyectoId = ProyectoId)
    {
        var agrupacion = DatosPrueba.Agrupacion(AgrupacionId, proyectoId);
        _agrupaciones.ObtenerPorIdAsync(AgrupacionId, Arg.Any<CancellationToken>()).Returns(agrupacion);
        return agrupacion;
    }

    // Contrato POST agrupaciones: 404 PROYECTO_NO_ENCONTRADO
    [Fact]
    public async Task CrearAsync_ProyectoInexistente_LanzaProyectoNoEncontrado()
    {
        // Act
        var ex = await Assert.ThrowsAsync<EntidadNoEncontradaException>(
            () => _sut.CrearAsync(ProyectoId, _request, CancellationToken.None));

        // Assert
        Assert.Equal(CodigosError.ProyectoNoEncontrado, ex.Codigo);
    }

    // RN-22, CA-23: no se crean agrupaciones en un proyecto archivado
    [Fact]
    public async Task CrearAsync_ProyectoArchivado_LanzaProyectoArchivadoYNoAgrega()
    {
        // Arrange
        ExisteProyecto(archivado: true);

        // Act
        var accion = () => _sut.CrearAsync(ProyectoId, _request, CancellationToken.None);

        // Assert
        await Assert.ThrowsAsync<ProyectoArchivadoException>(accion);
        await _agrupaciones.DidNotReceive().AgregarAsync(Arg.Any<Agrupacion>(), Arg.Any<CancellationToken>());
    }

    // RN-27, CA-30: nombre repetido en el mismo proyecto
    [Fact]
    public async Task CrearAsync_NombreDuplicadoEnElProyecto_LanzaConflictoNombreDuplicado()
    {
        // Arrange
        ExisteProyecto();
        _agrupaciones.ExisteNombreAsync(ProyectoId, "MÓDULO PAGOS", null, Arg.Any<CancellationToken>())
            .Returns(true);

        // Act
        var ex = await Assert.ThrowsAsync<ConflictoException>(() => _sut.CrearAsync(ProyectoId,
            _request with { Nombre = "MÓDULO PAGOS" }, CancellationToken.None));

        // Assert
        Assert.Equal(CodigosError.AgrupacionNombreDuplicado, ex.Codigo);
        await _agrupaciones.DidNotReceive().AgregarAsync(Arg.Any<Agrupacion>(), Arg.Any<CancellationToken>());
    }

    // RN-27, RN-31: la unicidad se consulta con el nombre recortado y dentro del proyecto
    [Fact]
    public async Task CrearAsync_NombreConEspacios_ConsultaUnicidadConNombreRecortadoEnElProyecto()
    {
        // Arrange
        ExisteProyecto();

        // Act
        var dto = await _sut.CrearAsync(ProyectoId, _request with { Nombre = "  Módulo pagos  " },
            CancellationToken.None);

        // Assert
        Assert.Equal("Módulo pagos", dto.Nombre);
        await _agrupaciones.Received(1).ExisteNombreAsync(ProyectoId, "Módulo pagos", null,
            Arg.Any<CancellationToken>());
    }

    // RN-27, CA-30: el mismo nombre en otro proyecto se acepta
    [Fact]
    public async Task CrearAsync_NombreUsadoEnOtroProyecto_Crea()
    {
        // Arrange
        ExisteProyecto();
        _agrupaciones.ExisteNombreAsync(Arg.Is<int>(id => id != ProyectoId), Arg.Any<string>(),
            Arg.Any<int?>(), Arg.Any<CancellationToken>()).Returns(true);

        // Act
        await _sut.CrearAsync(ProyectoId, _request, CancellationToken.None);

        // Assert
        await _agrupaciones.Received(1).AgregarAsync(Arg.Any<Agrupacion>(), Arg.Any<CancellationToken>());
    }

    // RN-28, CA-29: se crea en el proyecto de la URL con la fecha del reloj
    [Fact]
    public async Task CrearAsync_Valido_CreaEnElProyectoDeLaUrl()
    {
        // Arrange
        ExisteProyecto();

        // Act
        var dto = await _sut.CrearAsync(ProyectoId, _request, CancellationToken.None);

        // Assert
        Assert.Equal(ProyectoId, dto.ProyectoId);
        Assert.Equal(DatosPrueba.AhoraTruncado, dto.FechaCreacion);
    }

    // RN-26: datos inválidos no llegan a la persistencia
    [Fact]
    public async Task CrearAsync_DescripcionVacia_LanzaValidationException()
    {
        // Arrange
        ExisteProyecto();

        // Act
        var ex = await Assert.ThrowsAsync<ValidationException>(() => _sut.CrearAsync(ProyectoId,
            _request with { Descripcion = "   " }, CancellationToken.None));

        // Assert
        Assert.Contains(ex.Errors, e => e.PropertyName == nameof(AgrupacionRequest.Descripcion));
    }

    // RN-29, CA-32: agrupación de otro proyecto → 404 AGRUPACION_NO_ENCONTRADA
    [Fact]
    public async Task EditarAsync_AgrupacionDeOtroProyecto_LanzaAgrupacionNoEncontrada()
    {
        // Arrange
        ExisteProyecto();
        ExisteAgrupacion(proyectoId: 4);

        // Act
        var ex = await Assert.ThrowsAsync<EntidadNoEncontradaException>(
            () => _sut.EditarAsync(ProyectoId, AgrupacionId, _request, CancellationToken.None));

        // Assert
        Assert.Equal(CodigosError.AgrupacionNoEncontrada, ex.Codigo);
    }

    // RN-27: renombrar a su mismo nombre (otras mayúsculas) excluye su propio id
    [Fact]
    public async Task EditarAsync_RenombrarASuMismoNombre_ExcluyeSuIdYActualiza()
    {
        // Arrange
        ExisteProyecto();
        var agrupacion = ExisteAgrupacion();

        // Act
        var dto = await _sut.EditarAsync(ProyectoId, AgrupacionId, _request with { Nombre = "MÓDULO PAGOS" },
            CancellationToken.None);

        // Assert
        Assert.Equal("MÓDULO PAGOS", dto.Nombre);
        await _agrupaciones.Received(1).ExisteNombreAsync(ProyectoId, "MÓDULO PAGOS", AgrupacionId,
            Arg.Any<CancellationToken>());
        await _agrupaciones.Received(1).ActualizarAsync(agrupacion, Arg.Any<CancellationToken>());
    }

    // RN-27, CA-30: renombrar a un nombre de otra agrupación del proyecto
    [Fact]
    public async Task EditarAsync_NombreDeOtraAgrupacion_LanzaConflictoNombreDuplicado()
    {
        // Arrange
        ExisteProyecto();
        ExisteAgrupacion();
        _agrupaciones.ExisteNombreAsync(ProyectoId, "Otra", AgrupacionId, Arg.Any<CancellationToken>())
            .Returns(true);

        // Act
        var ex = await Assert.ThrowsAsync<ConflictoException>(() => _sut.EditarAsync(ProyectoId,
            AgrupacionId, _request with { Nombre = "Otra" }, CancellationToken.None));

        // Assert
        Assert.Equal(CodigosError.AgrupacionNombreDuplicado, ex.Codigo);
    }

    // RN-22, CA-23: no se editan agrupaciones de un proyecto archivado
    [Fact]
    public async Task EditarAsync_ProyectoArchivado_LanzaProyectoArchivadoYNoActualiza()
    {
        // Arrange
        ExisteProyecto(archivado: true);
        ExisteAgrupacion();

        // Act
        var accion = () => _sut.EditarAsync(ProyectoId, AgrupacionId, _request, CancellationToken.None);

        // Assert
        await Assert.ThrowsAsync<ProyectoArchivadoException>(accion);
        await _agrupaciones.DidNotReceive().ActualizarAsync(Arg.Any<Agrupacion>(), Arg.Any<CancellationToken>());
    }

    // RN-22, CA-23: no se eliminan agrupaciones de un proyecto archivado
    [Fact]
    public async Task EliminarAsync_ProyectoArchivado_LanzaProyectoArchivadoYNoElimina()
    {
        // Arrange
        ExisteProyecto(archivado: true);
        ExisteAgrupacion();

        // Act
        var accion = () => _sut.EliminarAsync(ProyectoId, AgrupacionId, CancellationToken.None);

        // Assert
        await Assert.ThrowsAsync<ProyectoArchivadoException>(accion);
        await _agrupaciones.DidNotReceive().EliminarAsync(Arg.Any<int>(), Arg.Any<CancellationToken>());
        await _uow.DidNotReceive().IniciarAsync(Arg.Any<CancellationToken>());
    }

    // RN-30, CA-31: eliminación física dentro de IUnitOfWork con commit
    [Fact]
    public async Task EliminarAsync_Valido_EliminaDentroDeTransaccion()
    {
        // Arrange
        ExisteProyecto();
        ExisteAgrupacion();

        // Act
        await _sut.EliminarAsync(ProyectoId, AgrupacionId, CancellationToken.None);

        // Assert
        Received.InOrder(() =>
        {
            _uow.IniciarAsync(Arg.Any<CancellationToken>());
            _agrupaciones.EliminarAsync(AgrupacionId, Arg.Any<CancellationToken>());
            _uow.ConfirmarAsync(Arg.Any<CancellationToken>());
        });
    }

    // Contrato DELETE: 404 AGRUPACION_NO_ENCONTRADA si no existe
    [Fact]
    public async Task EliminarAsync_AgrupacionInexistente_LanzaAgrupacionNoEncontrada()
    {
        // Arrange
        ExisteProyecto();

        // Act
        var ex = await Assert.ThrowsAsync<EntidadNoEncontradaException>(
            () => _sut.EliminarAsync(ProyectoId, 99, CancellationToken.None));

        // Assert
        Assert.Equal(CodigosError.AgrupacionNoEncontrada, ex.Codigo);
    }

    // RN-29, CA-32: eliminar por un proyecto que no es el suyo → 404
    [Fact]
    public async Task EliminarAsync_AgrupacionDeOtroProyecto_LanzaAgrupacionNoEncontrada()
    {
        // Arrange
        ExisteProyecto();
        ExisteAgrupacion(proyectoId: 4);

        // Act
        var ex = await Assert.ThrowsAsync<EntidadNoEncontradaException>(
            () => _sut.EliminarAsync(ProyectoId, AgrupacionId, CancellationToken.None));

        // Assert
        Assert.Equal(CodigosError.AgrupacionNoEncontrada, ex.Codigo);
        await _agrupaciones.DidNotReceive().EliminarAsync(Arg.Any<int>(), Arg.Any<CancellationToken>());
    }

    // RN-29, CA-32: obtener por un proyecto que no es el suyo → 404
    [Fact]
    public async Task ObtenerAsync_AgrupacionDeOtroProyecto_LanzaAgrupacionNoEncontrada()
    {
        // Arrange
        ExisteProyecto();
        ExisteAgrupacion(proyectoId: 4);

        // Act
        var ex = await Assert.ThrowsAsync<EntidadNoEncontradaException>(
            () => _sut.ObtenerAsync(ProyectoId, AgrupacionId, CancellationToken.None));

        // Assert
        Assert.Equal(CodigosError.AgrupacionNoEncontrada, ex.Codigo);
    }

    // CA-33: las agrupaciones de un proyecto archivado se consultan
    [Fact]
    public async Task ListarAsync_ProyectoArchivado_DevuelveLasAgrupaciones()
    {
        // Arrange
        ExisteProyecto(archivado: true);
        _agrupaciones.ListarPorProyectoAsync(ProyectoId, Arg.Any<CancellationToken>())
            .Returns([DatosPrueba.Agrupacion(8, ProyectoId), DatosPrueba.Agrupacion(9, ProyectoId, "Otra")]);

        // Act
        var lista = await _sut.ListarAsync(ProyectoId, CancellationToken.None);

        // Assert
        Assert.Equal(2, lista.Count);
    }

    // Contrato GET agrupaciones: 404 PROYECTO_NO_ENCONTRADO
    [Fact]
    public async Task ListarAsync_ProyectoInexistente_LanzaProyectoNoEncontrado()
    {
        // Act
        var ex = await Assert.ThrowsAsync<EntidadNoEncontradaException>(
            () => _sut.ListarAsync(99, CancellationToken.None));

        // Assert
        Assert.Equal(CodigosError.ProyectoNoEncontrado, ex.Codigo);
    }
}
