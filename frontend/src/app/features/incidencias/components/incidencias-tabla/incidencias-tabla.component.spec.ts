import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { IncidenciaResumenDto } from '../../models/incidencia.model';
import { IncidenciasTablaComponent } from './incidencias-tabla.component';

/** Fila del listado. */
function incidencia(id: number, extra: Partial<IncidenciaResumenDto> = {}): IncidenciaResumenDto {
  return {
    id,
    codigo: `PRJ-0001-${id}`,
    numero: id,
    titulo: `Incidencia ${id}`,
    tipo: { id: 2, nombre: 'Historia de Usuario', activo: true, nivelNumero: 2 },
    estado: { id: 1, nombre: 'Por hacer', activo: true, esFinal: false, categoria: null },
    prioridad: { id: 2, nombre: 'Media', orden: 20, activo: true },
    responsable: null,
    fechaVencimiento: null,
    vencida: false,
    estimacion: null,
    agrupacion: null,
    padre: null,
    cantidadHijas: 0,
    fechaCreacion: '2026-10-09T13:00:00Z',
    ...extra,
  };
}

/** Tests de IncidenciasTablaComponent modificado por RF-005 (CA-41, CA-44, CA-45, RN-36). */
describe('IncidenciasTablaComponent (RF-005)', () => {
  let fixture: ComponentFixture<IncidenciasTablaComponent>;

  /** Crea la tabla con las filas indicadas. */
  async function crear(filas: IncidenciaResumenDto[]): Promise<HTMLElement> {
    fixture = TestBed.createComponent(IncidenciasTablaComponent);
    fixture.componentRef.setInput('proyectoId', 1);
    fixture.componentRef.setInput('incidencias', filas);
    fixture.componentRef.setInput('ordenarPor', 'numero');
    fixture.componentRef.setInput('direccion', 'desc');
    await fixture.whenStable();
    return fixture.nativeElement as HTMLElement;
  }

  /** Índice de la columna con el título indicado. */
  function indiceColumna(raiz: HTMLElement, titulo: string): number {
    return Array.from(raiz.querySelectorAll('thead th')).findIndex((th) =>
      th.textContent?.includes(titulo),
    );
  }

  /** Celda de la fila y columna indicadas. */
  function celda(raiz: HTMLElement, fila: number, columna: number): HTMLElement {
    return raiz.querySelectorAll('tbody tr')[fila].querySelectorAll<HTMLElement>('td')[columna];
  }

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [IncidenciasTablaComponent],
      providers: [provideRouter([])],
    });
  });

  // CA-41: columna Estimación después de Responsable, formateada ("—" sin estimar)
  it('muestra la columna Estimación después de Responsable con el valor formateado', async () => {
    const raiz = await crear([incidencia(1, { estimacion: 2.5 }), incidencia(2), incidencia(3, { estimacion: 0 })]);
    const columna = indiceColumna(raiz, 'Estimación');

    expect(columna).toBe(indiceColumna(raiz, 'Responsable') + 1);
    expect([0, 1, 2].map((f) => celda(raiz, f, columna).textContent?.trim())).toEqual([
      '2,5 h',
      '—',
      '0 h',
    ]);
  });

  // CA-44: la columna Estimación es ordenable y emite "estimacion"
  it('clic en el encabezado Estimación emite ordenar("estimacion")', async () => {
    const raiz = await crear([incidencia(1)]);
    const emitidos: string[] = [];
    fixture.componentInstance.ordenar.subscribe((o) => emitidos.push(o));

    raiz.querySelectorAll<HTMLElement>('thead th')[indiceColumna(raiz, 'Estimación')]
      .querySelector('button')!
      .click();

    expect(emitidos).toEqual(['estimacion']);
  });

  // RN-36, CA-45: la alerta sigue a `vencida` de la API, no a la fecha del navegador
  it('aplica la alerta de vencida según el campo vencida y no según la fecha', async () => {
    const raiz = await crear([
      incidencia(1, { fechaVencimiento: '2000-01-01', vencida: false }),
      incidencia(2, { fechaVencimiento: '2999-12-31', vencida: true }),
    ]);
    const columna = indiceColumna(raiz, 'Vencimiento');

    expect(celda(raiz, 0, columna).classList.contains('texto-alerta')).toBe(false);
    expect(celda(raiz, 1, columna).classList.contains('texto-alerta')).toBe(true);
  });

  // RN-36: se eliminó el input `hoy` (la alerta no depende de la fecha del navegador)
  it('no expone el input hoy', async () => {
    await crear([incidencia(1)]);

    expect(Object.keys(fixture.componentInstance)).not.toContain('hoy');
  });
});
