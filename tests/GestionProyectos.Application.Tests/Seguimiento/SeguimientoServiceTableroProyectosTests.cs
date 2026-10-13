using FluentValidation;
using GestionProyectos.Application.Proyectos;
using GestionProyectos.Application.Seguimiento;
using NSubstitute;
using static GestionProyectos.Application.Tests.Comun.DatosSeguimiento;

namespace GestionProyectos.Application.Tests.Seguimiento;

/// <summary>
/// Tests de SeguimientoService.ObtenerTableroProyectosAsync (RN-02, RN-10, RN-27, CA-01 a CA-05).
/// </summary>
/// <remarks>
/// Creado: 2026-10-11
/// Modificado: 2026-10-11 - Versión inicial (tests RF-005)
/// </remarks>
public class SeguimientoServiceTableroProyectosTests : ContextoSeguimientoService
{
    private void ConProyectos(int cantidad) =>
        Seguimiento.ListarProyectosAsync(Arg.Any<FiltroProyectosSeguimiento>(), Arg.Any<CancellationToken>())
            .Returns(Enumerable.Range(1, cantidad)
                .Select(id => new ProyectoSeguimientoLeido(ProyectoSeguimiento(id), 2)).ToList());

    private Task<TableroProyectosDto> Obtener(ConsultaTableroProyectosQuery? query = null) =>
        Sut().ObtenerTableroProyectosAsync(query ?? new ConsultaTableroProyectosQuery(), CancellationToken.None);

    // RN-02, CA-01: por defecto se excluyen los archivados y no se filtra por texto
    [Fact]
    public async Task ObtenerTableroProyectosAsync_QueryVacia_ListaProyectosExcluyendoArchivados()
    {
        // Act
        await Obtener();

        // Assert
        await Seguimiento.Received(1).ListarProyectosAsync(
            new FiltroProyectosSeguimiento(null, FiltroArchivados.Excluir), Arg.Any<CancellationToken>());
    }

    // CA-01: archivados=incluir y texto recortado llegan al repositorio
    [Fact]
    public async Task ObtenerTableroProyectosAsync_ArchivadosIncluirYTexto_PasaElFiltro()
    {
        // Act
        await Obtener(new ConsultaTableroProyectosQuery(Texto: " portal ", Archivados: "INCLUIR"));

        // Assert
        await Seguimiento.Received(1).ListarProyectosAsync(
            new FiltroProyectosSeguimiento("portal", FiltroArchivados.Incluir), Arg.Any<CancellationToken>());
    }

    // Contrato tablero general: defaults orden codigo desc, página 1 y tamaño 20
    [Fact]
    public async Task ObtenerTableroProyectosAsync_QueryVacia_OrdenaPorCodigoDescEnPagina1De20()
    {
        // Arrange
        ConProyectos(3);

        // Act
        var tablero = await Obtener();

        // Assert
        Assert.Equal(["PRJ-0003", "PRJ-0002", "PRJ-0001"], tablero.Proyectos.Items.Select(f => f.Proyecto.Codigo));
        Assert.Equal((1, 20, 3), (tablero.Proyectos.Pagina, tablero.Proyectos.TamanioPagina, tablero.Proyectos.Total));
    }

    // RN-10: la fecha de referencia se pasa a los conteos y se devuelve en fechaReferencia
    [Fact]
    public async Task ObtenerTableroProyectosAsync_Siempre_UsaLaFechaDeReferencia()
    {
        // Act
        var tablero = await Obtener();

        // Assert
        Assert.Equal(Hoy, tablero.FechaReferencia);
        await Seguimiento.Received(1).ContarAsync(Arg.Is<FiltroConteoIncidencias>(f => f.FechaReferencia == Hoy
            && f.ProyectoId == null && f.Proyectos != null), AgrupacionConteo.Proyecto, Arg.Any<CancellationToken>());
    }

