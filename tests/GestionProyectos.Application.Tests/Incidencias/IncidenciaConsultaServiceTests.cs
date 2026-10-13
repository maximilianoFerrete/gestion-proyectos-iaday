using FluentValidation;
using GestionProyectos.Application.Abstracciones.Persistencia;
using GestionProyectos.Application.Comun;
using GestionProyectos.Application.Incidencias;
using GestionProyectos.Application.Tests.Comun;
using GestionProyectos.Domain.Excepciones;
using NSubstitute;

namespace GestionProyectos.Application.Tests.Incidencias;

/// <summary>
/// Tests de IncidenciaConsultaService: listado, árbol y detalle (CA-19, CA-20, RN-36, RN-37).
/// </summary>
/// <remarks>
/// Creado: 2026-10-10
/// Modificado: 2026-10-10 - Versión inicial (tests RF-002)
/// Modificado: 2026-10-11 - Vencida con fecha de referencia, estimación y árbol por ámbito (tests RF-005)
/// </remarks>
public class IncidenciaConsultaServiceTests
{
    private const int ProyectoId = 1;
    private static readonly DateOnly Hoy = new(2026, 10, 10);
    private static readonly DateOnly Ayer = new(2026, 10, 9);
    private readonly IIncidenciaRepository _incidencias = Substitute.For<IIncidenciaRepository>();
    private readonly IProyectoRepository _proyectos = Substitute.For<IProyectoRepository>();
    private readonly IFechaReferencia _fechaReferencia = Substitute.For<IFechaReferencia>();
    private readonly IncidenciaConsultaService _sut;

    public IncidenciaConsultaServiceTests()
    {
        _fechaReferencia.Hoy().Returns(Hoy);
        _sut = new IncidenciaConsultaService(_incidencias, _proyectos, new ListarIncidenciasQueryValidator(),
            new ArbolIncidenciasQueryValidator(), _fechaReferencia);
    }

    private void ExisteProyecto(bool archivado = false) =>
        _proyectos.ObtenerPorIdAsync(ProyectoId, Arg.Any<CancellationToken>())
            .Returns(DatosPrueba.Proyecto(ProyectoId, archivado: archivado, codigo: "PRJ-0001"));

    private void ListadoDevuelve(params IncidenciaResumenDto[] items) =>
        _incidencias.ListarAsync(Arg.Any<FiltroIncidencias>(), Arg.Any<CancellationToken>())
            .Returns(new Paginado<IncidenciaResumenDto>(items, 1, 20, items.Length));

    private static EstadoIncidenciaReferenciaDto EstadoRef(bool esFinal) =>
        new(esFinal ? DatosIncidencias.Hecho : DatosIncidencias.PorHacer, esFinal ? "Hecho" : "Por hacer", true,
            esFinal);

    private static IncidenciaResumenDto Resumen(int id, DateOnly? vencimiento, bool esFinal = false,
        decimal? estimacion = null) =>
        new(id, $"PRJ-0001-{id}", id, "Título", new TipoReferenciaDto(2, "Historia de Usuario", true, 2),
            EstadoRef(esFinal), new PrioridadReferenciaDto(2, "Media", 20, true), null, vencimiento, null, null, 0,
            DatosPrueba.FechaAnterior, estimacion);

    private static NodoArbolIncidenciaDto Nodo(int id, DateOnly? vencimiento, bool esFinal = false) =>
        new(id, $"PRJ-0001-{id}", "Título", null, new TipoReferenciaDto(2, "Historia de Usuario", true, 2),
            EstadoRef(esFinal), new PrioridadReferenciaDto(2, "Media", 20, true), null, vencimiento, null);

    private static IncidenciaReferenciaDto Referencia(int id, DateOnly? vencimiento, bool esFinal = false) =>
        new(id, $"PRJ-0001-{id}", "Título", new TipoReferenciaDto(3, "Subtarea", true, 3), EstadoRef(esFinal), null,
            vencimiento);

    // Contrato GET incidencias: defaults página 1, 20, numero desc
    [Fact]
    public async Task ListarAsync_QueryVacia_ConsultaConDefaults()
    {
        // Arrange
        ExisteProyecto();
        ListadoDevuelve();

        // Act
        await _sut.ListarAsync(ProyectoId, new ListarIncidenciasQuery(), CancellationToken.None);

        // Assert
        await _incidencias.Received(1).ListarAsync(Arg.Is<FiltroIncidencias>(f => f.ProyectoId == ProyectoId
            && f.Pagina == 1 && f.TamanioPagina == 20 && f.OrdenarPor == OrdenIncidencias.Numero && f.Descendente),
            Arg.Any<CancellationToken>());
    }

