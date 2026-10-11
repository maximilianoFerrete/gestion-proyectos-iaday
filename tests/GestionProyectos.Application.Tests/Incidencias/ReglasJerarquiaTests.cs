using GestionProyectos.Application.Tests.Comun;
using GestionProyectos.Domain.Entidades;
using GestionProyectos.Domain.Excepciones;
using GestionProyectos.Domain.Reglas;

namespace GestionProyectos.Application.Tests.Incidencias;

/// <summary>
/// Tests de las reglas puras de jerarquía de incidencias (RN-25, RN-26, RN-28, RN-29, RN-31, RN-32).
/// </summary>
/// <remarks>
/// Creado: 2026-10-10
/// Modificado: 2026-10-10 - Versión inicial (tests RF-002)
/// </remarks>
public class ReglasJerarquiaTests
{
    private static readonly DateOnly Nov30 = new(2026, 11, 30);
    private static readonly DateOnly Dic01 = new(2026, 12, 1);
    private static readonly DateOnly Nov20 = new(2026, 11, 20);
    private static readonly DateOnly Nov22 = new(2026, 11, 22);
    private static readonly DateOnly Nov25 = new(2026, 11, 25);

    // RN-26, CA-26: el padre propuesto es la propia incidencia
    [Fact]
    public void AsegurarSinCiclo_PadreEsLaPropiaIncidencia_LanzaRelacionCircular()
    {
        // Act
        var ex = Assert.Throws<ConflictoException>(() => ReglasJerarquia.AsegurarSinCiclo(15, 15, []));

        // Assert
        Assert.Equal(CodigosError.RelacionCircular, ex.Codigo);
    }

    // RN-26, CA-26: el padre propuesto es descendiente (sus ancestros contienen la incidencia)
    [Fact]
    public void AsegurarSinCiclo_AncestrosDelPadreContienenLaIncidencia_LanzaRelacionCircular()
    {
        // Act
        var ex = Assert.Throws<ConflictoException>(() => ReglasJerarquia.AsegurarSinCiclo(15, 16, [15, 3]));

        // Assert
        Assert.Equal(CodigosError.RelacionCircular, ex.Codigo);
    }

    // RN-26: ancestros sin la incidencia → válido
    [Fact]
    public void AsegurarSinCiclo_AncestrosSinLaIncidencia_NoLanza()
    {
        // Act
        var ex = Record.Exception(() => ReglasJerarquia.AsegurarSinCiclo(15, 4, [3, 1]));

        // Assert
        Assert.Null(ex);
    }

    // RN-25, CA-24: el par tipo padre → tipo hijo no está en las relaciones
    [Fact]
    public void AsegurarRelacionPermitida_ParInexistente_LanzaRelacionNoPermitida()
    {
        // Arrange
        var hu = DatosIncidencias.Tipo(2, "Historia de Usuario");
        var epica = DatosIncidencias.Tipo(1, "Épica", nivelId: 1);

        // Act
        var ex = Assert.Throws<ValidacionNegocioException>(
            () => ReglasJerarquia.AsegurarRelacionPermitida(hu, epica, [3, 4, 5]));

        // Assert
        Assert.Equal(CodigosError.RelacionNoPermitida, ex.Codigo);
    }

    // RN-25, CA-04: el par existe → válido
    [Fact]
    public void AsegurarRelacionPermitida_ParExistente_NoLanza()
    {
        // Arrange
        var hu = DatosIncidencias.Tipo(2, "Historia de Usuario");
        var subtarea = DatosIncidencias.Tipo(3, "Subtarea", nivelId: 3);

        // Act
        var ex = Record.Exception(() => ReglasJerarquia.AsegurarRelacionPermitida(hu, subtarea, [3, 4, 5]));

        // Assert
        Assert.Null(ex);
    }

    // RN-31, CA-31: vencimiento de la hija posterior al del padre
    [Fact]
    public void AsegurarVencimientoNoPosteriorAlPadre_HijaPosterior_LanzaVencimientoPosteriorAlPadre()
    {
        // Arrange
        var padre = DatosIncidencias.Incidencia(15, vencimiento: Nov30);

        // Act
        var ex = Assert.Throws<ValidacionNegocioException>(
            () => ReglasJerarquia.AsegurarVencimientoNoPosteriorAlPadre(Dic01, padre));

        // Assert
        Assert.Equal(CodigosError.VencimientoPosteriorAlPadre, ex.Codigo);
    }

    public static TheoryData<DateOnly?, DateOnly?> VencimientosValidos => new()
    {
        { new DateOnly(2026, 11, 30), new DateOnly(2026, 11, 30) },
        { null, new DateOnly(2026, 11, 30) },
        { new DateOnly(2026, 12, 10), null },
        { null, null },
        { new DateOnly(2026, 11, 1), new DateOnly(2026, 11, 30) },
    };

