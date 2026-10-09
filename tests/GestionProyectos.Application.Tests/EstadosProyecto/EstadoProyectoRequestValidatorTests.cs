using GestionProyectos.Application.EstadosProyecto;
using GestionProyectos.Application.Tests.Comun;

namespace GestionProyectos.Application.Tests.EstadosProyecto;

/// <summary>
/// Tests del validador de formato de EstadoProyectoRequest (RN-07).
/// </summary>
/// <remarks>
/// Creado: 2026-10-09
/// Modificado: 2026-10-09 - Versión inicial (tests RF-001)
/// </remarks>
public class EstadoProyectoRequestValidatorTests
{
    private readonly EstadoProyectoRequestValidator _sut = new();

    public static TheoryData<string?> NombresInvalidos => [null, "", "  ", DatosPrueba.Texto(51)];

    public static TheoryData<int, string?> ValoresEnLimites => new()
    {
        { 1, null },
        { 9998, DatosPrueba.Texto(250) },
    };

    // RN-07: nombre obligatorio y máximo 50
    [Theory]
    [MemberData(nameof(NombresInvalidos))]
    public void Validate_NombreInvalido_DevuelveErrorEnNombre(string? nombre)
    {
        // Act
        var resultado = _sut.Validate(new EstadoProyectoRequest(nombre, null, 10, false));

        // Assert
        Assert.Contains(resultado.Errors, e => e.PropertyName == nameof(EstadoProyectoRequest.Nombre));
    }

    // RN-07: descripción máximo 250
    [Fact]
    public void Validate_DescripcionDe251_DevuelveErrorEnDescripcion()
    {
        // Act
        var resultado = _sut.Validate(new EstadoProyectoRequest("En revisión", DatosPrueba.Texto(251), 10, false));

        // Assert
        Assert.Contains(resultado.Errors, e => e.PropertyName == nameof(EstadoProyectoRequest.Descripcion));
    }

    // RN-07: orden obligatorio entre 1 y 9998 (9999 reservado al de sistema)
    [Theory]
    [InlineData(null)]
    [InlineData(0)]
    [InlineData(9999)]
    public void Validate_OrdenFueraDeRango_DevuelveErrorEnOrden(int? orden)
    {
        // Act
        var resultado = _sut.Validate(new EstadoProyectoRequest("En revisión", null, orden, false));

        // Assert
        Assert.Contains(resultado.Errors, e => e.PropertyName == nameof(EstadoProyectoRequest.Orden));
    }

    // RN-07: límites válidos (orden 1 y 9998, nombre 50, descripción 250 u omitida)
    [Theory]
    [MemberData(nameof(ValoresEnLimites))]
    public void Validate_ValoresEnLimites_SinErrores(int orden, string? descripcion)
    {
        // Act
        var resultado = _sut.Validate(new EstadoProyectoRequest(DatosPrueba.Texto(50), descripcion, orden, false));

        // Assert
        Assert.True(resultado.IsValid);
    }
}
