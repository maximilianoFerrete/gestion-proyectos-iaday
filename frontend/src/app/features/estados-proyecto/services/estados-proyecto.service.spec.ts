import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { API_URL } from '../../../core/config/api-url.token';
import { EstadosProyectoService } from './estados-proyecto.service';

/** Tests de EstadosProyectoService según el contrato de /api/estados-proyecto. */
describe('EstadosProyectoService', () => {
  let servicio: EstadosProyectoService;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        { provide: API_URL, useValue: '/api' },
      ],
    });
    servicio = TestBed.inject(EstadosProyectoService);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  // CA-10: POST /api/estados-proyecto/2/marcar-inicial
  it('marcarInicial(2) envía POST /api/estados-proyecto/2/marcar-inicial', () => {
    servicio.marcarInicial(2).subscribe();

    const req = http.expectOne('/api/estados-proyecto/2/marcar-inicial');
    expect(req.request.method).toBe('POST');
    req.flush({});
  });

  // CA-12, CA-13: acciones desactivar y activar
  it('desactivar y activar envían POST a sus acciones', () => {
    servicio.desactivar(4).subscribe();
    servicio.activar(4).subscribe();

    expect(http.expectOne('/api/estados-proyecto/4/desactivar').request.method).toBe('POST');
    expect(http.expectOne('/api/estados-proyecto/4/activar').request.method).toBe('POST');
  });

  // P2: estados activos para el texto del estado inicial
  it('listar({ soloActivos: true }) envía soloActivos=true', () => {
    servicio.listar({ soloActivos: true }).subscribe();

    const req = http.expectOne((r) => r.url === '/api/estados-proyecto');
    expect(req.request.params.get('soloActivos')).toBe('true');
    req.flush([]);
  });

  // CA-06: sin filtros no envía parámetros
  it('listar() sin filtros no envía query params', () => {
    servicio.listar().subscribe();

    const req = http.expectOne((r) => r.url === '/api/estados-proyecto');
    expect(req.request.params.keys()).toEqual([]);
    req.flush([]);
  });

  // CA-07: POST con el body del contrato
  it('crear envía POST /api/estados-proyecto con el body exacto', () => {
    const body = { nombre: 'En revisión', descripcion: null, orden: 35, esFinal: false };

    servicio.crear(body).subscribe();

    const req = http.expectOne('/api/estados-proyecto');
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual(body);
    req.flush({});
  });
});
