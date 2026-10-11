import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { FlujoIncidenciaDto } from '../../../configuracion-incidencias/models/flujo-incidencia.model';
import { PrioridadIncidenciaDto } from '../../../configuracion-incidencias/models/prioridad-incidencia.model';
import { RelacionTipoIncidenciaDto } from '../../../configuracion-incidencias/models/relacion-tipo-incidencia.model';
import { TipoIncidenciaDto } from '../../../configuracion-incidencias/models/tipo-incidencia.model';
import { PersonaDto } from '../../../personas/models/persona.model';
import { AgrupacionDto } from '../../../proyectos/models/agrupacion.model';
import { IncidenciaDto } from '../../models/incidencia.model';
import { ValoresIncidenciaForm } from '../../utils/incidencia-request';
import { PadreSeleccionado } from '../../utils/padre-incidencia';
import { IncidenciaFormComponent, ModoIncidenciaForm } from './incidencia-form.component';

const CREADO = '2026-10-09T13:00:00Z';

/** Tipo de prueba (todos en el flujo general). */
function tipo(id: number, nombre: string, nivel: number, activo = true): TipoIncidenciaDto {
  return {
    id,
    nombre,
    descripcion: null,
    nivel: { id: nivel, numero: nivel, nombre: `Nivel ${nivel}` },
    flujo: { id: 1, nombre: 'Flujo general' },
    activo,
    cantidadIncidencias: 0,
    fechaCreacion: CREADO,
    fechaModificacion: null,
  };
}

/** Relación padre → hijo. */
function relacion(padre: TipoIncidenciaDto, hijo: TipoIncidenciaDto): RelacionTipoIncidenciaDto {
  return {
    id: padre.id * 10 + hijo.id,
    tipoPadre: {
      id: padre.id,
      nombre: padre.nombre,
      activo: true,
      nivelNumero: padre.nivel.numero,
    },
    tipoHijo: { id: hijo.id, nombre: hijo.nombre, activo: true, nivelNumero: hijo.nivel.numero },
    fechaCreacion: CREADO,
  };
}

const EPICA = tipo(1, 'Épica', 1);
const HU = tipo(2, 'Historia de Usuario', 2);
const SUBTAREA = tipo(3, 'Subtarea', 3);
const CASO = tipo(4, 'Caso de Prueba', 3);
const BUG = tipo(5, 'Bug', 3);
const VIEJO = tipo(6, 'Viejo', 3, false);
const TIPOS = [EPICA, HU, SUBTAREA, CASO, BUG, VIEJO];
const RELACIONES = [
  relacion(EPICA, HU),
  relacion(HU, SUBTAREA),
  relacion(HU, CASO),
  relacion(HU, BUG),
];

const FLUJO_GENERAL: FlujoIncidenciaDto = {
  id: 1,
  nombre: 'Flujo general',
  descripcion: null,
  estados: [
    { estadoId: 1, nombre: 'Por hacer', activo: true, orden: 1, esInicial: true, esFinal: false },
    {
      estadoId: 2,
      nombre: 'En progreso',
      activo: false,
      orden: 2,
      esInicial: false,
      esFinal: false,
    },
    { estadoId: 3, nombre: 'Hecho', activo: true, orden: 3, esInicial: false, esFinal: true },
  ],
  tipos: [],
  fechaCreacion: CREADO,
  fechaModificacion: null,
};

const PRIORIDADES: PrioridadIncidenciaDto[] = [
  {
    id: 1,
    nombre: 'Baja',
    descripcion: null,
    orden: 10,
    esPredeterminada: false,
    activo: true,
    cantidadIncidencias: 0,
  },
  {
    id: 2,
    nombre: 'Media',
    descripcion: null,
    orden: 20,
    esPredeterminada: true,
    activo: true,
    cantidadIncidencias: 0,
  },
];

const PERSONAS: PersonaDto[] = [
  {
    id: 2,
    nombre: 'Ana Gómez',
    email: 'ana@empresa.com',
    esAdmin: false,
    activo: true,
    fechaCreacion: CREADO,
    fechaModificacion: null,
  },
];

const AGRUPACIONES: AgrupacionDto[] = [
  {
    id: 8,
    proyectoId: 1,
    nombre: 'Módulo pagos',
    descripcion: 'Pagos.',
    cantidadIncidencias: 0,
    fechaCreacion: CREADO,
    fechaModificacion: null,
  },
  {
    id: 9,
    proyectoId: 1,
    nombre: 'Envíos',
    descripcion: 'Envíos.',
    cantidadIncidencias: 0,
    fechaCreacion: CREADO,
    fechaModificacion: null,
  },
];

