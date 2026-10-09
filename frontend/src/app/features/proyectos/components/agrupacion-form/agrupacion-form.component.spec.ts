import { ComponentFixture, TestBed } from '@angular/core/testing';
import { AgrupacionRequest } from '../../models/agrupacion.model';
import { AgrupacionFormComponent } from './agrupacion-form.component';

/** Tests de AgrupacionFormComponent (P5, RN-26, CA-30). */
describe('AgrupacionFormComponent', () => {
  let fixture: ComponentFixture<AgrupacionFormComponent>;
  let raiz: HTMLElement;

  /** Escribe en un campo y dispara input/blur. */
  async function escribir(selector: string, valor: string): Promise<void> {
    const elemento = raiz.querySelector<HTMLInputElement | HTMLTextAreaElement>(selector);
    if (!elemento) {
      throw new Error(`No existe ${selector}`);
    }
    elemento.value = valor;
    elemento.dispatchEvent(new Event('input'));
    elemento.dispatchEvent(new Event('blur'));
    await fixture.whenStable();
  }

  beforeEach(async () => {
    fixture = TestBed.createComponent(AgrupacionFormComponent);
    await fixture.whenStable();
    raiz = fixture.nativeElement as HTMLElement;
  });

  // RN-26: nombre y descripción requeridos
  it('vacío deja el form inválido y Guardar deshabilitado', () => {
    expect(fixture.componentInstance.form.invalid).toBe(true);
    expect(raiz.querySelector<HTMLButtonElement>('button[type="submit"]')?.disabled).toBe(true);
  });

  // RN-26: nombre máximo 100
  it('nombre de 101 caracteres tiene error maxlength', async () => {
    await escribir('#agrupacion-nombre', 'a'.repeat(101));

    expect(fixture.componentInstance.form.controls.nombre.hasError('maxlength')).toBe(true);
  });

  // RN-26: nombre de 100 es válido (límite)
  it('nombre de 100 caracteres es válido', async () => {
    await escribir('#agrupacion-nombre', 'a'.repeat(100));

    expect(fixture.componentInstance.form.controls.nombre.valid).toBe(true);
  });

  // RN-26: descripción máximo 1000
  it('descripción de 1001 caracteres tiene error maxlength', async () => {
    await escribir('#agrupacion-descripcion', 'a'.repeat(1001));

    expect(fixture.componentInstance.form.controls.descripcion.hasError('maxlength')).toBe(true);
  });

  // RN-31: descripción solo espacios es requerida
  it('descripción de solo espacios es requerida', async () => {
    await escribir('#agrupacion-descripcion', '    ');

    expect(raiz.querySelector('#agrupacion-descripcion-error')?.textContent).toContain('obligatori');
  });

  // CA-30: el 409 duplicado se muestra en el campo Nombre
  it('error del servidor en nombre se muestra junto al campo Nombre', async () => {
    fixture.componentRef.setInput('errorServidor', {
      campos: { nombre: ['Ya existe una agrupación con ese nombre.'] },
      general: null,
    });
    await fixture.whenStable();

    expect(raiz.querySelector('#agrupacion-nombre-error')?.textContent).toContain(
      'Ya existe una agrupación con ese nombre.',
    );
  });

  // CA-29: Guardar emite nombre y descripción recortados
  it('válido emite el request con textos recortados', async () => {
    let emitido: AgrupacionRequest | null = null;
    fixture.componentInstance.guardar.subscribe((r) => (emitido = r));
    await escribir('#agrupacion-nombre', '  Módulo pagos ');
    await escribir('#agrupacion-descripcion', ' Incidencias de pagos. ');

    raiz.querySelector<HTMLButtonElement>('button[type="submit"]')?.click();

    expect(emitido).toEqual({ nombre: 'Módulo pagos', descripcion: 'Incidencias de pagos.' });
  });
});
