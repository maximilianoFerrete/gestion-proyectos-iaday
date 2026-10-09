using GestionProyectos.Domain.Excepciones;
using Microsoft.Data.SqlClient;

namespace GestionProyectos.Infrastructure.SqlServer.Conexion;

/// <summary>
/// Traduce violaciones de unicidad de SQL Server (2627/2601) a errores 409 del catálogo.
/// </summary>
/// <remarks>
/// Creado: 2026-10-09
/// Modificado: 2026-10-09 - Versión inicial (RF-001)
/// </remarks>
internal static class TraductorErroresSql
{
    private const int ViolacionRestriccionUnica = 2627;
    private const int ViolacionIndiceUnico = 2601;

    private static readonly IReadOnlyDictionary<string, (string Codigo, string Mensaje)> Restricciones =
        new Dictionary<string, (string, string)>(StringComparer.OrdinalIgnoreCase)
        {
            ["UQ_Personas_Email"] =
                (CodigosError.PersonaEmailDuplicado, "Ya existe una persona con ese email."),
            ["UQ_EstadosProyecto_Nombre"] =
                (CodigosError.EstadoNombreDuplicado, "Ya existe un estado de proyecto con ese nombre."),
            ["UQ_Agrupaciones_ProyectoId_Nombre"] =
                (CodigosError.AgrupacionNombreDuplicado, "Ya existe una agrupación con ese nombre en el proyecto."),
        };

    /// <summary>Indica si es una violación de unicidad conocida (respaldo de la validación propia).</summary>
    public static bool EsDuplicado(SqlException ex) =>
        ex.Number is ViolacionRestriccionUnica or ViolacionIndiceUnico && BuscarRestriccion(ex) is not null;

    /// <summary>Convierte la violación de unicidad en ConflictoException con su código.</summary>
    public static ConflictoException Traducir(SqlException ex)
    {
        var (codigo, mensaje) = Restricciones[BuscarRestriccion(ex)
            ?? throw new InvalidOperationException("La excepción no es un duplicado conocido.", ex)];
        return new ConflictoException(codigo, mensaje);
    }

    private static string? BuscarRestriccion(SqlException ex) =>
        Restricciones.Keys.FirstOrDefault(nombre => ex.Message.Contains(nombre, StringComparison.OrdinalIgnoreCase));
}
