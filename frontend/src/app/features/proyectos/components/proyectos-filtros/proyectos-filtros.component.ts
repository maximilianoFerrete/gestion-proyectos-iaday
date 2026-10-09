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
import { EstadoProyectoDto } from '../../../estados-proyecto/models/estado-proyecto.model';
import { PersonaDto } from '../../../personas/models/persona.model';
import { FiltroArchivados, FiltrosProyectos, LIMITES_PROYECTO } from '../../models/proyecto.model';
import { opcionesFiltroEstado, opcionesFiltroResponsable } from '../../utils/opciones-proyecto';

/** Filtros editables desde el panel (sin paginación ni orden). */
export type CambioFiltrosProyectos = Pick<
  FiltrosProyectos,
  'texto' | 'estadoId' | 'responsableId' | 'archivados'
>;

/** Compara los filtros editables para no emitir (ni volver a la página 1) sin cambios. */
function sonIguales(cambio: CambioFiltrosProyectos, actuales: FiltrosProyectos): boolean {
  return (
    cambio.texto === actuales.texto &&
    cambio.estadoId === actuales.estadoId &&
    cambio.responsableId === actuales.responsableId &&
    cambio.archivados === actuales.archivados
  );
}

/** Debounce de la búsqueda por texto (P1). */
export const DEBOUNCE_TEXTO_MS = 300;

/**
 * Panel de filtros del listado de proyectos (P1). El texto se emite con debounce de 300 ms;
 * los selects, al cambiar.
 *
 * Creado: 2026-10-09
 * Modificado: 2026-10-09 - Creación inicial (RF-001)
 */
@Component({
  selector: 'app-proyectos-filtros',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [ReactiveFormsModule, CampoErrorComponent],
  template: `
    <form class="filtros" [formGroup]="form" (ngSubmit)="emitir()" role="search">
      <div class="campo">
        <label for="filtro-texto">Texto (código o nombre)</label>
        <input
          id="filtro-texto"
          type="search"
          formControlName="texto"
          [attr.aria-invalid]="form.controls.texto.invalid"
          aria-describedby="filtro-texto-error"
        />
        <app-campo-error [mensajes]="erroresTexto()" idError="filtro-texto-error" />
      </div>
      <div class="campo">
        <label for="filtro-estado">Estado</label>
        <select id="filtro-estado" formControlName="estadoId" (change)="emitir()">
          <option [ngValue]="null">Todos</option>
          @for (opcion of opcionesEstado(); track opcion.valor) {
            <option [ngValue]="opcion.valor">{{ opcion.etiqueta }}</option>
          }
        </select>
      </div>
      <div class="campo">
        <label for="filtro-responsable">Responsable</label>
        <select id="filtro-responsable" formControlName="responsableId" (change)="emitir()">
          <option [ngValue]="null">Todos</option>
          @for (opcion of opcionesResponsable(); track opcion.valor) {
            <option [ngValue]="opcion.valor">{{ opcion.etiqueta }}</option>
          }
        </select>
      </div>
      <div class="campo">
        <label for="filtro-archivados">Archivados</label>
        <select id="filtro-archivados" formControlName="archivados" (change)="emitir()">
          <option value="excluir">No archivados</option>
          <option value="solo">Solo archivados</option>
          <option value="incluir">Todos</option>
        </select>
      </div>
      <div class="campo campo-accion">
        <button type="button" class="btn" (click)="limpiar.emit()">Limpiar filtros</button>
      </div>
    </form>
  `,
})
export class ProyectosFiltrosComponent {
  private readonly fb = inject(NonNullableFormBuilder);

  readonly filtros = input.required<FiltrosProyectos>();
  readonly estados = input.required<readonly EstadoProyectoDto[]>();
  readonly personas = input.required<readonly PersonaDto[]>();
  /** Emite los filtros editados (la página vuelve a la página 1). */
  readonly cambio = output<CambioFiltrosProyectos>();
  /** Solicita volver a los filtros por defecto. */
  readonly limpiar = output<void>();

  readonly form = this.fb.group({
    texto: this.fb.control('', [Validators.maxLength(LIMITES_PROYECTO.textoFiltroMax)]),
    estadoId: this.fb.control<number | null>(null),
    responsableId: this.fb.control<number | null>(null),
    archivados: this.fb.control<FiltroArchivados>('excluir'),
  });

  readonly opcionesEstado = computed(() => opcionesFiltroEstado(this.estados()));
  readonly opcionesResponsable = computed(() => opcionesFiltroResponsable(this.personas()));

  constructor() {
    effect(() => {
      const filtros = this.filtros();
      untracked(() => this.sincronizarFormulario(filtros));
    });
    this.form.controls.texto.valueChanges
      .pipe(debounceTime(DEBOUNCE_TEXTO_MS), takeUntilDestroyed())
      .subscribe(() => this.emitir());
  }

  /** Mensajes de error del filtro de texto. */
  erroresTexto(): string[] {
    return mensajesVisibles(this.form.controls.texto, 'El texto');
  }

  /** Emite los filtros actuales si son válidos. */
  emitir(): void {
    if (this.form.invalid) {
      return;
    }
    const valores = this.form.getRawValue();
    const texto = valores.texto.trim();
    const cambio: CambioFiltrosProyectos = {
      texto: texto.length > 0 ? texto : undefined,
      estadoId: valores.estadoId ?? undefined,
      responsableId: valores.responsableId ?? undefined,
      archivados: valores.archivados,
    };
    if (!sonIguales(cambio, this.filtros())) {
      this.cambio.emit(cambio);
    }
  }

  /** Refleja en el formulario los filtros recibidos (URL) sin disparar emisiones. */
  private sincronizarFormulario(filtros: FiltrosProyectos): void {
    const textoForm = this.form.controls.texto.value;
    const textoUrl = filtros.texto ?? '';
    this.form.patchValue(
      {
        // Conserva lo tipeado (p. ej. espacios finales) si equivale al filtro vigente.
        texto: textoForm.trim() === textoUrl ? textoForm : textoUrl,
        estadoId: filtros.estadoId ?? null,
        responsableId: filtros.responsableId ?? null,
        archivados: filtros.archivados,
      },
      { emitEvent: false },
    );
  }
}
