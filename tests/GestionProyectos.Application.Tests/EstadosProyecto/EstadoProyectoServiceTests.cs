using GestionProyectos.Application.Abstracciones.Persistencia;
using GestionProyectos.Application.EstadosProyecto;
using GestionProyectos.Application.Tests.Comun;
using GestionProyectos.Domain.Entidades;
using GestionProyectos.Domain.Excepciones;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace GestionProyectos.Application.Tests.EstadosProyecto;

/// <summary>
/// Tests de EstadoProyectoService (RN-07 a RN-13).
/// </summary>
/// <remarks>
/// Creado: 2026-10-09
/// Modificado: 2026-10-09 - Versión inicial (tests RF-001)
/// </remarks>
public class EstadoProyectoServiceTests
{
    private readonly IEstadoProyectoRepository _repositorio = Substitute.For<IEstadoProyectoRepository>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();
    private readonly EstadoProyectoService _sut;

    public EstadoProyectoServiceTests()
    {
        _sut = new EstadoProyectoService(_repositorio, _uow, new EstadoProyectoRequestValidator(),
            DatosPrueba.Reloj(), NullLogger<EstadoProyectoService>.Instance);
    }

    private void Existe(EstadoProyecto estado) =>
        _repositorio.ObtenerPorIdAsync(estado.Id, Arg.Any<CancellationToken>()).Returns(estado);

    public static TheoryData<Func<EstadoProyectoService, Task>> OperacionesSobreIdInexistente =>
    [
        s => s.ObtenerAsync(99, CancellationToken.None),
        s => s.EditarAsync(99, new EstadoProyectoRequest("X", null, 10, false), CancellationToken.None),
        s => s.MarcarInicialAsync(99, CancellationToken.None),
        s => s.DesactivarAsync(99, CancellationToken.None),
        s => s.ActivarAsync(99, CancellationToken.None),
    ];

    // RN-07, CA-09: nombre duplicado (el repo compara sin mayúsculas)
    [Fact]
    public async Task CrearAsync_NombreDuplicado_LanzaConflictoNombreDuplicado()
    {
        // Arrange
        _repositorio.ExisteNombreAsync("EN CURSO", null, Arg.Any<CancellationToken>()).Returns(true);
        var request = new EstadoProyectoRequest("  EN CURSO ", null, 35, false);

        // Act
        var ex = await Assert.ThrowsAsync<ConflictoException>(
            () => _sut.CrearAsync(request, CancellationToken.None));

        // Assert
        Assert.Equal(CodigosError.EstadoNombreDuplicado, ex.Codigo);
        await _repositorio.DidNotReceive().AgregarAsync(Arg.Any<EstadoProyecto>(), Arg.Any<CancellationToken>());
    }

    // RN-07, CA-07: se crea activo, no inicial y sin código de sistema
    [Fact]
    public async Task CrearAsync_Valido_CreaActivoNoInicialSinCodigo()
    {
        // Arrange
        var request = new EstadoProyectoRequest("En revisión", "Esperando aprobación.", 35, false);

        // Act
        var dto = await _sut.CrearAsync(request, CancellationToken.None);

        // Assert
        Assert.True(dto.Activo);
        Assert.False(dto.EsInicial);
        Assert.Null(dto.Codigo);
        Assert.False(dto.EsSistema);
        Assert.Equal(35, dto.Orden);
        await _repositorio.Received(1).AgregarAsync(Arg.Any<EstadoProyecto>(), Arg.Any<CancellationToken>());
    }

    // Spec 0.2: descripción opcional vacía tras trim se guarda como null
    [Fact]
    public async Task CrearAsync_DescripcionSoloEspacios_GuardaDescripcionNull()
    {
        // Arrange
        var request = new EstadoProyectoRequest("En revisión", "   ", 35, false);

        // Act
        var dto = await _sut.CrearAsync(request, CancellationToken.None);

        // Assert
        Assert.Null(dto.Descripcion);
    }

