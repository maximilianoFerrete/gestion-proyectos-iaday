using FluentValidation;
using GestionProyectos.Application.Seguimiento;
using GestionProyectos.Domain.Comun;
using GestionProyectos.Domain.Excepciones;
using NSubstitute;
using static GestionProyectos.Application.Tests.Comun.DatosSeguimiento;

namespace GestionProyectos.Application.Tests.Seguimiento;

/// <summary>
/// Tests de SeguimientoService.ObtenerTableroAsync: grilla con ámbito, alcance y filtros
/// (RN-20, RN-21, RN-30, RN-32, RN-36, CA-21, CA-23, CA-26, CA-29, CA-48).
/// </summary>
/// <remarks>
/// Creado: 2026-10-11
/// Modificado: 2026-10-11 - Versión inicial (tests RF-005)
/// </remarks>
public class SeguimientoServiceTableroTests : ContextoSeguimientoService
{
    private Task<TableroIncidenciasDto> Obtener(ConsultaTableroIncidenciasQuery? query = null,
        int proyectoId = ProyectoId) =>
        Sut().ObtenerTableroAsync(proyectoId, query ?? new ConsultaTableroIncidenciasQuery(), CancellationToken.None);

    private void ConIncidencias(params IncidenciaTableroLeida[] leidas) =>
        Seguimiento.ListarIncidenciasTableroAsync(Arg.Any<FiltroTableroIncidencias>(), Arg.Any<CancellationToken>())
            .Returns(leidas);

    // CA-23: proyecto inexistente → 404 PROYECTO_NO_ENCONTRADO
    [Fact]
    public async Task ObtenerTableroAsync_ProyectoInexistente_LanzaProyectoNoEncontrado()
    {
        // Act
        var ex = await Assert.ThrowsAsync<EntidadNoEncontradaException>(() => Obtener(proyectoId: 99));

        // Assert
        Assert.Equal(CodigosError.ProyectoNoEncontrado, ex.Codigo);
    }

    // RN-01, CA-21: agrupación de otro proyecto → 404 AGRUPACION_NO_ENCONTRADA sin consultar incidencias
    [Fact]
    public async Task ObtenerTableroAsync_AgrupacionDeOtroProyecto_LanzaAgrupacionNoEncontrada()
    {
        // Act
        var ex = await Assert.ThrowsAsync<EntidadNoEncontradaException>(
            () => Obtener(new ConsultaTableroIncidenciasQuery(AgrupacionId: AgrupacionOtroProyecto)));

        // Assert
        Assert.Equal(CodigosError.AgrupacionNoEncontrada, ex.Codigo);
        await Seguimiento.DidNotReceive().ListarIncidenciasTableroAsync(Arg.Any<FiltroTableroIncidencias>(),
            Arg.Any<CancellationToken>());
    }

    // RN-07, RN-04: pasa al repositorio el ámbito y solo los tipos efectivos del alcance
    [Fact]
    public async Task ObtenerTableroAsync_AgrupacionYNivel2_PasaAmbitoYTiposEfectivos()
    {
        // Act
        await Obtener(new ConsultaTableroIncidenciasQuery(AgrupacionId: AgrupacionPagos, NivelId: [Nivel2]));

        // Assert
        await Seguimiento.Received(1).ListarIncidenciasTableroAsync(Arg.Is<FiltroTableroIncidencias>(f =>
            f.ProyectoId == ProyectoId && f.Ambito == new AmbitoIncidencias(AgrupacionPagos, false)
            && f.TipoIds!.SequenceEqual(new[] { Hu })), Arg.Any<CancellationToken>());
    }

    // RN-21, 0.1, CA-26: indicador normalizado, estadoId, texto recortado y fecha de referencia llegan al repo
    [Fact]
    public async Task ObtenerTableroAsync_TresFiltros_PasaLosFiltrosNormalizadosYLaFecha()
    {
        // Act
        await Obtener(new ConsultaTableroIncidenciasQuery(Indicador: "VENCIDAS", EstadoId: Bloqueada,
            Texto: "  pago "));

        // Assert
        await Seguimiento.Received(1).ListarIncidenciasTableroAsync(Arg.Is<FiltroTableroIncidencias>(f =>
            f.Indicador == IndicadorSeguimiento.Vencidas && f.EstadoId == Bloqueada && f.Texto == "pago"
            && f.FechaReferencia == Hoy), Arg.Any<CancellationToken>());
    }

