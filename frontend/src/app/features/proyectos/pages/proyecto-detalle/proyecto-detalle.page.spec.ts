import { signal } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { of, throwError } from 'rxjs';
import { ErrorApi } from '../../../../core/models/error-api.model';
import { ConfirmacionService } from '../../../../core/services/confirmacion.service';
import { UsuarioActualService } from '../../../../core/services/usuario-actual.service';
import { AgrupacionDto } from '../../models/agrupacion.model';
import { ProyectoDto } from '../../models/proyecto.model';
import { AgrupacionesService } from '../../services/agrupaciones.service';
import { ProyectosService } from '../../services/proyectos.service';
import { ProyectoDetallePage, mensajeEliminarAgrupacion } from './proyecto-detalle.page';

const PROYECTO: ProyectoDto = {
  id: 3,
  codigo: 'PRJ-0003',
  nombre: 'Portal clientes',
  descripcion: 'Nuevo portal de autogestión.',
  responsable: { id: 2, nombre: 'Ana Gómez', activo: true },
  fechaInicio: '2026-10-01',
  fechaObjetivo: '2026-12-15',
  estado: { id: 5, nombre: 'Finalizado', esFinal: true, activo: true, esSistema: false },
  archivado: false,
  fechaArchivado: null,
  archivadoPor: null,
  estadoAlArchivar: null,
  cantidadAgrupaciones: 1,
  cantidadIncidencias: 0,
  fechaCreacion: '2026-10-09T13:05:00Z',
  fechaModificacion: null,
};

const ARCHIVADO: ProyectoDto = {
  ...PROYECTO,
  archivado: true,
  estado: { id: 7, nombre: 'Archivado', esFinal: false, activo: true, esSistema: true },
  fechaArchivado: '2026-11-02T10:00:00Z',
  archivadoPor: { id: 1, nombre: 'Administrador', activo: true },
  estadoAlArchivar: { id: 5, nombre: 'Finalizado', esFinal: true, activo: true, esSistema: false },
};

const AGRUPACION: AgrupacionDto = {
  id: 8,
  proyectoId: 3,
  nombre: 'Módulo pagos',
  descripcion: 'Incidencias del circuito de pagos.',
  cantidadIncidencias: 0,
  fechaCreacion: '2026-10-09T13:10:00Z',
  fechaModificacion: null,
};

/**
 * Tests de ProyectoDetallePage (P3, CA-21 a CA-23, CA-28, CA-31).
 * RF-002 "Cambios en P3": botones Incidencias/Árbol, cantidad de incidencias y diálogo de
 * eliminar agrupación con la cantidad (CA-40, CA-41).
 */
