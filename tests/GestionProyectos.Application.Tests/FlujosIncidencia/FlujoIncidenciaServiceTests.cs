using GestionProyectos.Application.Abstracciones;
using GestionProyectos.Application.Abstracciones.Persistencia;
using GestionProyectos.Application.FlujosIncidencia;
using GestionProyectos.Application.Tests.Comun;
using GestionProyectos.Domain.Entidades;
using GestionProyectos.Domain.Excepciones;
using GestionProyectos.Domain.Reglas;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using static GestionProyectos.Application.Tests.Comun.DatosIncidencias;

namespace GestionProyectos.Application.Tests.FlujosIncidencia;

/// <summary>
/// Tests de FlujoIncidenciaService (RN-01, RN-11, RN-13 a RN-15, CA-08).
/// </summary>
/// <remarks>
/// Creado: 2026-10-10
/// Modificado: 2026-10-10 - Versión inicial (tests RF-002)
/// </remarks>
public class FlujoIncidenciaServiceTests
{
    private const int EnRevision = 5;
    private readonly IFlujoIncidenciaRepository _flujos = Substitute.For<IFlujoIncidenciaRepository>();
    private readonly IEstadoIncidenciaRepository _estados = Substitute.For<IEstadoIncidenciaRepository>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();
    private readonly FlujoIncidencia _general = FlujoGeneralConEstados();

    /// <summary>Request que reproduce la composición actual del Flujo general.</summary>
    private static readonly FlujoIncidenciaRequest General = new("Flujo general", null,
        [new(PorHacer, true, false), new(EnProgreso, false, false), new(Hecho, false, true)]);

