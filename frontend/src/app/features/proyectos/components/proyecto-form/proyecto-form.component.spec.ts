import { ComponentFixture, TestBed } from '@angular/core/testing';
import { EstadoProyectoDto } from '../../../estados-proyecto/models/estado-proyecto.model';
import { PersonaDto } from '../../../personas/models/persona.model';
import { ProyectoDto } from '../../models/proyecto.model';
import { MENSAJE_FECHA_OBJETIVO_ANTERIOR } from '../../validators/fecha-objetivo-no-anterior.validator';
import { ProyectoFormComponent, ValoresProyectoForm } from './proyecto-form.component';

/** Crea una persona de prueba. */
function persona(id: number, nombre: string, activo = true): PersonaDto {
  return {
    id,
    nombre,
    email: `p${id}@empresa.com`,
    esAdmin: false,
    activo,
    fechaCreacion: '2026-10-09T13:00:00Z',
    fechaModificacion: null,
  };
}

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

const PERSONAS = [persona(2, 'Ana Gómez'), persona(3, 'Bruno Díaz')];
const ESTADOS = [
  estado(1, 'En planificación', { esInicial: true }),
  estado(3, 'En curso'),
  estado(4, 'Pausado', { activo: false }),
  estado(7, 'Archivado', { esSistema: true, codigo: 'ARCHIVADO', orden: 9999 }),
];

const PROYECTO: ProyectoDto = {
  id: 3,
  codigo: 'PRJ-0003',
  nombre: 'Portal clientes',
  descripcion: 'Nuevo portal de autogestión.',
  responsable: { id: 5, nombre: 'Carla Ruiz', activo: false },
  fechaInicio: '2026-10-01',
  fechaObjetivo: '2026-12-15',
  estado: { id: 4, nombre: 'Pausado', esFinal: false, activo: false, esSistema: false },
  archivado: false,
  fechaArchivado: null,
  archivadoPor: null,
  estadoAlArchivar: null,
  cantidadAgrupaciones: 0,
  fechaCreacion: '2026-10-09T13:05:00Z',
  fechaModificacion: null,
};

