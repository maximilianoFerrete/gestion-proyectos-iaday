using GestionProyectos.Application.Abstracciones;
using GestionProyectos.Application.Abstracciones.Persistencia;
using GestionProyectos.Application.EstadosIncidencia;
using GestionProyectos.Application.Tests.Comun;
using GestionProyectos.Domain.Entidades;
using GestionProyectos.Domain.Excepciones;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using static GestionProyectos.Application.Tests.Comun.DatosIncidencias;

namespace GestionProyectos.Application.Tests.EstadosIncidencia;

/// <summary>
/// Tests de EstadoIncidenciaService (RN-01, RN-10, RN-11, CA-10).
/// </summary>
/// <remarks>
/// Creado: 2026-10-10
/// Modificado: 2026-10-10 - Versión inicial (tests RF-002)
/// </remarks>
public class EstadoIncidenciaServiceTests
{
    private readonly IEstadoIncidenciaRepository _estados = Substitute.For<IEstadoIncidenciaRepository>();
    private readonly EstadoIncidenciaRequest _request = new("En revisión", "Esperando revisión.", 25);

    public EstadoIncidenciaServiceTests()
    {
        _estados.ObtenerDtoAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(c => new EstadoIncidenciaDto(c.Arg<int>(), "Estado", null, 10, true, 0, []));
        _estados.ListarFlujosDondeEsInicialAsync(Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns([]);
    }

    private EstadoIncidenciaService Sut(IUsuarioActual? usuario = null) =>
        new(_estados, usuario ?? Configurador(), new EstadoIncidenciaRequestValidator(), DatosPrueba.Reloj(),
            NullLogger<EstadoIncidenciaService>.Instance);

    private EstadoIncidencia Existe(int id, bool activo = true)
    {
        var estado = Estado(id, $"Estado {id}", activo);
        _estados.ObtenerPorIdAsync(id, Arg.Any<CancellationToken>()).Returns(estado);
        return estado;
    }

    // RN-10: nombre duplicado → 409 ESTADO_INCIDENCIA_NOMBRE_DUPLICADO
    [Fact]
    public async Task CrearAsync_NombreDuplicado_LanzaEstadoIncidenciaNombreDuplicado()
    {
        // Arrange
        _estados.ExisteNombreAsync("En revisión", null, Arg.Any<CancellationToken>()).Returns(true);

        // Act
        var ex = await Assert.ThrowsAsync<ConflictoException>(() => Sut().CrearAsync(_request, CancellationToken.None));

        // Assert
        Assert.Equal(CodigosError.EstadoIncidenciaNombreDuplicado, ex.Codigo);
    }

    // RN-10: crea el estado activo
    [Fact]
    public async Task CrearAsync_Valido_AgregaEstadoActivo()
    {
        // Act
        await Sut().CrearAsync(_request, CancellationToken.None);

        // Assert
        await _estados.Received(1).AgregarAsync(Arg.Is<EstadoIncidencia>(e => e.Nombre == "En revisión"
            && e.Orden == 25 && e.Activo), Arg.Any<CancellationToken>());
    }

    // RN-01, CA-02: crear sin permiso → 403
    [Fact]
    public async Task CrearAsync_SinPermiso_LanzaPermisoDenegado()
    {
        // Act
        var accion = () => Sut(Usuario()).CrearAsync(_request, CancellationToken.None);

        // Assert
        await Assert.ThrowsAsync<PermisoDenegadoException>(accion);
    }

    // RN-11, CA-10: desactivar el inicial de un flujo → 409 ESTADO_INCIDENCIA_INICIAL_EN_FLUJO
    [Fact]
    public async Task DesactivarAsync_InicialDeUnFlujo_LanzaEstadoIncidenciaInicialEnFlujo()
    {
        // Arrange
        Existe(PorHacer);
        _estados.ListarFlujosDondeEsInicialAsync(PorHacer, Arg.Any<CancellationToken>()).Returns(["Flujo general"]);

        // Act
        var ex = await Assert.ThrowsAsync<ConflictoException>(
            () => Sut().DesactivarAsync(PorHacer, CancellationToken.None));

        // Assert
        Assert.Equal(CodigosError.EstadoIncidenciaInicialEnFlujo, ex.Codigo);
        Assert.Contains("Flujo general", ex.Message);
    }

    // RN-11, CA-10: desactivar un estado con incidencias solo modifica el estado
    [Fact]
    public async Task DesactivarAsync_EstadoConIncidencias_DesactivaSoloElEstado()
    {
        // Arrange
        var estado = Existe(EnProgreso);

        // Act
        await Sut().DesactivarAsync(EnProgreso, CancellationToken.None);

        // Assert
        Assert.False(estado.Activo);
        await _estados.Received(1).ActualizarAsync(estado, Arg.Any<CancellationToken>());
    }

    // RN-11: desactivar un estado ya inactivo es idempotente
    [Fact]
    public async Task DesactivarAsync_YaInactivo_NoActualiza()
    {
        // Arrange
        Existe(EnProgreso, activo: false);

        // Act
        await Sut().DesactivarAsync(EnProgreso, CancellationToken.None);

        // Assert
        await _estados.DidNotReceive().ActualizarAsync(Arg.Any<EstadoIncidencia>(), Arg.Any<CancellationToken>());
    }

    // RN-10: activar un estado ya activo es idempotente
    [Fact]
    public async Task ActivarAsync_YaActivo_NoActualiza()
    {
        // Arrange
        Existe(EnProgreso);

        // Act
        await Sut().ActivarAsync(EnProgreso, CancellationToken.None);

        // Assert
        await _estados.DidNotReceive().ActualizarAsync(Arg.Any<EstadoIncidencia>(), Arg.Any<CancellationToken>());
    }

    // RN-10: activar un estado inactivo
    [Fact]
    public async Task ActivarAsync_Inactivo_Activa()
    {
        // Arrange
        var estado = Existe(EnProgreso, activo: false);

        // Act
        await Sut().ActivarAsync(EnProgreso, CancellationToken.None);

        // Assert
        Assert.True(estado.Activo);
    }

    // Contrato POST desactivar: 404 ESTADO_INCIDENCIA_NO_ENCONTRADO
    [Fact]
    public async Task DesactivarAsync_Inexistente_LanzaEstadoIncidenciaNoEncontrado()
    {
        // Act
        var ex = await Assert.ThrowsAsync<EntidadNoEncontradaException>(
            () => Sut().DesactivarAsync(99, CancellationToken.None));

        // Assert
        Assert.Equal(CodigosError.EstadoIncidenciaNoEncontrado, ex.Codigo);
    }

    public static TheoryData<EstadoIncidenciaRequest, string> RequestsInvalidos => new()
    {
        { new EstadoIncidenciaRequest("", null, 10), nameof(EstadoIncidenciaRequest.Nombre) },
        { new EstadoIncidenciaRequest(DatosPrueba.Texto(51), null, 10), nameof(EstadoIncidenciaRequest.Nombre) },
        { new EstadoIncidenciaRequest("Ok", DatosPrueba.Texto(251), 10), nameof(EstadoIncidenciaRequest.Descripcion) },
        { new EstadoIncidenciaRequest("Ok", null, 0), nameof(EstadoIncidenciaRequest.Orden) },
        { new EstadoIncidenciaRequest("Ok", null, 10000), nameof(EstadoIncidenciaRequest.Orden) },
        { new EstadoIncidenciaRequest("Ok", null, null), nameof(EstadoIncidenciaRequest.Orden) },
    };

    // RN-10: nombre 1–50, descripción ≤ 250, orden 1–9999
    [Theory]
    [MemberData(nameof(RequestsInvalidos))]
    public void Validator_CampoInvalido_DevuelveErrorEnElCampo(EstadoIncidenciaRequest request, string campo)
    {
        // Act
        var resultado = new EstadoIncidenciaRequestValidator().Validate(request);

        // Assert
        Assert.Contains(resultado.Errors, e => e.PropertyName == campo);
    }
}
