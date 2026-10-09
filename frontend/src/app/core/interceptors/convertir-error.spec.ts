import { HttpErrorResponse } from '@angular/common/http';
import { MENSAJES_GENERICOS, convertirError, normalizarClaveCampo } from './convertir-error';

/** Crea un HttpErrorResponse con el status y cuerpo indicados. */
function respuesta(status: number, error: unknown): HttpErrorResponse {
  return new HttpErrorResponse({ status, error, url: '/api/x' });
}

/** Tests de convertirError: ProblemDetails → ErrorApi (spec 0.3). */
describe('convertirError', () => {
  // Spec 0.3: 409 de negocio conserva codigo y detail
  it('ProblemDetails 409 conserva codigo y detail', () => {
    const error = convertirError(
      respuesta(409, { status: 409, codigo: 'AGRUPACION_NOMBRE_DUPLICADO', detail: 'Duplicado.' }),
    );

    expect(error.codigo).toBe('AGRUPACION_NOMBRE_DUPLICADO');
    expect(error.detalle).toBe('Duplicado.');
    expect(error.tieneCodigo('AGRUPACION_NOMBRE_DUPLICADO')).toBe(true);
  });

  // Spec 0.3: errors con prefijo "$." y PascalCase se normalizan
  it('normaliza las claves de errors a camelCase y agrupa mensajes', () => {
    const error = convertirError(
      respuesta(400, {
        status: 400,
        codigo: 'VALIDACION',
        errors: { '$.FechaInicio': ['Formato inválido.'], fechaInicio: ['Obligatoria.'] },
      }),
    );

    expect(error.errores).toEqual({ fechaInicio: ['Formato inválido.', 'Obligatoria.'] });
    expect(error.detalle).toBe(MENSAJES_GENERICOS.validacion);
  });

  // CA-28: 404 sin cuerpo → "No se encontró el recurso"
  it('404 sin ProblemDetails devuelve el mensaje de recurso no encontrado', () => {
    const error = convertirError(respuesta(404, null));

    expect(error.detalle).toBe(MENSAJES_GENERICOS.noEncontrado);
    expect(error.codigo).toBeNull();
  });

  // Sin conexión (status 0)
  it('status 0 devuelve el mensaje de sin conexión', () => {
    expect(convertirError(respuesta(0, null)).detalle).toBe(MENSAJES_GENERICOS.sinConexion);
  });

  // Spec 0.3: 500 nunca muestra el detail del servidor
  it('500 con detail usa el mensaje genérico', () => {
    const error = convertirError(respuesta(500, { status: 500, detail: 'NullReferenceException' }));

    expect(error.detalle).toBe(MENSAJES_GENERICOS.inesperado);
  });

  it.each([
    ['$.FechaObjetivo', 'fechaObjetivo'],
    ['Nombre', 'nombre'],
    ['estadoId', 'estadoId'],
  ])('normalizarClaveCampo(%s) devuelve %s', (clave, esperado) => {
    expect(normalizarClaveCampo(clave)).toBe(esperado);
  });
});
