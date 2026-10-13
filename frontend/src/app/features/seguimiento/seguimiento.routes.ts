import { Routes } from '@angular/router';

/**
 * Rutas de la feature Seguimiento (RF-005), cargadas en forma diferida desde `app.routes.ts`:
 * - `routes`: T1 – Tablero general, bajo `/seguimiento`.
 * - `routesSeguimientoProyecto`: T2 – Seguimiento de proyecto / agrupación, bajo
 *   `/proyectos/:id/seguimiento` (el `:id` se hereda de la ruta padre sin componente).
 *
 * Creado: 2026-10-10
 * Modificado: 2026-10-10 - Creación inicial (RF-005)
 */
export const routes: Routes = [
  {
    path: '',
    title: 'Tablero de seguimiento',
    loadComponent: () =>
      import('./pages/tablero-general/tablero-general.page').then((m) => m.TableroGeneralPage),
  },
];

export const routesSeguimientoProyecto: Routes = [
  {
    path: '',
    title: 'Seguimiento del proyecto',
    loadComponent: () =>
      import('./pages/seguimiento-proyecto/seguimiento-proyecto.page').then(
        (m) => m.SeguimientoProyectoPage,
      ),
  },
];