/** HU PRJ-0001-15 en "Módulo pagos" con vencimiento 30/11 (padre de CA-23 y CA-31). */
const PADRE_HU: PadreSeleccionado = {
  id: 15,
  codigo: 'PRJ-0001-15',
  titulo: 'Pagar con tarjeta',
  tipo: { id: HU.id, nombre: HU.nombre, activo: true, nivelNumero: 2 },
  fechaVencimiento: '2026-11-30',
  agrupacion: { id: 8, nombre: 'Módulo pagos' },
};

/** Subtarea en "En progreso" (estado hoy inactivo) para el modo edición. */
const SUBTAREA_EN_EDICION: IncidenciaDto = {
  id: 16,
  codigo: 'PRJ-0001-16',
  numero: 16,
  proyecto: { id: 1, codigo: 'PRJ-0001', nombre: 'Portal clientes', archivado: false },
  titulo: 'Validar tarjeta',
  descripcion: 'Validar número.',
  tipo: { id: SUBTAREA.id, nombre: SUBTAREA.nombre, activo: true, nivelNumero: 3 },
  estado: { id: 2, nombre: 'En progreso', activo: false, esFinal: false },
  prioridad: { id: 2, nombre: 'Media', orden: 20, activo: true },
  responsable: null,
  fechaVencimiento: '2026-11-20',
  agrupacion: { id: 8, nombre: 'Módulo pagos' },
  agrupacionHeredada: true,
  padre: {
    id: 15,
    codigo: 'PRJ-0001-15',
    titulo: 'Pagar con tarjeta',
    tipo: { id: HU.id, nombre: HU.nombre, activo: true, nivelNumero: 2 },
    estado: { id: 1, nombre: 'Por hacer', activo: true, esFinal: false },
    responsable: null,
    fechaVencimiento: '2026-11-30',
  },
  hijas: [],
  tiposHijaPermitidos: [],
  fechaCreacion: CREADO,
  fechaModificacion: null,
};

