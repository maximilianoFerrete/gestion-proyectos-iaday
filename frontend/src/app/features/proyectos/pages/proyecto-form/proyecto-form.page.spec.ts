import { TestBed } from '@angular/core/testing';
import { Router, provideRouter } from '@angular/router';
import { of, throwError } from 'rxjs';
import { ErrorApi } from '../../../../core/models/error-api.model';
import { NotificacionService } from '../../../../core/services/notificacion.service';
import { EstadosProyectoService } from '../../../estados-proyecto/services/estados-proyecto.service';
import { PersonasService } from '../../../personas/services/personas.service';
import { ProyectoDto } from '../../models/proyecto.model';
import { ProyectosService } from '../../services/proyectos.service';
import { MENSAJE_PROYECTO_ARCHIVADO_NO_EDITABLE, ProyectoFormPage } from './proyecto-form.page';

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

const VALORES = {
  nombre: 'Portal clientes',
  descripcion: 'Nuevo portal de autogestión.',
  responsableId: 2,
  fechaInicio: '2026-10-01',
  fechaObjetivo: '2026-12-15',
  estadoId: null,
};

/** Tests de ProyectoFormPage (P2 alta / P4 edición; en la spec "ProyectoEditarPage"). */
describe('ProyectoFormPage', () => {
  const proyectos = { obtener: vi.fn(), crear: vi.fn(), editar: vi.fn() };
  const personas = { listar: vi.fn() };
  const estados = { listar: vi.fn() };
  let router: Router;
  let notificacion: NotificacionService;

  beforeEach(() => {
    vi.clearAllMocks();
    personas.listar.mockReturnValue(of([]));
    estados.listar.mockReturnValue(of([]));
    TestBed.configureTestingModule({
      imports: [ProyectoFormPage],
      providers: [
        provideRouter([]),
        { provide: ProyectosService, useValue: proyectos },
        { provide: PersonasService, useValue: personas },
        { provide: EstadosProyectoService, useValue: estados },
      ],
    });
    router = TestBed.inject(Router);
    notificacion = TestBed.inject(NotificacionService);
    vi.spyOn(router, 'navigate').mockResolvedValue(true);
  });

  /** Crea la página con el parámetro de ruta `id` (undefined = alta). */
  async function crear(id?: string): Promise<ProyectoFormPage> {
    const fixture = TestBed.createComponent(ProyectoFormPage);
    fixture.componentRef.setInput('id', id);
    await fixture.whenStable();
    return fixture.componentInstance;
  }

  // CA-24: proyecto archivado → redirige al detalle con el mensaje
  it('edición de un proyecto archivado redirige al detalle con el mensaje', async () => {
    const espiaInfo = vi.spyOn(notificacion, 'info');
    proyectos.obtener.mockReturnValue(of({ ...PROYECTO, archivado: true }));

    const pagina = await crear('3');

    expect(router.navigate).toHaveBeenCalledWith(['/proyectos', 3]);
    expect(espiaInfo).toHaveBeenCalledWith(MENSAJE_PROYECTO_ARCHIVADO_NO_EDITABLE);
    expect(pagina.datos()).toBeNull();
  });

  // P2: los estados se cargan con soloActivos=true para el texto del estado inicial
  it('alta carga personas y estados activos sin pedir el proyecto', async () => {
    await crear();

    expect(estados.listar).toHaveBeenCalledWith({ soloActivos: true });
    expect(proyectos.obtener).not.toHaveBeenCalled();
  });

  // CA-15, P2: guardar el alta crea, avisa con el código y navega al detalle
  it('guardar en alta llama a crear sin estado y navega al detalle', async () => {
    const espiaExito = vi.spyOn(notificacion, 'exito');
    proyectos.crear.mockReturnValue(of({ ...PROYECTO, id: 10, codigo: 'PRJ-0010' }));
    const pagina = await crear();

    pagina.guardar(VALORES);

    expect(proyectos.crear).toHaveBeenCalledWith({
      nombre: 'Portal clientes',
      descripcion: 'Nuevo portal de autogestión.',
      responsableId: 2,
      fechaInicio: '2026-10-01',
      fechaObjetivo: '2026-12-15',
    });
    expect(espiaExito).toHaveBeenCalledWith('Proyecto PRJ-0010 creado.');
    expect(router.navigate).toHaveBeenCalledWith(['/proyectos', 10]);
  });

  // CA-19, P4: guardar la edición envía PUT con estadoId
  it('guardar en edición llama a editar con el estado', async () => {
    proyectos.obtener.mockReturnValue(of(PROYECTO));
    proyectos.editar.mockReturnValue(of(PROYECTO));
    const pagina = await crear('3');

    pagina.guardar({ ...VALORES, estadoId: 3 });

    expect(proyectos.editar).toHaveBeenCalledWith(3, expect.objectContaining({ estadoId: 3 }));
    expect(router.navigate).toHaveBeenCalledWith(['/proyectos', 3]);
  });

  // CA-23: 409 al guardar se muestra arriba del formulario
  it('409 PROYECTO_ARCHIVADO al guardar se muestra como error general', async () => {
    proyectos.obtener.mockReturnValue(of(PROYECTO));
    proyectos.editar.mockReturnValue(
      throwError(() => new ErrorApi(409, 'PROYECTO_ARCHIVADO', 'El proyecto está archivado.')),
    );
    const pagina = await crear('3');

    pagina.guardar({ ...VALORES, estadoId: 3 });

    expect(pagina.errorServidor()).toEqual({ campos: {}, general: 'El proyecto está archivado.' });
    expect(pagina.enviando()).toBe(false);
  });

  // CA-28: id inválido → no encontrado
  it('edición con id no numérico muestra no encontrado sin llamar a la API', async () => {
    const pagina = await crear('abc');

    expect(pagina.noEncontrado()).toBe(true);
    expect(proyectos.obtener).not.toHaveBeenCalled();
  });
});
