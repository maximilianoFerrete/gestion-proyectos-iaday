import { Routes } from '@angular/router';

/**
 * Rutas raíz con lazy loading por feature. Ruta por defecto: `/proyectos`.
 * `proyectos/:id/incidencias` y `proyectos/:id/seguimiento` se declaran antes de `proyectos`
 * para que el lazy de proyectos no las capture (RF-002, RF-005).
 *
 * Creado: 2026-10-09
 * Modificado: 2026-10-10 - Rutas de seguimiento T1 y T2 (RF-005)
 */
export const routes: Routes = [
  { path: '', pathMatch: 'full', redirectTo: 'proyectos' },
  {
    path: 'proyectos/:id/incidencias',
    loadChildren: () => import('./features/incidencias/incidencias.routes').then((m) => m.routes),
  },
  {
    path: 'proyectos/:id/seguimiento',
    loadChildren: () =>
      import('./features/seguimiento/seguimiento.routes').then((m) => m.routesSeguimientoProyecto),
  },
  {
    path: 'proyectos',
    loadChildren: () => import('./features/proyectos/proyectos.routes').then((m) => m.routes),
  },
  {
    path: 'seguimiento',
    loadChildren: () => import('./features/seguimiento/seguimiento.routes').then((m) => m.routes),
  },
  {
    path: 'personas',
    loadChildren: () => import('./features/personas/personas.routes').then((m) => m.routes),
  },
  {
    path: 'configuracion/estados-proyecto',
    loadChildren: () =>
      import('./features/estados-proyecto/estados-proyecto.routes').then((m) => m.routes),
  },
  {
    path: 'configuracion/incidencias',
    loadChildren: () =>
      import('./features/configuracion-incidencias/configuracion-incidencias.routes').then(
        (m) => m.routes,
      ),
  },
  { path: '**', redirectTo: 'proyectos' },
];
