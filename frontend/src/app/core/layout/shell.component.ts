import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { ConfirmacionDialogComponent } from '../../shared/components/confirmacion-dialog/confirmacion-dialog.component';
import { ConfirmacionService } from '../services/confirmacion.service';
import { NotificacionService } from '../services/notificacion.service';
import { UsuarioActualService } from '../services/usuario-actual.service';
import { ToastsComponent } from './toasts.component';
import { UsuarioSelectorComponent } from './usuario-selector.component';

/**
 * Layout de la aplicación: barra superior con título, menú y selector "Usuario actual";
 * contenido ruteado; toasts y diálogo de confirmación únicos.
 *
 * Creado: 2026-10-09
 * Modificado: 2026-10-09 - Creación inicial (RF-001)
 */
@Component({
  selector: 'app-shell',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    RouterOutlet,
    RouterLink,
    RouterLinkActive,
    UsuarioSelectorComponent,
    ToastsComponent,
    ConfirmacionDialogComponent,
  ],
  template: `
    <a class="saltar-contenido" href="#contenido">Saltar al contenido</a>
    <header class="barra-superior">
      <span class="titulo-app">Gestión de Proyectos</span>
      <nav class="menu" aria-label="Menú principal">
        <a routerLink="/proyectos" routerLinkActive="activo">Proyectos</a>
        <a routerLink="/personas" routerLinkActive="activo">Personas</a>
        <span class="menu-grupo">
          <span class="menu-grupo-titulo">Configuración</span>
          <a routerLink="/configuracion/estados-proyecto" routerLinkActive="activo">
            Estados de proyecto
          </a>
        </span>
      </nav>
      <app-usuario-selector
        [personas]="usuarioActual.personasSeleccionables()"
        [personaId]="usuarioActual.personaId()"
        (cambio)="usuarioActual.seleccionar($event)"
      />
    </header>
    <main id="contenido" class="contenido" tabindex="-1">
      <router-outlet />
    </main>
    <app-toasts
      [notificaciones]="notificacion.notificaciones()"
      (cerrar)="notificacion.cerrar($event)"
    />
    <app-confirmacion-dialog
      [solicitud]="confirmacion.solicitud()"
      (respuesta)="confirmacion.responder($event)"
    />
  `,
})
export class ShellComponent {
  protected readonly usuarioActual = inject(UsuarioActualService);
  protected readonly notificacion = inject(NotificacionService);
  protected readonly confirmacion = inject(ConfirmacionService);

  constructor() {
    this.usuarioActual.inicializar();
  }
}
