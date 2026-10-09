import { Routes } from '@angular/router';

/**
 * Rutas de la feature Proyectos (P1–P5), cargadas en forma diferida.
 *
 * Creado: 2026-10-09
 * Modificado: 2026-10-09 - Creación inicial (RF-001)
 */
export const routes: Routes = [
  {
    path: '',
    title: 'Proyectos',
    loadComponent: () =>
      import('./pages/proyectos-listado/proyectos-listado.page').then(
        (m) => m.ProyectosListadoPage,
      ),
  },
  {
    path: 'nuevo',
    title: 'Nuevo proyecto',
    loadComponent: () =>
      import('./pages/proyecto-form/proyecto-form.page').then((m) => m.ProyectoFormPage),
  },
  {
    path: ':id',
    title: 'Detalle de proyecto',
    loadComponent: () =>
      import('./pages/proyecto-detalle/proyecto-detalle.page').then((m) => m.ProyectoDetallePage),
  },
  {
    path: ':id/editar',
    title: 'Editar proyecto',
    loadComponent: () =>
      import('./pages/proyecto-form/proyecto-form.page').then((m) => m.ProyectoFormPage),
  },
  {
    path: ':id/agrupaciones/nueva',
    title: 'Nueva agrupación',
    loadComponent: () =>
      import('./pages/agrupacion-form/agrupacion-form.page').then((m) => m.AgrupacionFormPage),
  },
  {
    path: ':id/agrupaciones/:agrupacionId/editar',
    title: 'Editar agrupación',
    loadComponent: () =>
      import('./pages/agrupacion-form/agrupacion-form.page').then((m) => m.AgrupacionFormPage),
  },
];
