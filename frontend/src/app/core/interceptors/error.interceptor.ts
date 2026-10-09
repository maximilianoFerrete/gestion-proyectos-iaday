import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, throwError } from 'rxjs';
import { API_URL } from '../config/api-url.token';
import { ErrorApi } from '../models/error-api.model';
import { NotificacionService } from '../services/notificacion.service';
import { MENSAJE_USUARIO_INVALIDO, UsuarioActualService } from '../services/usuario-actual.service';
import { convertirError } from './convertir-error';

/**
 * Manejo centralizado de errores HTTP de la API (spec 0.3 y "Pantallas"):
 * - Convierte la respuesta (ProblemDetails) en `ErrorApi` y la propaga al llamador.
 * - 401 `USUARIO_INVALIDO`: limpia el usuario actual y avisa (CA-35).
 * - Otros 401/403, 5xx y fallas de conexión: toast con el mensaje.
 * - 400/404/409 los resuelve cada pantalla (junto a campos, arriba del form o toast).
 *
 * Creado: 2026-10-09
 * Modificado: 2026-10-09 - Creación inicial (RF-001)
 */
export const errorInterceptor: HttpInterceptorFn = (req, next) => {
  const apiUrl = inject(API_URL);
  const notificacion = inject(NotificacionService);
  const usuarioActual = inject(UsuarioActualService);

  return next(req).pipe(
    catchError((error: unknown) => {
      if (!(error instanceof HttpErrorResponse) || !req.url.startsWith(apiUrl)) {
        return throwError(() => error);
      }
      const errorApi = convertirError(error);
      notificarSiCorresponde(errorApi, notificacion, usuarioActual);
      return throwError(() => errorApi);
    }),
  );
};

/** Muestra el toast o efecto global que corresponde al error. */
function notificarSiCorresponde(
  error: ErrorApi,
  notificacion: NotificacionService,
  usuarioActual: UsuarioActualService,
): void {
  if (error.tieneCodigo('USUARIO_INVALIDO')) {
    usuarioActual.limpiar(MENSAJE_USUARIO_INVALIDO);
    return;
  }
  if (error.status === 0 || error.status === 401 || error.status === 403 || error.status >= 500) {
    notificacion.error(error.detalle);
  }
}
