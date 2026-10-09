import { InjectionToken } from '@angular/core';
import { environment } from '../../../environments/environment';

/**
 * Token con la URL base de la API (sin barra final). Se toma de `environment.apiUrl` y puede
 * sobrescribirse en tests con `{ provide: API_URL, useValue: '...' }`.
 *
 * Creado: 2026-10-09
 * Modificado: 2026-10-09 - Creación inicial (RF-001)
 */
export const API_URL = new InjectionToken<string>('API_URL', {
  providedIn: 'root',
  factory: () => environment.apiUrl.replace(/\/+$/, ''),
});
