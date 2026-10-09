using GestionProyectos.Application.Agrupaciones;
using GestionProyectos.Application.Tests.Comun;

namespace GestionProyectos.Application.Tests.Agrupaciones;

/// <summary>
/// Tests del validador de formato de AgrupacionRequest (RN-26).
/// </summary>
/// <remarks>
/// Creado: 2026-10-09
/// Modificado: 2026-10-09 - Versión inicial (tests RF-001)
/// </remarks>
public class AgrupacionRequestValidatorTests
{
    private readonly AgrupacionRequestValidator _sut = new();

    public static TheoryData<AgrupacionRequest, string> RequestsInvalidos => new()
    {
        { new AgrupacionRequest("", "Descripción"), nameof(AgrupacionRequest.Nombre) },
        { new AgrupacionRequest("   ", "Descripción"), nameof(AgrupacionRequest.Nombre) },
        { new AgrupacionRequest(DatosPrueba.Texto(101), "Descripción"), nameof(AgrupacionRequest.Nombre) },
        { new AgrupacionRequest("Módulo", null), nameof(AgrupacionRequest.Descripcion) },
        { new AgrupacionRequest("Módulo", DatosPrueba.Texto(1001)), nameof(AgrupacionRequest.Descripcion) },
    };

    // RN-26: nombre 1–100 y descripción 1–1000 obligatorios
    [Theory]
    [MemberData(nameof(RequestsInvalidos))]
    public void Validate_CampoInvalido_DevuelveErrorEnElCampo(AgrupacionRequest request, string campo)
    {
        // Act
        var resultado = _sut.Validate(request);

        // Assert
        Assert.Contains(resultado.Errors, e => e.PropertyName == campo);
    }

    // RN-26: límites exactos 100 y 1000 son válidos
    [Fact]
    public void Validate_NombreDe100YDescripcionDe1000_SinErrores()
    {
        // Act
        var resultado = _sut.Validate(new AgrupacionRequest(DatosPrueba.Texto(100), DatosPrueba.Texto(1000)));

        // Assert
        Assert.True(resultado.IsValid);
    }
}
