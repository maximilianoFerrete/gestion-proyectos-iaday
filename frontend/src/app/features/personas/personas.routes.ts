import { Routes } from '@angular/router';

/**
 * Rutas de la feature Personas (P6), cargadas en forma diferida.
 *
 * Creado: 2026-10-09
 * Modificado: 2026-10-09 - Creación inicial (RF-001)
 */
export const routes: Routes = [
  {
    path: '',
    title: 'Personas',
    loadComponent: () =>
      import('./pages/personas-listado/personas-listado.page').then((m) => m.PersonasListadoPage),
  },
  {
    path: 'nueva',
    title: 'Nueva persona',
    loadComponent: () =>
      import('./pages/persona-form/persona-form.page').then((m) => m.PersonaFormPage),
  },
  {
    path: ':id/editar',
    title: 'Editar persona',
    loadComponent: () =>
      import('./pages/persona-form/persona-form.page').then((m) => m.PersonaFormPage),
  },
];
