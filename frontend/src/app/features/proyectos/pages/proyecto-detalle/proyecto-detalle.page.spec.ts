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
import { ProyectoDetallePage } from './proyecto-detalle.page';

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

/** Tests de ProyectoDetallePage (P3, CA-21 a CA-23, CA-28, CA-31). */
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

  // CA-28: 404 al cargar → "No se encontró el recurso"
  it('proyecto inexistente muestra "No se encontró el recurso"', async () => {
    proyectos.obtener.mockReturnValue(
      throwError(() => new ErrorApi(404, 'PROYECTO_NO_ENCONTRADO', 'No existe.')),
    );
    agrupaciones.listar.mockReturnValue(of([]));
    fixture = TestBed.createComponent(ProyectoDetallePage);
    fixture.componentRef.setInput('id', '99');
    await fixture.whenStable();

    expect((fixture.nativeElement as HTMLElement).textContent).toContain('No se encontró el recurso');
  });
});
