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
import { FiltroArchivados } from '../../../proyectos/models/proyecto.model';
import { ConsultaTableroProyectos, LIMITES_SEGUIMIENTO } from '../../models/seguimiento.model';

/** Filtros editables del tablero general (sin alcance, paginación ni orden). */
export type CambioFiltrosTableroGeneral = Pick<ConsultaTableroProyectos, 'texto' | 'archivados'>;

/** Debounce de la búsqueda por texto (T1). */
export const DEBOUNCE_TEXTO_SEGUIMIENTO_MS = 300;

/**
 * Filtros de texto (código o nombre, máx. 150, debounce 300 ms) y Archivados del tablero
 * general (T1, RF-005), con "Limpiar filtros".
 *
 * Creado: 2026-10-10
 * Modificado: 2026-10-10 - Creación inicial (RF-005)
 */
@Component({
  selector: 'app-filtros-tablero-general',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [ReactiveFormsModule, CampoErrorComponent],
  template: `
    <form class="filtros" [formGroup]="form" (ngSubmit)="emitir()" role="search">
      <div class="campo">
        <label for="t1-filtro-texto">Texto (código o nombre)</label>
        <input
          id="t1-filtro-texto"
          type="search"
          formControlName="texto"
          [maxlength]="limites.textoProyectosMax"
          [attr.aria-invalid]="form.controls.texto.invalid"
          aria-describedby="t1-filtro-texto-error"
        />
        <app-campo-error [mensajes]="erroresTexto()" idError="t1-filtro-texto-error" />
      </div>
      <div class="campo">
        <label for="t1-filtro-archivados">Archivados</label>
        <select id="t1-filtro-archivados" formControlName="archivados" (change)="emitir()">
          <option value="excluir">No archivados</option>
          <option value="solo">Solo archivados</option>
          <option value="incluir">Todos</option>
        </select>
      </div>
      <div class="campo campo-accion">
        <button type="button" class="btn" [disabled]="!hayFiltros()" (click)="limpiar.emit()">
          Limpiar filtros
        </button>
      </div>
    </form>
  `,
})
export class FiltrosTableroGeneralComponent {
  private readonly fb = inject(NonNullableFormBuilder);

  readonly consulta = input.required<ConsultaTableroProyectos>();
  /** Emite texto y archivados (la página vuelve a la página 1). */
  readonly cambio = output<CambioFiltrosTableroGeneral>();
  /** Solicita volver a los filtros por defecto (incluido el alcance). */
  readonly limpiar = output<void>();

  readonly limites = LIMITES_SEGUIMIENTO;

  readonly form = this.fb.group({
    texto: this.fb.control('', [Validators.maxLength(LIMITES_SEGUIMIENTO.textoProyectosMax)]),
    archivados: this.fb.control<FiltroArchivados>('excluir'),
  });

  /** Hay algún filtro distinto del valor por defecto (texto, archivados o alcance). */
  readonly hayFiltros = computed(() => {
    const c = this.consulta();
    return (
      c.texto !== undefined ||
      c.archivados !== 'excluir' ||
      c.nivelIds.length > 0 ||
      c.tipoIds.length > 0
    );
  });

  constructor() {
    effect(() => {
      const consulta = this.consulta();
      untracked(() => this.sincronizar(consulta));
    });
    this.form.controls.texto.valueChanges
      .pipe(debounceTime(DEBOUNCE_TEXTO_SEGUIMIENTO_MS), takeUntilDestroyed())
      .subscribe(() => this.emitir());
  }

  /** Mensajes de error del filtro de texto. */
  erroresTexto(): string[] {
    return mensajesVisibles(this.form.controls.texto, 'El texto');
  }

  /** Emite los filtros si son válidos y cambiaron. */
  emitir(): void {
    if (this.form.invalid) {
      return;
    }
    const valores = this.form.getRawValue();
    const texto = valores.texto.trim();
    const cambio: CambioFiltrosTableroGeneral = {
      texto: texto.length > 0 ? texto : undefined,
      archivados: valores.archivados,
    };
    const actual = this.consulta();
    if (cambio.texto !== actual.texto || cambio.archivados !== actual.archivados) {
      this.cambio.emit(cambio);
    }
  }

  /** Refleja en el formulario la consulta vigente (URL) sin disparar emisiones. */
  private sincronizar(consulta: ConsultaTableroProyectos): void {
    const textoForm = this.form.controls.texto.value;
    const textoUrl = consulta.texto ?? '';
    this.form.patchValue(
      {
        texto: textoForm.trim() === textoUrl ? textoForm : textoUrl,
        archivados: consulta.archivados,
      },
      { emitEvent: false },
    );
  }
}
