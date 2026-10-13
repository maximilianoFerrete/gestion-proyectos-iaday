import { ComponentFixture, TestBed } from '@angular/core/testing';
import { AlcanceSeguimientoDto, IndicadoresDto } from '../../models/seguimiento.model';
import { ClaveTarjetaIndicador } from '../../utils/indicadores';
import { IndicadoresComponent } from './indicadores.component';

const INDICADORES: IndicadoresDto = {
  total: 42,
  abiertas: 30,
  finalizadas: 12,
  vencidas: 5,
  bloqueadas: 0,
  enEspera: 0,
  sinResponsable: 7,
  porEstado: [],
};

const ALCANCE: AlcanceSeguimientoDto = {
  todosLosNiveles: true,
  niveles: [],
  todosLosTipos: true,
  tipos: [],
};

/** Tests de IndicadoresComponent: tarjetas clicables (T2) e informativas (T1) (CA-24, CA-50, DA-21). */
describe('IndicadoresComponent', () => {
  let fixture: ComponentFixture<IndicadoresComponent>;
  let emitidos: ClaveTarjetaIndicador[];

  /** Crea el componente; `clicable` undefined = sin el input (default). */
  async function crear(clicable?: boolean): Promise<HTMLElement> {
    fixture = TestBed.createComponent(IndicadoresComponent);
    fixture.componentRef.setInput('indicadores', INDICADORES);
    fixture.componentRef.setInput('alcance', ALCANCE);
    fixture.componentRef.setInput('fechaReferencia', '2026-10-10');
    if (clicable !== undefined) {
      fixture.componentRef.setInput('clicable', clicable);
    }
    emitidos = [];
    fixture.componentInstance.indicadorSeleccionado.subscribe((clave) => emitidos.push(clave));
    await fixture.whenStable();
    return fixture.nativeElement as HTMLElement;
  }

  /** Tarjetas renderizadas (botones o divs). */
  function tarjetas(raiz: HTMLElement): HTMLElement[] {
    return Array.from(raiz.querySelectorAll<HTMLElement>('.tarjeta-indicador'));
  }

  beforeEach(() => {
    TestBed.configureTestingModule({ imports: [IndicadoresComponent] });
  });

  // "IndicadoresComponent": seis tarjetas con valor y título
  it('muestra seis tarjetas con sus títulos y valores', async () => {
    const raiz = await crear();

    expect(
      tarjetas(raiz).map((t) => [
        t.querySelector('.tarjeta-titulo')?.textContent?.trim(),
        t.querySelector('.tarjeta-valor')?.textContent?.trim(),
      ]),
    ).toEqual([
      ['Total', '42'],
      ['Abiertas', '30'],
      ['Vencidas', '5'],
      ['Bloqueadas', '0'],
      ['En espera', '0'],
      ['Sin responsable', '7'],
    ]);
  });

  // "IndicadoresComponent": alerta en Vencidas > 0 (no en Bloqueadas = 0)
  it('aplica el estilo de alerta solo a Vencidas cuando es mayor que 0', async () => {
    const raiz = await crear();

    const conAlerta = tarjetas(raiz)
      .filter((t) => t.classList.contains('tarjeta-alerta'))
      .map((t) => t.querySelector('.tarjeta-titulo')?.textContent?.trim());
    expect(conAlerta).toEqual(['Vencidas']);
  });

  // CA-24: sin input clicable (default true) las tarjetas son botones
  it('sin el input clicable las tarjetas son botones', async () => {
    const raiz = await crear();

    expect(tarjetas(raiz).every((t) => t.tagName === 'BUTTON')).toBe(true);
  });

  // CA-24, RN-24: clic en Vencidas emite "vencidas" y en Total emite "total"
  it('clicable: el clic en una tarjeta emite su clave ("total" para Total)', async () => {
    const raiz = await crear(true);

    tarjetas(raiz)[2].click();
    tarjetas(raiz)[0].click();

    expect(emitidos).toEqual(['vencidas', 'total']);
  });

  // DA-21, CA-50: clicable = false → sin botones, sin rol, sin tabindex y sin cursor de enlace
  it('no clicable: las tarjetas no son botones ni tienen rol, tabindex ni clase clicable', async () => {
    const raiz = await crear(false);

    expect(raiz.querySelectorAll('button').length).toBe(0);
    for (const tarjeta of tarjetas(raiz)) {
      expect(tarjeta.tagName).toBe('DIV');
      expect(tarjeta.hasAttribute('role')).toBe(false);
      expect(tarjeta.hasAttribute('tabindex')).toBe(false);
      expect(tarjeta.classList.contains('tarjeta-clicable')).toBe(false);
    }
  });

  // DA-21, CA-50: clicable = false → el clic no emite indicadorSeleccionado
  it('no clicable: el clic en cualquier tarjeta no emite', async () => {
    const raiz = await crear(false);

    tarjetas(raiz).forEach((t) => t.click());

    expect(emitidos).toEqual([]);
  });

  // RN-23: cada tarjeta lleva la definición en el title
  it('cada tarjeta muestra la definición del indicador en el title', async () => {
    const raiz = await crear(false);

    expect(tarjetas(raiz)[2].title).toBe('Abiertas con vencimiento anterior al 10/10/2026.');
  });
});
