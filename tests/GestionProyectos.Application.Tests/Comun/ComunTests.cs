using GestionProyectos.Application.Abstracciones.Persistencia;
using GestionProyectos.Application.Comun;
using NSubstitute;

namespace GestionProyectos.Application.Tests.Comun;

/// <summary>
/// Tests de utilidades comunes: normalización de textos, reloj y unidad de trabajo.
/// </summary>
/// <remarks>
/// Creado: 2026-10-09
/// Modificado: 2026-10-09 - Versión inicial (tests RF-001)
/// </remarks>
public class ComunTests
{
    // RN-31: trim; solo espacios equivale a vacío (null)
    [Theory]
    [InlineData("  Ana  ", "Ana")]
    [InlineData("   ", null)]
    [InlineData("", null)]
    [InlineData(null, null)]
    public void Normalizar_Texto_DevuelveRecortadoONull(string? entrada, string? esperado)
    {
        // Act
        var resultado = TextoEntrada.Normalizar(entrada);

        // Assert
        Assert.Equal(esperado, resultado);
    }

    // RN-02: email en minúsculas y sin espacios
    [Fact]
    public void NormalizarMinusculas_EmailConMayusculasYEspacios_DevuelveMinusculasRecortado()
    {
        // Act
        var resultado = TextoEntrada.NormalizarMinusculas("  Ana.Gomez@Empresa.COM ");

        // Assert
        Assert.Equal("ana.gomez@empresa.com", resultado);
    }

    // Spec 0.2: fechas con hora en UTC con precisión de segundos (DATETIME2(0))
    [Fact]
    public void AhoraUtc_RelojConMilisegundos_DevuelveUtcTruncadoASegundos()
    {
        // Arrange
        var reloj = DatosPrueba.Reloj();

        // Act
        var ahora = reloj.AhoraUtc();

        // Assert
        Assert.Equal(DatosPrueba.AhoraTruncado, ahora);
        Assert.Equal(DateTimeKind.Utc, ahora.Kind);
    }

    // RN-10, RN-21, RN-30: la operación se confirma dentro de la transacción
    [Fact]
    public async Task EjecutarEnTransaccionAsync_OperacionExitosa_IniciaYConfirma()
    {
        // Arrange
        var uow = Substitute.For<IUnitOfWork>();

        // Act
        await uow.EjecutarEnTransaccionAsync(() => Task.CompletedTask, CancellationToken.None);

        // Assert
        Received.InOrder(() =>
        {
            uow.IniciarAsync(Arg.Any<CancellationToken>());
            uow.ConfirmarAsync(Arg.Any<CancellationToken>());
        });
        await uow.DidNotReceive().RevertirAsync(Arg.Any<CancellationToken>());
    }

    // CA-23: si la operación falla no se guarda nada (rollback)
    [Fact]
    public async Task EjecutarEnTransaccionAsync_OperacionFalla_RevierteYRelanza()
    {
        // Arrange
        var uow = Substitute.For<IUnitOfWork>();

        // Act
        var accion = () => uow.EjecutarEnTransaccionAsync(
            () => throw new InvalidOperationException("falla"), CancellationToken.None);

        // Assert
        await Assert.ThrowsAsync<InvalidOperationException>(accion);
        await uow.Received(1).RevertirAsync(Arg.Any<CancellationToken>());
        await uow.DidNotReceive().ConfirmarAsync(Arg.Any<CancellationToken>());
    }
}