    // CA-19: responsableId con sinResponsable=true → ValidationException sin consultar
    [Fact]
    public async Task ListarAsync_ResponsableYSinResponsable_LanzaValidationException()
    {
        // Arrange
        ExisteProyecto();

        // Act
        var accion = () => _sut.ListarAsync(ProyectoId, new ListarIncidenciasQuery(ResponsableId: 2,
            SinResponsable: true), CancellationToken.None);

        // Assert
        await Assert.ThrowsAsync<ValidationException>(accion);
        await _incidencias.DidNotReceive().ListarAsync(Arg.Any<FiltroIncidencias>(), Arg.Any<CancellationToken>());
    }

    // RN-36, CA-21 (RF-002): el listado de un proyecto archivado se consulta; RF-005 RN-36 devuelve copia
    [Fact]
    public async Task ListarAsync_ProyectoArchivado_DevuelveLaLista()
    {
        // Arrange
        ExisteProyecto(archivado: true);
        var item = Resumen(15, vencimiento: null);
        ListadoDevuelve(item);

        // Act
        var resultado = await _sut.ListarAsync(ProyectoId, new ListarIncidenciasQuery(), CancellationToken.None);

        // Assert
        Assert.Equal(item, Assert.Single(resultado.Items));
        Assert.Equal((1, 20, 1), (resultado.Pagina, resultado.TamanioPagina, resultado.Total));
        Assert.False(resultado.Items[0].Vencida);
    }

    // Contrato GET incidencias: proyecto inexistente → 404 PROYECTO_NO_ENCONTRADO
    [Fact]
    public async Task ListarAsync_ProyectoInexistente_LanzaProyectoNoEncontrado()
    {
        // Act
        var ex = await Assert.ThrowsAsync<EntidadNoEncontradaException>(
            () => _sut.ListarAsync(99, new ListarIncidenciasQuery(), CancellationToken.None));

        // Assert
        Assert.Equal(CodigosError.ProyectoNoEncontrado, ex.Codigo);
    }

    // RF-005 CA-44, C-RF002-04: ordenarPor=estimacion llega al repositorio
    [Fact]
    public async Task ListarAsync_OrdenarPorEstimacion_PasaElOrdenAlRepositorio()
    {
        // Arrange
        ExisteProyecto();
        ListadoDevuelve();

        // Act
        await _sut.ListarAsync(ProyectoId, new ListarIncidenciasQuery(OrdenarPor: "Estimacion", Direccion: "asc"),
            CancellationToken.None);

        // Assert
        await _incidencias.Received(1).ListarAsync(
            Arg.Is<FiltroIncidencias>(f => f.OrdenarPor == OrdenIncidencias.Estimacion && !f.Descendente),
            Arg.Any<CancellationToken>());
    }

    // RF-005 RN-11, RN-36, CA-45: vencida con la fecha de referencia (vence hoy no; finalizada no)
    [Theory]
    [InlineData(2026, 10, 9, false, true)]
    [InlineData(2026, 10, 10, false, false)]
    [InlineData(2026, 10, 9, true, false)]
    public async Task ListarAsync_SegunVencimientoYEstado_CalculaVencidaConLaFechaDeReferencia(int anio, int mes,
        int dia, bool esFinal, bool esperado)
    {
        // Arrange
        ExisteProyecto();
        ListadoDevuelve(Resumen(15, new DateOnly(anio, mes, dia), esFinal));

        // Act
        var resultado = await _sut.ListarAsync(ProyectoId, new ListarIncidenciasQuery(), CancellationToken.None);

        // Assert
        Assert.Equal(esperado, resultado.Items[0].Vencida);
    }

    // RF-005 RN-34, CA-41: la estimación del repositorio se devuelve sin cambios en el listado
    [Fact]
    public async Task ListarAsync_ItemConEstimacion_DevuelveLaEstimacion()
    {
        // Arrange
        ExisteProyecto();
        ListadoDevuelve(Resumen(15, null, estimacion: 12.5m));

        // Act
        var resultado = await _sut.ListarAsync(ProyectoId, new ListarIncidenciasQuery(), CancellationToken.None);

        // Assert
        Assert.Equal(12.5m, resultado.Items[0].Estimacion);
    }

