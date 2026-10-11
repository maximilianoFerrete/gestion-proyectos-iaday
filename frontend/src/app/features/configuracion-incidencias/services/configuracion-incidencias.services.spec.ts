import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { API_URL } from '../../../core/config/api-url.token';
import { EstadosIncidenciaService } from './estados-incidencia.service';
import { FlujosIncidenciaService } from './flujos-incidencia.service';
import { NivelesIncidenciaService } from './niveles-incidencia.service';
import { PrioridadesIncidenciaService } from './prioridades-incidencia.service';
import { RelacionesTiposIncidenciaService } from './relaciones-tipos-incidencia.service';
import { TiposIncidenciaService } from './tipos-incidencia.service';

/**
 * Tests de los clientes HTTP de configuración de incidencias (C1–C5): URL, método y body exactos
 * del contrato de RF-002.
 */
describe('Servicios de configuración de incidencias', () => {
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        { provide: API_URL, useValue: '/api' },
      ],
    });
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  /** Verifica una única request con método, URL (con params) y body. */
  function esperar(metodo: string, url: string, body: unknown = null): void {
    const req = http.expectOne((r) => r.urlWithParams === url);
    expect(req.request.method).toBe(metodo);
    expect(req.request.body).toEqual(body);
    req.flush({});
  }

  describe('NivelesIncidenciaService', () => {
    // RN-04: listar, obtener, crear y editar
    it('usa las URLs de /api/niveles-incidencia', () => {
      const servicio = TestBed.inject(NivelesIncidenciaService);

      servicio.listar().subscribe();
      esperar('GET', '/api/niveles-incidencia');
      servicio.obtener(2).subscribe();
      esperar('GET', '/api/niveles-incidencia/2');
      servicio.crear({ numero: 4, nombre: 'Nivel 4', descripcion: null }).subscribe();
      esperar('POST', '/api/niveles-incidencia', {
        numero: 4,
        nombre: 'Nivel 4',
        descripcion: null,
      });
      servicio.editar(4, { nombre: 'Nivel cuatro', descripcion: null }).subscribe();
      esperar('PUT', '/api/niveles-incidencia/4', { nombre: 'Nivel cuatro', descripcion: null });
    });
  });

  describe('TiposIncidenciaService', () => {
    // RN-05 a RN-07: listado con filtros y acciones
    it('usa las URLs de /api/tipos-incidencia', () => {
      const servicio = TestBed.inject(TiposIncidenciaService);
      const alta = {
        nombre: 'Spike',
        descripcion: 'Investigación acotada.',
        nivelId: 3,
        flujoId: 1,
      };

      servicio.listar({ soloActivos: true, nivelId: 3 }).subscribe();
      esperar('GET', '/api/tipos-incidencia?soloActivos=true&nivelId=3');
      servicio.obtener(6).subscribe();
      esperar('GET', '/api/tipos-incidencia/6');
      servicio.crear(alta).subscribe();
      esperar('POST', '/api/tipos-incidencia', alta);
      servicio.editar(6, { nombre: 'Spike técnico', descripcion: null, flujoId: 2 }).subscribe();
      esperar('PUT', '/api/tipos-incidencia/6', {
        nombre: 'Spike técnico',
        descripcion: null,
        flujoId: 2,
      });
      servicio.desactivar(6).subscribe();
      esperar('POST', '/api/tipos-incidencia/6/desactivar');
      servicio.activar(6).subscribe();
      esperar('POST', '/api/tipos-incidencia/6/activar');
    });

    // Contrato GET tipos: sin filtros no se envían parámetros
    it('listar sin filtros envía GET /api/tipos-incidencia sin parámetros', () => {
      TestBed.inject(TiposIncidenciaService).listar().subscribe();

      esperar('GET', '/api/tipos-incidencia');
    });
  });

  describe('RelacionesTiposIncidenciaService', () => {
    // RN-08: listar con filtros y crear
    it('usa las URLs de /api/relaciones-tipos-incidencia', () => {
      const servicio = TestBed.inject(RelacionesTiposIncidenciaService);

      servicio.listar({ tipoPadreId: 2 }).subscribe();
      esperar('GET', '/api/relaciones-tipos-incidencia?tipoPadreId=2');
      servicio.listar({ tipoHijoId: 4 }).subscribe();
      esperar('GET', '/api/relaciones-tipos-incidencia?tipoHijoId=4');
      servicio.crear({ tipoPadreId: 2, tipoHijoId: 6 }).subscribe();
      esperar('POST', '/api/relaciones-tipos-incidencia', { tipoPadreId: 2, tipoHijoId: 6 });
    });

    // RN-09, CA-06: sin métodos de editar ni eliminar
    it('no expone métodos editar ni eliminar', () => {
      const servicio = TestBed.inject(RelacionesTiposIncidenciaService) as unknown as Record<
        string,
        unknown
      >;

      expect(servicio['editar']).toBeUndefined();
      expect(servicio['eliminar']).toBeUndefined();
    });
  });

  describe('EstadosIncidenciaService', () => {
    // RN-10, RN-11: listado y acciones
    it('usa las URLs de /api/estados-incidencia', () => {
      const servicio = TestBed.inject(EstadosIncidenciaService);
      const request = { nombre: 'En revisión', descripcion: null, orden: 25 };

      servicio.listar({ soloActivos: true }).subscribe();
      esperar('GET', '/api/estados-incidencia?soloActivos=true');
      servicio.crear(request).subscribe();
      esperar('POST', '/api/estados-incidencia', request);
      servicio.editar(5, request).subscribe();
      esperar('PUT', '/api/estados-incidencia/5', request);
      servicio.desactivar(5).subscribe();
      esperar('POST', '/api/estados-incidencia/5/desactivar');
      servicio.activar(5).subscribe();
      esperar('POST', '/api/estados-incidencia/5/activar');
    });
  });

  describe('FlujosIncidenciaService', () => {
    // RN-13, RN-14: listar, crear y editar (reemplazo completo)
    it('usa las URLs de /api/flujos-incidencia', () => {
      const servicio = TestBed.inject(FlujosIncidenciaService);
      const request = {
        nombre: 'Flujo con revisión',
        descripcion: null,
        estados: [
          { estadoId: 1, esInicial: true, esFinal: false },
          { estadoId: 3, esInicial: false, esFinal: true },
        ],
      };

      servicio.listar().subscribe();
      esperar('GET', '/api/flujos-incidencia');
      servicio.obtener(1).subscribe();
      esperar('GET', '/api/flujos-incidencia/1');
      servicio.crear(request).subscribe();
      esperar('POST', '/api/flujos-incidencia', request);
      servicio.editar(1, request).subscribe();
      esperar('PUT', '/api/flujos-incidencia/1', request);
    });
  });

  describe('PrioridadesIncidenciaService', () => {
    // RN-17: listado y acciones
    it('usa las URLs de /api/prioridades-incidencia', () => {
      const servicio = TestBed.inject(PrioridadesIncidenciaService);
      const request = { nombre: 'Bloqueante', descripcion: null, orden: 50 };

      servicio.listar({ soloActivas: true }).subscribe();
      esperar('GET', '/api/prioridades-incidencia?soloActivas=true');
      servicio.crear(request).subscribe();
      esperar('POST', '/api/prioridades-incidencia', request);
      servicio.editar(5, request).subscribe();
      esperar('PUT', '/api/prioridades-incidencia/5', request);
      servicio.marcarPredeterminada(3).subscribe();
      esperar('POST', '/api/prioridades-incidencia/3/marcar-predeterminada');
      servicio.desactivar(5).subscribe();
      esperar('POST', '/api/prioridades-incidencia/5/desactivar');
      servicio.activar(5).subscribe();
      esperar('POST', '/api/prioridades-incidencia/5/activar');
    });
  });
});