    // RN-31, CA-31, DA-05: límites válidos (igual, hija sin fecha, padre sin fecha, anterior)
    [Theory]
    [MemberData(nameof(VencimientosValidos))]
    public void AsegurarVencimientoNoPosteriorAlPadre_Limites_NoLanza(DateOnly? hija, DateOnly? delPadre)
    {
        // Arrange
        var padre = DatosIncidencias.Incidencia(15, vencimiento: delPadre);

        // Act
        var ex = Record.Exception(() => ReglasJerarquia.AsegurarVencimientoNoPosteriorAlPadre(hija, padre));

        // Assert
        Assert.Null(ex);
    }

    // RN-32, CA-32: nuevo vencimiento anterior al de alguna hija → 409 con los códigos de esas hijas
    [Fact]
    public void AsegurarVencimientoNoAnteriorAHijas_AnteriorAUnaHija_LanzaConCodigoDeEsaHija()
    {
        // Arrange
        HijaIncidencia[] hijas = [DatosIncidencias.Hija(16, vencimiento: Nov20), DatosIncidencias.Hija(17, vencimiento: Nov25)];

        // Act
        var ex = Assert.Throws<ConflictoException>(
            () => ReglasJerarquia.AsegurarVencimientoNoAnteriorAHijas(Nov22, hijas));

        // Assert
        Assert.Equal(CodigosError.VencimientoAnteriorAHijas, ex.Codigo);
        Assert.Equal(["PRJ-0001-17"], (string[])ex.Extensiones[ReglasJerarquia.ExtensionIncidencias]!);
    }

    // RN-32, CA-32: quitar el vencimiento del padre siempre es válido
    [Fact]
    public void AsegurarVencimientoNoAnteriorAHijas_NuevoVencimientoNull_NoLanza()
    {
        // Arrange
        HijaIncidencia[] hijas = [DatosIncidencias.Hija(16, vencimiento: Nov20), DatosIncidencias.Hija(17, vencimiento: Nov25)];

        // Act
        var ex = Record.Exception(() => ReglasJerarquia.AsegurarVencimientoNoAnteriorAHijas(null, hijas));

        // Assert
        Assert.Null(ex);
    }

    // RN-32: hijas con fecha igual o sin fecha no bloquean
    [Fact]
    public void AsegurarVencimientoNoAnteriorAHijas_HijasIgualesOSinFecha_NoLanza()
    {
        // Arrange
        HijaIncidencia[] hijas = [DatosIncidencias.Hija(16, vencimiento: Nov25), DatosIncidencias.Hija(17)];

        // Act
        var ex = Record.Exception(() => ReglasJerarquia.AsegurarVencimientoNoAnteriorAHijas(Nov25, hijas));

        // Assert
        Assert.Null(ex);
    }

    // RN-25, CA-38: alguna hija incompatible con el nuevo tipo → 409 con sus códigos
    [Fact]
    public void AsegurarHijasCompatibles_HijaIncompatible_LanzaTipoIncompatibleConHijas()
    {
        // Arrange
        var nuevoTipo = DatosIncidencias.Tipo(10, "Otro nivel 2");
        HijaIncidencia[] hijas = [DatosIncidencias.Hija(16, tipoId: 3), DatosIncidencias.Hija(17, tipoId: 4)];

        // Act
        var ex = Assert.Throws<ConflictoException>(
            () => ReglasJerarquia.AsegurarHijasCompatibles(nuevoTipo, hijas, [3]));

        // Assert
        Assert.Equal(CodigosError.TipoIncompatibleConHijas, ex.Codigo);
        Assert.Equal(["PRJ-0001-17"], (string[])ex.Extensiones[ReglasJerarquia.ExtensionIncidencias]!);
    }

    // RN-25: todas las hijas compatibles → válido
    [Fact]
    public void AsegurarHijasCompatibles_TodasCompatibles_NoLanza()
    {
        // Arrange
        var nuevoTipo = DatosIncidencias.Tipo(10, "Otro nivel 2");
        HijaIncidencia[] hijas = [DatosIncidencias.Hija(16, tipoId: 3), DatosIncidencias.Hija(17, tipoId: 4)];

        // Act
        var ex = Record.Exception(() => ReglasJerarquia.AsegurarHijasCompatibles(nuevoTipo, hijas, [3, 4]));

        // Assert
        Assert.Null(ex);
    }