    public FlujoIncidenciaServiceTests()
    {
        EstadosDelCatalogo(Estado(PorHacer), Estado(EnProgreso), Estado(Hecho), Estado(EnRevision));
        _flujos.ObtenerPorIdAsync(FlujoGeneral, Arg.Any<CancellationToken>()).Returns(_general);
        _flujos.ContarIncidenciasPorEstadoAsync(FlujoGeneral, Arg.Any<CancellationToken>()).Returns([]);
        _flujos.ObtenerDtoAsync(Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(c =>
            new FlujoIncidenciaDto(c.Arg<int>(), "Flujo", null, [], [], DatosPrueba.FechaAnterior, null));
    }

    private void EstadosDelCatalogo(params EstadoIncidencia[] estados) =>
        _estados.ListarPorIdsAsync(Arg.Any<IReadOnlyCollection<int>>(), Arg.Any<CancellationToken>()).Returns(estados);

    private FlujoIncidenciaService Sut(IUsuarioActual? usuario = null) =>
        new(_flujos, _estados, _uow, usuario ?? Configurador(), new FlujoIncidenciaRequestValidator(),
            DatosPrueba.Reloj(), NullLogger<FlujoIncidenciaService>.Instance);

    // RN-11: crear un flujo con un estado inactivo → 400 ESTADO_INCIDENCIA_INVALIDO
    [Fact]
    public async Task CrearAsync_ConEstadoInactivo_LanzaEstadoIncidenciaInvalido()
    {
        // Arrange
        EstadosDelCatalogo(Estado(PorHacer), Estado(EnRevision, activo: false));

        // Act
        var ex = await Assert.ThrowsAsync<ValidacionNegocioException>(() => Sut().CrearAsync(
            new FlujoIncidenciaRequest("Con revisión", null, [new(PorHacer, true, false), new(EnRevision, false, true)]),
            CancellationToken.None));

        // Assert
        Assert.Equal(CodigosError.EstadoIncidenciaInvalido, ex.Codigo);
        await _flujos.DidNotReceive().AgregarAsync(Arg.Any<FlujoIncidencia>(), Arg.Any<CancellationToken>());
    }

    // RN-13, RN-14: crear válido inserta en transacción con orden por posición
    [Fact]
    public async Task CrearAsync_Valido_AgregaEnTransaccion()
    {
        // Act
        await Sut().CrearAsync(General with { Nombre = "Otro flujo" }, CancellationToken.None);

        // Assert
        Received.InOrder(() =>
        {
            _uow.IniciarAsync(Arg.Any<CancellationToken>());
            _flujos.AgregarAsync(Arg.Is<FlujoIncidencia>(f => f.EstadoInicialId == PorHacer && f.Estados.Count == 3),
                Arg.Any<CancellationToken>());
            _uow.ConfirmarAsync(Arg.Any<CancellationToken>());
        });
    }

    // RN-11: editar conservando un estado inactivo ya incluido (no inicial) → válido
    [Fact]
    public async Task EditarAsync_ConservaEstadoInactivoYaIncluido_Actualiza()
    {
        // Arrange
        EstadosDelCatalogo(Estado(PorHacer), Estado(EnProgreso, activo: false), Estado(Hecho));

        // Act
        await Sut().EditarAsync(FlujoGeneral, General with { Descripcion = "Nueva" }, CancellationToken.None);

        // Assert
        await _flujos.Received(1).ActualizarAsync(_general, Arg.Any<CancellationToken>());
    }

    // RN-11: editar agregando un estado inactivo → 400 ESTADO_INCIDENCIA_INVALIDO
    [Fact]
    public async Task EditarAsync_AgregaEstadoInactivo_LanzaEstadoIncidenciaInvalido()
    {
        // Arrange
        EstadosDelCatalogo(Estado(PorHacer), Estado(EnProgreso), Estado(Hecho), Estado(EnRevision, activo: false));

        // Act
        var ex = await Assert.ThrowsAsync<ValidacionNegocioException>(() => Sut().EditarAsync(FlujoGeneral,
            General with { Estados = [.. General.Estados!, new(EnRevision, false, false)] }, CancellationToken.None));

        // Assert
        Assert.Equal(CodigosError.EstadoIncidenciaInvalido, ex.Codigo);
    }

    // RN-15, CA-08: quitar "En progreso" con incidencias → 409 FLUJO_ESTADO_EN_USO y el flujo no cambia
    [Fact]
    public async Task EditarAsync_QuitaEstadoEnUso_LanzaFlujoEstadoEnUsoSinActualizar()
    {
        // Arrange
        _flujos.ContarIncidenciasPorEstadoAsync(FlujoGeneral, Arg.Any<CancellationToken>())
            .Returns([new CantidadPorEstado(EnProgreso, "En progreso", 4)]);

        // Act
        var ex = await Assert.ThrowsAsync<ConflictoException>(() => Sut().EditarAsync(FlujoGeneral,
            General with { Estados = [new(PorHacer, true, false), new(Hecho, false, true)] }, CancellationToken.None));

        // Assert
        Assert.Equal(CodigosError.FlujoEstadoEnUso, ex.Codigo);
        Assert.Equal(3, _general.Estados.Count);
        await _flujos.DidNotReceive().ActualizarAsync(Arg.Any<FlujoIncidencia>(), Arg.Any<CancellationToken>());
        await _uow.Received(1).RevertirAsync(Arg.Any<CancellationToken>());
    }

    // RN-15: cambiar el inicial no modifica incidencias; el nuevo inicial queda en el flujo
    [Fact]
    public async Task EditarAsync_CambiaElInicial_ActualizaSoloElFlujo()
    {
        // Act
        await Sut().EditarAsync(FlujoGeneral, General with
        {
            Estados = [new(PorHacer, false, false), new(EnProgreso, true, false), new(Hecho, false, true)],
        }, CancellationToken.None);

        // Assert
        Assert.Equal(EnProgreso, _general.EstadoInicialId);
        await _flujos.Received(1).ActualizarAsync(_general, Arg.Any<CancellationToken>());
    }

    // RN-13: nombre duplicado → 409 FLUJO_NOMBRE_DUPLICADO
    [Fact]
    public async Task CrearAsync_NombreDuplicado_LanzaFlujoNombreDuplicado()
    {
        // Arrange
        _flujos.ExisteNombreAsync("Flujo general", null, Arg.Any<CancellationToken>()).Returns(true);

        // Act
        var ex = await Assert.ThrowsAsync<ConflictoException>(() => Sut().CrearAsync(General, CancellationToken.None));

        // Assert
        Assert.Equal(CodigosError.FlujoNombreDuplicado, ex.Codigo);
    }

    // RN-01, CA-02: sin permiso → 403
    [Fact]
    public async Task CrearAsync_SinPermiso_LanzaPermisoDenegado()
    {
        // Act
        var accion = () => Sut(Usuario()).CrearAsync(General, CancellationToken.None);

        // Assert
        await Assert.ThrowsAsync<PermisoDenegadoException>(accion);
    }

    // Contrato PUT flujos: 404 FLUJO_NO_ENCONTRADO
    [Fact]
    public async Task EditarAsync_Inexistente_LanzaFlujoNoEncontrado()
    {
        // Act
        var ex = await Assert.ThrowsAsync<EntidadNoEncontradaException>(
            () => Sut().EditarAsync(99, General, CancellationToken.None));

        // Assert
        Assert.Equal(CodigosError.FlujoNoEncontrado, ex.Codigo);
    }
}
