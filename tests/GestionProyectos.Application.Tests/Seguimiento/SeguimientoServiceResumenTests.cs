using FluentValidation;
using GestionProyectos.Application.Seguimiento;
using GestionProyectos.Domain.Excepciones;
using NSubstitute;
using static GestionProyectos.Application.Tests.Comun.DatosSeguimiento;

namespace GestionProyectos.Application.Tests.Seguimiento;

/// <summary>
/// Tests de SeguimientoService.ObtenerSeguimientoAsync: ámbito, alcance y tabla por agrupación
/// (RN-01, RN-03, RN-04, RN-26, CA-19 a CA-23).
/// </summary>
/// <remarks>
/// Creado: 2026-10-11
/// Modificado: 2026-10-11 - Versión inicial (tests RF-005)
/// </remarks>
public class SeguimientoServiceResumenTests : ContextoSeguimientoService
{
    private Task<SeguimientoDto> Obtener(ConsultaSeguimientoQuery? query = null, int proyectoId = ProyectoId) =>
        Sut().ObtenerSeguimientoAsync(proyectoId, query ?? new ConsultaSeguimientoQuery(), CancellationToken.None);

    // CA-23: proyecto inexistente → 404 PROYECTO_NO_ENCONTRADO
    [Fact]
    public async Task ObtenerSeguimientoAsync_ProyectoInexistente_LanzaProyectoNoEncontrado()
    {
        // Act
        var ex = await Assert.ThrowsAsync<EntidadNoEncontradaException>(() => Obtener(proyectoId: 99));

        // Assert
        Assert.Equal(CodigosError.ProyectoNoEncontrado, ex.Codigo);
    }

    // RN-01, CA-21: agrupación de otro proyecto o inexistente → 404 AGRUPACION_NO_ENCONTRADA
    [Theory]
    [InlineData(AgrupacionOtroProyecto)]
    [InlineData(99)]
    public async Task ObtenerSeguimientoAsync_AgrupacionAjena_LanzaAgrupacionNoEncontrada(int agrupacionId)
    {
        // Act
        var ex = await Assert.ThrowsAsync<EntidadNoEncontradaException>(
            () => Obtener(new ConsultaSeguimientoQuery(AgrupacionId: agrupacionId)));

        // Assert
        Assert.Equal(CodigosError.AgrupacionNoEncontrada, ex.Codigo);
    }

    // RN-01, CA-21: agrupacionId con sinAgrupacion=true → ValidationException sin contar
    [Fact]
    public async Task ObtenerSeguimientoAsync_AgrupacionYSinAgrupacion_LanzaValidationException()
    {
        // Act
        var accion = () => Obtener(new ConsultaSeguimientoQuery(AgrupacionPagos, true));

        // Assert
        await Assert.ThrowsAsync<ValidationException>(accion);
        await Seguimiento.DidNotReceive().ContarAsync(Arg.Any<FiltroConteoIncidencias>(), Arg.Any<AgrupacionConteo>(),
            Arg.Any<CancellationToken>());
    }

    // RN-26: ámbito proyecto pide conteos agrupados por agrupación con el ámbito completo
    [Fact]
    public async Task ObtenerSeguimientoAsync_AmbitoProyecto_CuentaAgrupandoPorAgrupacion()
    {
        // Act
        await Obtener();

        // Assert
        await Seguimiento.Received(1).ContarAsync(Arg.Is<FiltroConteoIncidencias>(f => f.ProyectoId == ProyectoId
            && f.Ambito == AmbitoIncidencias.Proyecto && f.FechaReferencia == Hoy), AgrupacionConteo.Agrupacion,
            Arg.Any<CancellationToken>());
    }

    // RN-26, CA-19: filas por agrupación (orden del repositorio) + "Sin agrupación" al final; proyecto = suma
    [Fact]
    public async Task ObtenerSeguimientoAsync_AmbitoProyecto_DevuelveFilasPorAgrupacionYProyectoComoSuma()
    {
        // Arrange
        ConConteos(Conteo(PorHacer, 2, vencidas: 1, grupo: AgrupacionPagos),
            Conteo(Bloqueada, 1, sinResponsable: 1, grupo: AgrupacionEnvios),
            Conteo(Hecho, 3, esFinal: true, grupo: null));

        // Act
        var resumen = await Obtener();

        // Assert
        Assert.Equal("proyecto", resumen.Ambito);
        Assert.Equal(["Envíos", "Pagos", null], resumen.Agrupaciones.Select(a => a.Agrupacion?.Nombre));
        Assert.Equal([1, 2, 3], resumen.Agrupaciones.Select(a => a.Indicadores.Total));
        Assert.Equal(resumen.Indicadores.Total, resumen.Agrupaciones.Sum(a => a.Indicadores.Total));
        Assert.Equal((6, 3, 1, 1, 1), (resumen.Indicadores.Total, resumen.Indicadores.Abiertas,
            resumen.Indicadores.Vencidas, resumen.Indicadores.Bloqueadas, resumen.Indicadores.SinResponsable));
    }

