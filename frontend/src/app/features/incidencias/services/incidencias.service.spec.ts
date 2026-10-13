import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { API_URL } from '../../../core/config/api-url.token';
import {
  CambiarTipoIncidenciaRequest,
  CrearIncidenciaRequest,
  EditarIncidenciaRequest,
} from '../models/incidencia.model';
import { IncidenciasService } from './incidencias.service';

/** Tests de IncidenciasService: URL, método, query params y body según el contrato (RF-002). */
describe('IncidenciasService', () => {
  let servicio: IncidenciasService;
  let http: HttpTestingController;

  const alta: CrearIncidenciaRequest = {
    tipoId: 3,
    titulo: 'Validar tarjeta',
    descripcion: 'Validar número y vencimiento.',
    prioridadId: null,
    responsableId: 2,
    fechaVencimiento: '2026-11-20',
    agrupacionId: null,
    padreId: 15,
    estimacion: null,
  };

  const edicion: EditarIncidenciaRequest = {
    titulo: 'Pagar con tarjeta de crédito',
    descripcion: 'Como cliente quiero pagar con tarjeta...',
    estadoId: 3,
    prioridadId: 3,
    responsableId: null,
    fechaVencimiento: '2026-11-25',
    agrupacionId: null,
    padreId: 3,
    estimacion: null,
  };

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        { provide: API_URL, useValue: '/api' },
      ],
    });
    servicio = TestBed.inject(IncidenciasService);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  // CA-19: tipoId repetible y sinResponsable=true
  it('listar(1, {tipoIds:[1,2], sinResponsable:true}) envía tipoId repetido y sinResponsable', () => {
    servicio.listar(1, { tipoIds: [1, 2], sinResponsable: true }).subscribe();

    const req = http.expectOne(
      (r) =>
        r.urlWithParams === '/api/proyectos/1/incidencias?tipoId=1&tipoId=2&sinResponsable=true',
    );
    expect(req.request.method).toBe('GET');
    req.flush({ items: [], pagina: 1, tamanioPagina: 20, total: 0 });
  });

  // CA-19: los booleanos en false y los filtros vacíos no se envían
  it('listar sin filtros no envía query params', () => {
    servicio
      .listar(1, { tipoIds: [], sinResponsable: false, sinAgrupacion: false, texto: '  ' })
      .subscribe();

    const req = http.expectOne((r) => r.url === '/api/proyectos/1/incidencias');
    expect(req.request.params.keys()).toEqual([]);
    req.flush({ items: [], pagina: 1, tamanioPagina: 20, total: 0 });
  });

  // CA-19: "Sin agrupación" envía sinAgrupacion=true; texto recortado; orden y página
  it('listar con sinAgrupacion, texto, orden y paginación envía esos parámetros', () => {
    servicio
      .listar(1, {
        tipoIds: [],
        texto: ' PRJ-0001-1 ',
        sinAgrupacion: true,
        pagina: 2,
        tamanioPagina: 50,
        ordenarPor: 'titulo',
        direccion: 'asc',
      })
      .subscribe();

    const req = http.expectOne((r) => r.url === '/api/proyectos/1/incidencias');
    expect(req.request.params.get('texto')).toBe('PRJ-0001-1');
    expect(req.request.params.get('sinAgrupacion')).toBe('true');
    expect(req.request.params.get('pagina')).toBe('2');
    expect(req.request.params.get('tamanioPagina')).toBe('50');
    expect(req.request.params.get('ordenarPor')).toBe('titulo');
    expect(req.request.params.get('direccion')).toBe('asc');
    req.flush({ items: [], pagina: 2, tamanioPagina: 50, total: 0 });
  });

  // CA-34: árbol del proyecto
  it('arbol(1) envía GET /api/proyectos/1/incidencias/arbol', () => {
    servicio.arbol(1).subscribe();

    const req = http.expectOne('/api/proyectos/1/incidencias/arbol');
    expect(req.request.method).toBe('GET');
    req.flush([]);
  });

  // CA-35: detalle
  it('obtener(1, 15) envía GET /api/proyectos/1/incidencias/15', () => {
    servicio.obtener(1, 15).subscribe();

    const req = http.expectOne('/api/proyectos/1/incidencias/15');
    expect(req.request.method).toBe('GET');
    req.flush({});
  });

  // RN-20, CA-13: alta con el body del contrato (sin estado)
  it('crear envía POST /api/proyectos/1/incidencias con el body exacto', () => {
    servicio.crear(1, alta).subscribe();

    const req = http.expectOne('/api/proyectos/1/incidencias');
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual(alta);
    expect(req.request.body).not.toHaveProperty('estadoId');
    req.flush({});
  });

  // CA-17: edición con el body del contrato (sin tipo)
  it('editar envía PUT /api/proyectos/1/incidencias/15 con el body exacto', () => {
    servicio.editar(1, 15, edicion).subscribe();

    const req = http.expectOne('/api/proyectos/1/incidencias/15');
    expect(req.request.method).toBe('PUT');
    expect(req.request.body).toEqual(edicion);
    expect(req.request.body).not.toHaveProperty('tipoId');
    req.flush({});
  });

  // RN-02, CA-37: cambiar tipo
  it('cambiarTipo envía POST .../15/cambiar-tipo con tipoId y estadoId', () => {
    const request: CambiarTipoIncidenciaRequest = { tipoId: 5, estadoId: null };

    servicio.cambiarTipo(1, 15, request).subscribe();

    const req = http.expectOne('/api/proyectos/1/incidencias/15/cambiar-tipo');
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({ tipoId: 5, estadoId: null });
    req.flush({});
  });

  // RN-34, CA-22: no hay método para eliminar incidencias
  it('no expone un método eliminar', () => {
    expect((servicio as unknown as Record<string, unknown>)['eliminar']).toBeUndefined();
  });

  // RF-005 CA-44: orden por estimación, sin otros parámetros nuevos respecto de RF-002
  it('listar con ordenarPor=estimacion envía solo ese orden', () => {
    servicio.listar(1, { tipoIds: [], sinResponsable: false, sinAgrupacion: false, ordenarPor: 'estimacion' }).subscribe();

    const req = http.expectOne((r) => r.url === '/api/proyectos/1/incidencias');
    expect(req.request.params.keys()).toEqual(['ordenarPor']);
    expect(req.request.params.get('ordenarPor')).toBe('estimacion');
    req.flush({ items: [], pagina: 1, tamanioPagina: 20, total: 0 });
  });

  // RF-005 RN-22, CA-28: árbol con ámbito agrupación
  it('arbol(1, {agrupacionId:8}) envía agrupacionId', () => {
    servicio.arbol(1, { agrupacionId: 8 }).subscribe();

    const req = http.expectOne((r) => r.urlWithParams === '/api/proyectos/1/incidencias/arbol?agrupacionId=8');
    expect(req.request.method).toBe('GET');
    req.flush([]);
  });

  // RF-005 RN-01: árbol con ámbito "sin agrupación" (gana sobre agrupacionId)
  it('arbol(1, {agrupacionId:8, sinAgrupacion:true}) envía solo sinAgrupacion', () => {
    servicio.arbol(1, { agrupacionId: 8, sinAgrupacion: true }).subscribe();

    const req = http.expectOne((r) => r.url === '/api/proyectos/1/incidencias/arbol');
    expect(req.request.params.keys()).toEqual(['sinAgrupacion']);
    req.flush([]);
  });

  // RF-005 RN-34, CA-41: el alta envía la estimación numérica
  it('crear con estimación 2.5 la envía en el body', () => {
    servicio.crear(1, { ...alta, estimacion: 2.5 }).subscribe();

    const req = http.expectOne('/api/proyectos/1/incidencias');
    expect(req.request.body.estimacion).toBe(2.5);
    req.flush({});
  });

  // RF-005 RN-34, CA-43: la edición envía siempre estimacion (null deja sin estimar)
  it('editar con estimación null la envía explícitamente como null', () => {
    servicio.editar(1, 15, { ...edicion, estimacion: null }).subscribe();

    const req = http.expectOne('/api/proyectos/1/incidencias/15');
    expect(req.request.body).toHaveProperty('estimacion', null);
    req.flush({});
  });
});
