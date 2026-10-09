import { FormControl, FormGroup } from '@angular/forms';
import { ErrorApi } from '../../core/models/error-api.model';
import { MENSAJES_GENERICOS } from '../../core/interceptors/convertir-error';
import {
  SIN_ERRORES,
  aplicarErroresServidor,
  esNoEncontrado,
  interpretarErrorFormulario,
  mensajeErrorAccion,
} from './errores-servidor';
import { ERROR_SERVIDOR } from './mensajes-error';

/** Tests de interpretarErrorFormulario y utilidades de errores de pantalla ("Pantallas"). */
describe('errores-servidor', () => {
  // Pantallas: 400 con errors → junto a cada campo
  it('interpretarErrorFormulario con errors por campo los devuelve por campo', () => {
    const error = new ErrorApi(400, 'VALIDACION', 'Hay datos inválidos.', {
      nombre: ['El nombre es obligatorio.'],
    });

    expect(interpretarErrorFormulario(error)).toEqual({
      campos: { nombre: ['El nombre es obligatorio.'] },
      general: null,
    });
  });

  // Pantallas: 409 de negocio → detail arriba del formulario
  it('interpretarErrorFormulario con 409 de negocio devuelve el detail como general', () => {
    const error = new ErrorApi(409, 'PROYECTO_ARCHIVADO', 'El proyecto está archivado.');

    expect(interpretarErrorFormulario(error)).toEqual({
      campos: {},
      general: 'El proyecto está archivado.',
    });
  });

  // CA-30, P5: AGRUPACION_NOMBRE_DUPLICADO se muestra en el campo Nombre
  it('interpretarErrorFormulario mapea un código a su campo', () => {
    const error = new ErrorApi(409, 'AGRUPACION_NOMBRE_DUPLICADO', 'Nombre repetido.');

    expect(interpretarErrorFormulario(error, { AGRUPACION_NOMBRE_DUPLICADO: 'nombre' })).toEqual({
      campos: { nombre: ['Nombre repetido.'] },
      general: null,
    });
  });

  // CA-02, P6: PERSONA_EMAIL_DUPLICADO en el campo Email
  it('interpretarErrorFormulario mapea PERSONA_EMAIL_DUPLICADO al campo email', () => {
    const error = new ErrorApi(409, 'PERSONA_EMAIL_DUPLICADO', 'Email repetido.');

    expect(interpretarErrorFormulario(error, { PERSONA_EMAIL_DUPLICADO: 'email' }).campos).toEqual({
      email: ['Email repetido.'],
    });
  });

  // Pantallas: 401/403/5xx ya se notificaron con toast
  it.each([401, 403, 500, 0])('interpretarErrorFormulario con status %i no repite el mensaje', (status) => {
    expect(interpretarErrorFormulario(new ErrorApi(status, null, 'x'))).toEqual(SIN_ERRORES);
  });

  // Errores que no vienen de la API → mensaje genérico
  it('interpretarErrorFormulario con error desconocido devuelve mensaje genérico', () => {
    expect(interpretarErrorFormulario(new Error('x')).general).toBe(MENSAJES_GENERICOS.inesperado);
  });

  // Acciones: 409 → toast con detail; 403 → null (ya notificado)
  it('mensajeErrorAccion devuelve el detail salvo 401/403/5xx', () => {
    expect(mensajeErrorAccion(new ErrorApi(409, 'PROYECTO_ARCHIVADO', 'Archivado.'))).toBe('Archivado.');
    expect(mensajeErrorAccion(new ErrorApi(403, 'PERMISO_DENEGADO', 'Sin permiso.'))).toBeNull();
  });

  // CA-28: 404 al cargar
  it('esNoEncontrado es true solo para 404', () => {
    expect(esNoEncontrado(new ErrorApi(404, 'PROYECTO_NO_ENCONTRADO', 'x'))).toBe(true);
    expect(esNoEncontrado(new ErrorApi(409, null, 'x'))).toBe(false);
  });

  // Pantallas: errores con campo inexistente van arriba del form
  it('aplicarErroresServidor marca los controles y devuelve los mensajes sin campo', () => {
    const form = new FormGroup({ nombre: new FormControl('x') });

    const sinCampo = aplicarErroresServidor(form, {
      nombre: ['Nombre repetido.'],
      otro: ['Error general.'],
    });

    expect(form.controls.nombre.getError(ERROR_SERVIDOR)).toBe('Nombre repetido.');
    expect(form.controls.nombre.touched).toBe(true);
    expect(sinCampo).toEqual(['Error general.']);
  });
});
