import { ComponentFixture, TestBed } from '@angular/core/testing';
import {
  ConfiguracionTableroDto,
  ConfiguracionTableroRequest,
} from '../../models/seguimiento.model';
import {
  ConfigurarColumnasDialogComponent,
  TEXTO_ALCANCE_CONFIGURACION,
} from './configurar-columnas-dialog.component';

const SIN_ESTIMACION: ConfiguracionTableroDto = {
  proyectoId: 1,
  personalizada: true,
  mostrarEstado: true,
  mostrarEstimacion: false,
  mostrarResponsable: false,
  fechaModificacion: '2026-10-10T13:00:00Z',
};

/** Tests de ConfigurarColumnasDialogComponent (RF-005 RN-29 a RN-31, CA-32, CA-33). */
describe('ConfigurarColumnasDialogComponent', () => {
  let fixture: ComponentFixture<ConfigurarColumnasDialogComponent>;
  let guardados: ConfiguracionTableroRequest[];

  /** Crea el diálogo abierto con la configuración indicada. */
  async function crear(
    configuracion: ConfiguracionTableroDto | null = null,
    error: string | null = null,
  ): Promise<HTMLElement> {
    fixture = TestBed.createComponent(ConfigurarColumnasDialogComponent);
    fixture.componentRef.setInput('abierto', true);
    fixture.componentRef.setInput('configuracion', configuracion);
    fixture.componentRef.setInput('error', error);
    guardados = [];
    fixture.componentInstance.guardar.subscribe((r) => guardados.push(r));
    await fixture.whenStable();
    return fixture.nativeElement as HTMLElement;
  }

  /** Checkboxes con su texto, marcado y deshabilitado. */
  function checks(raiz: HTMLElement): [string, boolean, boolean][] {
    return Array.from(raiz.querySelectorAll<HTMLLabelElement>('label.campo-check')).map((label) => {
      const input = label.querySelector<HTMLInputElement>('input')!;
      return [label.textContent?.replace(/\s+/g, ' ').trim() ?? '', input.checked, input.disabled];
    });
  }

  /** Botón por texto. */
  function boton(raiz: HTMLElement, texto: string): HTMLButtonElement {
    return Array.from(raiz.querySelectorAll<HTMLButtonElement>('button')).find(
      (b) => b.textContent?.trim() === texto,
    )!;
  }

  beforeEach(() => {
    TestBed.configureTestingModule({ imports: [ConfigurarColumnasDialogComponent] });
  });

  // CA-33, RN-29: Tipo, Nro y Título marcados y deshabilitados con "(obligatoria)"
  it('muestra Tipo, Nro y Título marcados, deshabilitados y obligatorios', async () => {
    const raiz = await crear();

    expect(checks(raiz).slice(0, 3)).toEqual([
      ['Tipo (obligatoria)', true, true],
      ['Nro (obligatoria)', true, true],
      ['Título (obligatoria)', true, true],
    ]);
  });

  // RN-30: carga la configuración vigente en las tres configurables editables
  it('carga la configuración vigente en Estado, Estimación y Responsable editables', async () => {
    const raiz = await crear(SIN_ESTIMACION);

    expect(checks(raiz).slice(3)).toEqual([
      ['Estado', true, false],
      ['Estimación', false, false],
      ['Responsable', false, false],
    ]);
  });

  // RN-30: texto de alcance de la configuración
  it('informa que la configuración aplica a todo el proyecto y a todos los usuarios', async () => {
    const raiz = await crear();

    expect(raiz.textContent).toContain(TEXTO_ALCANCE_CONFIGURACION);
  });

  // "Vista Tablero": Restablecer marca las tres configurables
  it('Restablecer marca las tres columnas configurables', async () => {
    const raiz = await crear(SIN_ESTIMACION);

    boton(raiz, 'Restablecer').click();
    await fixture.whenStable();

    expect(fixture.componentInstance.form.getRawValue()).toEqual({
      mostrarEstado: true,
      mostrarEstimacion: true,
      mostrarResponsable: true,
    });
  });

  // RN-31, CA-32: Guardar emite los tres booleanos (reemplazo completo)
  it('Guardar emite los tres booleanos', async () => {
    const raiz = await crear();
    const estimacion = checks(raiz).findIndex(([texto]) => texto === 'Estimación');
    raiz.querySelectorAll<HTMLInputElement>('label.campo-check input')[estimacion].click();
    await fixture.whenStable();

    boton(raiz, 'Guardar').click();

    expect(guardados).toEqual([
      { mostrarEstado: true, mostrarEstimacion: false, mostrarResponsable: true },
    ]);
  });

  // "Vista Tablero": errores 401/403/409 se muestran dentro del diálogo
  it('muestra el mensaje de error dentro del diálogo', async () => {
    const raiz = await crear(null, 'El proyecto PRJ-0001 está archivado y no admite modificaciones.');

    expect(raiz.querySelector('[role="alert"]')?.textContent).toContain('está archivado');
  });

  // "Vista Tablero": Cancelar emite cancelar
  it('Cancelar emite cancelar', async () => {
    const raiz = await crear();
    let cancelado = false;
    fixture.componentInstance.cancelar.subscribe(() => (cancelado = true));

    boton(raiz, 'Cancelar').click();

    expect(cancelado).toBe(true);
  });
});
