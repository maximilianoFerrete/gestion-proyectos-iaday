import {
  ChangeDetectionStrategy,
  Component,
  computed,
  effect,
  inject,
  input,
  output,
  untracked,
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { debounceTime } from 'rxjs';
import { EstadoIncidenciaDto } from '../../../configuracion-incidencias/models/estado-incidencia.model';
import {
  EstadoFiltroTableroDto,
  FiltrosTablero,
  IndicadorTablero,
  LIMITES_SEGUIMIENTO,
  NOMBRES_INDICADOR_TABLERO,
  SIN_FILTROS_TABLERO,
  VALORES_INDICADOR_TABLERO,
} from '../../models/seguimiento.model';
import { hayFiltrosTablero } from '../../utils/filtros-seguimiento';

/** Debounce del texto del tablero (RF-005). */
export const DEBOUNCE_TEXTO_TABLERO_MS = 300;

/** Filtro individual que se puede quitar desde su chip. */
export type ClaveFiltroTablero = keyof FiltrosTablero;

/** Chip de un filtro activo. */
export interface ChipFiltroTablero {
  readonly clave: ClaveFiltroTablero;
  readonly texto: string;
}

/** Chips de los filtros activos ("Indicador: Vencidas", "Estado: Bloqueada", "Texto: «pago»"). */
export function chipsFiltrosTablero(
  filtros: FiltrosTablero,
  nombreEstado: string | null,
): ChipFiltroTablero[] {
  const chips: ChipFiltroTablero[] = [];
  if (filtros.indicador !== null) {
    chips.push({
      clave: 'indicador',
      texto: `Indicador: ${NOMBRES_INDICADOR_TABLERO[filtros.indicador]}`,
    });
  }
  if (filtros.estadoId !== null) {
    chips.push({ clave: 'estadoId', texto: `Estado: ${nombreEstado ?? `#${filtros.estadoId}`}` });
  }
  if (filtros.texto !== null) {
    chips.push({ clave: 'texto', texto: `Texto: «${filtros.texto}»` });
  }
  return chips;
}

/**
 * Filtros del Tablero (RF-005 RN-21): texto (código o título, máx. 200, debounce 300 ms), estado
 * (catálogo por orden, inactivos marcados) e indicador; chips por filtro activo con botón para
 * quitarlo y "Limpiar filtros" (emite los tres vacíos; deshabilitado sin filtros). Cada cambio
 * se emite para que la página lo refleje en la URL.
 *
 * Creado: 2026-10-10
 * Modificado: 2026-10-10 - Creación inicial (RF-005)
 */
@Component({
  selector: 'app-filtros-tablero',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [ReactiveFormsModule],
  template: `
    <form class="filtros" [formGroup]="form" (ngSubmit)="emitirTexto()" role="search">
      <div class="campo">
        <label for="tablero-filtro-texto">Buscar por código o título</label>
        <input
          id="tablero-filtro-texto"
          type="search"
          formControlName="texto"
          [maxlength]="limites.textoTableroMax"
          autocomplete="off"
        />
      </div>
      <div class="campo">
        <label for="tablero-filtro-estado">Estado</label>
        <select id="tablero-filtro-estado" formControlName="estadoId" (change)="emitirSelects()">
          <option [ngValue]="null">Todos</option>
          @for (estado of estadosOrdenados(); track estado.id) {
            <option [ngValue]="estado.id">
              {{ estado.nombre }}{{ estado.activo ? '' : ' (inactivo)' }}
            </option>
          }
        </select>
      </div>
      <div class="campo">
        <label for="tablero-filtro-indicador">Indicador</label>
        <select
          id="tablero-filtro-indicador"
          formControlName="indicador"
          (change)="emitirSelects()"
        >
          <option [ngValue]="null">Todos</option>
          @for (indicador of indicadores; track indicador) {
            <option [ngValue]="indicador">{{ nombresIndicador[indicador] }}</option>
          }
        </select>
      </div>
      <div class="campo campo-accion">
        <button type="button" class="btn" [disabled]="!hayFiltros()" (click)="limpiarTodo()">
          Limpiar filtros
        </button>
      </div>
    </form>
    @if (chips().length > 0) {
      <ul class="chips" aria-label="Filtros activos">
        @for (chip of chips(); track chip.clave) {
          <li class="chip">
            <span>{{ chip.texto }}</span>
            <button
              type="button"
              class="chip-quitar"
              [attr.aria-label]="'Quitar filtro ' + chip.texto"
              (click)="quitar(chip.clave)"
            >
              ×
            </button>
          </li>
        }
      </ul>
    }
  `,
})
export class FiltrosTableroComponent {
  private readonly fb = inject(NonNullableFormBuilder);

  readonly filtros = input.required<FiltrosTablero>();
  /** Catálogo de estados (incluidos inactivos). */
  readonly estados = input.required<readonly EstadoIncidenciaDto[]>();
  /** Estado filtrado según la respuesta (`filtros.estado`), para el nombre del chip. */
  readonly estadoAplicado = input<EstadoFiltroTableroDto | null>(null);
  /** Emite los filtros del tablero completos tras cada cambio. */
  readonly cambio = output<FiltrosTablero>();

  readonly limites = LIMITES_SEGUIMIENTO;
  readonly indicadores = VALORES_INDICADOR_TABLERO;
  readonly nombresIndicador = NOMBRES_INDICADOR_TABLERO;

  readonly form = this.fb.group({
    texto: this.fb.control('', [Validators.maxLength(LIMITES_SEGUIMIENTO.textoTableroMax)]),
    estadoId: this.fb.control<number | null>(null),
    indicador: this.fb.control<IndicadorTablero | null>(null),
  });

  readonly estadosOrdenados = computed(() =>
    [...this.estados()].sort((a, b) => a.orden - b.orden || a.nombre.localeCompare(b.nombre, 'es')),
  );
  readonly hayFiltros = computed(() => hayFiltrosTablero(this.filtros()));

  /** Nombre del estado filtrado: de la respuesta o, si no llegó todavía, del catálogo. */
  private readonly nombreEstado = computed(() => {
    const id = this.filtros().estadoId;
    if (id === null) {
      return null;
    }
    const aplicado = this.estadoAplicado();
    if (aplicado !== null && aplicado.id === id) {
      return aplicado.nombre;
    }
    return this.estados().find((e) => e.id === id)?.nombre ?? null;
  });

  readonly chips = computed(() => chipsFiltrosTablero(this.filtros(), this.nombreEstado()));

  constructor() {
    effect(() => {
      const filtros = this.filtros();
      untracked(() => this.sincronizar(filtros));
    });
    this.form.controls.texto.valueChanges
      .pipe(debounceTime(DEBOUNCE_TEXTO_TABLERO_MS), takeUntilDestroyed())
      .subscribe(() => this.emitirTexto());
  }

  /** Emite el texto (recortado; vacío = sin filtro) si cambió. */
  emitirTexto(): void {
    if (this.form.controls.texto.invalid) {
      return;
    }
    const texto = this.form.controls.texto.value.trim();
    const valor = texto.length > 0 ? texto : null;
    if (valor !== this.filtros().texto) {
      this.cambio.emit({ ...this.filtros(), texto: valor });
    }
  }

  /** Emite el estado y el indicador elegidos. */
  emitirSelects(): void {
    const { estadoId, indicador } = this.form.getRawValue();
    const actuales = this.filtros();
    if (estadoId !== actuales.estadoId || indicador !== actuales.indicador) {
      this.cambio.emit({ ...actuales, estadoId, indicador });
    }
  }

  /** Quita un filtro desde su chip. */
  quitar(clave: ClaveFiltroTablero): void {
    this.cambio.emit({ ...this.filtros(), [clave]: null });
  }

  /** Quita los tres filtros (conserva ámbito y alcance, que maneja la página). */
  limpiarTodo(): void {
    this.form.patchValue({ texto: '', estadoId: null, indicador: null }, { emitEvent: false });
    this.cambio.emit(SIN_FILTROS_TABLERO);
  }

  /** Refleja en el formulario los filtros vigentes (URL) sin disparar emisiones. */
  private sincronizar(filtros: FiltrosTablero): void {
    const textoForm = this.form.controls.texto.value;
    const textoUrl = filtros.texto ?? '';
    this.form.patchValue(
      {
        texto: textoForm.trim() === textoUrl ? textoForm : textoUrl,
        estadoId: filtros.estadoId,
        indicador: filtros.indicador,
      },
      { emitEvent: false },
    );
  }
}
