import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { API_URL } from '../../../core/config/api-url.token';
import { SeguimientoService } from './seguimiento.service';

/** Tests de SeguimientoService: URL, método, query params y body según el contrato (RF-005). */
describe('SeguimientoService', () => {
  let servicio: SeguimientoService;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        { provide: API_URL, useValue: '/api' },
      ],
    });
    servicio = TestBed.inject(SeguimientoService);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  // Contrato tablero general: nivelId repetido y ordenarPor
  it('tableroProyectos({nivelIds:[1,2], ordenarPor:"vencidas"}) envía nivelId repetido', () => {
    servicio.tableroProyectos({ nivelIds: [1, 2], ordenarPor: 'vencidas' }).subscribe();

    const req = http.expectOne(
      (r) => r.urlWithParams === '/api/seguimiento/proyectos?nivelId=1&nivelId=2&ordenarPor=vencidas',
    );
    expect(req.request.method).toBe('GET');
    req.flush({});
  });

  // RN-01: ámbito "sin agrupación"
  it('seguimiento(1, {sinAgrupacion:true}) envía sinAgrupacion=true', () => {
    servicio.seguimiento(1, { sinAgrupacion: true, nivelIds: [], tipoIds: [] }).subscribe();

    const req = http.expectOne(
      (r) => r.urlWithParams === '/api/proyectos/1/seguimiento?sinAgrupacion=true',
    );
    expect(req.request.method).toBe('GET');
    req.flush({});
  });

  // RN-01: sinAgrupacion gana sobre agrupacionId (excluyentes)
  it('seguimiento con agrupacionId y sinAgrupacion envía solo sinAgrupacion', () => {
    servicio.seguimiento(1, { agrupacionId: 8, sinAgrupacion: true, nivelIds: [], tipoIds: [] }).subscribe();

    const req = http.expectOne((r) => r.url === '/api/proyectos/1/seguimiento');
    expect(req.request.params.keys()).toEqual(['sinAgrupacion']);
    req.flush({});
  });

  // RN-07: tablero con ámbito y alcance
  it('tablero(1, {agrupacionId:8, nivelIds:[3]}) envía agrupacionId y nivelId', () => {
    servicio.tablero(1, { agrupacionId: 8, nivelIds: [3] }).subscribe();

    const req = http.expectOne(
      (r) => r.urlWithParams === '/api/proyectos/1/seguimiento/tablero?agrupacionId=8&nivelId=3',
    );
    expect(req.request.method).toBe('GET');
    req.flush({});
  });

  // RN-21: los tres filtros del tablero llegan como query params
  it('tablero con indicador, estadoId y texto envía los tres filtros', () => {
    servicio.tablero(1, { indicador: 'vencidas', estadoId: 4, texto: ' pago ' }).subscribe();

    const req = http.expectOne(
      (r) =>
        r.urlWithParams ===
        '/api/proyectos/1/seguimiento/tablero?indicador=vencidas&estadoId=4&texto=pago',
    );
    req.flush({});
  });

  // RN-21: filtros vacíos o null no se envían
  it('tablero con filtros null o texto vacío no envía query params', () => {
    servicio.tablero(1, { indicador: null, estadoId: null, texto: '   ', agrupacionId: null }).subscribe();

    const req = http.expectOne((r) => r.url === '/api/proyectos/1/seguimiento/tablero');
    expect(req.request.params.keys()).toEqual([]);
    req.flush({});
  });

  // RN-30: GET de la configuración del tablero
  it('obtenerConfiguracionTablero(1) envía GET .../tablero/configuracion', () => {
    servicio.obtenerConfiguracionTablero(1).subscribe();

    const req = http.expectOne('/api/proyectos/1/seguimiento/tablero/configuracion');
    expect(req.request.method).toBe('GET');
    req.flush({});
  });

  // RN-31, CA-32: PUT de la configuración con los tres booleanos
  it('guardarConfiguracionTablero envía PUT con los tres booleanos', () => {
    const request = { mostrarEstado: true, mostrarEstimacion: false, mostrarResponsable: true };

    servicio.guardarConfiguracionTablero(1, request).subscribe();

    const req = http.expectOne('/api/proyectos/1/seguimiento/tablero/configuracion');
    expect(req.request.method).toBe('PUT');
    expect(req.request.body).toEqual(request);
    req.flush({});
  });
});
