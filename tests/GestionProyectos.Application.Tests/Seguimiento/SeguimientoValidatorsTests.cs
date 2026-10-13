using GestionProyectos.Application.Seguimiento;
using GestionProyectos.Application.Tests.Comun;

namespace GestionProyectos.Application.Tests.Seguimiento;

/// <summary>
/// Tests de los validadores de seguimiento y de la configuración del tablero (RN-01, RN-21, RN-28, RN-31).
/// </summary>
/// <remarks>
/// Creado: 2026-10-11
/// Modificado: 2026-10-11 - Versión inicial (tests RF-005)
/// </remarks>
public class SeguimientoValidatorsTests
{
    public static TheoryData<ConsultaTableroProyectosQuery, string> TablerosProyectosInvalidos => new()
    {
        { new ConsultaTableroProyectosQuery(OrdenarPor: "foo"), nameof(ConsultaTableroProyectosQuery.OrdenarPor) },
        { new ConsultaTableroProyectosQuery(Direccion: "x"), nameof(ConsultaTableroProyectosQuery.Direccion) },
        { new ConsultaTableroProyectosQuery(Archivados: "x"), nameof(ConsultaTableroProyectosQuery.Archivados) },
        { new ConsultaTableroProyectosQuery(Pagina: 0), nameof(ConsultaTableroProyectosQuery.Pagina) },
        { new ConsultaTableroProyectosQuery(TamanioPagina: 0), nameof(ConsultaTableroProyectosQuery.TamanioPagina) },
        { new ConsultaTableroProyectosQuery(TamanioPagina: 101), nameof(ConsultaTableroProyectosQuery.TamanioPagina) },
        { new ConsultaTableroProyectosQuery(Texto: DatosPrueba.Texto(151)), nameof(ConsultaTableroProyectosQuery.Texto) },
    };

    // CA-05, RN-28: parámetros inválidos del tablero general → error en el campo
    [Theory]
    [MemberData(nameof(TablerosProyectosInvalidos))]
    public void TableroProyectosValidator_ParametroInvalido_DevuelveErrorEnElCampo(ConsultaTableroProyectosQuery query,
        string campo)
    {
        // Act
        var resultado = new ConsultaTableroProyectosQueryValidator().Validate(query);

        // Assert
        Assert.Contains(resultado.Errors, e => e.PropertyName == campo);
    }

    // CA-05: límites válidos (tamanioPagina 100, texto 150) y cada orden de la lista blanca normalizado
    [Theory]
    [InlineData("codigo")]
    [InlineData("nombre")]
    [InlineData("total")]
    [InlineData("abiertas")]
    [InlineData("vencidas")]
    [InlineData("bloqueadas")]
    [InlineData("enEspera")]
    [InlineData("sinResponsable")]
    public void TableroProyectosValidator_LimitesYOrdenesValidos_EsValido(string ordenarPor)
    {
        // Arrange
        var query = new ConsultaTableroProyectosQuery(DatosPrueba.Texto(150), "incluir", [1], [2], 1, 100,
            ordenarPor, "ASC").Normalizar();

        // Act
        var resultado = new ConsultaTableroProyectosQueryValidator().Validate(query);

        // Assert
        Assert.True(resultado.IsValid);
    }

    // RN-01, CA-21: agrupacionId y sinAgrupacion son excluyentes en el resumen
    [Fact]
    public void SeguimientoValidator_AgrupacionYSinAgrupacion_DevuelveError()
    {
        // Act
        var resultado = new ConsultaSeguimientoQueryValidator().Validate(new ConsultaSeguimientoQuery(8, true));

        // Assert
        Assert.Contains(resultado.Errors, e => e.PropertyName == nameof(ConsultaSeguimientoQuery.AgrupacionId));
    }

    // RN-01: solo agrupacionId, solo sinAgrupacion o agrupacionId con sinAgrupacion=false son válidos
    [Theory]
    [InlineData(8, null)]
    [InlineData(8, false)]
    [InlineData(null, true)]
    public void SeguimientoValidator_AmbitoValido_EsValido(int? agrupacionId, bool? sinAgrupacion)
    {
        // Act
        var resultado = new ConsultaSeguimientoQueryValidator()
            .Validate(new ConsultaSeguimientoQuery(agrupacionId, sinAgrupacion));

        // Assert
        Assert.True(resultado.IsValid);
    }

