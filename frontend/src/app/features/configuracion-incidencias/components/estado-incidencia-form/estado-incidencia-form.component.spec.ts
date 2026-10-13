import { ComponentFixture, TestBed } from '@angular/core/testing';
import {
  EstadoIncidenciaDto,
  EstadoIncidenciaRequest,
} from '../../models/estado-incidencia.model';
import {
  AYUDA_CATEGORIA_ESTADO,
  EstadoIncidenciaFormComponent,
} from './estado-incidencia-form.component';

/** Estado con sus marcas en el Flujo general. */
function estado(esInicial: boolean, esFinal: boolean): EstadoIncidenciaDto {
  return {
    id: 2,
    nombre: 'En progreso',
    descripcion: null,
    orden: 20,
    activo: true,
    cantidadIncidencias: 0,
    flujos: [{ id: 1, nombre: 'Flujo general', esInicial, esFinal }],
    categoria: null,
  };
}

/** Tests de EstadoIncidenciaFormComponent: categoría de seguimiento (C3, RF-005 RN-17, RN-18, CA-39). */
describe('EstadoIncidenciaFormComponent', () => {
  let fixture: ComponentFixture<EstadoIncidenciaFormComponent>;

  /** Crea el formulario para el estado indicado (null = alta). */
  async function crear(dto: EstadoIncidenciaDto | null): Promise<HTMLElement> {
    fixture = TestBed.createComponent(EstadoIncidenciaFormComponent);
    fixture.componentRef.setInput('estado', dto);
    fixture.componentRef.setInput('ordenSugerido', 40);
    await fixture.whenStable();
    return fixture.nativeElement as HTMLElement;
  }

  /** Select de categoría. */
  function select(raiz: HTMLElement): HTMLSelectElement {
    return raiz.querySelector<HTMLSelectElement>('#estado-incidencia-categoria')!;
  }

  beforeEach(() => {
    TestBed.configureTestingModule({ imports: [EstadoIncidenciaFormComponent] });
  });

  // RN-17: opciones Ninguna / Bloqueada / En espera y ayuda
  it('ofrece Ninguna, Bloqueada y En espera con la ayuda de la spec', async () => {
    const raiz = await crear(null);

    expect(Array.from(select(raiz).options).map((o) => o.textContent?.trim())).toEqual([
      'Ninguna',
      'Bloqueada',
      'En espera',
    ]);
    expect(raiz.textContent).toContain(AYUDA_CATEGORIA_ESTADO);
  });

  // RN-18, CA-39: estado final en algún flujo → select deshabilitado
  it('deshabilita la categoría si el estado es final en algún flujo', async () => {
    const raiz = await crear(estado(false, true));

    expect(select(raiz).disabled).toBe(true);
    expect(raiz.textContent).toContain('Este estado es inicial o final en algún flujo.');
  });

  // RN-18, CA-39: estado inicial en algún flujo → select deshabilitado
  it('deshabilita la categoría si el estado es inicial en algún flujo', async () => {
    const raiz = await crear(estado(true, false));

    expect(select(raiz).disabled).toBe(true);
  });

  // RN-17: estado intermedio → select habilitado
  it('habilita la categoría si el estado no es inicial ni final', async () => {
    const raiz = await crear(estado(false, false));

    expect(select(raiz).disabled).toBe(false);
  });

  // RN-17, CA-38: guardar emite la categoría elegida
  it('enviar emite la categoría elegida', async () => {
    await crear(estado(false, false));
    let emitido: EstadoIncidenciaRequest | undefined;
    fixture.componentInstance.guardar.subscribe((r) => (emitido = r));
    fixture.componentInstance.form.controls.categoria.setValue('EN_ESPERA');

    fixture.componentInstance.enviar();

    expect(emitido).toEqual({ nombre: 'En progreso', descripcion: null, orden: 20, categoria: 'EN_ESPERA' });
  });

  // CA-39: 409 ESTADO_CATEGORIA_INCOMPATIBLE se muestra arriba del formulario
  it('muestra el error general del servidor arriba del formulario', async () => {
    const raiz = await crear(estado(false, false));

    fixture.componentRef.setInput('errorServidor', {
      campos: {},
      general: "El estado 'En progreso' es inicial o final en los flujos: Flujo general.",
    });
    await fixture.whenStable();

    expect(raiz.querySelector('[role="alert"]')?.textContent).toContain('es inicial o final');
  });
});