    // Contrato GET arbol: proyecto inexistente → 404 PROYECTO_NO_ENCONTRADO
    [Fact]
    public async Task ObtenerArbolAsync_ProyectoInexistente_LanzaProyectoNoEncontrado()
    {
        // Act
        var ex = await Assert.ThrowsAsync<EntidadNoEncontradaException>(
            () => _sut.ObtenerArbolAsync(99, new ArbolIncidenciasQuery(), CancellationToken.None));

        // Assert
        Assert.Equal(CodigosError.ProyectoNoEncontrado, ex.Codigo);
    }

    // RN-36, CA-34: el árbol de un proyecto archivado se consulta
    [Fact]
    public async Task ObtenerArbolAsync_ProyectoArchivado_DevuelveLosNodos()
    {
        // Arrange
        ExisteProyecto(archivado: true);
        _incidencias.ListarArbolAsync(ProyectoId, Arg.Any<CancellationToken>()).Returns([]);

        // Act
        var nodos = await _sut.ObtenerArbolAsync(ProyectoId, new ArbolIncidenciasQuery(), CancellationToken.None);

        // Assert
        Assert.Empty(nodos);
        await _incidencias.Received(1).ListarArbolAsync(ProyectoId, Arg.Any<CancellationToken>());
    }

    // RF-005 RN-36, CA-45: cada nodo del árbol informa vencida con la fecha de referencia
    [Fact]
    public async Task ObtenerArbolAsync_NodosConVencimiento_CalculaVencidaPorNodo()
    {
        // Arrange
        ExisteProyecto();
        _incidencias.ListarArbolAsync(ProyectoId, Arg.Any<CancellationToken>())
            .Returns([Nodo(1, Ayer), Nodo(2, Hoy), Nodo(3, Ayer, esFinal: true), Nodo(4, null)]);

        // Act
        var nodos = await _sut.ObtenerArbolAsync(ProyectoId, new ArbolIncidenciasQuery(), CancellationToken.None);

        // Assert
        Assert.Equal([true, false, false, false], nodos.Select(n => n.Vencida));
    }

    // RF-005 RN-01, C-RF002-04: agrupacionId y sinAgrupacion excluyentes → 400 sin consultar
    [Fact]
    public async Task ObtenerArbolAsync_AgrupacionYSinAgrupacion_LanzaValidationException()
    {
        // Arrange
        ExisteProyecto();

        // Act
        var accion = () => _sut.ObtenerArbolAsync(ProyectoId, new ArbolIncidenciasQuery(8, true),
            CancellationToken.None);

        // Assert
        await Assert.ThrowsAsync<ValidationException>(accion);
        await _incidencias.DidNotReceive().ListarArbolAsync(Arg.Any<int>(), Arg.Any<int?>(), Arg.Any<bool>(),
            Arg.Any<CancellationToken>());
    }

    // RF-005 RN-22, CA-28: árbol con agrupacionId consulta solo ese ámbito
    [Fact]
    public async Task ObtenerArbolAsync_ConAgrupacion_ConsultaElAmbitoDeLaAgrupacion()
    {
        // Arrange
        ExisteProyecto();
        _incidencias.ListarArbolAsync(ProyectoId, 8, false, Arg.Any<CancellationToken>()).Returns([]);

        // Act
        await _sut.ObtenerArbolAsync(ProyectoId, new ArbolIncidenciasQuery(AgrupacionId: 8), CancellationToken.None);

        // Assert
        await _incidencias.Received(1).ListarArbolAsync(ProyectoId, 8, false, Arg.Any<CancellationToken>());
        await _incidencias.DidNotReceive().ListarArbolAsync(ProyectoId, Arg.Any<CancellationToken>());
    }

