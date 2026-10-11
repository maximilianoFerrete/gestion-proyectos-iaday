using GestionProyectos.Application.Incidencias;

namespace GestionProyectos.Application.Tests.Incidencias;

/// <summary>
/// Tests de FormateadorCodigoIncidencia (RN-18, DA-15).
/// </summary>
/// <remarks>
/// Creado: 2026-10-10
/// Modificado: 2026-10-10 - Versión inicial (tests RF-002)
/// </remarks>
public class FormateadorCodigoIncidenciaTests
{
    // RN-18, DA-15, CA-13: código del proyecto + guion + número sin relleno
    [Theory]
    [InlineData("PRJ-0001", 15, "PRJ-0001-15")]
    [InlineData("PRJ-0001", 1, "PRJ-0001-1")]
    [InlineData("PRJ-10000", 1, "PRJ-10000-1")]
    public void Formatear_CodigoYNumero_DevuelveCodigoSinRelleno(string proyecto, int numero, string esperado)
    {
        // Act
        var codigo = FormateadorCodigoIncidencia.Formatear(proyecto, numero);

        // Assert
        Assert.Equal(esperado, codigo);
    }

    // RN-18: el número es correlativo desde 1
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Formatear_NumeroCeroONegativo_LanzaArgumentOutOfRange(int numero)
    {
        // Act
        Action accion = () => FormateadorCodigoIncidencia.Formatear("PRJ-0001", numero);

        // Assert
        Assert.Throws<ArgumentOutOfRangeException>(accion);
    }

    // RN-18: el código del proyecto es obligatorio
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Formatear_CodigoProyectoVacio_LanzaArgumentException(string proyecto)
    {
        // Act
        Action accion = () => FormateadorCodigoIncidencia.Formatear(proyecto, 1);

        // Assert
        Assert.Throws<ArgumentException>(accion);
    }
}
