using GestionProyectos.Application.Incidencias;
using GestionProyectos.Application.Tests.Comun;

namespace GestionProyectos.Application.Tests.Incidencias;

/// <summary>
/// Tests de los validadores de requests de incidencias y del filtro del listado (RN-19, CA-16, CA-19).
/// </summary>
/// <remarks>
/// Creado: 2026-10-10
/// Modificado: 2026-10-10 - Versión inicial (tests RF-002)
/// </remarks>
public class IncidenciaValidatorsTests
{
    private static readonly CrearIncidenciaRequest AltaValida =
        new(3, "Validar tarjeta", "Validar número y vencimiento.", null, null, null, null, null);

    private static readonly EditarIncidenciaRequest EdicionValida =
        new("Pagar con tarjeta", "Como cliente quiero pagar.", 2, 3, null, null, null, null);

    public static TheoryData<CrearIncidenciaRequest, string> AltasInvalidas => new()
    {
        { AltaValida with { TipoId = null }, nameof(CrearIncidenciaRequest.TipoId) },
        { AltaValida with { Titulo = "  " }, nameof(CrearIncidenciaRequest.Titulo) },
        { AltaValida with { Titulo = DatosPrueba.Texto(201) }, nameof(CrearIncidenciaRequest.Titulo) },
        { AltaValida with { Descripcion = null }, nameof(CrearIncidenciaRequest.Descripcion) },
        { AltaValida with { Descripcion = DatosPrueba.Texto(4001) }, nameof(CrearIncidenciaRequest.Descripcion) },
    };

    // RN-19, CA-16: tipo, título 1–200 y descripción 1–4000 obligatorios
    [Theory]
    [MemberData(nameof(AltasInvalidas))]
    public void CrearValidator_CampoInvalido_DevuelveErrorEnElCampo(CrearIncidenciaRequest request, string campo)
    {
        // Act
        var resultado = new CrearIncidenciaRequestValidator().Validate(request);

        // Assert
        Assert.Contains(resultado.Errors, e => e.PropertyName == campo);
    }

    // RN-19, RN-22: límites exactos válidos; responsable y vencimiento opcionales
    [Fact]
    public void CrearValidator_TituloDe200YDescripcionDe4000_EsValido()
    {
        // Act
        var resultado = new CrearIncidenciaRequestValidator().Validate(
            AltaValida with { Titulo = DatosPrueba.Texto(200), Descripcion = DatosPrueba.Texto(4000) });

        // Assert
        Assert.True(resultado.IsValid);
    }

    public static TheoryData<EditarIncidenciaRequest, string> EdicionesInvalidas => new()
    {
        { EdicionValida with { Titulo = null }, nameof(EditarIncidenciaRequest.Titulo) },
        { EdicionValida with { Descripcion = "" }, nameof(EditarIncidenciaRequest.Descripcion) },
        { EdicionValida with { EstadoId = null }, nameof(EditarIncidenciaRequest.EstadoId) },
        { EdicionValida with { PrioridadId = null }, nameof(EditarIncidenciaRequest.PrioridadId) },
    };

    // RN-19, RN-21, CA-16: título, descripción, estado y prioridad obligatorios en la edición
    [Theory]
    [MemberData(nameof(EdicionesInvalidas))]
    public void EditarValidator_CampoInvalido_DevuelveErrorEnElCampo(EditarIncidenciaRequest request, string campo)
    {
        // Act
        var resultado = new EditarIncidenciaRequestValidator().Validate(request);

        // Assert
        Assert.Contains(resultado.Errors, e => e.PropertyName == campo);
    }

    // RN-02: cambiar tipo exige tipoId
    [Fact]
    public void CambiarTipoValidator_TipoIdAusente_DevuelveErrorEnTipoId()
    {
        // Act
        var resultado = new CambiarTipoIncidenciaRequestValidator().Validate(new CambiarTipoIncidenciaRequest(null, 2));

        // Assert
        Assert.Contains(resultado.Errors, e => e.PropertyName == nameof(CambiarTipoIncidenciaRequest.TipoId));
    }

