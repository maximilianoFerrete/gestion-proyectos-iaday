import { ChangeDetectionStrategy, Component, computed, input, output } from '@angular/core';
import { NivelIncidenciaDto } from '../../../configuracion-incidencias/models/nivel-incidencia.model';
import { TipoIncidenciaDto } from '../../../configuracion-incidencias/models/tipo-incidencia.model';
import { AlcanceConsulta } from '../../models/seguimiento.model';
import {
  depurarTiposSeleccionados,
  nivelIdsConTipos,
  tiposAgrupadosPorNivel,
} from '../../utils/alcance';

/**
 * Filtro de alcance (RF-005 RN-04 a RN-06): multi-selección de niveles y de tipos agrupados por
 * nivel (inactivos con "(inactivo)"). Al elegir niveles, los tipos se limitan a esos niveles y
 * se quitan los que quedaron fuera; los niveles sin tipos no se pueden elegir, así nunca se
 * envía una intersección vacía (CA-16). Botón "Todos". La página guarda el valor en la URL.
 *
 * Creado: 2026-10-10
 * Modificado: 2026-10-10 - Creación inicial (RF-005)
 */
@Component({
  selector: 'app-filtro-alcance',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <details class="filtro-alcance" [open]="hayFiltro()">
      <summary>
        Alcance: {{ hayFiltro() ? 'niveles y tipos seleccionados' : 'todos los niveles y tipos' }}
      </summary>
      <div class="filtro-alcance-cuerpo">
        <fieldset class="grupo-checks">
          <legend>Niveles</legend>
          @for (nivel of nivelesOrdenados(); track nivel.id) {
            <label class="campo-check">
              <input
                type="checkbox"
                [checked]="nivelIds().includes(nivel.id)"
                [disabled]="!nivelesElegibles().has(nivel.id)"
                (change)="alternarNivel(nivel.id)"
              />
              Nivel {{ nivel.numero }} – {{ nivel.nombre }}
              @if (!nivelesElegibles().has(nivel.id)) {
                <span class="texto-secundario">(sin tipos)</span>
              }
            </label>
          } @empty {
            <p class="texto-secundario">No hay niveles configurados.</p>
          }
        </fieldset>
        @for (grupo of grupos(); track grupo.nivel.id) {
          <fieldset class="grupo-checks">
            <legend>Tipos de nivel {{ grupo.nivel.numero }}</legend>
            @for (tipo of grupo.tipos; track tipo.id) {
              <label class="campo-check">
                <input
                  type="checkbox"
                  [checked]="tipoIds().includes(tipo.id)"
                  (change)="alternarTipo(tipo.id)"
                />
                {{ tipo.nombre }}
                @if (!tipo.activo) {
                  <span class="texto-secundario">(inactivo)</span>
                }
              </label>
            }
          </fieldset>
        }
        <p class="ayuda">Sin selección se contabilizan todos los niveles o todos los tipos.</p>
        <div class="acciones">
          <button type="button" class="btn btn-chico" [disabled]="!hayFiltro()" (click)="todos()">
            Todos
          </button>
        </div>
      </div>
    </details>
  `,
})
export class FiltroAlcanceComponent {
  readonly niveles = input.required<readonly NivelIncidenciaDto[]>();
  /** Todos los tipos (activos e inactivos, RN-04). */
  readonly tipos = input.required<readonly TipoIncidenciaDto[]>();
  readonly nivelIds = input.required<readonly number[]>();
  readonly tipoIds = input.required<readonly number[]>();
  /** Emite el alcance elegido (la página lo refleja en la URL). */
  readonly cambio = output<AlcanceConsulta>();

  readonly hayFiltro = computed(() => this.nivelIds().length > 0 || this.tipoIds().length > 0);
  readonly nivelesOrdenados = computed(() =>
    [...this.niveles()].sort((a, b) => a.numero - b.numero),
  );
  readonly nivelesElegibles = computed(() => nivelIdsConTipos(this.niveles(), this.tipos()));
  readonly grupos = computed(() =>
    tiposAgrupadosPorNivel(this.niveles(), this.tipos(), this.nivelIds()),
  );

  /** Agrega o quita un nivel y depura los tipos que quedaron fuera. */
  alternarNivel(nivelId: number): void {
    const actuales = this.nivelIds();
    const nivelIds = actuales.includes(nivelId)
      ? actuales.filter((id) => id !== nivelId)
      : [...actuales, nivelId];
    this.cambio.emit({
      nivelIds,
      tipoIds: depurarTiposSeleccionados(this.tipoIds(), this.tipos(), nivelIds),
    });
  }

  /** Agrega o quita un tipo. */
  alternarTipo(tipoId: number): void {
    const actuales = this.tipoIds();
    const tipoIds = actuales.includes(tipoId)
      ? actuales.filter((id) => id !== tipoId)
      : [...actuales, tipoId];
    this.cambio.emit({ nivelIds: [...this.nivelIds()], tipoIds });
  }

  /** Vuelve a contabilizar todos los niveles y tipos. */
  todos(): void {
    this.cambio.emit({ nivelIds: [], tipoIds: [] });
  }
}
