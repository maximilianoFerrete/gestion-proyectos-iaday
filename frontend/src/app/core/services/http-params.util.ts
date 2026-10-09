import { HttpParams } from '@angular/common/http';

/** Valor admitido como query param. */
export type ValorParametro = string | number | boolean | null | undefined;

/** Arma HttpParams omitiendo valores null, undefined o strings vacíos. */
export function crearParams(valores: Readonly<Record<string, ValorParametro>>): HttpParams {
  let params = new HttpParams();
  for (const [clave, valor] of Object.entries(valores)) {
    if (valor === null || valor === undefined || valor === '') {
      continue;
    }
    params = params.set(clave, String(valor));
  }
  return params;
}
