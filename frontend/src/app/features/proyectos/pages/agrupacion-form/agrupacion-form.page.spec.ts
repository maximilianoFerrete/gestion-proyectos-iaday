import { TestBed } from '@angular/core/testing';
import { Router, provideRouter } from '@angular/router';
import { of, throwError } from 'rxjs';
import { ErrorApi } from '../../../../core/models/error-api.model';
import { NotificacionService } from '../../../../core/services/notificacion.service';
import { ProyectoDto } from '../../models/proyecto.model';
import { AgrupacionesService } from '../../services/agrupaciones.service';
import { ProyectosService } from '../../services/proyectos.service';
import { AgrupacionFormPage, MENSAJE_PROYECTO_ARCHIVADO_NO_MODIFICABLE } from './agrupacion-form.page';

const PROYECTO: ProyectoDto = {
  id: 3,
  codigo: 'PRJ-0003',
  nombre: 'Portal clientes',
  descripcion: 'Nuevo portal de autogestión.',
  responsable: { id: 2, nombre: 'Ana Gómez', activo: true },
  fechaInicio: '2026-10-01',
  fechaObjetivo: '2026-12-15',
  estado: { id: 3, nombre: 'En curso', esFinal: false, activo: true, esSistema: false },
  archivado: false,
  fechaArchivado: null,
  archivadoPor: null,
  estadoAlArchivar: null,
  cantidadAgrupaciones: 0,
  fechaCreacion: '2026-10-09T13:05:00Z',
  fechaModificacion: null,
};

/** Tests de AgrupacionFormPage (P5, CA-24, CA-29, CA-30). */
describe('AgrupacionFormPage', () => {
  const proyectos = { obtener: vi.fn() };
  const agrupaciones = { obtener: vi.fn(), crear: vi.fn(), editar: vi.fn() };
  let router: Router;
  let notificacion: NotificacionService;

  beforeEach(() => {
    vi.clearAllMocks();
    TestBed.configureTestingModule({
      imports: [AgrupacionFormPage],
      providers: [
        provideRouter([]),
        { provide: ProyectosService, useValue: proyectos },
        { provide: AgrupacionesService, useValue: agrupaciones },
      ],
    });
    router = TestBed.inject(Router);
    notificacion = TestBed.inject(NotificacionService);
    vi.spyOn(router, 'navigate').mockResolvedValue(true);
  });

  /** Crea la página (alta si no se indica agrupacionId). */
  async function crear(agrupacionId?: string): Promise<AgrupacionFormPage> {
    const fixture = TestBed.createComponent(AgrupacionFormPage);
    fixture.componentRef.setInput('id', '3');
    fixture.componentRef.setInput('agrupacionId', agrupacionId);
    await fixture.whenStable();
    return fixture.componentInstance;
  }

  // CA-24: proyecto archivado → redirige al detalle con el mensaje
  it('con proyecto archivado redirige al detalle con el mensaje', async () => {
    const espiaInfo = vi.spyOn(notificacion, 'info');
    proyectos.obtener.mockReturnValue(of({ ...PROYECTO, archivado: true }));

    await crear();

    expect(router.navigate).toHaveBeenCalledWith(['/proyectos', 3]);
    expect(espiaInfo).toHaveBeenCalledWith(MENSAJE_PROYECTO_ARCHIVADO_NO_MODIFICABLE);
  });

  // CA-30: 409 AGRUPACION_NOMBRE_DUPLICADO se muestra en el campo Nombre
  it('409 nombre duplicado se asigna al campo nombre', async () => {
    proyectos.obtener.mockReturnValue(of(PROYECTO));
    agrupaciones.crear.mockReturnValue(
      throwError(() => new ErrorApi(409, 'AGRUPACION_NOMBRE_DUPLICADO', 'Nombre repetido.')),
    );
    const pagina = await crear();

    pagina.guardar(3, { nombre: 'Módulo pagos', descripcion: 'Desc.' });

    expect(pagina.errorServidor()).toEqual({ campos: { nombre: ['Nombre repetido.'] }, general: null });
  });

  // CA-29: guardar alta → toast y vuelta al detalle
  it('guardar en alta crea, avisa y vuelve al detalle', async () => {
    const espiaExito = vi.spyOn(notificacion, 'exito');
    proyectos.obtener.mockReturnValue(of(PROYECTO));
    agrupaciones.crear.mockReturnValue(of({}));
    const pagina = await crear();

    pagina.guardar(3, { nombre: 'Módulo pagos', descripcion: 'Desc.' });

    expect(agrupaciones.crear).toHaveBeenCalledWith(3, { nombre: 'Módulo pagos', descripcion: 'Desc.' });
    expect(espiaExito).toHaveBeenCalledWith('Agrupación guardada.');
    expect(router.navigate).toHaveBeenCalledWith(['/proyectos', 3]);
  });

  // CA-32: agrupación de otro proyecto (404) → no encontrado
  it('404 al cargar la agrupación muestra no encontrado', async () => {
    proyectos.obtener.mockReturnValue(of(PROYECTO));
    agrupaciones.obtener.mockReturnValue(
      throwError(() => new ErrorApi(404, 'AGRUPACION_NO_ENCONTRADA', 'No existe.')),
    );

    const pagina = await crear('8');

    expect(pagina.noEncontrado()).toBe(true);
  });
});
