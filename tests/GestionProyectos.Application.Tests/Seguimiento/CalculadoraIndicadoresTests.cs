using GestionProyectos.Application.Seguimiento;
using GestionProyectos.Domain.Comun;
using GestionProyectos.Domain.Entidades;
using static GestionProyectos.Application.Tests.Comun.DatosSeguimiento;
using Datos = GestionProyectos.Application.Tests.Comun.DatosIncidencias;

namespace GestionProyectos.Application.Tests.Seguimiento;

/// <summary>
/// Tests de CalculadoraIndicadores: reglas de los indicadores sobre conteos crudos (RN-08 a RN-16).
/// </summary>
/// <remarks>
/// Creado: 2026-10-11
/// Modificado: 2026-10-11 - Versión inicial (tests RF-005)
/// </remarks>
public class CalculadoraIndicadoresTests
{
    private readonly CalculadoraIndicadores _sut = Calculadora();

    // RN-15, CA-02: sin filas todo vale 0 y porEstado lista los estados activos de los flujos en 0
    [Fact]
    public void Calcular_SinFilas_DevuelveTodoEnCeroConEstadosDelFlujo()
    {
        // Act
        var indicadores = _sut.Calcular([]);

        // Assert
        Assert.Equal((0, 0, 0, 0, 0, 0, 0), (indicadores.Total, indicadores.Abiertas, indicadores.Finalizadas,
            indicadores.Vencidas, indicadores.Bloqueadas, indicadores.EnEspera, indicadores.SinResponsable));
        Assert.Equal([PorHacer, EnProgreso, Esperando, Bloqueada, Hecho], indicadores.PorEstado.Select(p => p.Estado.Id));
        Assert.All(indicadores.PorEstado, p => Assert.Equal(0, p.Cantidad));
    }

    // RN-11: fila no final con ConVencimientoAnterior = 2 suma 2 en vencidas
    [Fact]
    public void Calcular_FilaAbiertaConVencidas_SumaEnVencidas()
    {
        // Act
        var indicadores = _sut.Calcular([Conteo(EnProgreso, 3, vencidas: 2)]);

        // Assert
        Assert.Equal(2, indicadores.Vencidas);
    }

    // RN-11, CA-07: fila final con vencimiento anterior no suma en vencidas
    [Fact]
    public void Calcular_FilaFinalConVencidas_NoSumaEnVencidas()
    {
        // Act
        var indicadores = _sut.Calcular([Conteo(Hecho, 1, esFinal: true, vencidas: 1)]);

        // Assert
        Assert.Equal(0, indicadores.Vencidas);
    }

    // RN-12, CA-08: fila abierta en estado BLOQUEADA con cantidad 2 → bloqueadas = 2
    [Fact]
    public void Calcular_FilaAbiertaBloqueada_SumaEnBloqueadas()
    {
        // Act
        var indicadores = _sut.Calcular([Conteo(Bloqueada, 2)]);

        // Assert
        Assert.Equal(2, indicadores.Bloqueadas);
    }

    // RN-13, CA-08: fila abierta en estado EN_ESPERA suma en enEspera
    [Fact]
    public void Calcular_FilaAbiertaEnEspera_SumaEnEnEspera()
    {
        // Act
        var indicadores = _sut.Calcular([Conteo(Esperando, 1)]);

        // Assert
        Assert.Equal(1, indicadores.EnEspera);
    }

    // RN-13, CA-12: sin estados con categoría EN_ESPERA el indicador vale 0
    [Fact]
    public void Calcular_SinEstadosEnEspera_EnEsperaEsCero()
    {
        // Arrange
        EstadoIncidencia[] estados = [Datos.Estado(PorHacer, "Por hacer"), Datos.Estado(Hecho, "Hecho", orden: 30)];
        var sut = new CalculadoraIndicadores(estados, [new(1, PorHacer, false), new(1, Hecho, true)]);

        // Act
        var indicadores = sut.Calcular([Conteo(PorHacer, 4), Conteo(Hecho, 2, esFinal: true)]);

        // Assert
        Assert.Equal(0, indicadores.EnEspera);
    }

    // RN-12: un estado con categoría pero final en el flujo no suma en bloqueadas (solo abiertas)
    [Fact]
    public void Calcular_FilaFinalEnEstadoBloqueada_NoSumaEnBloqueadas()
    {
        // Act
        var indicadores = _sut.Calcular([Conteo(Bloqueada, 2, esFinal: true)]);

        // Assert
        Assert.Equal(0, indicadores.Bloqueadas);
    }

    // RN-14, CA-08: fila final sin responsable no suma en sinResponsable
    [Fact]
    public void Calcular_FilaFinalSinResponsable_NoSumaEnSinResponsable()
    {
        // Act
        var indicadores = _sut.Calcular([Conteo(Hecho, 1, esFinal: true, sinResponsable: 1)]);

        // Assert
        Assert.Equal(0, indicadores.SinResponsable);
    }

    // RN-08, CA-09: una incidencia abierta, vencida, bloqueada y sin responsable suma en los tres
    [Fact]
    public void Calcular_FilaVencidaBloqueadaSinResponsable_SumaEnLosTres()
    {
        // Act
        var indicadores = _sut.Calcular([Conteo(Bloqueada, 1, vencidas: 1, sinResponsable: 1)]);

        // Assert
        Assert.Equal((1, 1, 1), (indicadores.Vencidas, indicadores.Bloqueadas, indicadores.SinResponsable));
    }

