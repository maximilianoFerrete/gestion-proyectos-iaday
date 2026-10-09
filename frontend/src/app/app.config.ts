import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { ApplicationConfig, provideBrowserGlobalErrorListeners } from '@angular/core';
import { provideRouter, withComponentInputBinding } from '@angular/router';
import { errorInterceptor } from './core/interceptors/error.interceptor';
import { usuarioActualInterceptor } from './core/interceptors/usuario-actual.interceptor';
import { routes } from './app.routes';

/**
 * Configuración de la aplicación: router con binding de parámetros a inputs y HttpClient con
 * los interceptores de usuario actual y de errores.
 *
 * Creado: 2026-10-09
 * Modificado: 2026-10-09 - Creación inicial (RF-001)
 */
export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    provideRouter(routes, withComponentInputBinding()),
    provideHttpClient(withInterceptors([usuarioActualInterceptor, errorInterceptor])),
  ],
};
