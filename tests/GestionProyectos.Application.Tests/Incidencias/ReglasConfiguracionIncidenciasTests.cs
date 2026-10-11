using GestionProyectos.Domain.Entidades;
using GestionProyectos.Domain.Excepciones;
using GestionProyectos.Domain.Reglas;
using static GestionProyectos.Application.Tests.Comun.DatosIncidencias;

namespace GestionProyectos.Application.Tests.Incidencias;

/// <summary>
/// Tests de las reglas puras de configuración (RN-08, RN-11, RN-12, RN-14, RN-15).
/// </summary>
/// <remarks>
/// Creado: 2026-10-10
/// Modificado: 2026-10-10 - Versión inicial (tests RF-002)
/// </remarks>
public class ReglasConfiguracionIncidenciasTests
{
    // RN-08, CA-05: nivel del padre mayor o igual que el del hijo
    [Theory]
    [InlineData(3, 1)]
    [InlineData(3, 3)]
    public void AsegurarNivelesDeRelacion_PadreNoMenorQueHijo_LanzaRelacionNivelesInvalidos(int padre, int hijo)
    {
        // Act
        var ex = Assert.Throws<ValidacionNegocioException>(() =>
            ReglasConfiguracionIncidencias.AsegurarNivelesDeRelacion(Nivel(1, padre), Nivel(2, hijo)));

        // Assert
        Assert.Equal(CodigosError.RelacionNivelesInvalidos, ex.Codigo);
    }

    // RN-08: nivel del padre menor que el del hijo (no hace falta que sea consecutivo)
    [Theory]
    [InlineData(1, 2)]
    [InlineData(1, 3)]
    public void AsegurarNivelesDeRelacion_PadreMenorQueHijo_NoLanza(int padre, int hijo)
    {
        // Act
        var ex = Record.Exception(() =>
            ReglasConfiguracionIncidencias.AsegurarNivelesDeRelacion(Nivel(1, padre), Nivel(2, hijo)));

        // Assert
        Assert.Null(ex);
    }

    // RN-11: un estado inexistente no puede formar parte del flujo
    [Fact]
    public void AsegurarEstadosAsignables_EstadoInexistente_LanzaEstadoIncidenciaInvalido()
    {
        // Arrange
        EstadoEnFlujo[] composicion = [new(PorHacer, 1, true, false), new(99, 2, false, true)];

        // Act
        var ex = Assert.Throws<ValidacionNegocioException>(() =>
            ReglasConfiguracionIncidencias.AsegurarEstadosAsignables(composicion, [Estado(PorHacer)], null));

        // Assert
        Assert.Equal(CodigosError.EstadoIncidenciaInvalido, ex.Codigo);
    }

    // RN-11: un estado inactivo no puede marcarse inicial (aunque ya esté en el flujo)
    [Fact]
    public void AsegurarEstadosAsignables_InicialInactivo_LanzaEstadoIncidenciaInvalido()
    {
        // Arrange
        EstadoEnFlujo[] composicion = [new(EnProgreso, 1, true, false)];

        // Act
        var ex = Assert.Throws<ValidacionNegocioException>(() =>
            ReglasConfiguracionIncidencias.AsegurarEstadosAsignables(composicion,
                [Estado(EnProgreso, activo: false)], FlujoGeneralConEstados()));

        // Assert
        Assert.Equal(CodigosError.EstadoIncidenciaInvalido, ex.Codigo);
    }

    // RN-15: quitar un estado con incidencias de tipos que usan el flujo
    [Fact]
    public void AsegurarEstadosQuitadosSinUso_QuitaEstadoConIncidencias_LanzaFlujoEstadoEnUso()
    {
        // Arrange
        EstadoEnFlujo[] nueva = [new(PorHacer, 1, true, false), new(Hecho, 2, false, true)];

        // Act
        var ex = Assert.Throws<ConflictoException>(() => ReglasConfiguracionIncidencias.AsegurarEstadosQuitadosSinUso(
            FlujoGeneralConEstados(), nueva, [new CantidadPorEstado(EnProgreso, "En progreso", 5)]));

        // Assert
        Assert.Equal(CodigosError.FlujoEstadoEnUso, ex.Codigo);
    }

    // RN-15: quitar un estado sin incidencias es válido
    [Fact]
    public void AsegurarEstadosQuitadosSinUso_QuitaEstadoSinIncidencias_NoLanza()
    {
        // Arrange
        EstadoEnFlujo[] nueva = [new(PorHacer, 1, true, false), new(Hecho, 2, false, true)];

        // Act
        var ex = Record.Exception(() => ReglasConfiguracionIncidencias.AsegurarEstadosQuitadosSinUso(
            FlujoGeneralConEstados(), nueva, [new CantidadPorEstado(PorHacer, "Por hacer", 5)]));

        // Assert
        Assert.Null(ex);
    }

    // RN-12, CA-09: el nuevo flujo no incluye estados con incidencias del tipo
    [Fact]
    public void AsegurarFlujoCompatible_EstadoConIncidenciasAusente_LanzaTipoFlujoIncompatible()
    {
        // Act
        var ex = Assert.Throws<ConflictoException>(() => ReglasConfiguracionIncidencias.AsegurarFlujoCompatible(
            FlujoBugConEstados(), [new CantidadPorEstado(EnProgreso, "En progreso", 2)]));

        // Assert
        Assert.Equal(CodigosError.TipoFlujoIncompatible, ex.Codigo);
        Assert.Contains("En progreso (2)", ex.Message);
    }

    // RN-12: todas las incidencias en estados del nuevo flujo
    [Fact]
    public void AsegurarFlujoCompatible_EstadosIncluidos_NoLanza()
    {
        // Act
        var ex = Record.Exception(() => ReglasConfiguracionIncidencias.AsegurarFlujoCompatible(
            FlujoBugConEstados(), [new CantidadPorEstado(Hecho, "Hecho", 2), new CantidadPorEstado(EnProgreso, "En progreso", 0)]));

        // Assert
        Assert.Null(ex);
    }
}
