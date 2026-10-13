using GestionProyectos.Application.Comun;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;

namespace GestionProyectos.Application.Tests.Seguimiento;

/// <summary>
/// Tests de la fecha de referencia del servidor: HoyEn, opciones y validación (RF-005 0.1, RN-10).
/// </summary>
/// <remarks>
/// Creado: 2026-10-11
/// Modificado: 2026-10-11 - Versión inicial (tests RF-005)
/// </remarks>
public class FechaReferenciaTests
{
    private const string ZonaArgentina = "America/Argentina/Buenos_Aires";
    private static readonly TimeZoneInfo UtcMenos3 =
        TimeZoneInfo.CreateCustomTimeZone("UTC-3", TimeSpan.FromHours(-3), "UTC-3", "UTC-3");

    private static FakeTimeProvider RelojEn(int dia, int hora) =>
        new(new DateTimeOffset(2026, 10, dia, hora, 0, 0, TimeSpan.Zero));

    private static FechaReferenciaServidor Fecha(TimeProvider reloj, string? zona) =>
        new(reloj, Options.Create(new OpcionesFechaReferencia { ZonaHoraria = zona }));

    // RN-10: 02:00 UTC del 10/10 en UTC-3 es todavía el 09/10
    [Fact]
    public void HoyEn_DosUtcEnZonaMenos3_DevuelveElDiaAnterior()
    {
        // Act
        var hoy = RelojEn(10, 2).HoyEn(UtcMenos3);

        // Assert
        Assert.Equal(new DateOnly(2026, 10, 9), hoy);
    }

    // RN-10: 03:00 UTC del 10/10 en UTC-3 es el límite (00:00 del 10/10)
    [Fact]
    public void HoyEn_TresUtcEnZonaMenos3_DevuelveElMismoDia()
    {
        // Act
        var hoy = RelojEn(10, 3).HoyEn(UtcMenos3);

        // Assert
        Assert.Equal(new DateOnly(2026, 10, 10), hoy);
    }

    // RF-005 0.1: zona nula → ArgumentNullException
    [Fact]
    public void HoyEn_ZonaNula_LanzaArgumentNullException()
    {
        // Act
        Action accion = () => RelojEn(10, 3).HoyEn(null!);

        // Assert
        Assert.Throws<ArgumentNullException>(accion);
    }

    // RF-005 0.1: sin zona configurada se usa TimeProvider.LocalTimeZone
    [Fact]
    public void ResolverZona_SinZonaConfigurada_UsaLaZonaLocalDelReloj()
    {
        // Arrange
        var reloj = RelojEn(10, 2);
        reloj.SetLocalTimeZone(UtcMenos3);

        // Act
        var zona = new OpcionesFechaReferencia { ZonaHoraria = " " }.ResolverZona(reloj);

        // Assert
        Assert.Same(UtcMenos3, zona);
    }

    // RF-005 0.1: sin zona configurada, Hoy() usa la zona local del reloj
    [Fact]
    public void Hoy_SinZonaConfigurada_CalculaConLaZonaLocal()
    {
        // Arrange
        var reloj = RelojEn(10, 2);
        reloj.SetLocalTimeZone(UtcMenos3);

        // Act
        var hoy = Fecha(reloj, null).Hoy();

        // Assert
        Assert.Equal(new DateOnly(2026, 10, 9), hoy);
    }

    // CA-45: 02:00 UTC del 11/10 con zona Buenos Aires → la fecha de referencia es el 10/10
    [Fact]
    public void Hoy_ZonaBuenosAiresDosUtcDel11_DevuelveEl10()
    {
        // Act
        var hoy = Fecha(RelojEn(11, 2), ZonaArgentina).Hoy();

        // Assert
        Assert.Equal(new DateOnly(2026, 10, 10), hoy);
    }

    // RF-005 0.1: id de zona inválido → falla la validación de opciones (ValidateOnStart)
    [Fact]
    public void Validate_ZonaInvalida_Falla()
    {
        // Arrange
        var validador = new ValidadorOpcionesFechaReferencia(RelojEn(10, 2));

        // Act
        var resultado = validador.Validate(null, new OpcionesFechaReferencia { ZonaHoraria = "Zona/Inexistente" });

        // Assert
        Assert.True(resultado.Failed);
        Assert.Contains("Zona/Inexistente", resultado.FailureMessage);
    }

    // RF-005 0.1: id IANA válido o vacío → validación correcta
    [Theory]
    [InlineData(ZonaArgentina)]
    [InlineData(null)]
    public void Validate_ZonaValidaOVacia_EsCorrecta(string? zona)
    {
        // Arrange
        var validador = new ValidadorOpcionesFechaReferencia(RelojEn(10, 2));

        // Act
        var resultado = validador.Validate(null, new OpcionesFechaReferencia { ZonaHoraria = zona });

        // Assert
        Assert.True(resultado.Succeeded);
    }

    // RF-005 0.1: construir el servicio con una zona inválida falla con mensaje claro
    [Fact]
    public void Constructor_ZonaInvalida_LanzaInvalidOperationException()
    {
        // Act
        Action accion = () => Fecha(RelojEn(10, 2), "Zona/Inexistente");

        // Assert
        Assert.Throws<InvalidOperationException>(accion);
    }
}
