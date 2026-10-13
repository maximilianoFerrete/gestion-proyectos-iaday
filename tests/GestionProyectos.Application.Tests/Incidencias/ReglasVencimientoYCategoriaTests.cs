using GestionProyectos.Domain.Comun;
using GestionProyectos.Domain.Entidades;
using GestionProyectos.Domain.Excepciones;
using GestionProyectos.Domain.Reglas;
using static GestionProyectos.Application.Tests.Comun.DatosIncidencias;

namespace GestionProyectos.Application.Tests.Incidencias;

/// <summary>
/// Tests de las reglas puras ReglasVencimiento (RN-11) y ReglasCategoriaEstado (RN-18) de RF-005.
/// </summary>
/// <remarks>
/// Creado: 2026-10-11
/// Modificado: 2026-10-11 - Versión inicial (tests RF-005)
/// </remarks>
public class ReglasVencimientoYCategoriaTests
{
    private static readonly DateOnly Hoy = new(2026, 10, 10);

    // RN-11: sin fecha de vencimiento nunca está vencida
    [Fact]
    public void EstaVencida_FechaNula_DevuelveFalse()
    {
        // Act
        var vencida = ReglasVencimiento.EstaVencida(null, false, Hoy);

        // Assert
        Assert.False(vencida);
    }

    // RN-11, CA-07: la que vence hoy no está vencida
    [Fact]
    public void EstaVencida_FechaIgualALaDeReferenciaNoFinal_DevuelveFalse()
    {
        // Act
        var vencida = ReglasVencimiento.EstaVencida(Hoy, false, Hoy);

        // Assert
        Assert.False(vencida);
    }

    // RN-11, CA-07: abierta con vencimiento anterior → vencida
    [Fact]
    public void EstaVencida_FechaAnteriorNoFinal_DevuelveTrue()
    {
        // Act
        var vencida = ReglasVencimiento.EstaVencida(Hoy.AddDays(-1), false, Hoy);

        // Assert
        Assert.True(vencida);
    }

    // RN-11, CA-07: finalizada con vencimiento anterior no cuenta como vencida
    [Fact]
    public void EstaVencida_FechaAnteriorFinal_DevuelveFalse()
    {
        // Act
        var vencida = ReglasVencimiento.EstaVencida(Hoy.AddDays(-9), true, Hoy);

        // Assert
        Assert.False(vencida);
    }

    // RN-18: categoría con inicial o final es incompatible; sin marcas o sin categoría es compatible
    [Theory]
    [InlineData(CategoriasEstadoIncidencia.Bloqueada, false, true, false)]
    [InlineData(CategoriasEstadoIncidencia.EnEspera, true, false, false)]
    [InlineData(CategoriasEstadoIncidencia.Bloqueada, false, false, true)]
    [InlineData(null, true, false, true)]
    [InlineData(null, false, true, true)]
    public void EsCompatible_SegunCategoriaYMarcas_DevuelveLoEsperado(string? categoria, bool esInicial,
        bool esFinal, bool esperado)
    {
        // Act
        var compatible = ReglasCategoriaEstado.EsCompatible(categoria, esInicial, esFinal);

        // Assert
        Assert.Equal(esperado, compatible);
    }

    // RN-18 (a), CA-39: asignar categoría a un estado inicial o final en algún flujo → 409
    [Fact]
    public void AsegurarCategoriaAsignable_EstadoFinalEnUnFlujo_LanzaEstadoCategoriaIncompatible()
    {
        // Act
        var ex = Assert.Throws<ConflictoException>(() => ReglasCategoriaEstado.AsegurarCategoriaAsignable("Hecho",
            CategoriasEstadoIncidencia.Bloqueada, ["Flujo general"]));

        // Assert
        Assert.Equal(CodigosError.EstadoCategoriaIncompatible, ex.Codigo);
        Assert.Contains("Flujo general", ex.Message);
    }

    // RN-18: sin categoría o sin flujos donde sea inicial/final no hay conflicto
    [Theory]
    [InlineData(null, true)]
    [InlineData(CategoriasEstadoIncidencia.EnEspera, false)]
    public void AsegurarCategoriaAsignable_SinCategoriaOSinFlujos_NoLanza(string? categoria, bool esInicialOFinal)
    {
        // Arrange
        string[] flujos = esInicialOFinal ? ["Flujo general"] : [];

        // Act
        var ex = Record.Exception(() => ReglasCategoriaEstado.AsegurarCategoriaAsignable("Estado", categoria, flujos));

        // Assert
        Assert.Null(ex);
    }

    // RN-18 (b), CA-39: marcar como final en el flujo un estado con categoría → 409 que nombra estado y flujo
    [Fact]
    public void AsegurarComposicionCompatible_EstadoConCategoriaFinal_LanzaEstadoCategoriaIncompatible()
    {
        // Arrange
        EstadoIncidencia[] estados = [Estado(PorHacer, "Por hacer"), Estado(4, "Bloqueada",
            categoria: CategoriasEstadoIncidencia.Bloqueada)];

        // Act
        var ex = Assert.Throws<ConflictoException>(() => ReglasCategoriaEstado.AsegurarComposicionCompatible(
            "Flujo general", [new EstadoEnFlujo(PorHacer, 1, true, false), new EstadoEnFlujo(4, 2, false, true)],
            estados));

        // Assert
        Assert.Equal(CodigosError.EstadoCategoriaIncompatible, ex.Codigo);
        Assert.Equal("El estado 'Bloqueada' tiene categoría de seguimiento y no puede ser inicial ni final en el "
            + "flujo 'Flujo general'.", ex.Message);
    }

    // RN-18 (b): estado con categoría en posición intermedia → compatible
    [Fact]
    public void AsegurarComposicionCompatible_EstadoConCategoriaIntermedio_NoLanza()
    {
        // Arrange
        EstadoIncidencia[] estados = [Estado(PorHacer), Estado(4, "Bloqueada", categoria: CategoriasEstadoIncidencia.Bloqueada),
            Estado(Hecho)];

        // Act
        var ex = Record.Exception(() => ReglasCategoriaEstado.AsegurarComposicionCompatible("Flujo general",
            [new EstadoEnFlujo(PorHacer, 1, true, false), new EstadoEnFlujo(4, 2, false, false),
                new EstadoEnFlujo(Hecho, 3, false, true)], estados));

        // Assert
        Assert.Null(ex);
    }
}