    // RN-21: los valores de indicador se traducen al enum (incluidos los compuestos con guion)
    [Theory]
    [InlineData("abiertas", IndicadorSeguimiento.Abiertas)]
    [InlineData("bloqueadas", IndicadorSeguimiento.Bloqueadas)]
    [InlineData("en-espera", IndicadorSeguimiento.EnEspera)]
    [InlineData("Sin-Responsable", IndicadorSeguimiento.SinResponsable)]
    public async Task ObtenerTableroAsync_Indicador_PasaElIndicadorAlRepositorio(string indicador,
        IndicadorSeguimiento esperado)
    {
        // Act
        await Obtener(new ConsultaTableroIncidenciasQuery(Indicador: indicador));

        // Assert
        await Seguimiento.Received(1).ListarIncidenciasTableroAsync(
            Arg.Is<FiltroTableroIncidencias>(f => f.Indicador == esperado), Arg.Any<CancellationToken>());
    }

    // RN-21, CA-26, CA-48: filtros devuelve el eco (indicador canónico, estado y texto recortado)
    [Fact]
    public async Task ObtenerTableroAsync_TresFiltros_DevuelveElEcoDeLosFiltros()
    {
        // Act
        var tablero = await Obtener(new ConsultaTableroIncidenciasQuery(Indicador: "VENCIDAS", EstadoId: Bloqueada,
            Texto: " pago "));

        // Assert
        Assert.Equal(new FiltrosTableroDto("vencidas",
            new EstadoFiltroTableroDto(Bloqueada, "Bloqueada", true, CategoriasEstadoIncidencia.Bloqueada), "pago"),
            tablero.Filtros);
    }

    // RN-21: texto solo espacios se trata como sin filtro
    [Fact]
    public async Task ObtenerTableroAsync_TextoSoloEspacios_SinFiltroDeTexto()
    {
        // Act
        var tablero = await Obtener(new ConsultaTableroIncidenciasQuery(Texto: "   "));

        // Assert
        Assert.Null(tablero.Filtros.Texto);
        await Seguimiento.Received(1).ListarIncidenciasTableroAsync(Arg.Is<FiltroTableroIncidencias>(f => f.Texto == null),
            Arg.Any<CancellationToken>());
    }

    // RN-21, CA-26: estadoId inexistente → ValidationException clave estadoId, sin consultar incidencias
    [Fact]
    public async Task ObtenerTableroAsync_EstadoInexistente_LanzaValidationExceptionSinConsultar()
    {
        // Act
        var ex = await Assert.ThrowsAsync<ValidationException>(
            () => Obtener(new ConsultaTableroIncidenciasQuery(EstadoId: 9999)));

        // Assert
        var error = Assert.Single(ex.Errors);
        Assert.Equal(("EstadoId", "El estado no existe."), (error.PropertyName, error.ErrorMessage));
        await Seguimiento.DidNotReceive().ListarIncidenciasTableroAsync(Arg.Any<FiltroTableroIncidencias>(),
            Arg.Any<CancellationToken>());
    }

    // RN-28, CA-26: indicador fuera de lista → ValidationException sin consultar incidencias
    [Fact]
    public async Task ObtenerTableroAsync_IndicadorInvalido_LanzaValidationException()
    {
        // Act
        var accion = () => Obtener(new ConsultaTableroIncidenciasQuery(Indicador: "foo"));

        // Assert
        await Assert.ThrowsAsync<ValidationException>(accion);
        await Seguimiento.DidNotReceive().ListarIncidenciasTableroAsync(Arg.Any<FiltroTableroIncidencias>(),
            Arg.Any<CancellationToken>());
    }

