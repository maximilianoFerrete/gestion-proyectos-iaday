import { esErrorApi } from '../../../core/models/error-api.model';

/**
 * Utilidades para mostrar la extensión `incidencias` de ProblemDetails (RF-002 0.2) como enlaces.
 *
 * Creado: 2026-10-09
 * Modificado: 2026-10-09 - Creación inicial (RF-002)
 */

/** Referencia mínima para resolver un código a id. */
export interface ReferenciaCodigo {
  readonly id: number;
  readonly codigo: string;
}

/** Enlace a una incidencia afectada: con id si se conoce; si no, búsqueda por código en I1. */
export interface EnlaceIncidencia {
  readonly codigo: string;
  readonly ruta: readonly (string | number)[];
  readonly queryParams: Readonly<Record<string, string>>;
}

/** Códigos de la extensión `incidencias` de un error (vacío si no es un `ErrorApi`). */
export function incidenciasDelError(error: unknown): readonly string[] {
  return esErrorApi(error) ? error.incidencias : [];
}

/**
 * Arma los enlaces: si el código está entre las referencias conocidas (p. ej. hijas) enlaza al
 * detalle; si no, al listado filtrado por ese código (la API solo informa códigos).
 */
export function enlacesIncidencias(
  proyectoId: number,
  codigos: readonly string[],
  conocidas: readonly ReferenciaCodigo[],
): EnlaceIncidencia[] {
  return codigos.map((codigo): EnlaceIncidencia => {
    const referencia = conocidas.find((c) => c.codigo === codigo);
    return referencia
      ? { codigo, ruta: ['/proyectos', proyectoId, 'incidencias', referencia.id], queryParams: {} }
      : { codigo, ruta: ['/proyectos', proyectoId, 'incidencias'], queryParams: { texto: codigo } };
  });
}
