import { ComponentFixture, TestBed } from '@angular/core/testing';
import { EstadoIncidenciaDto } from '../../../configuracion-incidencias/models/estado-incidencia.model';
import { FiltrosTablero, SIN_FILTROS_TABLERO } from '../../models/seguimiento.model';
import { DEBOUNCE_TEXTO_TABLERO_MS, FiltrosTableroComponent } from './filtros-tablero.component';

/** Estado del catálogo. */
function estado(id: number, nombre: string, orden: number, activo = true): EstadoIncidenciaDto {
  return {
    id,
    nombre,
    descripcion: null,
    orden,
    activo,
    cantidadIncidencias: 0,
    flujos: [],
    categoria: id === 4 ? 'BLOQUEADA' : null,
  };
}

const ESTADOS = [
  estado(3, 'Hecho', 30),
  estado(1, 'Por hacer', 10),
  estado(4, 'Bloqueada', 24),
  estado(9, 'Viejo', 5, false),
];

/** Tests de FiltrosTableroComponent (RF-005 RN-21, CA-24, CA-48). */
describe('FiltrosTableroComponent', () => {
  let fixture: ComponentFixture<FiltrosTableroComponent>;
  let emitidos: FiltrosTablero[];

  /** Crea el componente con los filtros vigentes indicados. */
  async function crear(filtros: FiltrosTablero = SIN_FILTROS_TABLERO): Promise<HTMLElement> {
    fixture = TestBed.createComponent(FiltrosTableroComponent);
    fixture.componentRef.setInput('filtros', filtros);
    fixture.componentRef.setInput('estados', ESTADOS);
    emitidos = [];
    fixture.componentInstance.cambio.subscribe((f) => emitidos.push(f));
    await fixture.whenStable();
    return fixture.nativeElement as HTMLElement;
  }

  /** Elige una opción de un select por su texto y dispara change. */
  function elegir(raiz: HTMLElement, id: string, texto: string): void {
    const select = raiz.querySelector<HTMLSelectElement>(`#${id}`)!;
    const indice = Array.from(select.options).findIndex((o) => o.textContent?.trim() === texto);
    select.selectedIndex = indice;
    select.dispatchEvent(new Event('change'));
  }

  /** Botón "Limpiar filtros". */
  function limpiar(raiz: HTMLElement): HTMLButtonElement {
    return Array.from(raiz.querySelectorAll<HTMLButtonElement>('button')).find(
      (b) => b.textContent?.trim() === 'Limpiar filtros',
    )!;
  }

  /** Textos de los chips activos. */
  function chips(raiz: HTMLElement): string[] {
    return Array.from(raiz.querySelectorAll('.chip > span')).map((s) => s.textContent?.trim() ?? '');
  }

  beforeEach(() => {
    TestBed.configureTestingModule({ imports: [FiltrosTableroComponent] });
  });

  afterEach(() => vi.useRealTimers());

  // RN-21: texto con debounce de 300 ms; recién entonces emite el texto recortado
  it('emite el texto recortado recién después del debounce de 300 ms', async () => {
    const raiz = await crear();
    vi.useFakeTimers();
    const input = raiz.querySelector<HTMLInputElement>('#tablero-filtro-texto')!;

    input.value = '  pago ';
    input.dispatchEvent(new Event('input'));
    vi.advanceTimersByTime(DEBOUNCE_TEXTO_TABLERO_MS - 1);
    expect(emitidos).toEqual([]);

    vi.advanceTimersByTime(1);
    expect(emitidos).toEqual([{ indicador: null, estadoId: null, texto: 'pago' }]);
  });

  // RN-21: el input de texto tiene maxlength 200
  it('el input de texto tiene maxlength 200', async () => {
    const raiz = await crear();

    expect(raiz.querySelector('#tablero-filtro-texto')?.getAttribute('maxlength')).toBe('200');
  });

  // RN-21: el select de estado emite el estadoId elegido
  it('elegir un estado emite el estadoId', async () => {
    const raiz = await crear();

    elegir(raiz, 'tablero-filtro-estado', 'Bloqueada');

    expect(emitidos).toEqual([{ indicador: null, estadoId: 4, texto: null }]);
  });

  // "Vista Tablero": estados por orden del catálogo, inactivos con "(inactivo)"
  it('ofrece Todos y los estados ordenados por orden con los inactivos marcados', async () => {
    const raiz = await crear();

    const opciones = Array.from(
      raiz.querySelectorAll<HTMLOptionElement>('#tablero-filtro-estado option'),
    ).map((o) => o.textContent?.trim());
    expect(opciones).toEqual(['Todos', 'Viejo (inactivo)', 'Por hacer', 'Bloqueada', 'Hecho']);
  });

  // RN-21: el select de indicador emite el indicador elegido
  it('elegir un indicador emite el indicador', async () => {
    const raiz = await crear();

    elegir(raiz, 'tablero-filtro-indicador', 'Vencidas');

    expect(emitidos).toEqual([{ indicador: 'vencidas', estadoId: null, texto: null }]);
  });

  // CA-48: un chip por filtro activo con su texto
  it('muestra un chip por cada filtro activo', async () => {
    const raiz = await crear({ indicador: 'vencidas', estadoId: 4, texto: 'pago' });

    expect(chips(raiz)).toEqual(['Indicador: Vencidas', 'Estado: Bloqueada', 'Texto: «pago»']);
  });

  // CA-48: el nombre del chip de estado se toma de filtros.estado de la respuesta
  it('usa el nombre de estadoAplicado para el chip de estado', async () => {
    const raiz = await crear({ indicador: null, estadoId: 4, texto: null });
    fixture.componentRef.setInput('estadoAplicado', {
      id: 4,
      nombre: 'Bloqueada (servidor)',
      activo: true,
      categoria: 'BLOQUEADA',
    });
    await fixture.whenStable();

    expect(chips(raiz)).toEqual(['Estado: Bloqueada (servidor)']);
  });

  // CA-24, CA-48: quitar un chip emite los filtros sin ese filtro
  it('quitar el chip de indicador emite los filtros sin indicador', async () => {
    const raiz = await crear({ indicador: 'vencidas', estadoId: null, texto: 'pago' });

    raiz.querySelector<HTMLButtonElement>('[aria-label="Quitar filtro Indicador: Vencidas"]')!.click();

    expect(emitidos).toEqual([{ indicador: null, estadoId: null, texto: 'pago' }]);
  });

  // CA-48: "Limpiar filtros" emite los tres vacíos
  it('Limpiar filtros emite los tres filtros vacíos', async () => {
    const raiz = await crear({ indicador: 'bloqueadas', estadoId: 4, texto: 'pago' });

    limpiar(raiz).click();

    expect(emitidos).toEqual([{ indicador: null, estadoId: null, texto: null }]);
  });

  // CA-48: "Limpiar filtros" deshabilitado sin filtros y sin chips
  it('sin filtros Limpiar filtros está deshabilitado y no hay chips', async () => {
    const raiz = await crear();

    expect(limpiar(raiz).disabled).toBe(true);
    expect(chips(raiz)).toEqual([]);
  });

  // RN-21: los filtros de la URL se reflejan en el formulario
  it('refleja en los controles los filtros vigentes', async () => {
    await crear({ indicador: 'en-espera', estadoId: 1, texto: 'pago' });

    expect(fixture.componentInstance.form.getRawValue()).toEqual({
      texto: 'pago',
      estadoId: 1,
      indicador: 'en-espera',
    });
  });
});