    public static TheoryData<ListarIncidenciasQuery, string> QueriesInvalidas => new()
    {
        { new ListarIncidenciasQuery(ResponsableId: 2, SinResponsable: true), nameof(ListarIncidenciasQuery.ResponsableId) },
        { new ListarIncidenciasQuery(AgrupacionId: 8, SinAgrupacion: true), nameof(ListarIncidenciasQuery.AgrupacionId) },
        { new ListarIncidenciasQuery(TamanioPagina: 101), nameof(ListarIncidenciasQuery.TamanioPagina) },
        { new ListarIncidenciasQuery(TamanioPagina: 0), nameof(ListarIncidenciasQuery.TamanioPagina) },
        { new ListarIncidenciasQuery(Pagina: 0), nameof(ListarIncidenciasQuery.Pagina) },
        { new ListarIncidenciasQuery(OrdenarPor: "foo"), nameof(ListarIncidenciasQuery.OrdenarPor) },
        { new ListarIncidenciasQuery(Direccion: "arriba"), nameof(ListarIncidenciasQuery.Direccion) },
        { new ListarIncidenciasQuery(Texto: DatosPrueba.Texto(201)), nameof(ListarIncidenciasQuery.Texto) },
    };

    // CA-19: combinaciones excluyentes y valores fuera de rango → error de validación
    [Theory]
    [MemberData(nameof(QueriesInvalidas))]
    public void ListarQueryValidator_ParametroInvalido_DevuelveErrorEnElParametro(ListarIncidenciasQuery query,
        string campo)
    {
        // Act
        var resultado = new ListarIncidenciasQueryValidator().Validate(query.Normalizar());

        // Assert
        Assert.Contains(resultado.Errors, e => e.PropertyName == campo);
    }

    // CA-19: tamaño 100, texto 200, orden en mayúsculas y "sin" sin id son válidos
    [Fact]
    public void ListarQueryValidator_LimitesValidos_EsValido()
    {
        // Arrange
        var query = new ListarIncidenciasQuery(Texto: DatosPrueba.Texto(200), TamanioPagina: 100,
            OrdenarPor: "FechaVencimiento", Direccion: "ASC", SinResponsable: true, SinAgrupacion: true);

        // Act
        var resultado = new ListarIncidenciasQueryValidator().Validate(query.Normalizar());

        // Assert
        Assert.True(resultado.IsValid);
    }

    // Contrato GET incidencias: defaults página 1, tamaño 20, numero desc
    [Fact]
    public void CrearFiltro_QueryVacia_AplicaDefaults()
    {
        // Act
        var filtro = OpcionesListadoIncidencias.CrearFiltro(1, new ListarIncidenciasQuery());

        // Assert
        Assert.Equal(new FiltroIncidencias(1, null, filtro.TipoIds, null, null, null, false, null, false, 1, 20,
            OrdenIncidencias.Numero, true), filtro);
        Assert.Empty(filtro.TipoIds);
    }

    // CA-19: tipoId repetible, sin duplicados
    [Fact]
    public void CrearFiltro_TipoIdsRepetidos_DevuelveTiposSinDuplicados()
    {
        // Act
        var filtro = OpcionesListadoIncidencias.CrearFiltro(1, new ListarIncidenciasQuery(TipoId: [1, 2, 1]));

        // Assert
        Assert.Equal([1, 2], filtro.TipoIds);
    }

    // Contrato GET incidencias: ordenarPor y direccion explícitos
    [Fact]
    public void CrearFiltro_OrdenTituloAsc_DevuelveTituloAscendente()
    {
        // Act
        var filtro = OpcionesListadoIncidencias.CrearFiltro(1,
            new ListarIncidenciasQuery(OrdenarPor: "Titulo", Direccion: "Asc").Normalizar());

        // Assert
        Assert.Equal(OrdenIncidencias.Titulo, filtro.OrdenarPor);
        Assert.False(filtro.Descendente);
    }
}
