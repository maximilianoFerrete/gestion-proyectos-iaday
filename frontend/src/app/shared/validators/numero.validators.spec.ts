import { FormControl } from '@angular/forms';
import { maxDecimales } from './numero.validators';

/** Tests del validador maxDecimales (RF-005 RN-34, CA-42). */
describe('maxDecimales', () => {
  const validador = maxDecimales(2);

  // RN-34: hasta 2 decimales es válido
  it.each([1.23, 0, 0.25, 2.5, 9999999.99])('acepta %s', (valor) => {
    expect(validador(new FormControl(valor))).toBeNull();
  });

  // CA-42: 1.234 tiene 3 decimales → error maxDecimales
  it('rechaza 1.234 con el error maxDecimales', () => {
    expect(validador(new FormControl(1.234))).toEqual({ maxDecimales: { decimales: 2 } });
  });

  // RN-34: vacío es válido (lo maneja required si corresponde)
  it.each([null, ''])('con %j no informa error', (valor) => {
    expect(validador(new FormControl(valor))).toBeNull();
  });
});