    // RN-09, CA-14: el estado de sistema no se edita
    [Fact]
    public async Task EditarAsync_EstadoSistema_LanzaSistemaNoModificable()
    {
        // Arrange
        Existe(DatosPrueba.EstadoArchivado());
        var request = new EstadoProyectoRequest("Archivo", null, 100, false);

        // Act
        var ex = await Assert.ThrowsAsync<ConflictoException>(
            () => _sut.EditarAsync(7, request, CancellationToken.None));

        // Assert
        Assert.Equal(CodigosError.EstadoSistemaNoModificable, ex.Codigo);
        await _repositorio.DidNotReceive().ActualizarAsync(Arg.Any<EstadoProyecto>(), Arg.Any<CancellationToken>());
    }

    // RN-07: renombrar a su mismo nombre con otras mayúsculas no es duplicado (se excluye su id)
    [Fact]
    public async Task EditarAsync_MismoNombreConOtrasMayusculas_ExcluyeSuIdYActualiza()
    {
        // Arrange
        Existe(DatosPrueba.Estado(3, "En curso"));

        // Act
        var dto = await _sut.EditarAsync(3, new EstadoProyectoRequest("EN CURSO", null, 30, false),
            CancellationToken.None);

        // Assert
        Assert.Equal("EN CURSO", dto.Nombre);
        await _repositorio.Received(1).ExisteNombreAsync("EN CURSO", 3, Arg.Any<CancellationToken>());
        await _repositorio.Received(1).ActualizarAsync(Arg.Any<EstadoProyecto>(), Arg.Any<CancellationToken>());
    }

    // RN-07, CA-09: renombrar a un nombre de otro estado
    [Fact]
    public async Task EditarAsync_NombreDeOtroEstado_LanzaConflictoNombreDuplicado()
    {
        // Arrange
        Existe(DatosPrueba.Estado(4, "Pausado"));
        _repositorio.ExisteNombreAsync("en curso", 4, Arg.Any<CancellationToken>()).Returns(true);

        // Act
        var ex = await Assert.ThrowsAsync<ConflictoException>(() => _sut.EditarAsync(4,
            new EstadoProyectoRequest("en curso", null, 40, false), CancellationToken.None));

        // Assert
        Assert.Equal(CodigosError.EstadoNombreDuplicado, ex.Codigo);
    }

    // RN-12: el estado inicial no puede marcarse final
    [Fact]
    public async Task EditarAsync_EstadoInicialConEsFinal_LanzaInicialNoFinal()
    {
        // Arrange
        Existe(DatosPrueba.Estado(1, "En planificación", 10, esInicial: true));
        var request = new EstadoProyectoRequest("En planificación", null, 10, true);

        // Act
        var ex = await Assert.ThrowsAsync<ConflictoException>(
            () => _sut.EditarAsync(1, request, CancellationToken.None));

        // Assert
        Assert.Equal(CodigosError.EstadoInicialNoFinal, ex.Codigo);
    }

    // RN-10, CA-10: desmarca el anterior y marca el nuevo en la misma transacción
    [Fact]
    public async Task MarcarInicialAsync_EstadoActivoNoFinal_DesmarcaAnteriorYMarcaNuevoEnTransaccion()
    {
        // Arrange
        var anterior = DatosPrueba.Estado(1, "En planificación", 10, esInicial: true);
        var nuevo = DatosPrueba.Estado(2, "Planificado", 20);
        Existe(nuevo);
        _repositorio.ObtenerInicialAsync(Arg.Any<CancellationToken>()).Returns(anterior);

        // Act
        var dto = await _sut.MarcarInicialAsync(2, CancellationToken.None);

        // Assert
        Assert.True(dto.EsInicial);
        Assert.False(anterior.EsInicial);
        Received.InOrder(() =>
        {
            _uow.IniciarAsync(Arg.Any<CancellationToken>());
            _repositorio.ActualizarAsync(anterior, Arg.Any<CancellationToken>());
            _repositorio.ActualizarAsync(nuevo, Arg.Any<CancellationToken>());
            _uow.ConfirmarAsync(Arg.Any<CancellationToken>());
        });
    }

