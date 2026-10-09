import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { API_URL } from '../../../core/config/api-url.token';
import { AgrupacionesService } from './agrupaciones.service';

/** Tests de AgrupacionesService: rutas anidadas bajo el proyecto (RN-28, RN-29). */
describe('AgrupacionesService', () => {
  let servicio: AgrupacionesService;
  let http: HttpTestingController;
  const body = { nombre: 'Módulo pagos', descripcion: 'Incidencias del circuito de pagos.' };

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        { provide: API_URL, useValue: '/api' },
      ],
    });
    servicio = TestBed.inject(AgrupacionesService);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  // CA-31: DELETE /api/proyectos/3/agrupaciones/8
  it('eliminar(3, 8) envía DELETE /api/proyectos/3/agrupaciones/8', () => {
    servicio.eliminar(3, 8).subscribe();

    const req = http.expectOne('/api/proyectos/3/agrupaciones/8');
    expect(req.request.method).toBe('DELETE');
    req.flush(null, { status: 204, statusText: 'No Content' });
  });

  // CA-29: POST con nombre y descripción (proyectoId en la URL)
  it('crear envía POST /api/proyectos/3/agrupaciones con el body exacto', () => {
    servicio.crear(3, body).subscribe();

    const req = http.expectOne('/api/proyectos/3/agrupaciones');
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual(body);
    req.flush({});
  });

  // CA-30: PUT /api/proyectos/3/agrupaciones/8
  it('editar envía PUT /api/proyectos/3/agrupaciones/8 con el body exacto', () => {
    servicio.editar(3, 8, body).subscribe();

    const req = http.expectOne('/api/proyectos/3/agrupaciones/8');
    expect(req.request.method).toBe('PUT');
    expect(req.request.body).toEqual(body);
    req.flush({});
  });

  // CA-33: GET de la lista del proyecto
  it('listar envía GET /api/proyectos/3/agrupaciones', () => {
    servicio.listar(3).subscribe();

    const req = http.expectOne('/api/proyectos/3/agrupaciones');
    expect(req.request.method).toBe('GET');
    req.flush([]);
  });
});
