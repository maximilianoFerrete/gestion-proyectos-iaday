using GestionProyectos.Application.Abstracciones;
using GestionProyectos.Application.Abstracciones.Persistencia;
using GestionProyectos.Application.Comun;
using GestionProyectos.Application.Tests.Comun;
using GestionProyectos.Application.TiposIncidencia;
using GestionProyectos.Domain.Entidades;
using GestionProyectos.Domain.Excepciones;
using GestionProyectos.Domain.Reglas;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using static GestionProyectos.Application.Tests.Comun.DatosIncidencias;

namespace GestionProyectos.Application.Tests.TiposIncidencia;

/// <summary>
/// Tests de TipoIncidenciaService (RN-01, RN-05 a RN-07, RN-12).
/// </summary>
/// <remarks>
/// Creado: 2026-10-10
/// Modificado: 2026-10-10 - Versión inicial (tests RF-002)
/// </remarks>
public class TipoIncidenciaServiceTests
{
    private const int TipoId = 2;
    private readonly ITipoIncidenciaRepository _tipos = Substitute.For<ITipoIncidenciaRepository>();
    private readonly INivelIncidenciaRepository _niveles = Substitute.For<INivelIncidenciaRepository>();
    private readonly IFlujoIncidenciaRepository _flujos = Substitute.For<IFlujoIncidenciaRepository>();
    private readonly CrearTipoIncidenciaRequest _alta = new("Spike", "Investigación acotada.", 3, FlujoGeneral);
    private readonly EditarTipoIncidenciaRequest _edicion = new("Historia de Usuario", null, FlujoGeneral);