/** Tests de IncidenciaFormComponent (I4/I5, CA-10, CA-16, CA-18, CA-23, CA-29, CA-31). */
describe('IncidenciaFormComponent', () => {
  let fixture: ComponentFixture<IncidenciaFormComponent>;

  /** Crea el formulario en el modo indicado y lo renderiza. */
  async function crear(
    modo: ModoIncidenciaForm,
    opciones: { padreInicial?: PadreSeleccionado; incidencia?: IncidenciaDto } = {},
  ): Promise<HTMLElement> {
    fixture = TestBed.createComponent(IncidenciaFormComponent);
    const ref = fixture.componentRef;
    ref.setInput('modo', modo);
    ref.setInput('proyectoId', 1);
    ref.setInput('incidencia', opciones.incidencia ?? null);
    ref.setInput('padreInicial', opciones.padreInicial ?? null);
    ref.setInput('tipos', TIPOS);
    ref.setInput('relaciones', RELACIONES);
    ref.setInput('flujos', [FLUJO_GENERAL]);
    ref.setInput('prioridades', PRIORIDADES);
    ref.setInput('personas', PERSONAS);
    ref.setInput('agrupaciones', AGRUPACIONES);
    await fixture.whenStable();
    return fixture.nativeElement as HTMLElement;
  }

  /** Textos de las opciones de un select (sin el placeholder deshabilitado). */
  function opciones(raiz: HTMLElement, id: string): string[] {
    return Array.from(
      raiz.querySelectorAll<HTMLOptionElement>(`#${id} option:not([disabled])`),
    ).map((o) => o.textContent?.trim() ?? '');
  }

  /** Escribe en un input/textarea y dispara el evento input. */
  function escribir(raiz: HTMLElement, id: string, valor: string): void {
    const campo = raiz.querySelector<HTMLInputElement | HTMLTextAreaElement>(`#${id}`)!;
    campo.value = valor;
    campo.dispatchEvent(new Event('input'));
  }

  /** Botón Guardar. */
  function guardar(raiz: HTMLElement): HTMLButtonElement {
    return raiz.querySelector<HTMLButtonElement>('button[type="submit"]')!;
  }

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [IncidenciaFormComponent],
      providers: [provideRouter([])],
    });
  });

  // CA-23: alta con padre → padre preseleccionado y Tipo limitado a los hijos permitidos
  it('alta con padreId preselecciona el padre y limita el Tipo a Subtarea, Caso de Prueba y Bug', async () => {
    const raiz = await crear('alta', { padreInicial: PADRE_HU });

    expect(raiz.textContent).toContain('PRJ-0001-15');
    expect(opciones(raiz, 'incidencia-tipo')).toEqual([
      'Subtarea (nivel 3)',
      'Caso de Prueba (nivel 3)',
      'Bug (nivel 3)',
    ]);
  });

  // CA-23, CA-29, RN-28: la agrupación aparece deshabilitada con la del padre
  it('alta con padre deshabilita Agrupación con la del padre y muestra "Se hereda del padre"', async () => {
    const raiz = await crear('alta', { padreInicial: PADRE_HU });

    const select = raiz.querySelector<HTMLSelectElement>('#incidencia-agrupacion')!;
    expect(select.disabled).toBe(true);
    expect(fixture.componentInstance.form.controls.agrupacionId.value).toBe(8);
    expect(raiz.textContent).toContain('Se hereda del padre');
  });

  // RN-07, RN-28: sin padre → agrupación habilitada y todos los tipos activos
  it('alta sin padre habilita Agrupación y ofrece todos los tipos activos', async () => {
    const raiz = await crear('alta');

    expect(raiz.querySelector<HTMLSelectElement>('#incidencia-agrupacion')!.disabled).toBe(false);
    expect(opciones(raiz, 'incidencia-tipo')).toEqual([
      'Épica (nivel 1)',
      'Historia de Usuario (nivel 2)',
      'Subtarea (nivel 3)',
      'Caso de Prueba (nivel 3)',
      'Bug (nivel 3)',
    ]);
  });

  // RN-21: en el alta se preselecciona la prioridad predeterminada y no hay campo Estado
  it('alta preselecciona la prioridad predeterminada y no muestra Estado', async () => {
    const raiz = await crear('alta');

    expect(fixture.componentInstance.form.controls.prioridadId.value).toBe(2);
    expect(raiz.querySelector('#incidencia-estado')).toBeNull();
  });

  // RN-20: texto informativo con el estado inicial del flujo del tipo elegido
  it('alta muestra "Se creará en estado Por hacer" al elegir un tipo', async () => {
    const raiz = await crear('alta');

    fixture.componentInstance.form.controls.tipoId.setValue(HU.id);
    await fixture.whenStable();

    expect(raiz.textContent).toContain('Se creará en estado Por hacer');
  });

  // CA-16: campos obligatorios vacíos → formulario inválido y Guardar deshabilitado
  it('alta con campos vacíos deja el formulario inválido y Guardar deshabilitado', async () => {
    const raiz = await crear('alta');

    expect(fixture.componentInstance.form.invalid).toBe(true);
    expect(guardar(raiz).disabled).toBe(true);
  });

  // CA-16: título de 201 caracteres → inválido aunque el resto sea válido
  it('título de 201 caracteres deja el formulario inválido', async () => {
    const raiz = await crear('alta');
    fixture.componentInstance.form.controls.tipoId.setValue(HU.id);
    escribir(raiz, 'incidencia-titulo', 'a'.repeat(201));
    escribir(raiz, 'incidencia-descripcion', 'Descripción');
    await fixture.whenStable();

    expect(fixture.componentInstance.form.controls.titulo.invalid).toBe(true);
    expect(guardar(raiz).disabled).toBe(true);
  });

  // CA-16: descripción de 4001 caracteres → inválido
  it('descripción de 4001 caracteres deja el formulario inválido', async () => {
    await crear('alta');
    const form = fixture.componentInstance.form;
    form.patchValue({ tipoId: HU.id, titulo: 'Título', descripcion: 'a'.repeat(4001) });

    expect(form.controls.descripcion.invalid).toBe(true);
  });

  // CA-16: completo y válido → Guardar habilitado
  it('alta completa y válida habilita Guardar', async () => {
    const raiz = await crear('alta');
    fixture.componentInstance.form.patchValue({
      tipoId: HU.id,
      titulo: 'Pagar',
      descripcion: 'Desc',
    });
    await fixture.whenStable();

    expect(guardar(raiz).disabled).toBe(false);
  });

  // RN-31, CA-31: vencimiento posterior al del padre → error en Vencimiento
  it('vencimiento 01/12 con padre 30/11 marca el error en Vencimiento', async () => {
    const raiz = await crear('alta', { padreInicial: PADRE_HU });

    fixture.componentInstance.form.controls.fechaVencimiento.setValue('2026-12-01');
    await fixture.whenStable();

    expect(raiz.querySelector('#incidencia-vencimiento-error')?.textContent).toContain(
      'El vencimiento no puede ser posterior al del padre (30/11/2026).',
    );
    expect(fixture.componentInstance.form.invalid).toBe(true);
  });

  // RN-31, CA-31: vencimiento igual al del padre es válido
  it('vencimiento igual al del padre no marca error', async () => {
    await crear('alta', { padreInicial: PADRE_HU });

    fixture.componentInstance.form.controls.fechaVencimiento.setValue('2026-11-30');

    expect(fixture.componentInstance.form.controls.fechaVencimiento.valid).toBe(true);
  });

  // I4: al elegir un padre cuyo tipo no admite el tipo elegido, el tipo se limpia
  it('al elegir un padre que no admite el tipo elegido, limpia el Tipo', async () => {
    await crear('alta');
    const componente = fixture.componentInstance;
    componente.form.controls.tipoId.setValue(EPICA.id);

    componente.seleccionarPadre(PADRE_HU);

    expect(componente.form.controls.tipoId.value).toBeNull();
  });

  // RN-28: con padre, el envío manda agrupacionId nulo (se hereda) y textos recortados
  it('enviar con padre emite agrupacionId null, padreId y textos recortados', async () => {
    await crear('alta', { padreInicial: PADRE_HU });
    const componente = fixture.componentInstance;
    let emitido: ValoresIncidenciaForm | undefined;
    componente.guardar.subscribe((v) => (emitido = v));
    componente.form.patchValue({
      tipoId: SUBTAREA.id,
      titulo: '  Validar  ',
      descripcion: ' Desc ',
    });

    componente.enviar();

    expect(emitido).toMatchObject({
      tipoId: SUBTAREA.id,
      titulo: 'Validar',
      descripcion: 'Desc',
      estadoId: null,
      prioridadId: 2,
      agrupacionId: null,
      padreId: 15,
      fechaVencimiento: null,
    });
  });

  // I5: en edición Código y Tipo son de solo lectura (no hay select de Tipo)
  it('edición muestra Tipo en solo lectura', async () => {
    const raiz = await crear('edicion', { incidencia: SUBTAREA_EN_EDICION });

    expect(raiz.querySelector('#incidencia-tipo')).toBeNull();
    const tipoLectura = raiz.querySelector<HTMLInputElement>('#incidencia-tipo-lectura')!;
    expect(tipoLectura.readOnly).toBe(true);
    expect(tipoLectura.value).toBe('Subtarea');
  });

  // CA-18, CA-10: Estado ofrece los activos del flujo y el actual inactivo marcado
  it('edición ofrece los estados activos del flujo más el actual "(inactivo)" en orden del flujo', async () => {
    const raiz = await crear('edicion', { incidencia: SUBTAREA_EN_EDICION });

    expect(opciones(raiz, 'incidencia-estado')).toEqual([
      'Por hacer',
      'En progreso (inactivo)',
      'Hecho',
    ]);
    expect(fixture.componentInstance.form.controls.estadoId.value).toBe(2);
  });

  // CA-29: en edición con padre la agrupación está deshabilitada
  it('edición con padre deshabilita Agrupación', async () => {
    const raiz = await crear('edicion', { incidencia: SUBTAREA_EN_EDICION });

    expect(raiz.querySelector<HTMLSelectElement>('#incidencia-agrupacion')!.disabled).toBe(true);
  });

  // CA-31: 400 VENCIMIENTO_POSTERIOR_AL_PADRE del servidor → error en Vencimiento
  it('error del servidor en fechaVencimiento se muestra junto a Vencimiento', async () => {
    const raiz = await crear('edicion', { incidencia: SUBTAREA_EN_EDICION });

    fixture.componentRef.setInput('errorServidor', {
      campos: {
        fechaVencimiento: [
          'El vencimiento 2026-12-01 es posterior al del padre PRJ-0001-15 (2026-11-30).',
        ],
      },
      general: null,
    });
    await fixture.whenStable();

    expect(raiz.querySelector('#incidencia-vencimiento-error')?.textContent).toContain(
      'El vencimiento 2026-12-01 es posterior al del padre PRJ-0001-15 (2026-11-30).',
    );
  });
});