    // RN-21: un estado inactivo existente es un filtro válido y se informa como inactivo
    [Fact]
    public async Task ObtenerTableroAsync_EstadoInactivo_DevuelveFiltroConEstadoInactivo()
    {
        // Act
        var tablero = await Obtener(new ConsultaTableroIncidenciasQuery(EstadoId: Obsoleto));

        // Assert
        Assert.False(tablero.Filtros.Estado!.Activo);
        Assert.Empty(tablero.Items);
    }

    // RN-21: sin filtros, filtros tiene los tres valores en null
    [Fact]
    public async Task ObtenerTableroAsync_SinFiltros_FiltrosEnNull()
    {
        // Act
        var tablero = await Obtener();

        // Assert
        Assert.Equal(new FiltrosTableroDto(null, null, null), tablero.Filtros);
    }

    // RN-32, RN-20, CA-29: ítems en orden por defecto y total = cantidad de ítems
    [Fact]
    public async Task ObtenerTableroAsync_ItemsDesordenados_DevuelveOrdenPorDefectoYTotal()
    {
        // Arrange
        ConIncidencias(Leida(15, Hu), Leida(16, Subtarea), Leida(3, Epica), Leida(7, Hu));

        // Act
        var tablero = await Obtener();

        // Assert
        Assert.Equal([3, 7, 15, 16], tablero.Items.Select(i => i.Numero));
        Assert.Equal(4, tablero.Total);
    }

    // RN-11, RN-36: vencida se calcula con la fecha de referencia (vence hoy no; finalizada no)
    [Fact]
    public async Task ObtenerTableroAsync_ItemsConVencimiento_CalculaVencida()
    {
        // Arrange
        ConIncidencias(Leida(1, Hu, Hoy.AddDays(-1)), Leida(2, Hu, Hoy), Leida(3, Hu, Hoy.AddDays(-1), esFinal: true),
            Leida(4, Hu));

        // Act
        var tablero = await Obtener();

        // Assert
        Assert.Equal([true, false, false, false], tablero.Items.Select(i => i.Vencida));
    }

    // RN-34, CA-41: la estimación (decimal, 0 o null) se devuelve tal cual
    [Fact]
    public async Task ObtenerTableroAsync_ItemsConEstimacion_DevuelveLaEstimacion()
    {
        // Arrange
        ConIncidencias(Leida(1, Hu, estimacion: 12.5m), Leida(2, Hu, estimacion: 0m), Leida(3, Hu));

        // Act
        var tablero = await Obtener();

        // Assert
        Assert.Equal([12.5m, 0m, null], tablero.Items.Select(i => i.Estimacion));
    }

    // RN-30, CA-36: sin configuración guardada → todas las columnas visibles y personalizada = false
    [Fact]
    public async Task ObtenerTableroAsync_SinConfiguracion_DevuelveConfiguracionPorDefecto()
    {
        // Act
        var tablero = await Obtener();

        // Assert
        Assert.Equal(new ConfiguracionTableroDto(ProyectoId, false, true, true, true, null), tablero.Configuracion);
    }

    // RN-30, CA-32: con configuración guardada se devuelven sus valores y personalizada = true
    [Fact]
    public async Task ObtenerTableroAsync_ConConfiguracion_DevuelveLaConfiguracionGuardada()
    {
        // Arrange
        var fecha = new DateTime(2026, 10, 10, 13, 0, 0, DateTimeKind.Utc);
        Configuraciones.ObtenerAsync(ProyectoId, Arg.Any<CancellationToken>())
            .Returns(new ConfiguracionTablero(ProyectoId, true, false, true, fecha));

        // Act
        var tablero = await Obtener();

        // Assert
        Assert.Equal(new ConfiguracionTableroDto(ProyectoId, true, true, false, true, fecha), tablero.Configuracion);
    }

    // RN-03, CA-22: un proyecto archivado devuelve el tablero
    [Fact]
    public async Task ObtenerTableroAsync_ProyectoArchivado_DevuelveDatos()
    {
        // Arrange
        ExisteProyecto(archivado: true);
        ConIncidencias(Leida(3, Epica));

        // Act
        var tablero = await Obtener();

        // Assert
        Assert.True(tablero.Proyecto.Archivado);
        Assert.Single(tablero.Items);
        Assert.Equal(Hoy, tablero.FechaReferencia);
    }
}
