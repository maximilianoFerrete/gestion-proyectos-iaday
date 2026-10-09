import { FormControl } from '@angular/forms';
import { emailValido } from './email.validators';

/** Tests del validador de email alineado con la regex del backend (RN-32). */
describe('email.validators', () => {
  // RN-32: sin dominio con punto el backend responde 400
  it.each(['a@b', 'ana.gomez', 'ana@@empresa.com', 'ana gomez@empresa.com', 'ana@empresa .com'])(
    'emailValido con "%s" devuelve email',
    (valor) => {
      expect(emailValido(new FormControl(valor))).toEqual({ email: true });
    },
  );

  // RN-32: formatos aceptados por el backend
  it.each(['a@b.com', 'Ana.Gomez@Empresa.com', '  ana@empresa.com.ar  '])(
    'emailValido con "%s" devuelve null',
    (valor) => {
      expect(emailValido(new FormControl(valor))).toBeNull();
    },
  );

  // RN-31: vacío lo informa textoRequerido, no este validador
  it.each(['', '   '])('emailValido con "%s" devuelve null', (valor) => {
    expect(emailValido(new FormControl(valor))).toBeNull();
  });
});
