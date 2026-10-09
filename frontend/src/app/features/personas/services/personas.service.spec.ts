import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { API_URL } from '../../../core/config/api-url.token';
import { PersonasService } from './personas.service';

/** Tests de PersonasService según el contrato de /api/personas. */
describe('PersonasService', () => {
  let servicio: PersonasService;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        { provide: API_URL, useValue: '/api' },
      ],
    });
    servicio = TestBed.inject(PersonasService);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  // P6: incluirInactivas=true
  it('listar({ incluirInactivas: true }) envía incluirInactivas=true', () => {
    servicio.listar({ incluirInactivas: true }).subscribe();

    const req = http.expectOne((r) => r.url === '/api/personas');
    expect(req.request.method).toBe('GET');
    expect(req.request.params.get('incluirInactivas')).toBe('true');
    req.flush([]);
  });

  // P6: texto recortado y sin parámetros vacíos
  it('listar con texto de solo espacios no envía texto', () => {
    servicio.listar({ texto: '   ' }).subscribe();

    const req = http.expectOne((r) => r.url === '/api/personas');
    expect(req.request.params.has('texto')).toBe(false);
    req.flush([]);
  });

  // CA-01: POST con el body del contrato
  it('crear envía POST /api/personas con el body exacto', () => {
    const body = { nombre: 'Ana Gómez', email: 'Ana.Gomez@Empresa.com', esAdmin: false };

    servicio.crear(body).subscribe();

    const req = http.expectOne('/api/personas');
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual(body);
    req.flush({});
  });

  // CA-04: acción desactivar
  it('desactivar(1) envía POST /api/personas/1/desactivar', () => {
    servicio.desactivar(1).subscribe();

    expect(http.expectOne('/api/personas/1/desactivar').request.method).toBe('POST');
  });
});
