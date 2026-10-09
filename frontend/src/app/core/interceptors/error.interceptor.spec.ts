import { HttpClient, provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { ALMACENAMIENTO, Almacenamiento } from '../config/almacenamiento.token';
import { API_URL } from '../config/api-url.token';
import { ErrorApi } from '../models/error-api.model';
import { NotificacionService } from '../services/notificacion.service';
import { CLAVE_USUARIO_ACTUAL, UsuarioActualService } from '../services/usuario-actual.service';
import { errorInterceptor } from './error.interceptor';

/** Tests de errorInterceptor (spec 0.3 y manejo de errores de "Pantallas"). */
describe('errorInterceptor', () => {
  let http: HttpTestingController;
  let cliente: HttpClient;
  let notificacion: NotificacionService;
  const datos = new Map<string, string>();
  const almacenamiento: Almacenamiento = {
    getItem: (clave) => datos.get(clave) ?? null,
    setItem: (clave, valor) => void datos.set(clave, valor),
    removeItem: (clave) => void datos.delete(clave),
  };

  /** Ejecuta un GET a la API, responde con el error indicado y devuelve lo recibido. */
  function fallar(status: number, cuerpo: string | object | null, url = '/api/proyectos/3'): unknown {
    let recibido: unknown = null;
    cliente.get(url).subscribe({ error: (e: unknown) => (recibido = e) });
    http.expectOne(url).flush(cuerpo, { status, statusText: 'Error' });
    return recibido;
  }

  beforeEach(() => {
    datos.clear();
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(withInterceptors([errorInterceptor])),
        provideHttpClientTesting(),
        { provide: API_URL, useValue: '/api' },
        { provide: ALMACENAMIENTO, useValue: almacenamiento },
      ],
    });
    http = TestBed.inject(HttpTestingController);
    cliente = TestBed.inject(HttpClient);
    notificacion = TestBed.inject(NotificacionService);
  });

  afterEach(() => http.verify());

  // Spec 0.3, CA-23: 409 expone codigo y detail al llamador sin toast
  it('con ProblemDetails 409 expone codigo y detail al llamador', () => {
    const espia = vi.spyOn(notificacion, 'error');

    const error = fallar(409, {
      status: 409,
      title: 'Conflicto',
      codigo: 'PROYECTO_ARCHIVADO',
      detail: 'El proyecto PRJ-0003 está archivado y no admite modificaciones.',
    });

    expect(error).toBeInstanceOf(ErrorApi);
    expect(error).toMatchObject({
      status: 409,
      codigo: 'PROYECTO_ARCHIVADO',
      detalle: 'El proyecto PRJ-0003 está archivado y no admite modificaciones.',
    });
    expect(espia).not.toHaveBeenCalled();
  });

  // Spec 0.3, CA-17: 400 con errors por campo normalizados a camelCase
  it('con ValidationProblemDetails expone los errores por campo', () => {
    const error = fallar(400, {
      status: 400,
      codigo: 'VALIDACION',
      errors: { FechaObjetivo: ['La fecha objetivo debe ser mayor o igual a la fecha de inicio.'] },
    });

    expect(error).toMatchObject({
      codigo: 'VALIDACION',
      errores: { fechaObjetivo: ['La fecha objetivo debe ser mayor o igual a la fecha de inicio.'] },
    });
  });

  // CA-22: 403 se notifica con toast del detail
  it('con 403 muestra un toast con el detail', () => {
    const espia = vi.spyOn(notificacion, 'error');

    fallar(403, { status: 403, codigo: 'PERMISO_DENEGADO', detail: 'Requiere administrador.' });

    expect(espia).toHaveBeenCalledWith('Requiere administrador.');
  });

  // CA-35: 401 USUARIO_INVALIDO limpia el usuario actual
  it('con 401 USUARIO_INVALIDO limpia el usuario actual', () => {
    datos.set(CLAVE_USUARIO_ACTUAL, '5');
    const usuario = TestBed.inject(UsuarioActualService);

    fallar(401, { status: 401, codigo: 'USUARIO_INVALIDO', detail: 'Inactivo.' });

    expect(usuario.personaId()).toBeNull();
    expect(datos.has(CLAVE_USUARIO_ACTUAL)).toBe(false);
  });

  // Spec 0.3: 500 muestra un mensaje genérico (sin detalles internos)
  it('con 500 muestra un mensaje genérico', () => {
    const espia = vi.spyOn(notificacion, 'error');

    const error = fallar(500, { status: 500, codigo: 'ERROR_INTERNO', detail: 'stack interno' });

    expect(error).toMatchObject({ status: 500, codigo: 'ERROR_INTERNO' });
    expect(espia).toHaveBeenCalledWith('Ocurrió un error inesperado. Intentá nuevamente.');
  });

  // Spec 0.3: errores de URLs ajenas a la API no se transforman
  it('fuera de la API propaga el HttpErrorResponse original', () => {
    const error = fallar(404, 'no', 'https://otro.sitio/x');

    expect(error).not.toBeInstanceOf(ErrorApi);
  });
});
