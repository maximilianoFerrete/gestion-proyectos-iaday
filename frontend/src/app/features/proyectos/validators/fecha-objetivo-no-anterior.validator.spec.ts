import { FormControl, FormGroup } from '@angular/forms';
import {
  ERROR_FECHA_OBJETIVO_ANTERIOR,
  fechaObjetivoNoAnterior,
} from './fecha-objetivo-no-anterior.validator';

/** Crea el grupo con las dos fechas y el validador de grupo. */
function grupo(inicio: string, objetivo: string): FormGroup {
  return new FormGroup(
    { fechaInicio: new FormControl(inicio), fechaObjetivo: new FormControl(objetivo) },
    { validators: [fechaObjetivoNoAnterior()] },
  );
}

/** Tests del validador de grupo fechaObjetivoNoAnterior (RN-16, CA-17). */
describe('fechaObjetivoNoAnterior', () => {
  // RN-16, CA-17: objetivo < inicio → error
  it('devuelve error si la fecha objetivo es anterior a la de inicio', () => {
    expect(grupo('2026-10-10', '2026-10-09').hasError(ERROR_FECHA_OBJETIVO_ANTERIOR)).toBe(true);
  });

  // RN-16, CA-17: igualdad permitida
  it('es válido si las fechas son iguales', () => {
    expect(grupo('2026-10-10', '2026-10-10').errors).toBeNull();
  });

  // RN-16: objetivo posterior válido (incluye cambio de año)
  it('es válido si la fecha objetivo es posterior', () => {
    expect(grupo('2026-12-31', '2027-01-01').errors).toBeNull();
  });

  // Si falta alguna fecha lo informa "required", no este validador
  it.each([
    ['', '2026-10-09'],
    ['2026-10-10', ''],
  ])('no informa error si falta alguna fecha (%s, %s)', (inicio, objetivo) => {
    expect(grupo(inicio, objetivo).errors).toBeNull();
  });
});