    // RN-28: con padre y agrupación nula se hereda la del padre
    [Fact]
    public void ResolverAgrupacionConPadre_AgrupacionNula_DevuelveLaDelPadre()
    {
        // Arrange
        var padre = DatosIncidencias.Incidencia(3, agrupacionId: 8);

        // Act
        var agrupacion = ReglasJerarquia.ResolverAgrupacionConPadre(null, padre);

        // Assert
        Assert.Equal(8, agrupacion);
    }

    // RN-28: con padre y agrupación igual a la del padre se acepta
    [Fact]
    public void ResolverAgrupacionConPadre_AgrupacionIgualALaDelPadre_DevuelveLaDelPadre()
    {
        // Arrange
        var padre = DatosIncidencias.Incidencia(3, agrupacionId: 8);

        // Act
        var agrupacion = ReglasJerarquia.ResolverAgrupacionConPadre(8, padre);

        // Assert
        Assert.Equal(8, agrupacion);
    }

    // RN-28, CA-29: con padre y agrupación distinta → 400 AGRUPACION_HEREDADA
    [Fact]
    public void ResolverAgrupacionConPadre_AgrupacionDistinta_LanzaAgrupacionHeredada()
    {
        // Arrange
        var padre = DatosIncidencias.Incidencia(3, agrupacionId: 8);

        // Act
        var ex = Assert.Throws<ValidacionNegocioException>(() => ReglasJerarquia.ResolverAgrupacionConPadre(9, padre));

        // Assert
        Assert.Equal(CodigosError.AgrupacionHeredada, ex.Codigo);
    }

    // RN-28: padre "sin agrupación" y se envía una agrupación → 400 AGRUPACION_HEREDADA
    [Fact]
    public void ResolverAgrupacionConPadre_PadreSinAgrupacionYSeEnviaUna_LanzaAgrupacionHeredada()
    {
        // Arrange
        var padre = DatosIncidencias.Incidencia(3);

        // Act
        var ex = Assert.Throws<ValidacionNegocioException>(() => ReglasJerarquia.ResolverAgrupacionConPadre(8, padre));

        // Assert
        Assert.Equal(CodigosError.AgrupacionHeredada, ex.Codigo);
    }

    // RN-29, CA-30: al quitar el padre sin enviar agrupación se conserva la actual
    [Fact]
    public void ResolverAgrupacionSinPadre_QuitaPadreSinAgrupacion_ConservaLaActual()
    {
        // Arrange
        var actual = DatosIncidencias.Incidencia(15, agrupacionId: 9, padreId: 3);

        // Act
        var agrupacion = ReglasJerarquia.ResolverAgrupacionSinPadre(null, actual);

        // Assert
        Assert.Equal(9, agrupacion);
    }

    // RN-29: sin padre previo, agrupación nula significa "sin agrupación"
    [Fact]
    public void ResolverAgrupacionSinPadre_SinPadrePrevioYAgrupacionNula_DevuelveNull()
    {
        // Arrange
        var actual = DatosIncidencias.Incidencia(15, agrupacionId: 9);

        // Act
        var agrupacion = ReglasJerarquia.ResolverAgrupacionSinPadre(null, actual);

        // Assert
        Assert.Null(agrupacion);
    }

    // RN-29: al cambiar a otro padre se propaga aunque la agrupación sea la misma
    [Fact]
    public void RequierePropagacion_CambioDePadreMismaAgrupacion_DevuelveTrue()
    {
        // Act
        var propagar = ReglasJerarquia.RequierePropagacion(8, 3, new UbicacionIncidencia(4, 8));

        // Assert
        Assert.True(propagar);
    }

    // RN-29: mismo padre y misma agrupación → no propaga
    [Fact]
    public void RequierePropagacion_SinCambios_DevuelveFalse()
    {
        // Act
        var propagar = ReglasJerarquia.RequierePropagacion(8, 3, new UbicacionIncidencia(3, 8));

        // Assert
        Assert.False(propagar);
    }

    // RN-29, CA-30: quitar el padre conservando la agrupación → no propaga
    [Fact]
    public void RequierePropagacion_QuitaPadreConservandoAgrupacion_DevuelveFalse()
    {
        // Act
        var propagar = ReglasJerarquia.RequierePropagacion(8, 3, new UbicacionIncidencia(null, 8));

        // Assert
        Assert.False(propagar);
    }

    // RN-29, CA-28: sin padre y cambia la agrupación → propaga
    [Fact]
    public void RequierePropagacion_CambioDeAgrupacion_DevuelveTrue()
    {
        // Act
        var propagar = ReglasJerarquia.RequierePropagacion(8, null, new UbicacionIncidencia(null, 9));

        // Assert
        Assert.True(propagar);
    }
}
