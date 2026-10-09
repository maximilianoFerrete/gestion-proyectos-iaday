import { Routes } from '@angular/router';

/**
 * Rutas raíz con lazy loading por feature. Ruta por defecto: `/proyectos`.
 *
 * Creado: 2026-10-09
 * Modificado: 2026-10-09 - Creación inicial (RF-001)
 */
export const routes: Routes = [
  { path: '', pathMatch: 'full', redirectTo: 'proyectos' },
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
  { path: '**', redirectTo: 'proyectos' },
];
