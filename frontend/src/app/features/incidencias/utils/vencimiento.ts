import { esFechaIso } from '../../../shared/utils/fechas';

/**
 * Reglas puras de vencimiento de incidencias (I1, RN-31, DA-09).
 *
 * Creado: 2026-10-09
 * Modificado: 2026-10-09 - Creación inicial (RF-002)
 */

/** Vencida: tiene fecha anterior a hoy y el estado no es final (estilo de alerta en I1/I2/I3). */
export function estaVencida(
  fechaVencimiento: string | null,
  esFinal: boolean,
  hoy: string,
): boolean {
  return !esFinal && esFechaIso(fechaVencimiento) && fechaVencimiento < hoy;
}

/**
 * RN-31: la hija no puede vencer después que el padre. Igualdad válida; si alguna fecha falta,
 * no hay error. El formato `yyyy-MM-dd` permite comparar como texto.
 */
export function esVencimientoPosteriorAlPadre(
  vencimientoHija: string | null,
  vencimientoPadre: string | null,
): boolean {
  return (
    esFechaIso(vencimientoHija) &&
    esFechaIso(vencimientoPadre) &&
    vencimientoHija > vencimientoPadre
  );
}
