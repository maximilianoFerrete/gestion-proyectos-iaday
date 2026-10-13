import { ChangeDetectionStrategy, Component, computed, input, output } from '@angular/core';
import { AlcanceSeguimientoDto, IndicadoresDto } from '../../models/seguimiento.model';
import { textoSubtituloAlcance } from '../../utils/alcance';
import { ClaveTarjetaIndicador, tarjetasIndicadores } from '../../utils/indicadores';

/** `title` de las tarjetas clicables (RF-005 "Vista Resumen"). */
export const TITULO_VER_EN_TABLERO = 'Ver en el tablero';

/**
 * Tarjetas de indicadores (RF-005): Total, Abiertas, Vencidas, Bloqueadas, En espera y Sin
 * responsable, con el valor, un subtítulo con el alcance y `title` con la definición; Vencidas y
 * Bloqueadas con estilo de alerta si > 0. Con `clicable` (T2) cada tarjeta es un enlace que
 * emite su clave ("total" para la tarjeta Total); sin `clicable` (T1, DA-21) son informativas.
 *
 * Creado: 2026-10-10
 * Modificado: 2026-10-11 - Salida `indicadorSeleccionado` con el nombre de la spec (RF-005)
 */
@Component({
  selector: 'app-indicadores',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <ul class="tarjetas-indicadores" aria-label="Indicadores">
      @for (tarjeta of tarjetas(); track tarjeta.clave) {
        <li>
          @if (clicable()) {
            <button
              type="button"
              role="link"
              class="tarjeta-indicador tarjeta-clicable"
              [class.tarjeta-alerta]="tarjeta.alerta"
              [title]="tarjeta.definicion + ' ' + tituloVerEnTablero + '.'"
              [attr.aria-label]="tarjeta.titulo + ': ' + tarjeta.valor + '. ' + tituloVerEnTablero"
              (click)="indicadorSeleccionado.emit(tarjeta.clave)"
            >
              <span class="tarjeta-titulo">{{ tarjeta.titulo }}</span>
              <span class="tarjeta-valor">{{ tarjeta.valor }}</span>
              <span class="tarjeta-subtitulo">{{ subtitulo() }}</span>
            </button>
          } @else {
            <div
              class="tarjeta-indicador"
              [class.tarjeta-alerta]="tarjeta.alerta"
              [title]="tarjeta.definicion"
            >
              <span class="tarjeta-titulo">{{ tarjeta.titulo }}</span>
              <span class="tarjeta-valor">{{ tarjeta.valor }}</span>
              <span class="tarjeta-subtitulo">{{ subtitulo() }}</span>
            </div>
          }
        </li>
      }
    </ul>
  `,
})
export class IndicadoresComponent {
  readonly indicadores = input.required<IndicadoresDto>();
  readonly alcance = input.required<AlcanceSeguimientoDto>();
  /** Fecha de referencia `yyyy-MM-dd` (definición de Vencidas). */
  readonly fechaReferencia = input.required<string>();
  /** Tarjetas clicables (T2) o solo informativas (T1, DA-21). */
  readonly clicable = input(true);
  /** Emite la tarjeta elegida (solo si es clicable). */
  readonly indicadorSeleccionado = output<ClaveTarjetaIndicador>();

  protected readonly tituloVerEnTablero = TITULO_VER_EN_TABLERO;

  readonly tarjetas = computed(() =>
    tarjetasIndicadores(this.indicadores(), this.fechaReferencia()),
  );
  readonly subtitulo = computed(() => textoSubtituloAlcance(this.alcance()));
}