    // RN-10: si falla la escritura se revierte la transacción
    [Fact]
    public async Task MarcarInicialAsync_FallaAlGuardar_RevierteTransaccion()
    {
        // Arrange
        Existe(DatosPrueba.Estado(2, "Planificado", 20));
        _repositorio.ObtenerInicialAsync(Arg.Any<CancellationToken>())
            .Returns(DatosPrueba.Estado(1, "En planificación", 10, esInicial: true));
        _repositorio.ActualizarAsync(Arg.Any<EstadoProyecto>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("falla de BD"));

        // Act
        var accion = () => _sut.MarcarInicialAsync(2, CancellationToken.None);

        // Assert
        await Assert.ThrowsAsync<InvalidOperationException>(accion);
        await _uow.Received(1).RevertirAsync(Arg.Any<CancellationToken>());
        await _uow.DidNotReceive().ConfirmarAsync(Arg.Any<CancellationToken>());
    }

    // RN-10: marcar inicial al que ya lo es no escribe nada
    [Fact]
    public async Task MarcarInicialAsync_EstadoYaInicial_NoEscribe()
    {
        // Arrange
        Existe(DatosPrueba.Estado(1, "En planificación", 10, esInicial: true));

        // Act
        var dto = await _sut.MarcarInicialAsync(1, CancellationToken.None);

        // Assert
        Assert.True(dto.EsInicial);
        await _uow.DidNotReceive().IniciarAsync(Arg.Any<CancellationToken>());
        await _repositorio.DidNotReceive().ActualizarAsync(Arg.Any<EstadoProyecto>(), Arg.Any<CancellationToken>());
    }

    // RN-11, CA-11: inactivo o final no puede ser inicial
    [Theory]
    [InlineData(false, false)]
    [InlineData(true, true)]
    public async Task MarcarInicialAsync_EstadoInactivoOFinal_LanzaNoElegibleInicial(bool activo, bool esFinal)
    {
        // Arrange
        Existe(DatosPrueba.Estado(5, "Finalizado", 50, esFinal: esFinal, activo: activo));

        // Act
        var ex = await Assert.ThrowsAsync<ConflictoException>(
            () => _sut.MarcarInicialAsync(5, CancellationToken.None));

        // Assert
        Assert.Equal(CodigosError.EstadoNoElegibleInicial, ex.Codigo);
        await _uow.DidNotReceive().IniciarAsync(Arg.Any<CancellationToken>());
    }

    // RN-09, CA-11: el estado de sistema no puede ser inicial
    [Fact]
    public async Task MarcarInicialAsync_EstadoSistema_LanzaSistemaNoModificable()
    {
        // Arrange
        Existe(DatosPrueba.EstadoArchivado());

        // Act
        var ex = await Assert.ThrowsAsync<ConflictoException>(
            () => _sut.MarcarInicialAsync(7, CancellationToken.None));

        // Assert
        Assert.Equal(CodigosError.EstadoSistemaNoModificable, ex.Codigo);
    }

    // RN-12, CA-12: el estado inicial no se desactiva
    [Fact]
    public async Task DesactivarAsync_EstadoInicial_LanzaInicialNoDesactivable()
    {
        // Arrange
        Existe(DatosPrueba.Estado(1, "En planificación", 10, esInicial: true));

        // Act
        var ex = await Assert.ThrowsAsync<ConflictoException>(
            () => _sut.DesactivarAsync(1, CancellationToken.None));

        // Assert
        Assert.Equal(CodigosError.EstadoInicialNoDesactivable, ex.Codigo);
        await _repositorio.DidNotReceive().ActualizarAsync(Arg.Any<EstadoProyecto>(), Arg.Any<CancellationToken>());
    }