/** Tests de ProyectoFormComponent en alta (P2) y edición (P4). */
describe('ProyectoFormComponent', () => {
  let fixture: ComponentFixture<ProyectoFormComponent>;

  /** Crea el componente con el modo indicado y lo renderiza. */
  async function crear(modo: 'alta' | 'edicion', proyecto: ProyectoDto | null = null): Promise<HTMLElement> {
    fixture = TestBed.createComponent(ProyectoFormComponent);
    fixture.componentRef.setInput('modo', modo);
    fixture.componentRef.setInput('proyecto', proyecto);
    fixture.componentRef.setInput('personas', PERSONAS);
    fixture.componentRef.setInput('estados', ESTADOS);
    await fixture.whenStable();
    return fixture.nativeElement as HTMLElement;
  }

  /** Escribe un valor en un input/textarea y dispara el evento `input`. */
  async function escribir(raiz: HTMLElement, selector: string, valor: string): Promise<void> {
    const elemento = raiz.querySelector<HTMLInputElement | HTMLTextAreaElement>(selector);
    if (!elemento) {
      throw new Error(`No existe ${selector}`);
    }
    elemento.value = valor;
    elemento.dispatchEvent(new Event('input'));
    elemento.dispatchEvent(new Event('blur'));
    await fixture.whenStable();
  }

  /** Textos de las opciones de un select. */
  function opciones(raiz: HTMLElement, selector: string): string[] {
    return Array.from(raiz.querySelectorAll(`${selector} option`)).map(
      (o) => o.textContent?.trim() ?? '',
    );
  }

  /** Botón Guardar. */
  function guardar(raiz: HTMLElement): HTMLButtonElement {
    const boton = raiz.querySelector<HTMLButtonElement>('button[type="submit"]');
    if (!boton) {
      throw new Error('No existe el botón Guardar');
    }
    return boton;
  }

  // CA-18: campos vacíos → form inválido y Guardar deshabilitado
  it('alta con campos vacíos deja el form inválido y Guardar deshabilitado', async () => {
    const raiz = await crear('alta');

    expect(fixture.componentInstance.form.invalid).toBe(true);
    expect(guardar(raiz).disabled).toBe(true);
  });

  // RN-15, RN-32: nombre 151 → maxlength
  it('nombre de 151 caracteres muestra error maxlength', async () => {
    const raiz = await crear('alta');

    await escribir(raiz, '#proyecto-nombre', 'a'.repeat(151));

    expect(fixture.componentInstance.form.controls.nombre.hasError('maxlength')).toBe(true);
    expect(raiz.querySelector('#proyecto-nombre-error')?.textContent).toContain('150');
  });

  // RN-15, RN-32: descripción 2001 → maxlength
  it('descripción de 2001 caracteres muestra error maxlength', async () => {
    const raiz = await crear('alta');

    await escribir(raiz, '#proyecto-descripcion', 'a'.repeat(2001));

    expect(fixture.componentInstance.form.controls.descripcion.hasError('maxlength')).toBe(true);
  });

  // RN-31: nombre solo con espacios es requerido
  it('nombre solo con espacios muestra error de obligatorio', async () => {
    const raiz = await crear('alta');

    await escribir(raiz, '#proyecto-nombre', '   ');

    expect(raiz.querySelector('#proyecto-nombre-error')?.textContent).toContain('obligatorio');
  });

  // RN-16, CA-17: fechas invertidas → mensaje en Fecha objetivo y Guardar deshabilitado
  it('fechas invertidas muestran el mensaje de fecha objetivo', async () => {
    const raiz = await crear('alta');

    await escribir(raiz, '#proyecto-fecha-inicio', '2026-10-10');
    await escribir(raiz, '#proyecto-fecha-objetivo', '2026-10-09');

    expect(raiz.querySelector('#proyecto-fecha-objetivo-error')?.textContent).toContain(
      MENSAJE_FECHA_OBJETIVO_ANTERIOR,
    );
    expect(guardar(raiz).disabled).toBe(true);
  });

  // RN-18, P2: en alta no hay campo Estado y se informa el estado inicial
  it('alta no muestra el campo Estado y muestra el nombre del estado inicial', async () => {
    const raiz = await crear('alta');

    expect(raiz.querySelector('#proyecto-estado')).toBeNull();
    expect(raiz.textContent).toContain('El proyecto se creará en estado En planificación');
  });

  // CA-05, P2: el select Responsable solo tiene personas activas
  it('alta ofrece solo personas activas como responsable', async () => {
    const raiz = await crear('alta');

    expect(opciones(raiz, '#proyecto-responsable')).toEqual([
      'Seleccioná un responsable',
      'Ana Gómez',
      'Bruno Díaz',
    ]);
  });

  // CA-13, CA-20: estados sin "Archivado" ni inactivos, salvo el actual inactivo
  it('edición ofrece estados activos y el actual inactivo, nunca Archivado', async () => {
    const raiz = await crear('edicion', PROYECTO);

    expect(opciones(raiz, '#proyecto-estado')).toEqual([
      'En planificación',
      'En curso',
      'Pausado (inactivo)',
    ]);
  });

  // RN-17, P4: responsable actual inactivo presente como "(inactivo)"
  it('edición incluye el responsable actual inactivo marcado', async () => {
    const raiz = await crear('edicion', PROYECTO);

    expect(opciones(raiz, '#proyecto-responsable')).toContain('Carla Ruiz (inactivo)');
  });

  // CA-19: código de solo lectura en edición
  it('edición muestra el código en solo lectura', async () => {
    const raiz = await crear('edicion', PROYECTO);

    const codigo = raiz.querySelector<HTMLInputElement>('#proyecto-codigo');
    expect(codigo?.value).toBe('PRJ-0003');
    expect(codigo?.readOnly).toBe(true);
  });

  // CA-19: Guardar emite los valores recortados y el estado
  it('edición válida emite los valores con textos recortados', async () => {
    const raiz = await crear('edicion', PROYECTO);
    let emitido: ValoresProyectoForm | null = null;
    fixture.componentInstance.guardar.subscribe((v) => (emitido = v));

    await escribir(raiz, '#proyecto-nombre', '  Portal v2  ');
    guardar(raiz).click();

    expect(emitido).toEqual({
      nombre: 'Portal v2',
      descripcion: 'Nuevo portal de autogestión.',
      responsableId: 5,
      fechaInicio: '2026-10-01',
      fechaObjetivo: '2026-12-15',
      estadoId: 4,
    });
  });

  // Pantallas: errores 400 del servidor junto al campo
  it('muestra los errores del servidor junto al campo', async () => {
    const raiz = await crear('alta');

    fixture.componentRef.setInput('errorServidor', {
      campos: { fechaObjetivo: ['La fecha objetivo debe ser mayor o igual a la fecha de inicio.'] },
      general: null,
    });
    await fixture.whenStable();

    expect(raiz.querySelector('#proyecto-fecha-objetivo-error')?.textContent).toContain(
      'La fecha objetivo debe ser mayor o igual a la fecha de inicio.',
    );
  });

  // Pantallas: 409/400 de negocio arriba del formulario
  it('muestra el error general de negocio arriba del formulario', async () => {
    const raiz = await crear('alta');

    fixture.componentRef.setInput('errorServidor', { campos: {}, general: 'Responsable inválido.' });
    await fixture.whenStable();

    expect(raiz.querySelector('[role="alert"]')?.textContent).toContain('Responsable inválido.');
  });
});
