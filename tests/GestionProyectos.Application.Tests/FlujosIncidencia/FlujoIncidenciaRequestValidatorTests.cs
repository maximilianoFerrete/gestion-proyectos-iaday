using GestionProyectos.Application.FlujosIncidencia;
using GestionProyectos.Application.Tests.Comun;

namespace GestionProyectos.Application.Tests.FlujosIncidencia;

/// <summary>
/// Tests de FlujoIncidenciaRequestValidator (RN-13, RN-14, CA-07).
/// </summary>
/// <remarks>
/// Creado: 2026-10-10
/// Modificado: 2026-10-10 - Versión inicial (tests RF-002)
/// </remarks>
public class FlujoIncidenciaRequestValidatorTests
{
    private readonly FlujoIncidenciaRequestValidator _sut = new();

    private static FlujoIncidenciaRequest Request(params EstadoFlujoRequest[] estados) =>
        new("Flujo con revisión", null, estados);

    public static TheoryData<EstadoFlujoRequest[]> ComposicionesInvalidas => new()
    {
        Array.Empty<EstadoFlujoRequest>(),
        new EstadoFlujoRequest[] { new(1, true, false), new(1, false, true) },
        new EstadoFlujoRequest[] { new(1, false, false), new(3, false, true) },
        new EstadoFlujoRequest[] { new(1, true, false), new(3, true, false) },
        new EstadoFlujoRequest[] { new(1, true, true) },
        new EstadoFlujoRequest[] { new(null, true, false) },
    };

    // RN-13, RN-14, CA-07: vacío, repetido, 0 o 2 iniciales, inicial y final, sin estadoId → error en "estados"
    [Theory]
    [MemberData(nameof(ComposicionesInvalidas))]
    public void Validate_ComposicionInvalida_ErrorEnEstados(EstadoFlujoRequest[] estados)
    {
        // Act
        var resultado = _sut.Validate(Request(estados));

        // Assert
        Assert.Contains(resultado.Errors, e => e.PropertyName == nameof(FlujoIncidenciaRequest.Estados));
    }

    // RN-13: estados nulo → error en "estados"
    [Fact]
    public void Validate_EstadosNulo_ErrorEnEstados()
    {
        // Act
        var resultado = _sut.Validate(new FlujoIncidenciaRequest("Flujo", null, null));

        // Assert
        Assert.Contains(resultado.Errors, e => e.PropertyName == nameof(FlujoIncidenciaRequest.Estados));
    }

    // RN-14: un inicial y cero finales es válido
    [Fact]
    public void Validate_UnInicialSinFinales_EsValido()
    {
        // Act
        var resultado = _sut.Validate(Request(new(1, true, false), new(2, false, false)));

        // Assert
        Assert.True(resultado.IsValid);
    }

    // RN-14: un inicial y varios finales es válido
    [Fact]
    public void Validate_UnInicialVariosFinales_EsValido()
    {
        // Act
        var resultado = _sut.Validate(Request(new(1, true, false), new(2, false, true), new(3, false, true)));

        // Assert
        Assert.True(resultado.IsValid);
    }

    public static TheoryData<string?> NombresInvalidos => new() { "", "   ", null, DatosPrueba.Texto(101) };

    // RN-13: nombre vacío o de 101 caracteres
    [Theory]
    [MemberData(nameof(NombresInvalidos))]
    public void Validate_NombreInvalido_ErrorEnNombre(string? nombre)
    {
        // Act
        var resultado = _sut.Validate(new FlujoIncidenciaRequest(nombre, null, [new(1, true, false)]));

        // Assert
        Assert.Contains(resultado.Errors, e => e.PropertyName == nameof(FlujoIncidenciaRequest.Nombre));
    }

    // RN-13: nombre de 100 caracteres es válido
    [Fact]
    public void Validate_NombreDe100_EsValido()
    {
        // Act
        var resultado = _sut.Validate(new FlujoIncidenciaRequest(DatosPrueba.Texto(100), null, [new(1, true, false)]));

        // Assert
        Assert.True(resultado.IsValid);
    }

    // RN-13: descripción de 251 caracteres
    [Fact]
    public void Validate_DescripcionDe251_ErrorEnDescripcion()
    {
        // Act
        var resultado = _sut.Validate(new FlujoIncidenciaRequest("Flujo", DatosPrueba.Texto(251), [new(1, true, false)]));

        // Assert
        Assert.Contains(resultado.Errors, e => e.PropertyName == nameof(FlujoIncidenciaRequest.Descripcion));
    }

    // RN-13: el orden es la posición en la lista (desde 1)
    [Fact]
    public void AComposicion_ListaDeEstados_AsignaOrdenPorPosicion()
    {
        // Arrange
        var request = Request(new(4, true, false), new(2, false, false), new(3, false, true));

        // Act
        var composicion = request.AComposicion();

        // Assert
        Assert.Equal([(4, 1), (2, 2), (3, 3)], composicion.Select(e => (e.EstadoId, e.Orden)));
    }
}
