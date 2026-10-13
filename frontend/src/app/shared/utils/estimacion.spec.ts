import { formatearEstimacion } from './estimacion';

/** Tests de formatearEstimacion (RF-005 RN-34, CA-41). */
describe('formatearEstimacion', () => {
  // CA-41: número es-AR con hasta 2 decimales seguido de " h"
  it.each([
    [12.5, '12,5 h'],
    [0, '0 h'],
    [8, '8 h'],
    [2.25, '2,25 h'],
    [9999999.99, '9.999.999,99 h'],
  ])('formatea %s como "%s"', (valor, esperado) => {
    expect(formatearEstimacion(valor)).toBe(esperado);
  });

  // CA-41: sin estimación → "—"
  it.each([null, undefined])('con %s devuelve "—"', (valor) => {
    expect(formatearEstimacion(valor)).toBe('—');
  });
});
