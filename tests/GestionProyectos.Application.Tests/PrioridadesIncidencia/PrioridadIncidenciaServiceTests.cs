using GestionProyectos.Application.Abstracciones;
using GestionProyectos.Application.Abstracciones.Persistencia;
using GestionProyectos.Application.PrioridadesIncidencia;
using GestionProyectos.Application.Tests.Comun;
using GestionProyectos.Domain.Entidades;
using GestionProyectos.Domain.Excepciones;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using static GestionProyectos.Application.Tests.Comun.DatosIncidencias;

namespace GestionProyectos.Application.Tests.PrioridadesIncidencia;

/// <summary>
/// Tests de PrioridadIncidenciaService (RN-01, RN-17, CA-11).
/// </summary>
/// <remarks>
/// Creado: 2026-10-10
/// Modificado: 2026-10-10 - Versión inicial (tests RF-002)
/// </remarks>
public class PrioridadIncidenciaServiceTests
{
    private const int Media = 2;
    private const int Alta = 3;
    private readonly IPrioridadIncidenciaRepository _prioridades = Substitute.For<IPrioridadIncidenciaRepository>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();
    private readonly PrioridadIncidencia _media = Prioridad(Media, "Media", predeterminada: true);

    public PrioridadIncidenciaServiceTests()
    {
        _prioridades.ObtenerPorIdAsync(Media, Arg.Any<CancellationToken>()).Returns(_media);
        _prioridades.ObtenerPredeterminadaAsync(Arg.Any<CancellationToken>()).Returns(_media);
    }

    private PrioridadIncidenciaService Sut(IUsuarioActual? usuario = null) =>
        new(_prioridades, _uow, usuario ?? Configurador(), new PrioridadIncidenciaRequestValidator(),
            DatosPrueba.Reloj(), NullLogger<PrioridadIncidenciaService>.Instance);

    private PrioridadIncidencia ExisteAlta(bool activo = true)
    {
        var alta = Prioridad(Alta, "Alta", activo: activo);
        _prioridades.ObtenerPorIdAsync(Alta, Arg.Any<CancellationToken>()).Returns(alta);
        return alta;
    }

    // RN-17, CA-11: marcar "Alta" desmarca "Media" en la misma transacción
    [Fact]
    public async Task MarcarPredeterminadaAsync_OtraActiva_DesmarcaLaAnteriorEnTransaccion()
    {
        // Arrange
        var alta = ExisteAlta();

        // Act
        await Sut().MarcarPredeterminadaAsync(Alta, CancellationToken.None);

        // Assert
        Assert.Equal((false, true), (_media.EsPredeterminada, alta.EsPredeterminada));
        Received.InOrder(() =>
        {
            _uow.IniciarAsync(Arg.Any<CancellationToken>());
            _prioridades.ActualizarAsync(_media, Arg.Any<CancellationToken>());
            _prioridades.ActualizarAsync(alta, Arg.Any<CancellationToken>());
            _uow.ConfirmarAsync(Arg.Any<CancellationToken>());
        });
    }

    // RN-17: marcar la que ya es predeterminada → sin cambios
    [Fact]
    public async Task MarcarPredeterminadaAsync_YaPredeterminada_NoActualiza()
    {
        // Act
        await Sut().MarcarPredeterminadaAsync(Media, CancellationToken.None);

        // Assert
        await _prioridades.DidNotReceive().ActualizarAsync(Arg.Any<PrioridadIncidencia>(), Arg.Any<CancellationToken>());
    }

    // RN-17: marcar predeterminada una inactiva → 409 PRIORIDAD_NO_ELEGIBLE_PREDETERMINADA
    [Fact]
    public async Task MarcarPredeterminadaAsync_Inactiva_LanzaPrioridadNoElegiblePredeterminada()
    {
        // Arrange
        ExisteAlta(activo: false);

        // Act
        var ex = await Assert.ThrowsAsync<ConflictoException>(
            () => Sut().MarcarPredeterminadaAsync(Alta, CancellationToken.None));

        // Assert
        Assert.Equal(CodigosError.PrioridadNoElegiblePredeterminada, ex.Codigo);
        Assert.True(_media.EsPredeterminada);
    }

    // RN-17, CA-11: desactivar la predeterminada → 409 PRIORIDAD_PREDETERMINADA_NO_DESACTIVABLE
    [Fact]
    public async Task DesactivarAsync_Predeterminada_LanzaPrioridadPredeterminadaNoDesactivable()
    {
        // Act
        var ex = await Assert.ThrowsAsync<ConflictoException>(() => Sut().DesactivarAsync(Media, CancellationToken.None));

        // Assert
        Assert.Equal(CodigosError.PrioridadPredeterminadaNoDesactivable, ex.Codigo);
    }

    // RN-17: desactivar una no predeterminada solo modifica la prioridad
    [Fact]
    public async Task DesactivarAsync_NoPredeterminada_Desactiva()
    {
        // Arrange
        var alta = ExisteAlta();

        // Act
        await Sut().DesactivarAsync(Alta, CancellationToken.None);

        // Assert
        Assert.False(alta.Activo);
    }

    // RN-17: nombre duplicado → 409 PRIORIDAD_NOMBRE_DUPLICADO
    [Fact]
    public async Task CrearAsync_NombreDuplicado_LanzaPrioridadNombreDuplicado()
    {
        // Arrange
        _prioridades.ExisteNombreAsync("Bloqueante", null, Arg.Any<CancellationToken>()).Returns(true);

        // Act
        var ex = await Assert.ThrowsAsync<ConflictoException>(() => Sut().CrearAsync(
            new PrioridadIncidenciaRequest("Bloqueante", null, 50), CancellationToken.None));

        // Assert
        Assert.Equal(CodigosError.PrioridadNombreDuplicado, ex.Codigo);
    }

    // RN-17: se crea activa y no predeterminada
    [Fact]
    public async Task CrearAsync_Valido_DevuelveActivaNoPredeterminada()
    {
        // Act
        var dto = await Sut().CrearAsync(new PrioridadIncidenciaRequest("Bloqueante", null, 50), CancellationToken.None);

        // Assert
        Assert.Equal((true, false), (dto.Activo, dto.EsPredeterminada));
    }

    // RN-01, CA-02: sin permiso → 403
    [Fact]
    public async Task MarcarPredeterminadaAsync_SinPermiso_LanzaPermisoDenegado()
    {
        // Act
        var accion = () => Sut(Usuario()).MarcarPredeterminadaAsync(Alta, CancellationToken.None);

        // Assert
        await Assert.ThrowsAsync<PermisoDenegadoException>(accion);
    }
}
