namespace GestionProyectos.Domain.Excepciones;

/// <summary>
/// Se intentó modificar un proyecto archivado o algo que depende de él (RN-22).
/// </summary>
/// <remarks>
/// Creado: 2026-10-09
/// Modificado: 2026-10-09 - Versión inicial (RF-001)
/// </remarks>
public class ProyectoArchivadoException(string codigoProyecto)
    : ConflictoException(
        CodigosError.ProyectoArchivado,
        $"El proyecto {codigoProyecto} está archivado y no admite modificaciones.");