    // RF-005 RN-01 (c): árbol con sinAgrupacion=true consulta las incidencias sin agrupación
    [Fact]
    public async Task ObtenerArbolAsync_SinAgrupacion_ConsultaElAmbitoSinAgrupacion()
    {
        // Arrange
        ExisteProyecto();
        _incidencias.ListarArbolAsync(ProyectoId, null, true, Arg.Any<CancellationToken>()).Returns([]);

        // Act
        await _sut.ObtenerArbolAsync(ProyectoId, new ArbolIncidenciasQuery(SinAgrupacion: true),
            CancellationToken.None);

        // Assert
        await _incidencias.Received(1).ListarArbolAsync(ProyectoId, null, true, Arg.Any<CancellationToken>());
    }

    // RN-37, CA-20: incidencia de otro proyecto → 404 INCIDENCIA_NO_ENCONTRADA
    [Fact]
    public async Task ObtenerAsync_IncidenciaDeOtroProyecto_LanzaIncidenciaNoEncontrada()
    {
        // Arrange
        ExisteProyecto();
        _incidencias.ObtenerDetalleAsync(15, Arg.Any<CancellationToken>()).Returns(DatosIncidencias.Dto(15, 4));

        // Act
        var ex = await Assert.ThrowsAsync<EntidadNoEncontradaException>(
            () => _sut.ObtenerAsync(ProyectoId, 15, CancellationToken.None));

        // Assert
        Assert.Equal(CodigosError.IncidenciaNoEncontrada, ex.Codigo);
    }

    // CA-35 (RF-002): detalle de una incidencia del proyecto; RF-005 RN-36 devuelve copia con vencida
    [Fact]
    public async Task ObtenerAsync_IncidenciaDelProyecto_DevuelveElDetalle()
    {
        // Arrange
        ExisteProyecto();
        var detalle = DatosIncidencias.Dto(15, ProyectoId);
        _incidencias.ObtenerDetalleAsync(15, Arg.Any<CancellationToken>()).Returns(detalle);

        // Act
        var dto = await _sut.ObtenerAsync(ProyectoId, 15, CancellationToken.None);

        // Assert
        Assert.Equivalent(detalle, dto, strict: true);
        Assert.False(dto.Vencida);
    }

    // RF-005 RN-36, CA-45: el detalle calcula vencida en la incidencia, el padre y las hijas
    [Fact]
    public async Task ObtenerAsync_DetalleConPadreEHijasVencidos_CalculaVencidaEnLosTres()
    {
        // Arrange
        ExisteProyecto();
        var detalle = DatosIncidencias.Dto(15, ProyectoId) with
        {
            FechaVencimiento = Ayer,
            Padre = Referencia(3, Ayer),
            Hijas = [Referencia(16, Ayer), Referencia(17, Hoy), Referencia(18, Ayer, esFinal: true)],
        };
        _incidencias.ObtenerDetalleAsync(15, Arg.Any<CancellationToken>()).Returns(detalle);

        // Act
        var dto = await _sut.ObtenerAsync(ProyectoId, 15, CancellationToken.None);

        // Assert
        Assert.Equal((true, true), (dto.Vencida, dto.Padre!.Vencida));
        Assert.Equal([true, false, false], dto.Hijas.Select(h => h.Vencida));
    }

    // RF-005 RN-34, CA-41: el detalle conserva la estimación (decimal o null)
    [Theory]
    [InlineData(null)]
    [InlineData(2.5)]
    public async Task ObtenerAsync_ConEstimacion_DevuelveLaEstimacion(double? estimacion)
    {
        // Arrange
        ExisteProyecto();
        var esperado = (decimal?)estimacion;
        _incidencias.ObtenerDetalleAsync(15, Arg.Any<CancellationToken>())
            .Returns(DatosIncidencias.Dto(15, ProyectoId) with { Estimacion = esperado });

        // Act
        var dto = await _sut.ObtenerAsync(ProyectoId, 15, CancellationToken.None);

        // Assert
        Assert.Equal(esperado, dto.Estimacion);
    }

    // RF-005 RN-10, 0.1: el servicio usa IFechaReferencia (no la fecha del sistema)
    [Fact]
    public async Task ObtenerAsync_Siempre_ConsultaLaFechaDeReferencia()
    {
        // Arrange
        ExisteProyecto();
        _incidencias.ObtenerDetalleAsync(15, Arg.Any<CancellationToken>()).Returns(DatosIncidencias.Dto(15, ProyectoId));

        // Act
        await _sut.ObtenerAsync(ProyectoId, 15, CancellationToken.None);

        // Assert
        _fechaReferencia.Received(1).Hoy();
    }
}
