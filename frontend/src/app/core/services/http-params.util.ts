import { HttpParams } from '@angular/common/http';

/**
 * Utilidades para armar query params de la API.
 *
 * Creado: 2026-10-09
 * Modificado: 2026-10-09 - Admite parámetros repetibles (arrays), p. ej. `tipoId` (RF-002)
 */

/** Valor simple admitido como query param. */
export type ValorSimpleParametro = string | number | boolean | null | undefined;

/** Valor admitido como query param; un array genera el parámetro repetido (`a=1&a=2`). */
export type ValorParametro = ValorSimpleParametro | readonly (string | number)[];

/** Indica si el valor simple debe omitirse (null, undefined o string vacío). */
function esVacio(valor: ValorSimpleParametro): boolean {
  return valor === null || valor === undefined || valor === '';
}

/** Type guard: el valor es una lista (parámetro repetible). */
function esLista(valor: ValorParametro): valor is readonly (string | number)[] {
  return Array.isArray(valor);
}

/** Arma HttpParams omitiendo valores null, undefined o strings vacíos. */
export function crearParams(valores: Readonly<Record<string, ValorParametro>>): HttpParams {
  let params = new HttpParams();
  for (const [clave, valor] of Object.entries(valores)) {
    if (esLista(valor)) {
      for (const item of valor) {
        params = params.append(clave, String(item));
      }
      continue;
    }
    if (esVacio(valor)) {
      continue;
    }
    params = params.set(clave, String(valor));
  }
  return params;
}