describe('ProyectoDetallePage', () => {
  let fixture: ComponentFixture<ProyectoDetallePage>;
  const proyectos = { obtener: vi.fn(), archivar: vi.fn() };
  const agrupaciones = { listar: vi.fn(), eliminar: vi.fn() };
  const esAdmin = signal(false);

  /** Crea la página para el proyecto indicado y la renderiza. */
  async function crear(proyecto: ProyectoDto): Promise<HTMLElement> {
    proyectos.obtener.mockReturnValue(of(proyecto));
    agrupaciones.listar.mockReturnValue(of([AGRUPACION]));
    fixture = TestBed.createComponent(ProyectoDetallePage);
    fixture.componentRef.setInput('id', '3');
    await fixture.whenStable();
    return fixture.nativeElement as HTMLElement;
  }

  /** Busca un botón o enlace por su texto visible. */
  function accion(raiz: HTMLElement, texto: string): HTMLElement | undefined {
    return Array.from(raiz.querySelectorAll<HTMLElement>('a, button')).find(
      (e) => e.textContent?.trim() === texto,
    );
  }

  beforeEach(() => {
    vi.clearAllMocks();
    esAdmin.set(false);
    TestBed.configureTestingModule({
      imports: [ProyectoDetallePage],
      providers: [
        provideRouter([]),
        { provide: ProyectosService, useValue: proyectos },
        { provide: AgrupacionesService, useValue: agrupaciones },
        { provide: UsuarioActualService, useValue: { esAdmin } },
      ],
    });
  });

  // CA-21, CA-23: archivado → banner y sin acciones de modificación
  it('proyecto archivado muestra el banner y oculta todas las acciones de modificación', async () => {
    esAdmin.set(true);

    const raiz = await crear(ARCHIVADO);

    expect(raiz.querySelector('.banner-archivado')?.textContent).toContain('Solo lectura');
    expect(raiz.querySelector('.banner-archivado')?.textContent).toContain('Administrador');
    expect(raiz.querySelector('.banner-archivado')?.textContent).toContain('Finalizado');
    expect(accion(raiz, 'Editar')).toBeUndefined();
    expect(accion(raiz, 'Archivar')).toBeUndefined();
    expect(accion(raiz, 'Nueva agrupación')).toBeUndefined();
    expect(accion(raiz, 'Eliminar')).toBeUndefined();
  });

  // CA-33: el archivado igual lista sus agrupaciones
  it('proyecto archivado muestra la lista de agrupaciones', async () => {
    const raiz = await crear(ARCHIVADO);

    expect(raiz.textContent).toContain('Módulo pagos');
  });

  // CA-22: usuario no admin no ve Archivar
  it('usuario no admin no ve el botón Archivar pero sí Editar', async () => {
    const raiz = await crear(PROYECTO);

    expect(accion(raiz, 'Archivar')).toBeUndefined();
    expect(accion(raiz, 'Editar')).toBeDefined();
  });

  // CA-21: admin confirma → llama archivar y recarga el detalle
  it('admin que confirma el archivado llama a archivar y recarga', async () => {
    esAdmin.set(true);
    proyectos.archivar.mockReturnValue(of(ARCHIVADO));
    const raiz = await crear(PROYECTO);
    const confirmacion = TestBed.inject(ConfirmacionService);

    accion(raiz, 'Archivar')?.click();
    confirmacion.responder(true);
    await fixture.whenStable();

    expect(proyectos.archivar).toHaveBeenCalledWith(3);
    expect(proyectos.obtener).toHaveBeenCalledTimes(2);
  });

  // CA-21: el diálogo de archivar advierte que no podrá desarchivarse
  it('el diálogo de archivar muestra el código, el nombre y la advertencia', async () => {
    esAdmin.set(true);
    const raiz = await crear(PROYECTO);
    const confirmacion = TestBed.inject(ConfirmacionService);

    accion(raiz, 'Archivar')?.click();

    expect(confirmacion.solicitud()?.mensaje).toBe(
      '¿Archivar el proyecto PRJ-0003 – Portal clientes? ' +
        'El proyecto quedará en solo lectura y no podrá desarchivarse.',
    );
  });

  // CA-31: cancelar el diálogo de eliminar no llama a eliminar
  it('cancelar el diálogo de eliminar agrupación no llama a eliminar', async () => {
    const raiz = await crear(PROYECTO);
    const confirmacion = TestBed.inject(ConfirmacionService);

    accion(raiz, 'Eliminar')?.click();
    confirmacion.responder(false);
    await fixture.whenStable();

    expect(agrupaciones.eliminar).not.toHaveBeenCalled();
  });

  // CA-31: confirmar elimina y recarga la lista
  it('confirmar el diálogo de eliminar llama a eliminar y recarga la lista', async () => {
    agrupaciones.eliminar.mockReturnValue(of(undefined));
    const raiz = await crear(PROYECTO);
    const confirmacion = TestBed.inject(ConfirmacionService);

    accion(raiz, 'Eliminar')?.click();
    confirmacion.responder(true);
    await fixture.whenStable();

    expect(agrupaciones.eliminar).toHaveBeenCalledWith(3, 8);
    expect(agrupaciones.listar).toHaveBeenCalledTimes(2);
  });

  // RF-002 P3: botones Incidencias y Árbol con sus rutas
  it('muestra los botones Incidencias y Árbol con las rutas del proyecto', async () => {
    const raiz = await crear(PROYECTO);

    expect(accion(raiz, 'Incidencias')?.getAttribute('href')).toBe('/proyectos/3/incidencias');
    expect(accion(raiz, 'Árbol')?.getAttribute('href')).toBe('/proyectos/3/incidencias/arbol');
  });

  // RF-002 CA-21: el archivado también ofrece consultar incidencias y árbol
  it('proyecto archivado conserva los botones Incidencias y Árbol', async () => {
    const raiz = await crear(ARCHIVADO);

    expect(accion(raiz, 'Incidencias')).toBeDefined();
    expect(accion(raiz, 'Árbol')).toBeDefined();
  });

  // RF-002 CA-41: total de incidencias del proyecto y columna Incidencias de agrupaciones
  it('muestra la cantidad de incidencias del proyecto y de cada agrupación', async () => {
    proyectos.obtener.mockReturnValue(of({ ...PROYECTO, cantidadIncidencias: 42 }));
    agrupaciones.listar.mockReturnValue(of([{ ...AGRUPACION, cantidadIncidencias: 7 }]));
    fixture = TestBed.createComponent(ProyectoDetallePage);
    fixture.componentRef.setInput('id', '3');
    await fixture.whenStable();
    const raiz = fixture.nativeElement as HTMLElement;

    expect(raiz.textContent).toContain('42');
    const encabezados = Array.from(raiz.querySelectorAll('th')).map((th) => th.textContent?.trim());
    expect(encabezados).toContain('Incidencias');
    expect(raiz.querySelector('tbody')?.textContent).toContain('7');
  });

  // RF-002 CA-40: el diálogo de eliminar informa la cantidad de incidencias asociadas
  it('el diálogo de eliminar agrupación con incidencias informa la cantidad', async () => {
    proyectos.obtener.mockReturnValue(of(PROYECTO));
    agrupaciones.listar.mockReturnValue(of([{ ...AGRUPACION, cantidadIncidencias: 4 }]));
    fixture = TestBed.createComponent(ProyectoDetallePage);
    fixture.componentRef.setInput('id', '3');
    await fixture.whenStable();
    const confirmacion = TestBed.inject(ConfirmacionService);

    accion(fixture.nativeElement as HTMLElement, 'Eliminar')?.click();

    expect(confirmacion.solicitud()?.mensaje).toBe(
      '¿Eliminar la agrupación Módulo pagos? Las 4 incidencias asociadas no se eliminan: ' +
        'quedarán sin agrupación (incluidas sus jerarquías).',
    );
  });

  // RF-002 P3: con 0 incidencias se mantiene el texto de RF-001
  it('mensajeEliminarAgrupacion con 0 incidencias usa el texto de RF-001', () => {
    expect(mensajeEliminarAgrupacion(AGRUPACION)).toBe(
      '¿Eliminar la agrupación Módulo pagos? Las incidencias asociadas no se eliminan: quedarán sin agrupación.',
    );
  });

  // CA-28: 404 al cargar → "No se encontró el recurso"
  it('proyecto inexistente muestra "No se encontró el recurso"', async () => {
    proyectos.obtener.mockReturnValue(
      throwError(() => new ErrorApi(404, 'PROYECTO_NO_ENCONTRADO', 'No existe.')),
    );
    agrupaciones.listar.mockReturnValue(of([]));
    fixture = TestBed.createComponent(ProyectoDetallePage);
    fixture.componentRef.setInput('id', '99');
    await fixture.whenStable();

    expect((fixture.nativeElement as HTMLElement).textContent).toContain(
      'No se encontró el recurso',
    );
  });
});
