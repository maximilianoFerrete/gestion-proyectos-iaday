import { ChangeDetectionStrategy, Component } from '@angular/core';
import { ShellComponent } from './core/layout/shell.component';

/**
 * Componente raíz: delega en el layout (`ShellComponent`).
 *
 * Creado: 2026-10-09
 * Modificado: 2026-10-09 - Creación inicial (RF-001)
 */
@Component({
  selector: 'app-root',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [ShellComponent],
  template: '<app-shell />',
})
export class App {}
