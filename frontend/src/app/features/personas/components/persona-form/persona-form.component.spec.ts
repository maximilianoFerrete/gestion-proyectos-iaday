import { ComponentFixture, TestBed } from '@angular/core/testing';
import { PersonaRequest } from '../../models/persona.model';
import { PersonaFormComponent } from './persona-form.component';

/** Tests de PersonaFormComponent (P6, RN-01, RN-02, RN-05, CA-02, CA-03). */
describe('PersonaFormComponent', () => {
  let fixture: ComponentFixture<PersonaFormComponent>;
  let raiz: HTMLElement;

  /** Crea el componente indicando si el usuario actual es admin. */
  async function crear(puedeGestionarAdmin: boolean): Promise<void> {
    fixture = TestBed.createComponent(PersonaFormComponent);
    fixture.componentRef.setInput('puedeGestionarAdmin', puedeGestionarAdmin);
    await fixture.whenStable();
    raiz = fixture.nativeElement as HTMLElement;
  }

  /** Escribe en un campo y dispara input/blur. */
  async function escribir(selector: string, valor: string): Promise<void> {
    const elemento = raiz.querySelector<HTMLInputElement>(selector);
    if (!elemento) {
      throw new Error(`No existe ${selector}`);
    }
    elemento.value = valor;
    elemento.dispatchEvent(new Event('input'));
    elemento.dispatchEvent(new Event('blur'));
    await fixture.whenStable();
  }

  // RN-05, CA-03: usuario no admin → checkbox deshabilitado con ayuda
  it('usuario no admin ve el checkbox Administrador deshabilitado con la ayuda', async () => {
    await crear(false);

    expect(raiz.querySelector<HTMLInputElement>('#persona-admin')?.disabled).toBe(true);
    expect(raiz.textContent).toContain('Solo un administrador puede cambiar este valor');
  });

  // RN-05: usuario admin → checkbox habilitado
  it('usuario admin ve el checkbox Administrador habilitado', async () => {
    await crear(true);

    expect(raiz.querySelector<HTMLInputElement>('#persona-admin')?.disabled).toBe(false);
  });

  // RN-01: nombre de 151 → maxlength
  it('nombre de 151 caracteres tiene error maxlength', async () => {
    await crear(true);

    await escribir('#persona-nombre', 'a'.repeat(151));

    expect(fixture.componentInstance.form.controls.nombre.hasError('maxlength')).toBe(true);
  });

  // RN-02: email sin formato → error email
  it('email sin @ tiene error de formato', async () => {
    await crear(true);

    await escribir('#persona-email', 'ana.gomez');

    expect(fixture.componentInstance.form.controls.email.hasError('email')).toBe(true);
  });

  // RN-32: "a@b" lo rechaza el backend → el form no permite enviarlo y muestra el mensaje
  it('email "a@b" es inválido y muestra el mensaje de formato', async () => {
    await crear(true);
    await escribir('#persona-nombre', 'Ana');

    await escribir('#persona-email', 'a@b');

    expect(fixture.componentInstance.form.controls.email.hasError('email')).toBe(true);
    expect(fixture.componentInstance.form.invalid).toBe(true);
    expect(raiz.querySelector('#persona-email-error')?.textContent).toContain(
      'El email no tiene un formato válido.',
    );
  });

  // RN-32: email con espacios internos → error de formato
  it('email con espacios internos es inválido', async () => {
    await crear(true);

    await escribir('#persona-email', 'ana gomez@empresa.com');

    expect(fixture.componentInstance.form.controls.email.hasError('email')).toBe(true);
  });

  // RN-32: "a@b.com" cumple la regex del backend
  it('email "a@b.com" es válido', async () => {
    await crear(true);

    await escribir('#persona-email', 'a@b.com');

    expect(fixture.componentInstance.form.controls.email.valid).toBe(true);
  });

  // RN-02: email de 255 caracteres → maxlength (254)
  it('email de 255 caracteres tiene error maxlength', async () => {
    await crear(true);

    await escribir('#persona-email', `${'a'.repeat(243)}@empresa.com`);

    expect(fixture.componentInstance.form.controls.email.hasError('maxlength')).toBe(true);
  });

  // CA-02: el 409 de email duplicado se muestra en el campo Email
  it('error del servidor en email se muestra junto al campo Email', async () => {
    await crear(true);

    fixture.componentRef.setInput('errorServidor', {
      campos: { email: ['Ya existe una persona con el email ana.gomez@empresa.com.'] },
      general: null,
    });
    await fixture.whenStable();

    expect(raiz.querySelector('#persona-email-error')?.textContent).toContain('Ya existe una persona');
  });

  // CA-01: emite nombre y email recortados (el backend pasa a minúsculas)
  it('válido emite el request con textos recortados', async () => {
    await crear(true);
    let emitido: PersonaRequest | null = null;
    fixture.componentInstance.guardar.subscribe((r) => (emitido = r));
    await escribir('#persona-nombre', ' Ana Gómez ');
    await escribir('#persona-email', ' Ana.Gomez@Empresa.com ');

    raiz.querySelector<HTMLButtonElement>('button[type="submit"]')?.click();

    expect(emitido).toEqual({ nombre: 'Ana Gómez', email: 'Ana.Gomez@Empresa.com', esAdmin: false });
  });
});
