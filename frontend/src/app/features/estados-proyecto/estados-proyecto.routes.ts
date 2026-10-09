import { Routes } from '@angular/router';

/**
 * Rutas de la feature Estados de proyecto (P7), bajo `/configuracion/estados-proyecto`.
 *
 * Creado: 2026-10-09
 * Modificado: 2026-10-09 - Creación inicial (RF-001)
 */
export const routes: Routes = [
  {
    path: '',
    title: 'Estados de proyecto',
    loadComponent: () =>
      import('./pages/estados-proyecto-listado/estados-proyecto-listado.page').then(
        (m) => m.EstadosProyectoListadoPage,
      ),
  },
  {
    path: 'nuevo',
    title: 'Nuevo estado de proyecto',
    loadComponent: () =>
      import('./pages/estado-proyecto-form/estado-proyecto-form.page').then(
        (m) => m.EstadoProyectoFormPage,
      ),
  },
  {
    path: ':id/editar',
    title: 'Editar estado de proyecto',
    loadComponent: () =>
      import('./pages/estado-proyecto-form/estado-proyecto-form.page').then(
        (m) => m.EstadoProyectoFormPage,
      ),
  },
];