    // RN-26: una agrupación sin incidencias aparece con indicadores en cero
    [Fact]
    public async Task ObtenerSeguimientoAsync_AgrupacionSinIncidencias_ApareceEnCero()
    {
        // Arrange
        ConConteos(Conteo(PorHacer, 2, grupo: AgrupacionPagos));

        // Act
        var resumen = await Obtener();

        // Assert
        Assert.Equal(0, resumen.Agrupaciones.Single(a => a.Agrupacion?.Id == AgrupacionEnvios).Indicadores.Total);
    }

    // CA-20: ámbito agrupación filtra por AgrupacionId, informa nombre y descripción y agrupaciones = []
    [Fact]
    public async Task ObtenerSeguimientoAsync_AmbitoAgrupacion_FiltraYDevuelveLaAgrupacion()
    {
        // Act
        var resumen = await Obtener(new ConsultaSeguimientoQuery(AgrupacionId: AgrupacionPagos));

        // Assert
        Assert.Equal("agrupacion", resumen.Ambito);
        Assert.Equal(new AgrupacionSeguimientoDto(AgrupacionPagos, "Pagos", "Incidencias de pagos."), resumen.Agrupacion);
        Assert.Empty(resumen.Agrupaciones);
        await Seguimiento.Received(1).ContarAsync(
            Arg.Is<FiltroConteoIncidencias>(f => f.Ambito == new AmbitoIncidencias(AgrupacionPagos, false)),
            AgrupacionConteo.Ninguna, Arg.Any<CancellationToken>());
    }

    // RN-01 (c): ámbito sin agrupación filtra AgrupacionId IS NULL y ambito = sinAgrupacion
    [Fact]
    public async Task ObtenerSeguimientoAsync_SinAgrupacion_FiltraSinAgrupacion()
    {
        // Act
        var resumen = await Obtener(new ConsultaSeguimientoQuery(SinAgrupacion: true));

        // Assert
        Assert.Equal("sinAgrupacion", resumen.Ambito);
        Assert.Null(resumen.Agrupacion);
        Assert.Empty(resumen.Agrupaciones);
        await Seguimiento.Received(1).ContarAsync(
            Arg.Is<FiltroConteoIncidencias>(f => f.Ambito == new AmbitoIncidencias(null, true)),
            AgrupacionConteo.Ninguna, Arg.Any<CancellationToken>());
    }

    // RN-03, CA-22: un proyecto archivado se consulta y se informa archivado
    [Fact]
    public async Task ObtenerSeguimientoAsync_ProyectoArchivado_DevuelveDatos()
    {
        // Arrange
        ExisteProyecto(archivado: true);

        // Act
        var resumen = await Obtener();

        // Assert
        Assert.True(resumen.Proyecto.Archivado);
        Assert.Equal(Hoy, resumen.FechaReferencia);
    }

    // RN-04, CA-15: conteos y flujos reciben solo los tipos efectivos del alcance
    [Fact]
    public async Task ObtenerSeguimientoAsync_AlcanceNivel3ConSubtareaYBug_PasaSoloEsosTipos()
    {
        // Act
        var resumen = await Obtener(new ConsultaSeguimientoQuery(NivelId: [Nivel3], TipoId: [Subtarea, Bug]));

        // Assert
        Assert.Equal([Bug, Subtarea], resumen.Alcance.Tipos.Select(t => t.Id));
        await Seguimiento.Received(1).ContarAsync(
            Arg.Is<FiltroConteoIncidencias>(f => f.TipoIds!.SequenceEqual(new[] { Bug, Subtarea })),
            Arg.Any<AgrupacionConteo>(), Arg.Any<CancellationToken>());
        await Seguimiento.Received(1).ListarEstadosDeFlujosAsync(
            Arg.Is<IReadOnlyList<int>?>(t => t!.SequenceEqual(new[] { Bug, Subtarea })), Arg.Any<CancellationToken>());
    }

    // RN-06, CA-14: sin alcance no se filtran tipos (null) y el alcance informa todos
    [Fact]
    public async Task ObtenerSeguimientoAsync_SinAlcance_NoFiltraTipos()
    {
        // Act
        var resumen = await Obtener();

        // Assert
        Assert.True(resumen.Alcance.TodosLosNiveles && resumen.Alcance.TodosLosTipos);
        await Seguimiento.Received(1).ContarAsync(Arg.Is<FiltroConteoIncidencias>(f => f.TipoIds == null),
            Arg.Any<AgrupacionConteo>(), Arg.Any<CancellationToken>());
    }
}
