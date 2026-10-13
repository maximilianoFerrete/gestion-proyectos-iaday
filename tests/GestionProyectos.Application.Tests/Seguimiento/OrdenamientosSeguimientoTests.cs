using GestionProyectos.Application.Seguimiento;
using static GestionProyectos.Application.Tests.Comun.DatosSeguimiento;

namespace GestionProyectos.Application.Tests.Seguimiento;

/// <summary>
/// Tests de OrdenadorProyectosSeguimiento (orden y paginación en memoria) y OrdenTableroPorDefecto (RN-32).
/// </summary>
/// <remarks>
/// Creado: 2026-10-11
/// Modificado: 2026-10-11 - Versión inicial (tests RF-005)
/// </remarks>
public class OrdenamientosSeguimientoTests
{
    // CA-04: vencidas desc; los empates se ordenan por código desc
    [Fact]
    public void Ordenar_VencidasDescConEmpate_DesempataPorCodigoDesc()
    {
        // Arrange
        FilaTableroProyectosDto[] filas =
        [
            Fila(1, Indicadores(vencidas: 2)), Fila(2, Indicadores(vencidas: 5)), Fila(3, Indicadores(vencidas: 2)),
        ];

        // Act
        var ordenadas = OrdenadorProyectosSeguimiento.Ordenar(filas, OrdenTableroProyectos.Vencidas, descendente: true);

        // Assert
        Assert.Equal(["PRJ-0002", "PRJ-0003", "PRJ-0001"], ordenadas.Select(f => f.Proyecto.Codigo));
    }

    // Contrato tablero general: empate de código desempata por id
    [Fact]
    public void Ordenar_MismoCodigo_DesempataPorId()
    {
        // Arrange
        FilaTableroProyectosDto[] filas = [Fila(4, codigo: "PRJ-0001"), Fila(9, codigo: "PRJ-0001")];

        // Act
        var ordenadas = OrdenadorProyectosSeguimiento.Ordenar(filas, OrdenTableroProyectos.Total, descendente: true);

        // Assert
        Assert.Equal([9, 4], ordenadas.Select(f => f.Proyecto.Id));
    }

    // Contrato tablero general: orden por código asc respeta el largo (PRJ-9999 antes que PRJ-10000)
    [Fact]
    public void Ordenar_CodigoAsc_OrdenaPorLargoYLuegoOrdinal()
    {
        // Arrange
        FilaTableroProyectosDto[] filas =
        [
            Fila(1, codigo: "PRJ-10000"), Fila(2, codigo: "PRJ-9999"), Fila(3, codigo: "PRJ-0002"),
        ];

        // Act
        var ordenadas = OrdenadorProyectosSeguimiento.Ordenar(filas, OrdenTableroProyectos.Codigo, descendente: false);

        // Assert
        Assert.Equal(["PRJ-0002", "PRJ-9999", "PRJ-10000"], ordenadas.Select(f => f.Proyecto.Codigo));
    }

    // Contrato tablero general: orden por nombre sin distinguir mayúsculas
    [Fact]
    public void Ordenar_NombreAsc_NoDistingueMayusculas()
    {
        // Arrange
        FilaTableroProyectosDto[] filas = [Fila(1, nombre: "beta"), Fila(2, nombre: "Alfa"), Fila(3, nombre: "Gama")];

        // Act
        var ordenadas = OrdenadorProyectosSeguimiento.Ordenar(filas, OrdenTableroProyectos.Nombre, descendente: false);

        // Assert
        Assert.Equal(["Alfa", "beta", "Gama"], ordenadas.Select(f => f.Proyecto.Nombre));
    }

    // CA-03: página 2 de 25 con tamaño 20 → 5 ítems y total 25
    [Fact]
    public void Paginar_Pagina2De25Con20_Devuelve5ItemsYTotal25()
    {
        // Arrange
        var filas = Enumerable.Range(1, 25).ToList();

        // Act
        var pagina = OrdenadorProyectosSeguimiento.Paginar(filas, 2, 20);

        // Assert
        Assert.Equal([21, 22, 23, 24, 25], pagina.Items);
        Assert.Equal((2, 20, 25), (pagina.Pagina, pagina.TamanioPagina, pagina.Total));
    }

    // Contrato tablero general: página fuera de rango → ítems vacíos con el total correcto
    [Fact]
    public void Paginar_PaginaFueraDeRango_DevuelveItemsVaciosYTotal()
    {
        // Arrange
        var filas = Enumerable.Range(1, 25).ToList();

        // Act
        var pagina = OrdenadorProyectosSeguimiento.Paginar(filas, 3, 20);

        // Assert
        Assert.Empty(pagina.Items);
        Assert.Equal(25, pagina.Total);
    }

    // RN-32, CA-29: Épica #3, HU #15, Subtarea #16, HU #7 → 3, 7, 15, 16
    [Fact]
    public void OrdenTableroPorDefecto_TiposDeNivelesDistintos_OrdenaPorNivelYNumero()
    {
        // Arrange
        ItemTableroDto[] items = [Item(3, Epica), Item(15, Hu), Item(16, Subtarea), Item(7, Hu)];

        // Act
        var ordenados = OrdenTableroPorDefecto.Ordenar(items);

        // Assert
        Assert.Equal([3, 7, 15, 16], ordenados.Select(i => i.Numero));
    }

    // RN-32: dos tipos del mismo nivel → por nombre del tipo, luego número
    [Fact]
    public void OrdenTableroPorDefecto_TiposDelMismoNivel_OrdenaPorNombreDelTipoYNumero()
    {
        // Arrange
        ItemTableroDto[] items = [Item(16, Subtarea), Item(30, Bug), Item(12, Subtarea), Item(20, Bug)];

        // Act
        var ordenados = OrdenTableroPorDefecto.Ordenar(items);

        // Assert
        Assert.Equal([20, 30, 12, 16], ordenados.Select(i => i.Numero));
    }
}
