using GestionProyectos.Application.Proyectos;

namespace GestionProyectos.Application.Tests.Proyectos;

/// <summary>
/// Tests del formateo del código de proyecto (RN-14).
/// </summary>
/// <remarks>
/// Creado: 2026-10-09
/// Modificado: 2026-10-09 - Versión inicial (tests RF-001)
/// </remarks>
public class FormateadorCodigoProyectoTests
{
    // RN-14, CA-15: PRJ- con relleno a 4 dígitos; desde 10000 sin relleno
    [Theory]
    [InlineData(1, "PRJ-0001")]
    [InlineData(2, "PRJ-0002")]
    [InlineData(9999, "PRJ-9999")]
    [InlineData(10000, "PRJ-10000")]
    public void Formatear_NumeroPositivo_DevuelveCodigoConRelleno(int numero, string esperado)
    {
        // Act
        var codigo = FormateadorCodigoProyecto.Formatear(numero);

        // Assert
        Assert.Equal(esperado, codigo);
    }

    // RN-14: la secuencia nunca produce 0 ni negativos
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Formatear_CeroONegativo_LanzaArgumentOutOfRange(int numero)
    {
        // Act
        var accion = () => FormateadorCodigoProyecto.Formatear(numero);

        // Assert
        Assert.Throws<ArgumentOutOfRangeException>(accion);
    }
}
