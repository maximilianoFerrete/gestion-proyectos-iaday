import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { IncidenciaDto, IncidenciaReferenciaDto } from '../../models/incidencia.model';
import { IncidenciaHijasComponent } from '../incidencia-hijas/incidencia-hijas.component';
import { IncidenciaDatosComponent } from './incidencia-datos.component';

/** Referencia (padre o hija). */
function referencia(id: number, vencida: boolean): IncidenciaReferenciaDto {
  return {
    id,
    codigo: `PRJ-0001-${id}`,
    titulo: `Incidencia ${id}`,
    tipo: { id: 3, nombre: 'Subtarea', activo: true, nivelNumero: 3 },
    estado: { id: 1, nombre: 'Por hacer', activo: true, esFinal: false, categoria: null },
    responsable: null,
    fechaVencimiento: '2026-10-09',
    vencida,
  };
}

/** Detalle de la HU 15 con los datos indicados. */
function detalle(extra: Partial<IncidenciaDto> = {}): IncidenciaDto {
  return {
    id: 15,
    codigo: 'PRJ-0001-15',
    numero: 15,
    proyecto: { id: 1, codigo: 'PRJ-0001', nombre: 'Portal', archivado: false },
    titulo: 'Pagar con tarjeta',
    descripcion: 'Como cliente quiero pagar.',
    tipo: { id: 2, nombre: 'Historia de Usuario', activo: true, nivelNumero: 2 },
    estado: { id: 2, nombre: 'En progreso', activo: true, esFinal: false, categoria: null },
    prioridad: { id: 2, nombre: 'Media', orden: 20, activo: true },
    responsable: null,
    fechaVencimiento: '2026-10-09',
    vencida: false,
    estimacion: null,
    agrupacion: null,
    agrupacionHeredada: false,
    padre: null,
    hijas: [],
    tiposHijaPermitidos: [],
    fechaCreacion: '2026-10-09T13:00:00Z',
    fechaModificacion: null,
    ...extra,
  };
}

/** Tests de IncidenciaDatosComponent / IncidenciaHijasComponent (I3, RF-005 CA-41, RN-36). */
describe('IncidenciaDatosComponent (RF-005)', () => {
  let fixture: ComponentFixture<IncidenciaDatosComponent>;

  /** Renderiza los datos de la incidencia indicada. */
  async function crear(incidencia: IncidenciaDto): Promise<HTMLElement> {
    fixture = TestBed.createComponent(IncidenciaDatosComponent);
    fixture.componentRef.setInput('incidencia', incidencia);
    await fixture.whenStable();
    return fixture.nativeElement as HTMLElement;
  }

  /** Valor (dd) del dato con el título (dt) indicado. */
  function dato(raiz: HTMLElement, titulo: string): HTMLElement {
    const dt = Array.from(raiz.querySelectorAll('dt')).find((d) => d.textContent?.trim() === titulo)!;
    return dt.nextElementSibling as HTMLElement;
  }

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [IncidenciaDatosComponent, IncidenciaHijasComponent],
      providers: [provideRouter([])],
    });
  });

  // CA-41: "Estimación: 2,5 h"
  it('muestra la estimación formateada en horas', async () => {
    const raiz = await crear(detalle({ estimacion: 2.5 }));

    expect(dato(raiz, 'Estimación').textContent?.trim()).toBe('2,5 h');
  });

  // CA-41: sin estimación → "Sin estimar"; 0 → "0 h"
  it('muestra "Sin estimar" sin estimación y "0 h" con 0', async () => {
    const sinEstimar = await crear(detalle({ estimacion: null }));
    expect(dato(sinEstimar, 'Estimación').textContent?.trim()).toBe('Sin estimar');

    fixture.componentRef.setInput('incidencia', detalle({ estimacion: 0 }));
    await fixture.whenStable();
    expect(dato(sinEstimar, 'Estimación').textContent?.trim()).toBe('0 h');
  });

  // RN-36, CA-45: la alerta de vencida sigue al campo vencida de la API
  it('aplica la alerta de vencida según el campo vencida', async () => {
    const noVencida = await crear(detalle({ vencida: false }));
    expect(dato(noVencida, 'Vencimiento').classList.contains('texto-alerta')).toBe(false);

    fixture.componentRef.setInput('incidencia', detalle({ vencida: true }));
    await fixture.whenStable();
    expect(dato(noVencida, 'Vencimiento').classList.contains('texto-alerta')).toBe(true);
    expect(dato(noVencida, 'Vencimiento').textContent).toContain('(vencida)');
  });

  // RN-36: el padre informa vencida desde la API
  it('marca el padre como vencido si viene con vencida = true', async () => {
    const raiz = await crear(detalle({ padre: referencia(3, true) }));

    expect(raiz.textContent).toContain('Vencida');
  });

  // RN-36: las hijas aplican la alerta según su campo vencida
  it('IncidenciaHijasComponent aplica la alerta solo a las hijas vencidas', async () => {
    const hijas = TestBed.createComponent(IncidenciaHijasComponent);
    hijas.componentRef.setInput('proyectoId', 1);
    hijas.componentRef.setInput('hijas', [referencia(16, true), referencia(17, false)]);
    await hijas.whenStable();

    const alertas = (hijas.nativeElement as HTMLElement).querySelectorAll('tbody td.texto-alerta');
    expect(alertas.length).toBe(1);
  });
});
