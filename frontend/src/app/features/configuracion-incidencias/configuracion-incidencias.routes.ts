import { Routes } from '@angular/router';

/**
 * Rutas de la feature Configuración de incidencias (C1–C5), bajo `/configuracion/incidencias`.
 *
 * Creado: 2026-10-09
 * Modificado: 2026-10-09 - Creación inicial (RF-002)
 */
export const routes: Routes = [
  { path: '', pathMatch: 'full', redirectTo: 'tipos' },
  {
    path: 'tipos',
    title: 'Niveles y tipos de incidencia',
    loadComponent: () =>
      import('./pages/niveles-tipos/niveles-tipos.page').then((m) => m.NivelesTiposPage),
  },
  {
    path: 'tipos/nuevo',
    title: 'Nuevo tipo de incidencia',
    loadComponent: () => import('./pages/tipo-form/tipo-form.page').then((m) => m.TipoFormPage),
  },
  {
    path: 'tipos/:id/editar',
    title: 'Editar tipo de incidencia',
    loadComponent: () => import('./pages/tipo-form/tipo-form.page').then((m) => m.TipoFormPage),
  },
  {
    path: 'relaciones',
    title: 'Relaciones de tipos',
    loadComponent: () => import('./pages/relaciones/relaciones.page').then((m) => m.RelacionesPage),
  },
  {
    path: 'estados',
    title: 'Estados de incidencia',
    loadComponent: () =>
      import('./pages/estados-incidencia/estados-incidencia.page').then(
        (m) => m.EstadosIncidenciaPage,
      ),
  },
  {
    path: 'estados/nuevo',
    title: 'Nuevo estado de incidencia',
    loadComponent: () =>
      import('./pages/estado-incidencia-form/estado-incidencia-form.page').then(
        (m) => m.EstadoIncidenciaFormPage,
      ),
  },
  {
    path: 'estados/:id/editar',
    title: 'Editar estado de incidencia',
    loadComponent: () =>
      import('./pages/estado-incidencia-form/estado-incidencia-form.page').then(
        (m) => m.EstadoIncidenciaFormPage,
      ),
  },
  {
    path: 'flujos',
    title: 'Flujos de trabajo',
    loadComponent: () => import('./pages/flujos/flujos.page').then((m) => m.FlujosPage),
  },
  {
    path: 'flujos/nuevo',
    title: 'Nuevo flujo de trabajo',
    loadComponent: () => import('./pages/flujo-form/flujo-form.page').then((m) => m.FlujoFormPage),
  },
  {
    path: 'flujos/:id/editar',
    title: 'Editar flujo de trabajo',
    loadComponent: () => import('./pages/flujo-form/flujo-form.page').then((m) => m.FlujoFormPage),
  },
  {
    path: 'prioridades',
    title: 'Prioridades de incidencia',
    loadComponent: () =>
      import('./pages/prioridades/prioridades.page').then((m) => m.PrioridadesPage),
  },
  {
    path: 'prioridades/nueva',
    title: 'Nueva prioridad',
    loadComponent: () =>
      import('./pages/prioridad-form/prioridad-form.page').then((m) => m.PrioridadFormPage),
  },
  {
    path: 'prioridades/:id/editar',
    title: 'Editar prioridad',
    loadComponent: () =>
      import('./pages/prioridad-form/prioridad-form.page').then((m) => m.PrioridadFormPage),
  },
];
