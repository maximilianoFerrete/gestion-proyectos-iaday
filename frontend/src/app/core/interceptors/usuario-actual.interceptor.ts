import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { API_URL } from '../config/api-url.token';
import { UsuarioActualService } from '../services/usuario-actual.service';

/** Header del mecanismo provisional de usuario actual (spec 0.4, DA-04). */
export const HEADER_USUARIO_ID = 'X-Usuario-Id';

/**
 * Agrega `X-Usuario-Id` a las requests de la API cuando hay un usuario seleccionado.
 * Con "Sin usuario" no envía el header (CA-34).
 *
 * Creado: 2026-10-09
 * Modificado: 2026-10-09 - Creación inicial (RF-001)
 */
export const usuarioActualInterceptor: HttpInterceptorFn = (req, next) => {
  const apiUrl = inject(API_URL);
  const personaId = inject(UsuarioActualService).personaId();
  if (personaId === null || !req.url.startsWith(apiUrl)) {
    return next(req);
  }
  return next(req.clone({ setHeaders: { [HEADER_USUARIO_ID]: String(personaId) } }));
};
