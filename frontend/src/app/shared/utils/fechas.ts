/**
 * Utilidades puras para fechas sin hora (`yyyy-MM-dd`, contrato API 0.2).
 *
 * Creado: 2026-10-09
 * Modificado: 2026-10-09 - Creación inicial (RF-002)
 */

/** Patrón de una fecha sin hora `yyyy-MM-dd`. */
const PATRON_FECHA = /^(\d{4})-(\d{2})-(\d{2})$/;

/** Indica si el valor es una fecha `yyyy-MM-dd`. */
export function esFechaIso(valor: unknown): valor is string {
  return typeof valor === 'string' && PATRON_FECHA.test(valor);
}

/** Formatea `yyyy-MM-dd` como `dd/MM/yyyy`; devuelve el texto original si no tiene ese formato. */
export function formatearFecha(fecha: string): string {
  const partes = PATRON_FECHA.exec(fecha);
  return partes ? `${partes[3]}/${partes[2]}/${partes[1]}` : fecha;
}

/** Fecha local de hoy como `yyyy-MM-dd`. */
export function fechaHoyIso(ahora: Date = new Date()): string {
  const mes = String(ahora.getMonth() + 1).padStart(2, '0');
  const dia = String(ahora.getDate()).padStart(2, '0');
  return `${ahora.getFullYear()}-${mes}-${dia}`;
}