    // RN-09, RN-15: mismo estado final en un flujo y no final en otro → abiertas/finalizadas correctas
    [Fact]
    public void Calcular_EstadoFinalEnUnFlujoYNoEnOtro_SeparaAbiertasYFinalizadas()
    {
        // Arrange
        var sut = new CalculadoraIndicadores(Estados(), [new(1, Hecho, true), new(2, Hecho, false)]);

        // Act
        var indicadores = sut.Calcular([Conteo(Hecho, 2, esFinal: true), Conteo(Hecho, 1)]);

        // Assert
        Assert.Equal((3, 1, 2), (indicadores.Total, indicadores.Abiertas, indicadores.Finalizadas));
        var hecho = Assert.Single(indicadores.PorEstado);
        Assert.Equal((3, false), (hecho.Cantidad, hecho.Estado.EsFinal));
    }

    // RN-15, CA-11: estado inactivo con cantidad > 0 que no está en los flujos aparece en porEstado
    [Fact]
    public void Calcular_EstadoInactivoConIncidencias_ApareceEnPorEstadoComoInactivo()
    {
        // Act
        var indicadores = _sut.Calcular([Conteo(Obsoleto, 2)]);

        // Assert
        var obsoleto = indicadores.PorEstado.Single(p => p.Estado.Id == Obsoleto);
        Assert.Equal((2, false), (obsoleto.Cantidad, obsoleto.Estado.Activo));
    }

    // RN-15: un estado inactivo sin incidencias no aparece en porEstado
    [Fact]
    public void Calcular_EstadoInactivoSinIncidencias_NoApareceEnPorEstado()
    {
        // Arrange
        var sut = new CalculadoraIndicadores(Estados(), [.. EstadosFlujoGeneral(), new(1, Obsoleto, false)]);

        // Act
        var indicadores = sut.Calcular([]);

        // Assert
        Assert.DoesNotContain(indicadores.PorEstado, p => p.Estado.Id == Obsoleto);
    }

    // RN-15, CA-10: porEstado ordenado por Orden, luego Nombre y luego Id
    [Fact]
    public void Calcular_EstadosConMismoOrden_OrdenaPorOrdenNombreEId()
    {
        // Arrange
        EstadoIncidencia[] estados =
        [
            Datos.Estado(9, "Beta", orden: 10), Datos.Estado(8, "alfa", orden: 10), Datos.Estado(7, "Beta", orden: 10),
            Datos.Estado(1, "Zeta", orden: 5),
        ];
        var sut = new CalculadoraIndicadores(estados,
            [new(1, 9, false), new(1, 8, false), new(1, 7, false), new(1, 1, false)]);

        // Act
        var indicadores = sut.Calcular([]);

        // Assert
        Assert.Equal([1, 8, 7, 9], indicadores.PorEstado.Select(p => p.Estado.Id));
    }

    // RN-16, CA-10: total = Σ porEstado = abiertas + finalizadas
    [Fact]
    public void Calcular_VariasFilas_CumpleLaInvarianteDeTotales()
    {
        // Act
        var indicadores = _sut.Calcular([Conteo(PorHacer, 13), Conteo(EnProgreso, 15), Conteo(Bloqueada, 2),
            Conteo(Hecho, 12, esFinal: true)]);

        // Assert
        Assert.Equal(42, indicadores.Total);
        Assert.Equal(indicadores.Total, indicadores.PorEstado.Sum(p => p.Cantidad));
        Assert.Equal(indicadores.Total, indicadores.Abiertas + indicadores.Finalizadas);
        Assert.Equal((30, 12), (indicadores.Abiertas, indicadores.Finalizadas));
    }

    // RN-15: el estado del conteo informa categoría y esFinal según los flujos
    [Fact]
    public void Calcular_EstadoConCategoria_InformaCategoriaYEsFinal()
    {
        // Act
        var indicadores = _sut.Calcular([]);

        // Assert
        var bloqueada = indicadores.PorEstado.Single(p => p.Estado.Id == Bloqueada).Estado;
        var hecho = indicadores.PorEstado.Single(p => p.Estado.Id == Hecho).Estado;
        Assert.Equal((CategoriasEstadoIncidencia.Bloqueada, false), (bloqueada.Categoria, bloqueada.EsFinal));
        Assert.True(hecho.EsFinal);
    }

    // RN-26, RN-27: Sumar suma campo a campo y por estado
    [Fact]
    public void Sumar_DosIndicadores_SumaCampoACampoYPorEstado()
    {
        // Arrange
        var a = _sut.Calcular([Conteo(PorHacer, 3, vencidas: 1, sinResponsable: 2), Conteo(Bloqueada, 1)]);
        var b = _sut.Calcular([Conteo(PorHacer, 2, vencidas: 1), Conteo(Esperando, 1),
            Conteo(Hecho, 4, esFinal: true)]);

        // Act
        var suma = CalculadoraIndicadores.Sumar(a, b);

        // Assert
        Assert.Equal((11, 7, 4, 2, 1, 1, 2), (suma.Total, suma.Abiertas, suma.Finalizadas, suma.Vencidas,
            suma.Bloqueadas, suma.EnEspera, suma.SinResponsable));
        Assert.Equal(5, suma.PorEstado.Single(p => p.Estado.Id == PorHacer).Cantidad);
        Assert.Equal(suma.Total, suma.PorEstado.Sum(p => p.Cantidad));
    }
}
