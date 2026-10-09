/**
 * Respuesta paginada estándar de la API (spec 0.2).
 *
 * Creado: 2026-10-09
 * Modificado: 2026-10-09 - Creación inicial (RF-001)
 */
export interface Paginado<T> {
  readonly items: readonly T[];
  readonly pagina: number;
  readonly tamanioPagina: number;
  readonly total: number;
}
