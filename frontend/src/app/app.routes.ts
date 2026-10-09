import { Routes } from '@angular/router';

/**
 * Rutas raíz con lazy loading por feature. Ruta por defecto: `/proyectos`.
 * `proyectos/:id/incidencias` se declara antes de `proyectos` para que el lazy de proyectos no
 * la capture (RF-002).
 *
 * Creado: 2026-10-09
 * Modificado: 2026-10-09 - Rutas de incidencias y configuración de incidencias (RF-002)
 */
export const routes: Routes = [
  { path: '', pathMatch: 'full', redirectTo: 'proyectos' },
  {
    path: 'proyectos/:id/incidencias',
    loadChildren: () => import('./features/incidencias/incidencias.routes').then((m) => m.routes),
  },
  {
    path: 'proyectos',
    loadChildren: () => import('./features/proyectos/proyectos.routes').then((m) => m.routes),
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
