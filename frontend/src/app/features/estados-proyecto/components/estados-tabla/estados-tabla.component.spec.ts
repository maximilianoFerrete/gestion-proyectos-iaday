import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { EstadoProyectoDto } from '../../models/estado-proyecto.model';
import { EstadosTablaComponent } from './estados-tabla.component';

/** Crea un estado de prueba. */
function estado(id: number, nombre: string, extra: Partial<EstadoProyectoDto> = {}): EstadoProyectoDto {
  return {
    id,
    nombre,
    descripcion: null,
    orden: id * 10,
    esInicial: false,
    esFinal: false,
    activo: true,
    esSistema: false,
    codigo: null,
    cantidadProyectos: 0,
    ...extra,
  };
}

const ESTADOS = [
  estado(1, 'En planificación', { esInicial: true }),
  estado(3, 'En curso'),
  estado(4, 'Pausado', { activo: false }),
  estado(5, 'Finalizado', { esFinal: true }),
  estado(7, 'Archivado', { esSistema: true, codigo: 'ARCHIVADO', orden: 9999 }),
];

/** Tests de la tabla de estados de EstadosProyectoListadoPage (P7, CA-11, CA-12). */
describe('EstadosTablaComponent', () => {
  let fixture: ComponentFixture<EstadosTablaComponent>;
  let raiz: HTMLElement;

  /** Indica si existe la acción con el aria-label indicado. */
  function existe(etiqueta: string): boolean {
    return raiz.querySelector(`[aria-label="${etiqueta}"]`) !== null;
  }

  beforeEach(async () => {
    TestBed.configureTestingModule({ providers: [provideRouter([])] });
    fixture = TestBed.createComponent(EstadosTablaComponent);
    fixture.componentRef.setInput('estados', ESTADOS);
    await fixture.whenStable();
    raiz = fixture.nativeElement as HTMLElement;
  });

  // RN-12, CA-12: sin Desactivar en el inicial
  it('no ofrece Desactivar en el estado inicial', () => {
    expect(existe('Desactivar estado En planificación')).toBe(false);
  });

  // RN-09, CA-14: sin Desactivar ni Editar en el de sistema
  it('no ofrece Desactivar ni Editar en el estado de sistema', () => {
    expect(existe('Desactivar estado Archivado')).toBe(false);
    expect(existe('Editar estado Archivado')).toBe(false);
  });

  // RN-11, CA-11: sin "Marcar como inicial" en finales, inactivos, sistema e inicial
  it.each(['Finalizado', 'Pausado', 'Archivado', 'En planificación'])(
    'no ofrece "Marcar como inicial" en %s',
    (nombre) => {
      expect(existe(`Marcar como inicial el estado ${nombre}`)).toBe(false);
    },
  );

  // RN-10: un estado activo, no final ni sistema ofrece las acciones
  it('ofrece Marcar como inicial y Desactivar en un estado activo común', () => {
    expect(existe('Marcar como inicial el estado En curso')).toBe(true);
    expect(existe('Desactivar estado En curso')).toBe(true);
  });

  // RN-08: Activar en inactivos no de sistema
  it('ofrece Activar solo en el estado inactivo', () => {
    expect(existe('Activar estado Pausado')).toBe(true);
    expect(existe('Activar estado En curso')).toBe(false);
  });

  // CA-10: el clic emite el estado elegido
  it('el clic en Marcar como inicial emite el estado', () => {
    let emitido: EstadoProyectoDto | null = null;
    fixture.componentInstance.marcarInicial.subscribe((e) => (emitido = e));

    raiz.querySelector<HTMLButtonElement>('[aria-label="Marcar como inicial el estado En curso"]')?.click();

    expect(emitido).toEqual(ESTADOS[1]);
  });
});
