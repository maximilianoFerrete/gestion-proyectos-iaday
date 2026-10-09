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
import { CampoErrorComponent } from '../../../../shared/components/campo-error/campo-error.component';
import { mensajesVisibles } from '../../../../shared/forms/mensajes-error';
import { parsearId } from '../../../../shared/utils/ids';
import { EstadoIncidenciaDto } from '../../../configuracion-incidencias/models/estado-incidencia.model';
import { PrioridadIncidenciaDto } from '../../../configuracion-incidencias/models/prioridad-incidencia.model';
import { TipoIncidenciaDto } from '../../../configuracion-incidencias/models/tipo-incidencia.model';
import { PersonaDto } from '../../../personas/models/persona.model';
import { AgrupacionDto } from '../../../proyectos/models/agrupacion.model';
import { FiltrosIncidencias, LIMITES_INCIDENCIA } from '../../models/incidencia.model';
import { opcionesFiltro, opcionesFiltroEstado } from '../../utils/opciones-incidencia';

/** Filtros editables desde el panel (sin paginación ni orden). */
export type CambioFiltrosIncidencias = Pick<
  FiltrosIncidencias,
  | 'texto'
  | 'tipoIds'
  | 'estadoId'
  | 'prioridadId'
  | 'responsableId'
  | 'sinResponsable'
  | 'agrupacionId'
  | 'sinAgrupacion'
>;

/** Valor del option "Todos"/"Todas" en los selects con opción "Sin ...". */
export const VALOR_TODOS = '';
/** Valor del option "Sin responsable"/"Sin agrupación". */
export const VALOR_SIN = 'sin';

/** Debounce de la búsqueda por texto (I1). */
export const DEBOUNCE_TEXTO_INCIDENCIAS_MS = 300;

/** Convierte el valor de un select con "Todos"/"Sin ..." en id + marca "sin". */
export function leerSeleccionConSin(valor: string): {
  readonly id?: number;
  readonly sin: boolean;
} {
  if (valor === VALOR_SIN) {
    return { sin: true };
  }
  return { id: parsearId(valor) ?? undefined, sin: false };
}

/** Valor del select a partir de id + marca "sin". */
export function valorSeleccionConSin(id: number | undefined, sin: boolean): string {
  if (sin) {
    return VALOR_SIN;
  }
  return id === undefined ? VALOR_TODOS : String(id);
}

/** Compara dos listas de ids sin importar el orden. */
function mismosIds(a: readonly number[], b: readonly number[]): boolean {
  return a.length === b.length && a.every((id) => b.includes(id));
}

/** Compara los filtros editables para no emitir (ni volver a la página 1) sin cambios. */
function sonIguales(cambio: CambioFiltrosIncidencias, actuales: FiltrosIncidencias): boolean {
  return (
    cambio.texto === actuales.texto &&
    mismosIds(cambio.tipoIds, actuales.tipoIds) &&
    cambio.estadoId === actuales.estadoId &&
    cambio.prioridadId === actuales.prioridadId &&
    cambio.responsableId === actuales.responsableId &&
    cambio.sinResponsable === actuales.sinResponsable &&
    cambio.agrupacionId === actuales.agrupacionId &&
    cambio.sinAgrupacion === actuales.sinAgrupacion
  );
}

/**
 * Panel de filtros del listado de incidencias (I1). Texto con debounce de 300 ms; los selects
 * emiten al cambiar. Tipo es multi-selección; Responsable y Agrupación ofrecen "Sin ...".
 *
 * Creado: 2026-10-09
 * Modificado: 2026-10-09 - Creación inicial (RF-002)
 */
@Component({
  selector: 'app-incidencias-filtros',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [ReactiveFormsModule, CampoErrorComponent],
  template: `
    <form class="filtros" [formGroup]="form" (ngSubmit)="emitir()" role="search">
      <div class="campo">
        <label for="filtro-inc-texto">Texto (código o título)</label>
        <input
          id="filtro-inc-texto"
          type="search"
          formControlName="texto"
          [attr.aria-invalid]="form.controls.texto.invalid"
          aria-describedby="filtro-inc-texto-error"
        />
        <app-campo-error [mensajes]="erroresTexto()" idError="filtro-inc-texto-error" />
      </div>
      <div class="campo">
        <label for="filtro-inc-tipo">Tipo</label>
        <select
          id="filtro-inc-tipo"
          class="select-multiple"
          multiple
          formControlName="tipoIds"
          aria-describedby="filtro-inc-tipo-ayuda"
          (change)="emitir()"
        >
          @for (opcion of opcionesTipo(); track opcion.valor) {
            <option [ngValue]="opcion.valor">{{ opcion.etiqueta }}</option>
          }
        </select>
        <small id="filtro-inc-tipo-ayuda" class="ayuda">Ctrl + clic para elegir varios</small>
      </div>
      <div class="campo">
        <label for="filtro-inc-estado">Estado</label>
        <select id="filtro-inc-estado" formControlName="estadoId" (change)="emitir()">
          <option [ngValue]="null">Todos</option>
          @for (opcion of opcionesEstado(); track opcion.valor) {
            <option [ngValue]="opcion.valor">{{ opcion.etiqueta }}</option>
          }
        </select>
      </div>
      <div class="campo">
        <label for="filtro-inc-prioridad">Prioridad</label>
        <select id="filtro-inc-prioridad" formControlName="prioridadId" (change)="emitir()">
          <option [ngValue]="null">Todas</option>
          @for (opcion of opcionesPrioridad(); track opcion.valor) {
            <option [ngValue]="opcion.valor">{{ opcion.etiqueta }}</option>
          }
        </select>
      </div>
      <div class="campo">
        <label for="filtro-inc-responsable">Responsable</label>
        <select id="filtro-inc-responsable" formControlName="responsable" (change)="emitir()">
          <option [value]="valorTodos">Todos</option>
          <option [value]="valorSin">Sin responsable</option>
          @for (opcion of opcionesResponsable(); track opcion.valor) {
            <option [value]="opcion.valor">{{ opcion.etiqueta }}</option>
          }
        </select>
      </div>
      <div class="campo">
        <label for="filtro-inc-agrupacion">Agrupación</label>
        <select id="filtro-inc-agrupacion" formControlName="agrupacion" (change)="emitir()">
          <option [value]="valorTodos">Todas</option>
          <option [value]="valorSin">Sin agrupación</option>
          @for (agrupacion of agrupaciones(); track agrupacion.id) {
            <option [value]="agrupacion.id">{{ agrupacion.nombre }}</option>
          }
        </select>
      </div>
      <div class="campo campo-accion">
        <button type="button" class="btn" (click)="limpiar.emit()">Limpiar filtros</button>
      </div>
    </form>
  `,
})
export class IncidenciasFiltrosComponent {
  private readonly fb = inject(NonNullableFormBuilder);

