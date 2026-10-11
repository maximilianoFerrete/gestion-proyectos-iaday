using FluentValidation;
using GestionProyectos.Application.Abstracciones;
using GestionProyectos.Application.Abstracciones.Persistencia;
using GestionProyectos.Application.NivelesIncidencia;
using GestionProyectos.Application.Tests.Comun;
using GestionProyectos.Domain.Entidades;
using GestionProyectos.Domain.Excepciones;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using static GestionProyectos.Application.Tests.Comun.DatosIncidencias;

namespace GestionProyectos.Application.Tests.NivelesIncidencia;

/// <summary>
/// Tests de NivelIncidenciaService y sus validadores (RN-01, RN-04).
/// </summary>
/// <remarks>
/// Creado: 2026-10-10
/// Modificado: 2026-10-10 - Versión inicial (tests RF-002)
/// </remarks>
public class NivelIncidenciaServiceTests
{
    private readonly INivelIncidenciaRepository _niveles = Substitute.For<INivelIncidenciaRepository>();
    private readonly CrearNivelIncidenciaRequest _alta = new(4, "Nivel 4", "Subdivisión de subtareas.");

    public NivelIncidenciaServiceTests()
    {
        _niveles.ObtenerDtoAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(c => new NivelIncidenciaDto(c.Arg<int>(), 4, "Nivel 4", null, 0, DatosPrueba.FechaAnterior, null));
    }

    private NivelIncidenciaService Sut(IUsuarioActual? usuario = null) =>
        new(_niveles, usuario ?? Configurador(), new CrearNivelIncidenciaRequestValidator(),
            new EditarNivelIncidenciaRequestValidator(), DatosPrueba.Reloj(), NullLogger<NivelIncidenciaService>.Instance);

    // RN-04: número duplicado → 409 NIVEL_NUMERO_DUPLICADO
    [Fact]
    public async Task CrearAsync_NumeroDuplicado_LanzaNivelNumeroDuplicado()
    {
        // Arrange
        _niveles.ExisteNumeroAsync(4, Arg.Any<CancellationToken>()).Returns(true);

        // Act
        var ex = await Assert.ThrowsAsync<ConflictoException>(() => Sut().CrearAsync(_alta, CancellationToken.None));

        // Assert
        Assert.Equal(CodigosError.NivelNumeroDuplicado, ex.Codigo);
    }

    // RN-04: nombre duplicado → 409 NIVEL_NOMBRE_DUPLICADO
    [Fact]
    public async Task CrearAsync_NombreDuplicado_LanzaNivelNombreDuplicado()
    {
        // Arrange
        _niveles.ExisteNombreAsync("Nivel 4", null, Arg.Any<CancellationToken>()).Returns(true);

        // Act
        var ex = await Assert.ThrowsAsync<ConflictoException>(() => Sut().CrearAsync(_alta, CancellationToken.None));

        // Assert
        Assert.Equal(CodigosError.NivelNombreDuplicado, ex.Codigo);
    }

    // RN-04: número fuera de 1–99 → error de validación en "numero"
    [Theory]
    [InlineData(0)]
    [InlineData(100)]
    public async Task CrearAsync_NumeroFueraDeRango_LanzaValidationException(int numero)
    {
        // Act
        var ex = await Assert.ThrowsAsync<ValidationException>(
            () => Sut().CrearAsync(_alta with { Numero = numero }, CancellationToken.None));

        // Assert
        Assert.Contains(ex.Errors, e => e.PropertyName == nameof(CrearNivelIncidenciaRequest.Numero));
    }

    // RN-04: límites 1 y 99 son válidos
    [Theory]
    [InlineData(1)]
    [InlineData(99)]
    public void CrearValidator_NumeroEnLimite_EsValido(int numero)
    {
        // Act
        var resultado = new CrearNivelIncidenciaRequestValidator().Validate(_alta with { Numero = numero });

        // Assert
        Assert.True(resultado.IsValid);
    }

    // RN-04: editar no cambia el número
    [Fact]
    public async Task EditarAsync_Valido_NoCambiaElNumero()
    {
        // Arrange
        var nivel = Nivel(2, 2);
        _niveles.ObtenerPorIdAsync(2, Arg.Any<CancellationToken>()).Returns(nivel);

        // Act
        await Sut().EditarAsync(2, new EditarNivelIncidenciaRequest("Historias", null), CancellationToken.None);

        // Assert
        Assert.Equal((2, "Historias"), (nivel.Numero, nivel.Nombre));
        await _niveles.Received(1).ActualizarAsync(nivel, Arg.Any<CancellationToken>());
    }

    // Contrato PUT niveles: 404 NIVEL_NO_ENCONTRADO
    [Fact]
    public async Task EditarAsync_Inexistente_LanzaNivelNoEncontrado()
    {
        // Act
        var ex = await Assert.ThrowsAsync<EntidadNoEncontradaException>(() => Sut().EditarAsync(99,
            new EditarNivelIncidenciaRequest("Otro", null), CancellationToken.None));

        // Assert
        Assert.Equal(CodigosError.NivelNoEncontrado, ex.Codigo);
    }

    // RN-01: sin permiso → 403, sin consultar el repositorio
    [Fact]
    public async Task CrearAsync_SinPermiso_LanzaPermisoDenegado()
    {
        // Act
        var accion = () => Sut(Usuario()).CrearAsync(_alta, CancellationToken.None);

        // Assert
        await Assert.ThrowsAsync<PermisoDenegadoException>(accion);
        await _niveles.DidNotReceive().AgregarAsync(Arg.Any<NivelIncidencia>(), Arg.Any<CancellationToken>());
    }
}
