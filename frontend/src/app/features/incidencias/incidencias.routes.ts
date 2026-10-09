import { Routes } from '@angular/router';

/**
 * Rutas de la feature Incidencias (I1–I5), bajo `/proyectos/:id/incidencias` (el `:id` del
 * proyecto se hereda de la ruta padre sin componente). `arbol` y `nueva` se declaran antes de
 * `:incidenciaId`.
 *
 * Creado: 2026-10-09
 * Modificado: 2026-10-09 - Creación inicial (RF-002)
 */
export const routes: Routes = [
  {
    path: '',
    title: 'Incidencias',
    loadComponent: () =>
      import('./pages/incidencias-listado/incidencias-listado.page').then(
        (m) => m.IncidenciasListadoPage,
      ),
  },
  {
    path: 'arbol',
    title: 'Árbol de incidencias',
    loadComponent: () =>
      import('./pages/incidencias-arbol/incidencias-arbol.page').then(
        (m) => m.IncidenciasArbolPage,
      ),
  },
  {
    path: 'nueva',
    title: 'Nueva incidencia',
    loadComponent: () =>
      import('./pages/incidencia-form/incidencia-form.page').then((m) => m.IncidenciaFormPage),
  },
  {
    path: ':incidenciaId',
    title: 'Detalle de incidencia',
    loadComponent: () =>
      import('./pages/incidencia-detalle/incidencia-detalle.page').then(
        (m) => m.IncidenciaDetallePage,
      ),
  },
  {
    path: ':incidenciaId/editar',
    title: 'Editar incidencia',
    loadComponent: () =>
      import('./pages/incidencia-form/incidencia-form.page').then((m) => m.IncidenciaFormPage),
  },
];