    // RN-13, CA-13: desactivar un estado con proyectos solo cambia el estado
    [Fact]
    public async Task DesactivarAsync_EstadoConProyectos_DesactivaSinTocarProyectos()
    {
        // Arrange
        var pausado = DatosPrueba.Estado(4, "Pausado", 40);
        Existe(pausado);
        _repositorio.ContarProyectosAsync(4, Arg.Any<CancellationToken>()).Returns(3);

        // Act
        var dto = await _sut.DesactivarAsync(4, CancellationToken.None);

        // Assert
        Assert.False(dto.Activo);
        Assert.Equal(3, dto.CantidadProyectos);
        await _repositorio.Received(1).ActualizarAsync(pausado, Arg.Any<CancellationToken>());
    }

    // RN-08: desactivar es idempotente
    [Fact]
    public async Task DesactivarAsync_EstadoYaInactivo_NoActualiza()
    {
        // Arrange
        Existe(DatosPrueba.Estado(4, "Pausado", 40, activo: false));

        // Act
        var dto = await _sut.DesactivarAsync(4, CancellationToken.None);

        // Assert
        Assert.False(dto.Activo);
        await _repositorio.DidNotReceive().ActualizarAsync(Arg.Any<EstadoProyecto>(), Arg.Any<CancellationToken>());
    }

    // RN-09, CA-14: el estado de sistema no se desactiva
    [Fact]
    public async Task DesactivarAsync_EstadoSistema_LanzaSistemaNoModificable()
    {
        // Arrange
        Existe(DatosPrueba.EstadoArchivado());

        // Act
        var ex = await Assert.ThrowsAsync<ConflictoException>(
            () => _sut.DesactivarAsync(7, CancellationToken.None));

        // Assert
        Assert.Equal(CodigosError.EstadoSistemaNoModificable, ex.Codigo);
    }

    // RN-09, CA-14: el estado de sistema no se activa
    [Fact]
    public async Task ActivarAsync_EstadoSistema_LanzaSistemaNoModificable()
    {
        // Arrange
        Existe(DatosPrueba.EstadoArchivado());

        // Act
        var ex = await Assert.ThrowsAsync<ConflictoException>(
            () => _sut.ActivarAsync(7, CancellationToken.None));

        // Assert
        Assert.Equal(CodigosError.EstadoSistemaNoModificable, ex.Codigo);
    }

    // RN-08: un estado inactivo se puede reactivar
    [Fact]
    public async Task ActivarAsync_EstadoInactivo_ActivaYActualiza()
    {
        // Arrange
        Existe(DatosPrueba.Estado(4, "Pausado", 40, activo: false));

        // Act
        var dto = await _sut.ActivarAsync(4, CancellationToken.None);

        // Assert
        Assert.True(dto.Activo);
        await _repositorio.Received(1).ActualizarAsync(Arg.Any<EstadoProyecto>(), Arg.Any<CancellationToken>());
    }

    // Contrato: 404 ESTADO_NO_ENCONTRADO en cualquier operación por id
    [Theory]
    [MemberData(nameof(OperacionesSobreIdInexistente))]
    public async Task Operacion_IdInexistente_LanzaEstadoNoEncontrado(Func<EstadoProyectoService, Task> operacion)
    {
        // Act
        var ex = await Assert.ThrowsAsync<EntidadNoEncontradaException>(() => operacion(_sut));

        // Assert
        Assert.Equal(CodigosError.EstadoNoEncontrado, ex.Codigo);
    }

    // CA-06: el listado informa la cantidad de proyectos por estado (0 si no tiene)
    [Fact]
    public async Task ListarAsync_ConConteos_AsignaCantidadDeProyectosPorEstado()
    {
        // Arrange
        _repositorio.ListarAsync(false, Arg.Any<CancellationToken>())
            .Returns([DatosPrueba.Estado(1, "En planificación", 10, esInicial: true), DatosPrueba.EstadoArchivado()]);
        _repositorio.ContarProyectosPorEstadoAsync(Arg.Any<CancellationToken>())
            .Returns(new Dictionary<int, int> { [1] = 4 });

        // Act
        var lista = await _sut.ListarAsync(false, CancellationToken.None);

        // Assert
        Assert.Equal([4, 0], lista.Select(e => e.CantidadProyectos));
        Assert.True(lista[1].EsSistema);
    }
}
