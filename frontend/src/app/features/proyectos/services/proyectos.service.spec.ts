import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { API_URL } from '../../../core/config/api-url.token';
import {
  CrearProyectoRequest,
  EditarProyectoRequest,
  FILTROS_PROYECTOS_POR_DEFECTO,
} from '../models/proyecto.model';
import { ProyectosService } from './proyectos.service';

/** Tests de ProyectosService: URL, método, query params y body según el contrato (RF-001). */
describe('ProyectosService', () => {
  let servicio: ProyectosService;
  let http: HttpTestingController;

  const alta: CrearProyectoRequest = {
    nombre: 'Portal clientes',
    descripcion: 'Nuevo portal de autogestión.',
    responsableId: 2,
    fechaInicio: '2026-10-01',
    fechaObjetivo: '2026-12-15',
  };

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        { provide: API_URL, useValue: '/api' },
      ],
    });
    servicio = TestBed.inject(ProyectosService);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  // CA-25, CA-26: solo se envían los filtros definidos
  it('listar con defaults envía GET /api/proyectos sin texto, estado ni responsable', () => {
    servicio.listar(FILTROS_PROYECTOS_POR_DEFECTO).subscribe();

    const req = http.expectOne((r) => r.url === '/api/proyectos');
    expect(req.request.method).toBe('GET');
    expect(req.request.params.keys().sort()).toEqual(
      ['archivados', 'direccion', 'ordenarPor', 'pagina', 'tamanioPagina'].sort(),
    );
    expect(req.request.params.get('archivados')).toBe('excluir');
    req.flush({ items: [], pagina: 1, tamanioPagina: 20, total: 0 });
  });

  // CA-26: filtros de texto (recortado), estado y responsable
  it('listar con filtros envía texto recortado, estadoId y responsableId', () => {
    servicio
      .listar({ ...FILTROS_PROYECTOS_POR_DEFECTO, texto: '  prj-0003 ', estadoId: 3, responsableId: 2 })
      .subscribe();

    const req = http.expectOne((r) => r.url === '/api/proyectos');
    expect(req.request.params.get('texto')).toBe('prj-0003');
    expect(req.request.params.get('estadoId')).toBe('3');
    expect(req.request.params.get('responsableId')).toBe('2');
    req.flush({ items: [], pagina: 1, tamanioPagina: 20, total: 0 });
  });

  // Contrato POST /api/proyectos (sin estadoId, RN-18)
  it('crear envía POST /api/proyectos con el body exacto', () => {
    servicio.crear(alta).subscribe();

    const req = http.expectOne('/api/proyectos');
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual(alta);
    req.flush({});
  });

  // Contrato PUT /api/proyectos/{id}
  it('editar envía PUT /api/proyectos/3 con el body exacto', () => {
    const edicion: EditarProyectoRequest = { ...alta, estadoId: 3 };

    servicio.editar(3, edicion).subscribe();

    const req = http.expectOne('/api/proyectos/3');
    expect(req.request.method).toBe('PUT');
    expect(req.request.body).toEqual(edicion);
    req.flush({});
  });

  // RN-20: POST /api/proyectos/{id}/archivar sin body
  it('archivar envía POST /api/proyectos/3/archivar sin body', () => {
    servicio.archivar(3).subscribe();

    const req = http.expectOne('/api/proyectos/3/archivar');
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toBeNull();
    req.flush({});
  });

  // RN-24: el detalle se obtiene por id (también archivados)
  it('obtener envía GET /api/proyectos/3', () => {
    servicio.obtener(3).subscribe();

    const req = http.expectOne('/api/proyectos/3');
    expect(req.request.method).toBe('GET');
    req.flush({});
  });
});
