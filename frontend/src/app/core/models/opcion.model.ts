/**
 * Opción genérica para selects (valor + etiqueta visible).
 *
 * Creado: 2026-10-09
 * Modificado: 2026-10-09 - Creación inicial (RF-001)
 */
export interface Opcion<T> {
  readonly valor: T;
  readonly etiqueta: string;
}
