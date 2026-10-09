using GestionProyectos.Application.Personas;
using GestionProyectos.Application.Tests.Comun;

namespace GestionProyectos.Application.Tests.Personas;

/// <summary>
/// Tests del validador de formato de PersonaRequest (RN-01, RN-02, RN-31).
/// </summary>
/// <remarks>
/// Creado: 2026-10-09
/// Modificado: 2026-10-09 - Versión inicial (tests RF-001)
/// </remarks>
public class PersonaRequestValidatorTests
{
    private const string EmailValido = "ana.gomez@empresa.com";
    private readonly PersonaRequestValidator _sut = new();

    public static TheoryData<string?> NombresInvalidos => [null, "", "   ", DatosPrueba.Texto(151)];

    // 255 caracteres con formato válido: 243 + "@empresa.com" (12)
    public static TheoryData<string?> EmailsInvalidos =>
        [null, "", "   ", "ana", "ana@", "@empresa.com", "ana gomez@empresa.com", "a@b",
            DatosPrueba.Texto(243) + "@empresa.com"];

    // RN-01, RN-31: nombre obligatorio y máximo 150
    [Theory]
    [MemberData(nameof(NombresInvalidos))]
    public void Validate_NombreInvalido_DevuelveErrorEnNombre(string? nombre)
    {
        // Act
        var resultado = _sut.Validate(new PersonaRequest(nombre, EmailValido, false));

        // Assert
        Assert.Contains(resultado.Errors, e => e.PropertyName == nameof(PersonaRequest.Nombre));
    }

    // RN-02: email obligatorio, formato válido (backend exige dominio con punto) y máximo 254
    [Theory]
    [MemberData(nameof(EmailsInvalidos))]
    public void Validate_EmailInvalido_DevuelveErrorEnEmail(string? email)
    {
        // Act
        var resultado = _sut.Validate(new PersonaRequest("Ana", email, false));

        // Assert
        Assert.Contains(resultado.Errors, e => e.PropertyName == nameof(PersonaRequest.Email));
    }

    // RN-01, RN-02: límites exactos (150 y 254) son válidos
    [Fact]
    public void Validate_NombreDe150YEmailDe254_SinErrores()
    {
        // Arrange
        var email = DatosPrueba.Texto(242) + "@empresa.com";
        var request = new PersonaRequest(DatosPrueba.Texto(150), email, false);

        // Act
        var resultado = _sut.Validate(request);

        // Assert
        Assert.True(resultado.IsValid);
    }
}
