import { HttpClient, provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { Almacenamiento, ALMACENAMIENTO } from '../config/almacenamiento.token';
import { API_URL } from '../config/api-url.token';
import { errorInterceptor } from '../interceptors/error.interceptor';
import { PERMISOS } from '../models/permisos.model';
import {
  HEADER_USUARIO_ID,
  usuarioActualInterceptor,
} from '../interceptors/usuario-actual.interceptor';
import { NotificacionService } from './notificacion.service';
import {
  CLAVE_USUARIO_ACTUAL,
  MENSAJE_USUARIO_INVALIDO,
  UsuarioActualService,
} from './usuario-actual.service';

/** Almacenamiento en memoria que simula localStorage entre "recargas". */
function crearAlmacenamiento(inicial: Record<string, string> = {}): Almacenamiento {
  const datos = new Map<string, string>(Object.entries(inicial));
  return {
    getItem: (clave) => datos.get(clave) ?? null,
    setItem: (clave, valor) => void datos.set(clave, valor),
    removeItem: (clave) => void datos.delete(clave),
  };
}

/** Configura TestBed con los interceptores reales y el almacenamiento indicado. */
function configurar(almacenamiento: Almacenamiento): void {
  TestBed.configureTestingModule({
    providers: [
      provideHttpClient(withInterceptors([usuarioActualInterceptor, errorInterceptor])),
      provideHttpClientTesting(),
      { provide: API_URL, useValue: '/api' },
      { provide: ALMACENAMIENTO, useValue: almacenamiento },
    ],
  });
}

/**
 * Tests de UsuarioActualService y usuarioActualInterceptor (spec 0.4, CA-34, CA-35).
 * RF-002 0.1: `permisos` y `tienePermiso` (CA-03).
 */
describe('UsuarioActualService', () => {
  afterEach(() => TestBed.inject(HttpTestingController).verify());

  // CA-34: seleccionar persiste en localStorage
  it('seleccionar(id) guarda el id en el almacenamiento y lo expone', () => {
    const almacenamiento = crearAlmacenamiento();
    configurar(almacenamiento);
    const servicio = TestBed.inject(UsuarioActualService);

    servicio.seleccionar(2);

    expect(almacenamiento.getItem(CLAVE_USUARIO_ACTUAL)).toBe('2');
    expect(servicio.personaId()).toBe(2);
    TestBed.inject(HttpTestingController).expectOne('/api/usuario-actual').flush({
      identificado: true,
      personaId: 2,
      nombre: 'Ana',
      email: 'ana@empresa.com',
      esAdmin: false,
    });
  });

  // CA-34: la selección persiste al recargar (nuevo servicio con el mismo almacenamiento)
  it('al reiniciar recupera la selección guardada', () => {
    configurar(crearAlmacenamiento({ [CLAVE_USUARIO_ACTUAL]: '2' }));

    const servicio = TestBed.inject(UsuarioActualService);

    expect(servicio.personaId()).toBe(2);
  });

  // Spec 0.4: un valor guardado inválido se ignora
  it('al reiniciar ignora un id guardado no numérico', () => {
    configurar(crearAlmacenamiento({ [CLAVE_USUARIO_ACTUAL]: 'abc' }));

    const servicio = TestBed.inject(UsuarioActualService);

    expect(servicio.personaId()).toBeNull();
  });

  // CA-34: "Sin usuario" borra la clave
  it('seleccionar(null) borra la selección guardada', () => {
    const almacenamiento = crearAlmacenamiento({ [CLAVE_USUARIO_ACTUAL]: '2' });
    configurar(almacenamiento);
    const servicio = TestBed.inject(UsuarioActualService);

    servicio.seleccionar(null);

    expect(almacenamiento.getItem(CLAVE_USUARIO_ACTUAL)).toBeNull();
    expect(servicio.personaId()).toBeNull();
    TestBed.inject(HttpTestingController).expectOne('/api/usuario-actual').flush({
      identificado: false,
      personaId: null,
      nombre: null,
      email: null,
      esAdmin: false,
    });
  });

  // CA-35: 401 de /api/usuario-actual limpia la selección y avisa
  it('ante 401 USUARIO_INVALIDO limpia la selección y muestra el mensaje', () => {
    const almacenamiento = crearAlmacenamiento({ [CLAVE_USUARIO_ACTUAL]: '5' });
    configurar(almacenamiento);
    const servicio = TestBed.inject(UsuarioActualService);
    const notificacion = TestBed.inject(NotificacionService);
    const espiaError = vi.spyOn(notificacion, 'error');

    servicio.verificar();
    TestBed.inject(HttpTestingController)
      .expectOne('/api/usuario-actual')
      .flush(
        {
          status: 401,
          codigo: 'USUARIO_INVALIDO',
          detail: 'El usuario 5 no existe o está inactivo.',
        },
        { status: 401, statusText: 'Unauthorized' },
      );

    expect(servicio.personaId()).toBeNull();
    expect(almacenamiento.getItem(CLAVE_USUARIO_ACTUAL)).toBeNull();
    expect(espiaError).toHaveBeenCalledWith(MENSAJE_USUARIO_INVALIDO);
  });

  // CA-34: esAdmin refleja la respuesta de la API para la selección vigente
  it('verificar con admin expone esAdmin = true', () => {
    configurar(crearAlmacenamiento({ [CLAVE_USUARIO_ACTUAL]: '1' }));
    const servicio = TestBed.inject(UsuarioActualService);

    servicio.verificar();
    TestBed.inject(HttpTestingController).expectOne('/api/usuario-actual').flush({
      identificado: true,
      personaId: 1,
      nombre: 'Administrador',
      email: 'admin@gestionproyectos.local',
      esAdmin: true,
    });

    expect(servicio.esAdmin()).toBe(true);
  });

  // RF-002 0.1, CA-03: tienePermiso refleja los permisos de la respuesta
  it('tienePermiso devuelve true solo para los permisos informados por la API', () => {
    configurar(crearAlmacenamiento({ [CLAVE_USUARIO_ACTUAL]: '1' }));
    const servicio = TestBed.inject(UsuarioActualService);

    servicio.verificar();
    TestBed.inject(HttpTestingController)
      .expectOne('/api/usuario-actual')
      .flush({
        identificado: true,
        personaId: 1,
        nombre: 'Administrador',
        email: 'admin@gestionproyectos.local',
        esAdmin: true,
        permisos: [PERMISOS.configuracionIncidencias],
      });

    expect(servicio.tienePermiso(PERMISOS.configuracionIncidencias)).toBe(true);
    expect(servicio.tienePermiso(PERMISOS.incidenciasCambiarTipo)).toBe(false);
  });

  // RF-002 0.1, CA-03: no admin con permisos [] → sin permisos
  it('tienePermiso devuelve false con permisos vacío', () => {
    configurar(crearAlmacenamiento({ [CLAVE_USUARIO_ACTUAL]: '2' }));
    const servicio = TestBed.inject(UsuarioActualService);

    servicio.verificar();
    TestBed.inject(HttpTestingController).expectOne('/api/usuario-actual').flush({
      identificado: true,
      personaId: 2,
      nombre: 'Ana',
      email: 'ana@empresa.com',
      esAdmin: false,
      permisos: [],
    });

    expect(servicio.tienePermiso(PERMISOS.configuracionIncidencias)).toBe(false);
  });

  // RF-002 0.1: tolera respuestas sin `permisos` (backend anterior)
  it('tienePermiso devuelve false si la respuesta no trae permisos', () => {
    configurar(crearAlmacenamiento({ [CLAVE_USUARIO_ACTUAL]: '1' }));
    const servicio = TestBed.inject(UsuarioActualService);

    servicio.verificar();
    TestBed.inject(HttpTestingController).expectOne('/api/usuario-actual').flush({
      identificado: true,
      personaId: 1,
      nombre: 'Administrador',
      email: 'admin@gestionproyectos.local',
      esAdmin: true,
    });

    expect(servicio.tienePermiso(PERMISOS.incidenciasCambiarTipo)).toBe(false);
  });

  // RF-002 0.1: al cambiar a "Sin usuario" se pierden los permisos
  it('seleccionar(null) limpia los permisos del usuario anterior', () => {
    configurar(crearAlmacenamiento({ [CLAVE_USUARIO_ACTUAL]: '1' }));
    const servicio = TestBed.inject(UsuarioActualService);
    const http = TestBed.inject(HttpTestingController);
    servicio.verificar();
    http.expectOne('/api/usuario-actual').flush({
      identificado: true,
      personaId: 1,
      nombre: 'Administrador',
      email: 'admin@gestionproyectos.local',
      esAdmin: true,
      permisos: [PERMISOS.configuracionIncidencias, PERMISOS.incidenciasCambiarTipo],
    });

    servicio.seleccionar(null);

    expect(servicio.tienePermiso(PERMISOS.configuracionIncidencias)).toBe(false);
    http.expectOne('/api/usuario-actual').flush({
      identificado: false,
      personaId: null,
      nombre: null,
      email: null,
      esAdmin: false,
      permisos: [],
    });
  });
});

describe('usuarioActualInterceptor', () => {
  afterEach(() => TestBed.inject(HttpTestingController).verify());

  // CA-34: con usuario seleccionado agrega X-Usuario-Id
  it('con usuario seleccionado agrega X-Usuario-Id a las requests de la API', () => {
    configurar(crearAlmacenamiento({ [CLAVE_USUARIO_ACTUAL]: '2' }));

    TestBed.inject(HttpClient).get('/api/proyectos').subscribe();

    const req = TestBed.inject(HttpTestingController).expectOne('/api/proyectos');
    expect(req.request.headers.get(HEADER_USUARIO_ID)).toBe('2');
    req.flush({});
  });

  // CA-34: con "Sin usuario" no se envía el header
  it('sin usuario seleccionado no agrega el header', () => {
    configurar(crearAlmacenamiento());

    TestBed.inject(HttpClient).get('/api/proyectos').subscribe();

    const req = TestBed.inject(HttpTestingController).expectOne('/api/proyectos');
    expect(req.request.headers.has(HEADER_USUARIO_ID)).toBe(false);
    req.flush({});
  });

  // Spec 0.4: el header solo va a la API propia
  it('no agrega el header a URLs fuera de la API', () => {
    configurar(crearAlmacenamiento({ [CLAVE_USUARIO_ACTUAL]: '2' }));

    TestBed.inject(HttpClient).get('https://otro.sitio/recurso').subscribe();

    const req = TestBed.inject(HttpTestingController).expectOne('https://otro.sitio/recurso');
    expect(req.request.headers.has(HEADER_USUARIO_ID)).toBe(false);
    req.flush({});
  });
});