    public TipoIncidenciaServiceTests()
    {
        _niveles.ObtenerPorIdAsync(3, Arg.Any<CancellationToken>()).Returns(Nivel(3, 3));
        _flujos.ObtenerPorIdAsync(FlujoGeneral, Arg.Any<CancellationToken>()).Returns(FlujoGeneralConEstados());
        _flujos.ObtenerPorIdAsync(FlujoBug, Arg.Any<CancellationToken>()).Returns(FlujoBugConEstados());
        _tipos.ObtenerDtoAsync(Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(c => new TipoIncidenciaDto(
            c.Arg<int>(), "Tipo", null, new NivelReferenciaDto(3, 3, "Nivel 3"),
            new FlujoReferenciaDto(FlujoGeneral, "Flujo general"), true, 0, DatosPrueba.FechaAnterior, null));
    }

    private TipoIncidenciaService Sut(IUsuarioActual? usuario = null) =>
        new(_tipos, _niveles, _flujos, usuario ?? Configurador(),
            new TipoIncidenciaValidadores(new CrearTipoIncidenciaRequestValidator(),
                new EditarTipoIncidenciaRequestValidator()),
            DatosPrueba.Reloj(), NullLogger<TipoIncidenciaService>.Instance);

    private TipoIncidencia ExisteTipo(bool activo = true)
    {
        var tipo = Tipo(TipoId, "Historia de Usuario", nivelId: 2, activo: activo);
        _tipos.ObtenerPorIdAsync(TipoId, Arg.Any<CancellationToken>()).Returns(tipo);
        return tipo;
    }

    // RN-01, CA-02: crear sin el permiso de configuración → 403
    [Fact]
    public async Task CrearAsync_SinPermiso_LanzaPermisoDenegado()
    {
        // Act
        var accion = () => Sut(Usuario()).CrearAsync(_alta, CancellationToken.None);

        // Assert
        await Assert.ThrowsAsync<PermisoDenegadoException>(accion);
        await _tipos.DidNotReceive().AgregarAsync(Arg.Any<TipoIncidencia>(), Arg.Any<CancellationToken>());
    }

    // RN-01, CA-02: crear sin usuario → 401
    [Fact]
    public async Task CrearAsync_SinUsuario_LanzaUsuarioNoIdentificado()
    {
        // Act
        var accion = () => Sut(UsuarioAnonimo()).CrearAsync(_alta, CancellationToken.None);

        // Assert
        await Assert.ThrowsAsync<UsuarioNoIdentificadoException>(accion);
    }

    // RN-05: nombre duplicado (otra capitalización) → 409 TIPO_NOMBRE_DUPLICADO
    [Fact]
    public async Task CrearAsync_NombreDuplicado_LanzaTipoNombreDuplicado()
    {
        // Arrange
        _tipos.ExisteNombreAsync("SPIKE", null, Arg.Any<CancellationToken>()).Returns(true);

        // Act
        var ex = await Assert.ThrowsAsync<ConflictoException>(
            () => Sut().CrearAsync(_alta with { Nombre = "SPIKE" }, CancellationToken.None));

        // Assert
        Assert.Equal(CodigosError.TipoNombreDuplicado, ex.Codigo);
    }

    // RN-05: nivel inexistente → 400 NIVEL_INVALIDO
    [Fact]
    public async Task CrearAsync_NivelInexistente_LanzaNivelInvalido()
    {
        // Act
        var ex = await Assert.ThrowsAsync<ValidacionNegocioException>(
            () => Sut().CrearAsync(_alta with { NivelId = 99 }, CancellationToken.None));

        // Assert
        Assert.Equal(CodigosError.NivelInvalido, ex.Codigo);
    }

    // RN-05: flujo inexistente → 400 FLUJO_INVALIDO
    [Fact]
    public async Task CrearAsync_FlujoInexistente_LanzaFlujoInvalido()
    {
        // Act
        var ex = await Assert.ThrowsAsync<ValidacionNegocioException>(
            () => Sut().CrearAsync(_alta with { FlujoId = 99 }, CancellationToken.None));

        // Assert
        Assert.Equal(CodigosError.FlujoInvalido, ex.Codigo);
    }

    // RN-05, CA-04: crea el tipo activo en el nivel y flujo indicados
    [Fact]
    public async Task CrearAsync_Valido_AgregaTipoActivo()
    {
        // Act
        await Sut().CrearAsync(_alta, CancellationToken.None);

        // Assert
        await _tipos.Received(1).AgregarAsync(Arg.Is<TipoIncidencia>(t => t.Nombre == "Spike" && t.NivelId == 3
            && t.FlujoId == FlujoGeneral && t.Activo), Arg.Any<CancellationToken>());
    }

    // RN-06: el nivel no se edita (el request no lo acepta) y no cambia
    [Fact]
    public async Task EditarAsync_Valido_NoCambiaElNivel()
    {
        // Arrange
        var tipo = ExisteTipo();

        // Act
        await Sut().EditarAsync(TipoId, _edicion with { Nombre = "HU" }, CancellationToken.None);

        // Assert
        Assert.Equal(2, tipo.NivelId);
        Assert.Equal("HU", tipo.Nombre);
    }

    // RN-12, CA-09: cambiar a un flujo sin los estados de sus incidencias → 409 TIPO_FLUJO_INCOMPATIBLE
    [Fact]
    public async Task EditarAsync_CambiaFlujoConIncidenciasEnEstadosAusentes_LanzaTipoFlujoIncompatible()
    {
        // Arrange
        ExisteTipo();
        _tipos.ContarIncidenciasPorEstadoAsync(TipoId, Arg.Any<CancellationToken>())
            .Returns([new CantidadPorEstado(EnProgreso, "En progreso", 3)]);

        // Act
        var ex = await Assert.ThrowsAsync<ConflictoException>(
            () => Sut().EditarAsync(TipoId, _edicion with { FlujoId = FlujoBug }, CancellationToken.None));

        // Assert
        Assert.Equal(CodigosError.TipoFlujoIncompatible, ex.Codigo);
        await _tipos.DidNotReceive().ActualizarAsync(Arg.Any<TipoIncidencia>(), Arg.Any<CancellationToken>());
    }

    // RN-12: cambiar a un flujo compatible actualiza el flujo
    [Fact]
    public async Task EditarAsync_CambiaFlujoCompatible_ActualizaFlujo()
    {
        // Arrange
        var tipo = ExisteTipo();
        _tipos.ContarIncidenciasPorEstadoAsync(TipoId, Arg.Any<CancellationToken>())
            .Returns([new CantidadPorEstado(Hecho, "Hecho", 3)]);

        // Act
        await Sut().EditarAsync(TipoId, _edicion with { FlujoId = FlujoBug }, CancellationToken.None);

        // Assert
        Assert.Equal(FlujoBug, tipo.FlujoId);
        await _tipos.Received(1).ActualizarAsync(tipo, Arg.Any<CancellationToken>());
    }

    // RN-12: sin cambio de flujo no se cuentan incidencias por estado
    [Fact]
    public async Task EditarAsync_MismoFlujo_NoVerificaIncidencias()
    {
        // Arrange
        ExisteTipo();

        // Act
        await Sut().EditarAsync(TipoId, _edicion, CancellationToken.None);

        // Assert
        await _tipos.DidNotReceive().ContarIncidenciasPorEstadoAsync(Arg.Any<int>(), Arg.Any<CancellationToken>());
    }

    // Contrato PUT tipos: 404 TIPO_INCIDENCIA_NO_ENCONTRADO
    [Fact]
    public async Task EditarAsync_TipoInexistente_LanzaTipoIncidenciaNoEncontrado()
    {
        // Act
        var ex = await Assert.ThrowsAsync<EntidadNoEncontradaException>(
            () => Sut().EditarAsync(99, _edicion, CancellationToken.None));

        // Assert
        Assert.Equal(CodigosError.TipoIncidenciaNoEncontrado, ex.Codigo);
    }

    // RN-07: desactivar un tipo (con incidencias) solo modifica el tipo
    [Fact]
    public async Task DesactivarAsync_TipoActivo_DesactivaSinTocarIncidencias()
    {
        // Arrange
        var tipo = ExisteTipo();

        // Act
        await Sut().DesactivarAsync(TipoId, CancellationToken.None);

        // Assert
        Assert.False(tipo.Activo);
        await _tipos.Received(1).ActualizarAsync(tipo, Arg.Any<CancellationToken>());
        await _tipos.DidNotReceive().ContarIncidenciasPorEstadoAsync(Arg.Any<int>(), Arg.Any<CancellationToken>());
    }

    // RN-07: desactivar un tipo ya inactivo es idempotente
    [Fact]
    public async Task DesactivarAsync_TipoInactivo_NoActualiza()
    {
        // Arrange
        ExisteTipo(activo: false);

        // Act
        await Sut().DesactivarAsync(TipoId, CancellationToken.None);

        // Assert
        await _tipos.DidNotReceive().ActualizarAsync(Arg.Any<TipoIncidencia>(), Arg.Any<CancellationToken>());
    }

    // RN-07: activar un tipo inactivo
    [Fact]
    public async Task ActivarAsync_TipoInactivo_Activa()
    {
        // Arrange
        var tipo = ExisteTipo(activo: false);

        // Act
        await Sut().ActivarAsync(TipoId, CancellationToken.None);

        // Assert
        Assert.True(tipo.Activo);
    }

    // RN-01: activar/desactivar exige permiso
    [Fact]
    public async Task DesactivarAsync_SinPermiso_LanzaPermisoDenegado()
    {
        // Arrange
        ExisteTipo();

        // Act
        var accion = () => Sut(Usuario()).DesactivarAsync(TipoId, CancellationToken.None);

        // Assert
        await Assert.ThrowsAsync<PermisoDenegadoException>(accion);
    }

    // RN-01: las consultas no exigen permiso
    [Fact]
    public async Task ListarAsync_SinUsuario_DevuelveLista()
    {
        // Arrange
        _tipos.ListarAsync(true, 3, Arg.Any<CancellationToken>()).Returns([]);

        // Act
        var lista = await Sut(UsuarioAnonimo()).ListarAsync(true, 3, CancellationToken.None);

        // Assert
        Assert.Empty(lista);
    }
}
