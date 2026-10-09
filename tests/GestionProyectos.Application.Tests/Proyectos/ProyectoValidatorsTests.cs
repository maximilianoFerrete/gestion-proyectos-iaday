using GestionProyectos.Application.Proyectos;
using GestionProyectos.Application.Tests.Comun;

namespace GestionProyectos.Application.Tests.Proyectos;

/// <summary>
/// Tests de los validadores de alta, edición y listado de proyectos (RN-15, RN-16, CA-27).
/// </summary>
/// <remarks>
/// Creado: 2026-10-09
/// Modificado: 2026-10-09 - Versión inicial (tests RF-001)
/// </remarks>
public class ProyectoValidatorsTests
{
    private readonly CrearProyectoRequestValidator _alta = new();
    private readonly EditarProyectoRequestValidator _edicion = new();
    private readonly ListarProyectosQueryValidator _listado = new();

    private static readonly CrearProyectoRequest AltaValida = new("Portal clientes",
        "Nuevo portal.", 2, DatosPrueba.FechaInicio, DatosPrueba.FechaObjetivo);

    private static readonly EditarProyectoRequest EdicionValida = new("Portal clientes",
        "Nuevo portal.", 2, DatosPrueba.FechaInicio, DatosPrueba.FechaObjetivo, 3);

    public static TheoryData<CrearProyectoRequest, string> AltasInvalidas => new()
    {
        { AltaValida with { Nombre = "" }, nameof(CrearProyectoRequest.Nombre) },
        { AltaValida with { Nombre = DatosPrueba.Texto(151) }, nameof(CrearProyectoRequest.Nombre) },
        { AltaValida with { Descripcion = null }, nameof(CrearProyectoRequest.Descripcion) },
        { AltaValida with { Descripcion = DatosPrueba.Texto(2001) }, nameof(CrearProyectoRequest.Descripcion) },
        { AltaValida with { ResponsableId = 0 }, nameof(CrearProyectoRequest.ResponsableId) },
        { AltaValida with { ResponsableId = null }, nameof(CrearProyectoRequest.ResponsableId) },
        { AltaValida with { FechaInicio = null }, nameof(CrearProyectoRequest.FechaInicio) },
        { AltaValida with { FechaObjetivo = null }, nameof(CrearProyectoRequest.FechaObjetivo) },
        { AltaValida with { FechaObjetivo = DatosPrueba.FechaInicio.AddDays(-1) },
            nameof(CrearProyectoRequest.FechaObjetivo) },
    };

    public static TheoryData<ListarProyectosQuery, string> QueriesInvalidas => new()
    {
        { new ListarProyectosQuery(Pagina: 0), nameof(ListarProyectosQuery.Pagina) },
        { new ListarProyectosQuery(TamanioPagina: 0), nameof(ListarProyectosQuery.TamanioPagina) },
        { new ListarProyectosQuery(TamanioPagina: 101), nameof(ListarProyectosQuery.TamanioPagina) },
        { new ListarProyectosQuery(OrdenarPor: "foo"), nameof(ListarProyectosQuery.OrdenarPor) },
        { new ListarProyectosQuery(Direccion: "x"), nameof(ListarProyectosQuery.Direccion) },
        { new ListarProyectosQuery(Archivados: "x"), nameof(ListarProyectosQuery.Archivados) },
        { new ListarProyectosQuery(Texto: DatosPrueba.Texto(151)), nameof(ListarProyectosQuery.Texto) },
    };

    // RN-15, RN-16, CA-17, CA-18: obligatorios, largos y fecha objetivo >= inicio
    [Theory]
    [MemberData(nameof(AltasInvalidas))]
    public void ValidateAlta_CampoInvalido_DevuelveErrorEnElCampo(CrearProyectoRequest request, string campo)
    {
        // Act
        var resultado = _alta.Validate(request);

        // Assert
        Assert.Contains(resultado.Errors, e => e.PropertyName == campo);
    }

    // RN-16, CA-17: fechas iguales son válidas (límite)
    [Fact]
    public void ValidateAlta_FechaObjetivoIgualAInicio_SinErrores()
    {
        // Arrange
        var request = AltaValida with { FechaObjetivo = DatosPrueba.FechaInicio };

        // Act
        var resultado = _alta.Validate(request);

        // Assert
        Assert.True(resultado.IsValid);
    }

    // RN-15: límites exactos 150 y 2000 son válidos
    [Fact]
    public void ValidateAlta_NombreDe150YDescripcionDe2000_SinErrores()
    {
        // Arrange
        var request = AltaValida with { Nombre = DatosPrueba.Texto(150), Descripcion = DatosPrueba.Texto(2000) };

        // Act
        var resultado = _alta.Validate(request);

        // Assert
        Assert.True(resultado.IsValid);
    }

    // RN-16, CA-17: la edición también valida fechaObjetivo >= fechaInicio
    [Fact]
    public void ValidateEdicion_FechaObjetivoAnterior_DevuelveErrorEnFechaObjetivo()
    {
        // Arrange
        var request = EdicionValida with { FechaObjetivo = DatosPrueba.FechaInicio.AddDays(-1) };

        // Act
        var resultado = _edicion.Validate(request);

        // Assert
        Assert.Contains(resultado.Errors, e => e.PropertyName == nameof(EditarProyectoRequest.FechaObjetivo));
    }

    // RN-19, CA-18: la edición exige estadoId
    [Theory]
    [InlineData(null)]
    [InlineData(0)]
    public void ValidateEdicion_SinEstadoId_DevuelveErrorEnEstadoId(int? estadoId)
    {
        // Act
        var resultado = _edicion.Validate(EdicionValida with { EstadoId = estadoId });

        // Assert
        Assert.Contains(resultado.Errors, e => e.PropertyName == nameof(EditarProyectoRequest.EstadoId));
    }

    // CA-27: parámetros de listado fuera de rango o no permitidos
    [Theory]
    [MemberData(nameof(QueriesInvalidas))]
    public void ValidateListado_ParametroInvalido_DevuelveErrorEnElCampo(ListarProyectosQuery query, string campo)
    {
        // Act
        var resultado = _listado.Validate(query);

        // Assert
        Assert.Contains(resultado.Errors, e => e.PropertyName == campo);
    }

    // CA-27: tamanioPagina=100 es el límite válido; opciones de la lista blanca aceptadas
    [Fact]
    public void ValidateListado_TamanioPagina100YOpcionesValidas_SinErrores()
    {
        // Arrange
        var query = new ListarProyectosQuery("prj", 1, 2, "incluir", 1, 100, "fechaobjetivo", "asc");

        // Act
        var resultado = _listado.Validate(query);

        // Assert
        Assert.True(resultado.IsValid);
    }
}