    // RN-02, CA-02: un proyecto sin filas de conteo aparece con indicadores en cero
    [Fact]
    public async Task ObtenerTableroProyectosAsync_ProyectoSinIncidencias_IndicadoresEnCero()
    {
        // Arrange
        ConProyectos(1);

        // Act
        var tablero = await Obtener();

        // Assert
        var fila = Assert.Single(tablero.Proyectos.Items);
        Assert.Equal((0, 0, 0), (fila.Indicadores.Total, fila.Indicadores.Vencidas, fila.Indicadores.SinResponsable));
        Assert.Equal(2, fila.CantidadAgrupaciones);
    }

    // RN-27, CA-03: totales suma todos los proyectos, no solo los de la página
    [Fact]
    public async Task ObtenerTableroProyectosAsync_25ProyectosPagina2_DevuelveCincoYTotalesDeLos25()
    {
        // Arrange
        ConProyectos(25);
        ConConteos([.. Enumerable.Range(1, 25).Select(id => Conteo(PorHacer, 1, vencidas: 1, grupo: id))]);

        // Act
        var tablero = await Obtener(new ConsultaTableroProyectosQuery(Pagina: 2, TamanioPagina: 20));

        // Assert
        Assert.Equal(5, tablero.Proyectos.Items.Count);
        Assert.Equal(25, tablero.Proyectos.Total);
        Assert.Equal((25, 25), (tablero.Totales.Total, tablero.Totales.Vencidas));
    }

    // CA-04: ordenarPor=vencidas desc pone primero al de más vencidas; empate por código desc
    [Fact]
    public async Task ObtenerTableroProyectosAsync_OrdenVencidasDesc_OrdenaYDesempataPorCodigo()
    {
        // Arrange
        ConProyectos(3);
        ConConteos(Conteo(PorHacer, 1, vencidas: 1, grupo: 1), Conteo(PorHacer, 3, vencidas: 3, grupo: 2),
            Conteo(PorHacer, 1, vencidas: 1, grupo: 3));

        // Act
        var tablero = await Obtener(new ConsultaTableroProyectosQuery(OrdenarPor: "Vencidas", Direccion: "desc"));

        // Assert
        Assert.Equal([2, 3, 1], tablero.Proyectos.Items.Select(f => f.Proyecto.Id));
    }

    // RN-04: los conteos reciben solo los tipos efectivos del alcance
    [Fact]
    public async Task ObtenerTableroProyectosAsync_AlcanceNivel2_PasaLosTiposDelNivel()
    {
        // Act
        var tablero = await Obtener(new ConsultaTableroProyectosQuery(NivelId: [Nivel2]));

        // Assert
        await Seguimiento.Received(1).ContarAsync(Arg.Is<FiltroConteoIncidencias>(f => f.TipoIds!.SequenceEqual(new[] { Hu })),
            AgrupacionConteo.Proyecto, Arg.Any<CancellationToken>());
        Assert.False(tablero.Alcance.TodosLosNiveles);
    }

    // RN-05: alcance inválido → ValidationException sin consultar conteos
    [Fact]
    public async Task ObtenerTableroProyectosAsync_AlcanceInvalido_LanzaValidationExceptionSinContar()
    {
        // Act
        var accion = () => Obtener(new ConsultaTableroProyectosQuery(NivelId: [99]));

        // Assert
        await Assert.ThrowsAsync<ValidationException>(accion);
        await Seguimiento.DidNotReceive().ContarAsync(Arg.Any<FiltroConteoIncidencias>(), Arg.Any<AgrupacionConteo>(),
            Arg.Any<CancellationToken>());
    }

    // CA-05: parámetro inválido → ValidationException sin listar proyectos
    [Fact]
    public async Task ObtenerTableroProyectosAsync_OrdenInvalido_LanzaValidationExceptionSinListar()
    {
        // Act
        var accion = () => Obtener(new ConsultaTableroProyectosQuery(OrdenarPor: "foo"));

        // Assert
        await Assert.ThrowsAsync<ValidationException>(accion);
        await Seguimiento.DidNotReceive().ListarProyectosAsync(Arg.Any<FiltroProyectosSeguimiento>(),
            Arg.Any<CancellationToken>());
    }
}
