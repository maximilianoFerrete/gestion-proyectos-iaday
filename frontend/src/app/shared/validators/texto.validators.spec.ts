import { FormControl } from '@angular/forms';
import { longitudMaxima, recortarOpcional, textoRequerido } from './texto.validators';
import { entero } from './numero.validators';

/** Tests de validadores de texto y número (RN-31, RN-32). */
describe('texto.validators', () => {
  // RN-31: solo espacios equivale a vacío
  it.each(['', '   ', '\t'])('textoRequerido con "%s" devuelve required', (valor) => {
    expect(textoRequerido(new FormControl(valor))).toEqual({ required: true });
  });

  it('textoRequerido con texto devuelve null', () => {
    expect(textoRequerido(new FormControl(' Ana '))).toBeNull();
  });

  // RN-32: mismo límite que el backend, medido tras trim
  it('longitudMaxima(150) con 151 caracteres devuelve maxlength', () => {
    expect(longitudMaxima(150)(new FormControl('a'.repeat(151)))).toEqual({
      maxlength: { requiredLength: 150, actualLength: 151 },
    });
  });

  // RN-32: límite exacto válido y espacios externos no cuentan
  it('longitudMaxima(150) con 150 caracteres más espacios es válido', () => {
    expect(longitudMaxima(150)(new FormControl(`  ${'a'.repeat(150)}  `))).toBeNull();
  });

  // Spec 0.2: opcional vacío → null
  it('recortarOpcional con solo espacios devuelve null', () => {
    expect(recortarOpcional('   ')).toBeNull();
  });

  // P7: orden entero
  it.each([
    [10, null],
    [10.5, { entero: true }],
    [null, null],
  ])('entero(%s) devuelve %s', (valor, esperado) => {
    expect(entero(new FormControl(valor))).toEqual(esperado);
  });
});
