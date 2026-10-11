import { FormControl } from '@angular/forms';
import {
  ERROR_VENCIMIENTO_POSTERIOR_AL_PADRE,
  mensajeVencimientoPosteriorAlPadre,
  vencimientoNoPosteriorAlPadre,
} from './vencimiento-no-posterior-al-padre.validator';

/** Valida `valor` contra el vencimiento del padre indicado. */
function validar(
  valor: string | null,
  padre: string | null,
): ReturnType<ReturnType<typeof vencimientoNoPosteriorAlPadre>> {
  return vencimientoNoPosteriorAlPadre(() => padre)(new FormControl(valor));
}

/** Tests del validador vencimientoNoPosteriorAlPadre (I4/I5, RN-31, CA-31). */
describe('vencimientoNoPosteriorAlPadre', () => {
  // RN-31, CA-31: hija posterior al padre → error con la fecha del padre
  it('hija 01/12 con padre 30/11 devuelve el error con la fecha del padre', () => {
    expect(validar('2026-12-01', '2026-11-30')).toEqual({
      [ERROR_VENCIMIENTO_POSTERIOR_AL_PADRE]: { fechaPadre: '2026-11-30' },
    });
  });

  // RN-31, CA-31: igual al padre → válido
  it('hija igual al padre es válida', () => {
    expect(validar('2026-11-30', '2026-11-30')).toBeNull();
  });

  // RN-31: anterior al padre → válido
  it('hija anterior al padre es válida', () => {
    expect(validar('2026-11-20', '2026-11-30')).toBeNull();
  });

  // RN-31, DA-05: hija sin vencimiento → válido
  it('hija sin vencimiento es válida', () => {
    expect(validar(null, '2026-11-30')).toBeNull();
    expect(validar('', '2026-11-30')).toBeNull();
  });

  // RN-31: padre sin vencimiento (o sin padre) → válido
  it('padre sin vencimiento es válido', () => {
    expect(validar('2026-12-10', null)).toBeNull();
  });

  // RN-31: el padre puede cambiar después de crear el validador
  it('usa el vencimiento vigente del padre en cada validación', () => {
    let padre: string | null = '2026-12-31';
    const control = new FormControl(
      '2026-12-10',
      vencimientoNoPosteriorAlPadre(() => padre),
    );
    expect(control.valid).toBe(true);

    padre = '2026-11-30';
    control.updateValueAndValidity();

    expect(control.hasError(ERROR_VENCIMIENTO_POSTERIOR_AL_PADRE)).toBe(true);
  });

  // I4: mensaje con la fecha en dd/MM/yyyy
  it('mensajeVencimientoPosteriorAlPadre muestra la fecha del padre en dd/MM/yyyy', () => {
    expect(mensajeVencimientoPosteriorAlPadre('2026-11-30')).toBe(
      'El vencimiento no puede ser posterior al del padre (30/11/2026).',
    );
  });
});