    public static TheoryData<ConsultaTableroIncidenciasQuery, string> TablerosInvalidos => new()
    {
        { new ConsultaTableroIncidenciasQuery(8, true), nameof(ConsultaTableroIncidenciasQuery.AgrupacionId) },
        { new ConsultaTableroIncidenciasQuery(Indicador: "foo"), nameof(ConsultaTableroIncidenciasQuery.Indicador) },
        { new ConsultaTableroIncidenciasQuery(EstadoId: 0), nameof(ConsultaTableroIncidenciasQuery.EstadoId) },
        { new ConsultaTableroIncidenciasQuery(EstadoId: -1), nameof(ConsultaTableroIncidenciasQuery.EstadoId) },
        { new ConsultaTableroIncidenciasQuery(Texto: DatosPrueba.Texto(201)), nameof(ConsultaTableroIncidenciasQuery.Texto) },
    };

    // RN-01, RN-21, RN-28, CA-26: ámbito excluyente, indicador fuera de lista, estadoId <= 0, texto > 200
    [Theory]
    [MemberData(nameof(TablerosInvalidos))]
    public void TableroValidator_ParametroInvalido_DevuelveErrorEnElCampo(ConsultaTableroIncidenciasQuery query,
        string campo)
    {
        // Act
        var resultado = new ConsultaTableroIncidenciasQueryValidator().Validate(query);

        // Assert
        Assert.Contains(resultado.Errors, e => e.PropertyName == campo);
    }

    // CA-26: el mensaje del indicador inválido nombra el valor recibido
    [Fact]
    public void TableroValidator_IndicadorFoo_MensajeNombraElValor()
    {
        // Act
        var resultado = new ConsultaTableroIncidenciasQueryValidator()
            .Validate(new ConsultaTableroIncidenciasQuery(Indicador: "foo"));

        // Assert
        Assert.Equal("El indicador 'foo' no es válido.", Assert.Single(resultado.Errors).ErrorMessage);
    }

    // RN-21, CA-26: cada indicador de la lista (también en mayúsculas, tras normalizar) es válido
    [Theory]
    [InlineData("abiertas")]
    [InlineData("VENCIDAS")]
    [InlineData("bloqueadas")]
    [InlineData("En-Espera")]
    [InlineData("sin-responsable")]
    public void TableroValidator_IndicadorDeLaLista_EsValido(string indicador)
    {
        // Arrange
        var query = new ConsultaTableroIncidenciasQuery(Indicador: indicador).Normalizar();

        // Act
        var resultado = new ConsultaTableroIncidenciasQueryValidator().Validate(query);

        // Assert
        Assert.True(resultado.IsValid);
    }

    // RN-21: texto de 200 con indicador y estadoId juntos es válido
    [Fact]
    public void TableroValidator_TresFiltrosJuntosYTextoDe200_EsValido()
    {
        // Arrange
        var query = new ConsultaTableroIncidenciasQuery(8, null, [2], [2], "en-espera", 4, DatosPrueba.Texto(200));

        // Act
        var resultado = new ConsultaTableroIncidenciasQueryValidator().Validate(query);

        // Assert
        Assert.True(resultado.IsValid);
    }

    // RN-21: Normalizar recorta el texto (vacío = sin filtro) y pasa el indicador a minúsculas
    [Fact]
    public void Normalizar_TextoConEspaciosEIndicadorEnMayusculas_RecortaYPasaAMinusculas()
    {
        // Act
        var query = new ConsultaTableroIncidenciasQuery(Indicador: " VENCIDAS ", Texto: "   ").Normalizar();

        // Assert
        Assert.Equal(("vencidas", (string?)null), (query.Indicador, query.Texto));
    }

    public static TheoryData<ConfiguracionTableroRequest, string> ConfiguracionesInvalidas => new()
    {
        { new ConfiguracionTableroRequest(null, true, true), nameof(ConfiguracionTableroRequest.MostrarEstado) },
        { new ConfiguracionTableroRequest(true, null, true), nameof(ConfiguracionTableroRequest.MostrarEstimacion) },
        { new ConfiguracionTableroRequest(true, true, null), nameof(ConfiguracionTableroRequest.MostrarResponsable) },
    };

    // RN-31, CA-33: cada mostrar* ausente o null → error en la clave
    [Theory]
    [MemberData(nameof(ConfiguracionesInvalidas))]
    public void ConfiguracionValidator_ValorNull_DevuelveErrorEnElCampo(ConfiguracionTableroRequest request,
        string campo)
    {
        // Act
        var resultado = new ConfiguracionTableroRequestValidator().Validate(request);

        // Assert
        Assert.Equal(campo, Assert.Single(resultado.Errors).PropertyName);
    }

    // RN-31: los tres valores informados → válido
    [Fact]
    public void ConfiguracionValidator_TresValoresInformados_EsValido()
    {
        // Act
        var resultado = new ConfiguracionTableroRequestValidator()
            .Validate(new ConfiguracionTableroRequest(true, false, true));

        // Assert
        Assert.True(resultado.IsValid);
    }
}
