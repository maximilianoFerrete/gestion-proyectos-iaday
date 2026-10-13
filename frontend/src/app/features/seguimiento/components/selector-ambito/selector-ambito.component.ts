import { ChangeDetectionStrategy, Component, computed, input, output } from '@angular/core';
import { AgrupacionDto } from '../../../proyectos/models/agrupacion.model';

/** Ámbito elegido en el selector (RF-005 RN-01). */
export interface AmbitoElegido {
  readonly agrupacionId: number | null;
  readonly sinAgrupacion: boolean;
}

/** Valor del option "Todo el proyecto". */
const VALOR_PROYECTO = 'proyecto';
/** Valor del option "Sin agrupación". */
const VALOR_SIN_AGRUPACION = 'sin';

/** Convierte el valor del select en ámbito. */
export function ambitoDesdeValor(valor: string): AmbitoElegido {
  if (valor === VALOR_SIN_AGRUPACION) {
    return { agrupacionId: null, sinAgrupacion: true };
  }
  const id = Number(valor);
  return Number.isInteger(id) && id > 0
    ? { agrupacionId: id, sinAgrupacion: false }
    : { agrupacionId: null, sinAgrupacion: false };
}

/**
 * Selector de ámbito de T2 (RF-005): "Todo el proyecto", cada agrupación (por nombre) y "Sin
 * agrupación". Emite el ámbito elegido; la página lo refleja en la URL.
 *
 * Creado: 2026-10-10
 * Modificado: 2026-10-10 - Creación inicial (RF-005)
 */
@Component({
  selector: 'app-selector-ambito',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="campo">
      <label for="selector-ambito">Ámbito</label>
      <select id="selector-ambito" [value]="valor()" (change)="elegir($event)">
        <option [value]="valorProyecto" [selected]="valor() === valorProyecto">
          Todo el proyecto
        </option>
        @for (agrupacion of agrupacionesOrdenadas(); track agrupacion.id) {
          <option [value]="'' + agrupacion.id" [selected]="valor() === '' + agrupacion.id">
            {{ agrupacion.nombre }}
          </option>
        }
        <option [value]="valorSinAgrupacion" [selected]="valor() === valorSinAgrupacion">
          Sin agrupación
        </option>
      </select>
    </div>
  `,
})
export class SelectorAmbitoComponent {
  readonly agrupaciones = input.required<readonly AgrupacionDto[]>();
  readonly agrupacionId = input<number | null>(null);
  readonly sinAgrupacion = input(false);
  /** Emite el ámbito elegido. */
  readonly cambio = output<AmbitoElegido>();

  protected readonly valorProyecto = VALOR_PROYECTO;
  protected readonly valorSinAgrupacion = VALOR_SIN_AGRUPACION;

  readonly agrupacionesOrdenadas = computed(() =>
    [...this.agrupaciones()].sort((a, b) => a.nombre.localeCompare(b.nombre, 'es')),
  );

  /** Valor seleccionado según el ámbito vigente. */
  readonly valor = computed(() => {
    if (this.sinAgrupacion()) {
      return VALOR_SIN_AGRUPACION;
    }
    const id = this.agrupacionId();
    return id === null ? VALOR_PROYECTO : String(id);
  });

  /** Emite el ámbito del option elegido. */
  elegir(evento: Event): void {
    const destino = evento.target;
    if (destino instanceof HTMLSelectElement) {
      this.cambio.emit(ambitoDesdeValor(destino.value));
    }
  }
}
