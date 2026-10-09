import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { PersonaDto } from '../../features/personas/models/persona.model';

/** Valor del option "Sin usuario". */
const SIN_USUARIO = '';

/**
 * Selector "Usuario actual" de la barra superior (spec 0.4): personas activas, "(admin)" junto
 * a los administradores y opción "Sin usuario".
 *
 * Creado: 2026-10-09
 * Modificado: 2026-10-09 - Creación inicial (RF-001)
 */
@Component({
  selector: 'app-usuario-selector',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <label class="usuario-selector" for="usuario-actual">
      <span>Usuario actual</span>
      <select id="usuario-actual" (change)="alCambiar($event)">
        <option [value]="sinUsuario" [selected]="personaId() === null">Sin usuario</option>
        @for (persona of personas(); track persona.id) {
          <option [value]="persona.id" [selected]="persona.id === personaId()">
            {{ persona.nombre }}{{ persona.esAdmin ? ' (admin)' : '' }}
          </option>
        }
      </select>
    </label>
  `,
})
export class UsuarioSelectorComponent {
  readonly personas = input.required<readonly PersonaDto[]>();
  readonly personaId = input.required<number | null>();
  /** Emite el id elegido o null para "Sin usuario". */
  readonly cambio = output<number | null>();

  protected readonly sinUsuario = SIN_USUARIO;

  /** Convierte el valor del select en id de persona o null. */
  alCambiar(evento: Event): void {
    const destino = evento.target;
    if (!(destino instanceof HTMLSelectElement)) {
      return;
    }
    const id = Number(destino.value);
    this.cambio.emit(destino.value === SIN_USUARIO || !Number.isInteger(id) ? null : id);
  }
}
