import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { ConfiguracionTableroDto, ItemTableroDto } from '../../models/seguimiento.model';
import { TablaTableroComponent } from './tabla-tablero.component';

/** Ítem del tablero. */
function item(
  numero: number,
  tipo: { id: number; nombre: string; nivel: number },
  extra: Partial<ItemTableroDto> = {},
): ItemTableroDto {
  return {
    id: numero,
    codigo: `PRJ-0001-${numero}`,
    numero,
    titulo: `Incidencia ${numero}`,
    tipo: { id: tipo.id, nombre: tipo.nombre, activo: true, nivelNumero: tipo.nivel },
    estado: { id: 1, nombre: 'Por hacer', orden: 10, activo: true, esFinal: false, categoria: null },
    estimacion: null,
    vencida: false,
    responsable: null,
    ...extra,
  };
}

const EPICA = { id: 1, nombre: 'Épica', nivel: 1 };
const HU = { id: 2, nombre: 'Historia de Usuario', nivel: 2 };
const SUBTAREA = { id: 3, nombre: 'Subtarea', nivel: 3 };

/** Orden por defecto del servidor: Épica #3, HU #7, HU #15, Subtarea #16 (CA-29). */
const ITEMS = [
  item(3, EPICA, { estimacion: 8 }),
  item(7, HU, { estimacion: null }),
  item(15, HU, { estimacion: 12.5, vencida: true }),
  item(16, SUBTAREA, { estimacion: 0, responsable: { id: 5, nombre: 'Luis', activo: false } }),
];

const SIN_ESTIMACION: ConfiguracionTableroDto = {
  proyectoId: 1,
  personalizada: true,
  mostrarEstado: true,
  mostrarEstimacion: false,
  mostrarResponsable: true,
  fechaModificacion: '2026-10-10T13:00:00Z',
};

