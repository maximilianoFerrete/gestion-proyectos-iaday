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
/// </remarks>
public class IncidenciaConsultaServiceTests
{
    private const int ProyectoId = 1;
    private readonly IIncidenciaRepository _incidencias = Substitute.For<IIncidenciaRepository>();
    private readonly IProyectoRepository _proyectos = Substitute.For<IProyectoRepository>();
    private readonly IncidenciaConsultaService _sut;

    public IncidenciaConsultaServiceTests()
    {
        _sut = new IncidenciaConsultaService(_incidencias, _proyectos, new ListarIncidenciasQueryValidator());
    }

    private void ExisteProyecto(bool archivado = false) =>
        _proyectos.ObtenerPorIdAsync(ProyectoId, Arg.Any<CancellationToken>())
            .Returns(DatosPrueba.Proyecto(ProyectoId, archivado: archivado, codigo: "PRJ-0001"));

    // Contrato GET incidencias: defaults página 1, 20, numero desc
    [Fact]
    public async Task ListarAsync_QueryVacia_ConsultaConDefaults()
    {
        // Arrange
        ExisteProyecto();

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

    // RN-36, CA-21: el listado de un proyecto archivado se consulta
    [Fact]
    public async Task ListarAsync_ProyectoArchivado_DevuelveLaLista()
    {
        // Arrange
        ExisteProyecto(archivado: true);
        var pagina = new Paginado<IncidenciaResumenDto>([], 1, 20, 0);
        _incidencias.ListarAsync(Arg.Any<FiltroIncidencias>(), Arg.Any<CancellationToken>()).Returns(pagina);

        // Act
        var resultado = await _sut.ListarAsync(ProyectoId, new ListarIncidenciasQuery(), CancellationToken.None);

        // Assert
        Assert.Same(pagina, resultado);
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

    // Contrato GET arbol: proyecto inexistente → 404 PROYECTO_NO_ENCONTRADO
    [Fact]
    public async Task ObtenerArbolAsync_ProyectoInexistente_LanzaProyectoNoEncontrado()
    {
        // Act
        var ex = await Assert.ThrowsAsync<EntidadNoEncontradaException>(
            () => _sut.ObtenerArbolAsync(99, CancellationToken.None));

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
        var nodos = await _sut.ObtenerArbolAsync(ProyectoId, CancellationToken.None);

        // Assert
        Assert.Empty(nodos);
        await _incidencias.Received(1).ListarArbolAsync(ProyectoId, Arg.Any<CancellationToken>());
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

    // CA-35: detalle de una incidencia del proyecto
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
        Assert.Same(detalle, dto);
    }
}
