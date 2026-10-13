import { ChangeDetectionStrategy, Component, computed, input, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { formatearFecha } from '../../../../shared/utils/fechas';
import {
  CategoriaEstadoIncidencia,
  EstadoIncidenciaDto,
} from '../../../configuracion-incidencias/models/estado-incidencia.model';
import { AlcanceSeguimientoDto } from '../../models/seguimiento.model';
import { textoLeyendaAlcance } from '../../utils/alcance';
import {
  NOTA_SIN_ROLLUP,
  avisoSinEstadosCategoria,
  definicionCategoria,
  definicionIndicador,
  estadosConCategoria,
} from '../../utils/indicadores';

/** Nota de la vista Jerarquía cuando el alcance no es completo (RF-005 CA-18). */
export const NOTA_ATENUADOS = 'Los elementos atenuados no se contabilizan con el alcance actual.';

/** Contador para ids únicos (varias leyendas en la misma página). */
let siguienteId = 1;

/**
 * Leyenda de alcance de los indicadores (RF-005 RN-23): niveles y tipos contabilizados, fecha
 * de referencia ("Datos al dd/MM/yyyy"), definiciones desplegables de cada indicador y aviso
 * cuando ninguna categoría tiene estados (CA-12), con enlace a C3 si hay permiso.
 *
 * Creado: 2026-10-10
 * Modificado: 2026-10-10 - Creación inicial (RF-005)
 */
@Component({
  selector: 'app-leyenda-alcance',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterLink],
  template: `
    <section class="leyenda-alcance" aria-label="Alcance de los indicadores">
      <p>
        <span>{{ texto() }}</span>
        <span class="texto-secundario"> Datos al {{ fecha() }}.</span>
      </p>
      @if (notaAtenuados()) {
        <p class="texto-secundario">{{ notaAtenuadosTexto }}</p>
      }
      @for (aviso of avisos(); track aviso) {
        <p class="aviso" role="note">
          {{ aviso }}
          @if (puedeConfigurarEstados()) {
            <a routerLink="/configuracion/incidencias/estados">Configurar estados de incidencia</a>
          }
        </p>
      }
      <button
        type="button"
        class="btn-enlace"
        [attr.aria-expanded]="abierta()"
        [attr.aria-controls]="idDefiniciones"
        (click)="abierta.set(!abierta())"
      >
        ¿Qué cuenta cada indicador?
      </button>
      @if (abierta()) {
        <ul class="leyenda-definiciones" [id]="idDefiniciones">
          <li><strong>Vencidas:</strong> {{ definiciones().vencidas }}</li>
          <li><strong>Bloqueadas:</strong> {{ definiciones().bloqueadas }}</li>
          <li><strong>En espera:</strong> {{ definiciones().enEspera }}</li>
          <li><strong>Sin responsable:</strong> {{ definiciones().sinResponsable }}</li>
          <li><strong>Por estado:</strong> {{ definiciones().porEstado }}</li>
          <li>{{ nota }}</li>
        </ul>
      }
    </section>
  `,
})
export class LeyendaAlcanceComponent {
  readonly alcance = input.required<AlcanceSeguimientoDto>();
  /** Fecha de referencia del servidor `yyyy-MM-dd`. */
  readonly fechaReferencia = input.required<string>();
  /** Catálogo de estados (para nombrar los de cada categoría). */
  readonly estados = input<readonly EstadoIncidenciaDto[]>([]);
  /** Muestra el enlace a C3 en los avisos (permiso `Configuracion.Incidencias`). */
  readonly puedeConfigurarEstados = input(false);
  /** Muestra la nota de nodos atenuados (vista Jerarquía con alcance parcial). */
  readonly notaAtenuados = input(false);

  readonly abierta = signal(false);
  readonly idDefiniciones = `leyenda-definiciones-${siguienteId++}`;
  protected readonly nota = NOTA_SIN_ROLLUP;
  protected readonly notaAtenuadosTexto = NOTA_ATENUADOS;

  readonly texto = computed(() => textoLeyendaAlcance(this.alcance()));
  readonly fecha = computed(() => formatearFecha(this.fechaReferencia()));

  /** Nombres de los estados por categoría. */
  private readonly nombresPorCategoria = computed(() => ({
    BLOQUEADA: estadosConCategoria(this.estados(), 'BLOQUEADA'),
    EN_ESPERA: estadosConCategoria(this.estados(), 'EN_ESPERA'),
  }));

  readonly definiciones = computed(() => {
    const nombres = this.nombresPorCategoria();
    const fecha = this.fechaReferencia();
    return {
      vencidas: definicionIndicador('vencidas', fecha),
      bloqueadas: definicionCategoria('BLOQUEADA', nombres.BLOQUEADA),
      enEspera: definicionCategoria('EN_ESPERA', nombres.EN_ESPERA),
      sinResponsable: definicionIndicador('sin-responsable', fecha),
      porEstado: definicionIndicador('total', fecha),
    };
  });

  /** Avisos de categorías sin estados (solo con el catálogo cargado). */
  readonly avisos = computed(() => {
    if (this.estados().length === 0) {
      return [];
    }
    const nombres = this.nombresPorCategoria();
    const categorias: CategoriaEstadoIncidencia[] = ['BLOQUEADA', 'EN_ESPERA'];
    return categorias
      .filter((categoria) => nombres[categoria].length === 0)
      .map((categoria) => avisoSinEstadosCategoria(categoria));
  });
}