/** Tests de TablaTableroComponent (CA-29 a CA-32, CA-46, RN-29, RN-33, RN-36). */
describe('TablaTableroComponent', () => {
  let fixture: ComponentFixture<TablaTableroComponent>;

  /** Crea la grilla con los ítems y la configuración indicados. */
  async function crear(
    items: readonly ItemTableroDto[] = ITEMS,
    configuracion: ConfiguracionTableroDto | null = null,
  ): Promise<HTMLElement> {
    fixture = TestBed.createComponent(TablaTableroComponent);
    fixture.componentRef.setInput('proyectoId', 1);
    fixture.componentRef.setInput('items', items);
    fixture.componentRef.setInput('configuracion', configuracion);
    await fixture.whenStable();
    return fixture.nativeElement as HTMLElement;
  }

  /** Encabezados visibles (sin la flecha). */
  function encabezados(raiz: HTMLElement): string[] {
    return Array.from(raiz.querySelectorAll('thead th')).map(
      (th) => th.querySelector('button')?.firstChild?.textContent?.trim() ?? '',
    );
  }

  /** Números de las filas en el orden visible. */
  function numeros(raiz: HTMLElement): string[] {
    return Array.from(raiz.querySelectorAll('tbody tr .celda-nro a')).map(
      (a) => a.textContent?.trim() ?? '',
    );
  }

  /** Clic en el encabezado con el título indicado. */
  async function clicEncabezado(raiz: HTMLElement, titulo: string): Promise<void> {
    Array.from(raiz.querySelectorAll<HTMLButtonElement>('thead th button'))
      .find((b) => b.textContent?.includes(titulo))
      ?.click();
    await fixture.whenStable();
  }

  /** Botón "Orden predeterminado". */
  function ordenPredeterminado(raiz: HTMLElement): HTMLButtonElement {
    return Array.from(raiz.querySelectorAll<HTMLButtonElement>('button')).find(
      (b) => b.textContent?.trim() === 'Orden predeterminado',
    )!;
  }

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [TablaTableroComponent],
      providers: [provideRouter([])],
    });
  });

  // CA-29, RN-29: grilla con las seis columnas en orden fijo, una fila por incidencia
  it('sin configuración muestra las seis columnas y una fila por incidencia', async () => {
    const raiz = await crear();

    expect(encabezados(raiz)).toEqual(['Tipo', 'Nro', 'Título', 'Estado', 'Estimación', 'Responsable']);
    expect(numeros(raiz)).toEqual(['3', '7', '15', '16']);
  });

  // CA-29, DA-12: no hay agrupación por estado (una sola tabla, sin columnas por estado)
  it('no agrupa por estado: una sola tabla sin carriles', async () => {
    const raiz = await crear();

    expect(raiz.querySelectorAll('table').length).toBe(1);
    expect(raiz.querySelectorAll('tbody tr').length).toBe(4);
  });

  // CA-32, RN-29: mostrarEstimacion=false oculta la columna; Tipo/Nro/Título siempre
  it('con mostrarEstimacion=false no renderiza la columna Estimación', async () => {
    const raiz = await crear(ITEMS, SIN_ESTIMACION);

    expect(encabezados(raiz)).toEqual(['Tipo', 'Nro', 'Título', 'Estado', 'Responsable']);
    expect(raiz.querySelectorAll('tbody tr:first-child td').length).toBe(5);
  });

  // CA-41: estimación formateada; null → "—"; 0 → "0 h"
  it('muestra la estimación formateada y "—" sin estimación', async () => {
    const raiz = await crear();

    const celdas = Array.from(raiz.querySelectorAll('tbody td.celda-numero')).map(
      (td) => td.textContent?.trim(),
    );
    expect(celdas).toEqual(['8 h', '—', '12,5 h', '0 h']);
  });

  // CA-30, RN-33: clic en Estimación ordena asc con "—" al final; segundo clic desc con "—" al final
  it('clic en Estimación ordena asc y luego desc dejando las sin estimación al final', async () => {
    const raiz = await crear();

    await clicEncabezado(raiz, 'Estimación');
    expect(numeros(raiz)).toEqual(['16', '3', '15', '7']);

    await clicEncabezado(raiz, 'Estimación');
    expect(numeros(raiz)).toEqual(['15', '3', '16', '7']);
  });

  // CA-30: aria-sort refleja la dirección de la columna ordenada
  it('actualiza aria-sort de la columna ordenada', async () => {
    const raiz = await crear();

    await clicEncabezado(raiz, 'Estimación');
    const th = Array.from(raiz.querySelectorAll('thead th')).find((t) =>
      t.textContent?.includes('Estimación'),
    )!;
    expect(th.getAttribute('aria-sort')).toBe('ascending');

    await clicEncabezado(raiz, 'Estimación');
    expect(th.getAttribute('aria-sort')).toBe('descending');
  });

  // CA-30: "Orden predeterminado" vuelve a Tipo + Nro y queda deshabilitado
  it('Orden predeterminado restablece Tipo + Nro y queda deshabilitado', async () => {
    const raiz = await crear();
    expect(ordenPredeterminado(raiz).disabled).toBe(true);

    await clicEncabezado(raiz, 'Estimación');
    expect(ordenPredeterminado(raiz).disabled).toBe(false);

    ordenPredeterminado(raiz).click();
    await fixture.whenStable();

    expect(numeros(raiz)).toEqual(['3', '7', '15', '16']);
    expect(ordenPredeterminado(raiz).disabled).toBe(true);
  });

  // RN-33: si la columna ordenada queda oculta, vuelve al orden predeterminado
  it('si la columna ordenada queda oculta vuelve al orden predeterminado', async () => {
    const raiz = await crear();
    await clicEncabezado(raiz, 'Estimación');

    fixture.componentRef.setInput('configuracion', SIN_ESTIMACION);
    await fixture.whenStable();

    expect(numeros(raiz)).toEqual(['3', '7', '15', '16']);
  });

  // RN-33: al llegar otros ítems (cambio de filtros) el orden vuelve al predeterminado
  it('al recibir otros ítems restablece el orden predeterminado', async () => {
    const raiz = await crear();
    await clicEncabezado(raiz, 'Estimación');

    fixture.componentRef.setInput('items', [...ITEMS]);
    await fixture.whenStable();

    expect(ordenPredeterminado(raiz).disabled).toBe(true);
    expect(numeros(raiz)).toEqual(['3', '7', '15', '16']);
  });

  // CA-46: Nro es enlace a I3 con title = código
  it('Nro enlaza al detalle de la incidencia con el código como title', async () => {
    const raiz = await crear();

    const enlace = raiz.querySelector<HTMLAnchorElement>('tbody tr .celda-nro a')!;
    expect(enlace.getAttribute('href')).toBe('/proyectos/1/incidencias/3');
    expect(enlace.title).toBe('PRJ-0001-3');
  });

  // RN-36: ícono de vencida solo en las filas con vencida = true
  it('muestra la marca de vencida solo cuando vencida = true', async () => {
    const raiz = await crear();

    const filasConMarca = Array.from(raiz.querySelectorAll('tbody tr'))
      .filter((tr) => tr.querySelector('.marca-vencida[title="Vencida"]') !== null)
      .map((tr) => tr.querySelector('.celda-nro a')?.textContent?.trim());
    expect(filasConMarca).toEqual(['15']);
  });

  // "Vista Tablero": responsable inactivo marcado y "Sin responsable"
  it('muestra "Sin responsable" y el responsable inactivo marcado', async () => {
    const raiz = await crear();

    const texto = raiz.querySelector('tbody')?.textContent ?? '';
    expect(texto).toContain('Sin responsable');
    expect(texto).toMatch(/Luis\s*\(inactivo\)/);
  });

  // "Vista Tablero": clic en la fila emite el id de la incidencia (→ I3)
  it('clic en una fila emite el id de la incidencia', async () => {
    const raiz = await crear();
    const emitidos: number[] = [];
    fixture.componentInstance.seleccionar.subscribe((id) => emitidos.push(id));

    raiz.querySelectorAll<HTMLTableRowElement>('tbody tr')[1].click();

    expect(emitidos).toEqual([7]);
  });
});