  readonly filtros = input.required<FiltrosIncidencias>();
  readonly tipos = input.required<readonly TipoIncidenciaDto[]>();
  readonly estados = input.required<readonly EstadoIncidenciaDto[]>();
  readonly prioridades = input.required<readonly PrioridadIncidenciaDto[]>();
  readonly personas = input.required<readonly PersonaDto[]>();
  readonly agrupaciones = input.required<readonly AgrupacionDto[]>();
  /** Emite los filtros editados (la página vuelve a la página 1). */
  readonly cambio = output<CambioFiltrosIncidencias>();
  /** Solicita volver a los filtros por defecto. */
  readonly limpiar = output<void>();

  protected readonly valorTodos = VALOR_TODOS;
  protected readonly valorSin = VALOR_SIN;

  readonly form = this.fb.group({
    texto: this.fb.control('', [Validators.maxLength(LIMITES_INCIDENCIA.textoFiltroMax)]),
    tipoIds: this.fb.control<number[]>([]),
    estadoId: this.fb.control<number | null>(null),
    prioridadId: this.fb.control<number | null>(null),
    responsable: this.fb.control(VALOR_TODOS),
    agrupacion: this.fb.control(VALOR_TODOS),
  });

  readonly opcionesTipo = computed(() => opcionesFiltro(this.tipos()));
  readonly opcionesEstado = computed(() => opcionesFiltroEstado(this.estados()));
  readonly opcionesPrioridad = computed(() => opcionesFiltro(this.prioridades()));
  readonly opcionesResponsable = computed(() => opcionesFiltro(this.personas()));

  constructor() {
    effect(() => {
      const filtros = this.filtros();
      untracked(() => this.sincronizarFormulario(filtros));
    });
    this.form.controls.texto.valueChanges
      .pipe(debounceTime(DEBOUNCE_TEXTO_INCIDENCIAS_MS), takeUntilDestroyed())
      .subscribe(() => this.emitir());
  }

  /** Mensajes de error del filtro de texto. */
  erroresTexto(): string[] {
    return mensajesVisibles(this.form.controls.texto, 'El texto');
  }

  /** Emite los filtros actuales si son válidos y cambiaron. */
  emitir(): void {
    if (this.form.invalid) {
      return;
    }
    const valores = this.form.getRawValue();
    const texto = valores.texto.trim();
    const responsable = leerSeleccionConSin(valores.responsable);
    const agrupacion = leerSeleccionConSin(valores.agrupacion);
    const cambio: CambioFiltrosIncidencias = {
      texto: texto.length > 0 ? texto : undefined,
      tipoIds: valores.tipoIds,
      estadoId: valores.estadoId ?? undefined,
      prioridadId: valores.prioridadId ?? undefined,
      responsableId: responsable.id,
      sinResponsable: responsable.sin,
      agrupacionId: agrupacion.id,
      sinAgrupacion: agrupacion.sin,
    };
    if (!sonIguales(cambio, this.filtros())) {
      this.cambio.emit(cambio);
    }
  }

  /** Refleja en el formulario los filtros recibidos (URL) sin disparar emisiones. */
  private sincronizarFormulario(filtros: FiltrosIncidencias): void {
    const textoForm = this.form.controls.texto.value;
    const textoUrl = filtros.texto ?? '';
    this.form.patchValue(
      {
        texto: textoForm.trim() === textoUrl ? textoForm : textoUrl,
        tipoIds: [...filtros.tipoIds],
        estadoId: filtros.estadoId ?? null,
        prioridadId: filtros.prioridadId ?? null,
        responsable: valorSeleccionConSin(filtros.responsableId, filtros.sinResponsable),
        agrupacion: valorSeleccionConSin(filtros.agrupacionId, filtros.sinAgrupacion),
      },
      { emitEvent: false },
    );
  }
}
