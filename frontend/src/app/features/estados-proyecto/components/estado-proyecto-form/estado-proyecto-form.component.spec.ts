import { ComponentFixture, TestBed } from '@angular/core/testing';
import { EstadoProyectoDto } from '../../models/estado-proyecto.model';
import { EstadoProyectoFormComponent } from './estado-proyecto-form.component';

const INICIAL: EstadoProyectoDto = {
  id: 1,
  nombre: 'En planificación',
  descripcion: null,
  orden: 10,
  esInicial: true,
  esFinal: false,
  activo: true,
  esSistema: false,
  codigo: null,
  cantidadProyectos: 4,
};

/** Tests de EstadoProyectoFormComponent (P7, RN-07, RN-12). */
describe('EstadoProyectoFormComponent', () => {
  let fixture: ComponentFixture<EstadoProyectoFormComponent>;

  /** Crea el componente con el estado y el orden sugerido indicados. */
  async function crear(estado: EstadoProyectoDto | null, ordenSugerido: number | null = null): Promise<HTMLElement> {
    fixture = TestBed.createComponent(EstadoProyectoFormComponent);
    fixture.componentRef.setInput('estado', estado);
    fixture.componentRef.setInput('ordenSugerido', ordenSugerido);
    await fixture.whenStable();
    return fixture.nativeElement as HTMLElement;
  }

  // RN-12: el estado inicial no puede ser final → checkbox deshabilitado con ayuda
  it('estado inicial deshabilita "Es final" y muestra la ayuda', async () => {
    const raiz = await crear(INICIAL);

    expect(raiz.querySelector<HTMLInputElement>('#estado-final')?.disabled).toBe(true);
    expect(raiz.textContent).toContain('El estado inicial no puede ser final');
  });

  // RN-12: un estado no inicial permite marcar final
  it('estado no inicial deja "Es final" habilitado', async () => {
    const raiz = await crear({ ...INICIAL, id: 3, esInicial: false });

    expect(raiz.querySelector<HTMLInputElement>('#estado-final')?.disabled).toBe(false);
  });

  // P7: en alta se precarga el orden sugerido
  it('alta precarga el orden sugerido', async () => {
    await crear(null, 70);

    expect(fixture.componentInstance.form.controls.orden.value).toBe(70);
  });

  // RN-07: orden fuera de 1–9998
  it.each([0, 9999, 10.5])('orden %s es inválido', async (orden) => {
    await crear(null);

    fixture.componentInstance.form.controls.orden.setValue(orden);

    expect(fixture.componentInstance.form.controls.orden.invalid).toBe(true);
  });

  // RN-07: nombre máximo 50
  it('nombre de 51 caracteres tiene error maxlength', async () => {
    await crear(null, 10);

    fixture.componentInstance.form.controls.nombre.setValue('a'.repeat(51));

    expect(fixture.componentInstance.form.controls.nombre.hasError('maxlength')).toBe(true);
  });
});
