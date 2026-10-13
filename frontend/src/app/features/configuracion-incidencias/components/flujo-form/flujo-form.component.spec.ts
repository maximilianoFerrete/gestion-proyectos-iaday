import { ComponentFixture, TestBed } from '@angular/core/testing';
import {
  CategoriaEstadoIncidencia,
  EstadoIncidenciaDto,
} from '../../models/estado-incidencia.model';
import { FlujoIncidenciaDto } from '../../models/flujo-incidencia.model';
import { FlujoFormComponent, ayudaEstadoConCategoria } from './flujo-form.component';

/** Estado del catálogo. */
function estado(id: number, nombre: string, categoria: CategoriaEstadoIncidencia | null = null): EstadoIncidenciaDto {
  return {
    id,
    nombre,
    descripcion: null,
    orden: id * 10,
    activo: true,
    cantidadIncidencias: 0,
    flujos: [],
    categoria,
  };
}

const ESTADOS = [
  estado(1, 'Por hacer'),
  estado(2, 'En progreso'),
  estado(4, 'Bloqueada', 'BLOQUEADA'),
  estado(3, 'Hecho'),
];

/** Flujo general con Bloqueada en posición intermedia. */
const GENERAL: FlujoIncidenciaDto = {
  id: 1,
  nombre: 'Flujo general',
  descripcion: null,
  estados: [
    { estadoId: 1, nombre: 'Por hacer', activo: true, orden: 1, esInicial: true, esFinal: false },
    { estadoId: 2, nombre: 'En progreso', activo: true, orden: 2, esInicial: false, esFinal: false },
    { estadoId: 4, nombre: 'Bloqueada', activo: true, orden: 3, esInicial: false, esFinal: false },
    { estadoId: 3, nombre: 'Hecho', activo: true, orden: 4, esInicial: false, esFinal: true },
  ],
  tipos: [],
  fechaCreacion: '2026-10-09T13:00:00Z',
  fechaModificacion: null,
};

/** Tests de FlujoFormComponent: estados con categoría (C4, RF-005 RN-18, CA-39). */
describe('FlujoFormComponent (RF-005)', () => {
  let fixture: ComponentFixture<FlujoFormComponent>;

  /** Crea el formulario de edición del Flujo general. */
  async function crear(): Promise<HTMLElement> {
    fixture = TestBed.createComponent(FlujoFormComponent);
    fixture.componentRef.setInput('flujo', GENERAL);
    fixture.componentRef.setInput('estados', ESTADOS);
    await fixture.whenStable();
    return fixture.nativeElement as HTMLElement;
  }

  /** Fila del estado con el nombre indicado. */
  function fila(raiz: HTMLElement, nombre: string): HTMLElement {
    return Array.from(raiz.querySelectorAll<HTMLElement>('.lista-estados-flujo li')).find((li) =>
      li.querySelector('.estado-flujo-nombre')?.textContent?.includes(nombre),
    )!;
  }

  beforeEach(() => {
    TestBed.configureTestingModule({ imports: [FlujoFormComponent] });
  });

  // RN-18, CA-39: radio "Inicial" y check "Final" deshabilitados en el estado con categoría
  it('deshabilita Inicial y Final en el estado con categoría', async () => {
    const raiz = await crear();

    const bloqueada = fila(raiz, 'Bloqueada');
    expect(bloqueada.querySelector<HTMLInputElement>('input[type="radio"]')!.disabled).toBe(true);
    expect(bloqueada.querySelector<HTMLInputElement>('input[type="checkbox"]')!.disabled).toBe(true);
  });

  // "Cambios en C4": ayuda en el estado con categoría
  it('muestra la ayuda del estado con categoría', async () => {
    const raiz = await crear();

    expect(fila(raiz, 'Bloqueada').textContent).toContain(ayudaEstadoConCategoria('BLOQUEADA'));
    expect(ayudaEstadoConCategoria('BLOQUEADA')).toBe(
      'Estado de categoría Bloqueada: no puede ser inicial ni final',
    );
  });

  // RN-18: un estado sin categoría conserva Inicial y Final habilitados
  it('mantiene Inicial y Final habilitados en un estado sin categoría', async () => {
    const raiz = await crear();

    const enProgreso = fila(raiz, 'En progreso');
    expect(enProgreso.querySelector<HTMLInputElement>('input[type="radio"]')!.disabled).toBe(false);
    expect(enProgreso.querySelector<HTMLInputElement>('input[type="checkbox"]')!.disabled).toBe(false);
  });

  // RN-18: marcarInicial sobre un estado con categoría no cambia el inicial
  it('marcarInicial sobre el estado con categoría no lo marca como inicial', async () => {
    await crear();
    const componente = fixture.componentInstance;

    componente.marcarInicial(2);

    const iniciales = componente.form.controls.estados.controls.map((g) => g.controls.esInicial.value);
    expect(iniciales).toEqual([true, false, false, false]);
  });
});
