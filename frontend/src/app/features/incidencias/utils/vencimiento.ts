import { esFechaIso } from '../../../shared/utils/fechas';

/**
 * Reglas puras de vencimiento de incidencias (RN-31). "Vencida" ya no se calcula en el navegador:
 * la API devuelve `vencida` con la fecha de referencia del servidor (RF-005 RN-36).
 *
 * Creado: 2026-10-09
 * Modificado: 2026-10-10 - Se elimina `estaVencida` (vencida desde el servidor, RF-005)
 */

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
