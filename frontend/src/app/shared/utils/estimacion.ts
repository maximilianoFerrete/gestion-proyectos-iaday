/**
 * Formato de la estimación de una incidencia en horas (RF-005 RN-34, "Pantallas").
 *
 * Creado: 2026-10-10
 * Modificado: 2026-10-10 - Creación inicial (RF-005)
 */

/** Texto para una incidencia sin estimación. */
export const SIN_ESTIMACION = '—';

/** Formateador `es-AR` con hasta 2 decimales (`12,5`, `9.999.999,99`). */
const FORMATO_HORAS = new Intl.NumberFormat('es-AR', {
  minimumFractionDigits: 0,
  maximumFractionDigits: 2,
});

/** Estimación en horas: 12.5 → "12,5 h"; 0 → "0 h"; null → "—". */
export function formatearEstimacion(valor: number | null | undefined): string {
  if (valor === null || valor === undefined || Number.isNaN(valor)) {
    return SIN_ESTIMACION;
  }
  return `${FORMATO_HORAS.format(valor)} h`;
}
