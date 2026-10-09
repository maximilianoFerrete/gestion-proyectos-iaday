import {
  ChangeDetectionStrategy,
  Component,
  OnInit,
  effect,
  inject,
  input,
  output,
  signal,
  untracked,
} from '@angular/core';
import { NonNullableFormBuilder, ReactiveFormsModule } from '@angular/forms';
import { CampoErrorComponent } from '../../../../shared/components/campo-error/campo-error.component';
import {
  ErrorFormulario,
  SIN_ERRORES,
  aplicarErroresServidor,
} from '../../../../shared/forms/errores-servidor';
import { mensajesVisibles } from '../../../../shared/forms/mensajes-error';
import {
  longitudMaxima,
  recortar,
  textoRequerido,
} from '../../../../shared/validators/texto.validators';
import {
  AgrupacionDto,
  AgrupacionRequest,
  LIMITES_AGRUPACION,
} from '../../models/agrupacion.model';

/**
 * Formulario de agrupación (P5): Nombre (1–100) y Descripción (1–1000), RN-26.
 *
 * Creado: 2026-10-09
 * Modificado: 2026-10-09 - Creación inicial (RF-001)
 */
@Component({
  selector: 'app-agrupacion-form',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [ReactiveFormsModule, CampoErrorComponent],
  template: `
    <form class="formulario" [formGroup]="form" (ngSubmit)="enviar()" novalidate>
      @if (erroresGenerales().length > 0) {
        <div class="alerta alerta-error" role="alert">
          @for (mensaje of erroresGenerales(); track $index) {
            <p>{{ mensaje }}</p>
          }
        </div>
      }
      <div class="campo">
        <label for="agrupacion-nombre">Nombre <span aria-hidden="true">*</span></label>
        <input
          id="agrupacion-nombre"
          type="text"
          formControlName="nombre"
          required
          [attr.aria-invalid]="errores('nombre', 'El nombre').length > 0"
          aria-describedby="agrupacion-nombre-error"
        />
        <app-campo-error
          [mensajes]="errores('nombre', 'El nombre')"
          idError="agrupacion-nombre-error"
        />
      </div>
      <div class="campo">
        <label for="agrupacion-descripcion">Descripción <span aria-hidden="true">*</span></label>
        <textarea
          id="agrupacion-descripcion"
          rows="4"
          formControlName="descripcion"
          required
          [attr.aria-invalid]="errores('descripcion', 'La descripción').length > 0"
          aria-describedby="agrupacion-descripcion-contador agrupacion-descripcion-error"
        ></textarea>
        <small id="agrupacion-descripcion-contador" class="contador">
          {{ form.controls.descripcion.value.length }} / {{ limites.descripcionMax }}
        </small>
        <app-campo-error
          [mensajes]="errores('descripcion', 'La descripción')"
          idError="agrupacion-descripcion-error"
        />
      </div>
      <div class="acciones">
        <button type="button" class="btn" (click)="cancelar.emit()">Cancelar</button>
        <button type="submit" class="btn btn-primario" [disabled]="form.invalid || enviando()">
          {{ enviando() ? 'Guardando...' : 'Guardar' }}
        </button>
      </div>
    </form>
  `,
})
export class AgrupacionFormComponent implements OnInit {
  private readonly fb = inject(NonNullableFormBuilder);

  /** Agrupación a editar (null en alta). */
  readonly agrupacion = input<AgrupacionDto | null>(null);
  readonly enviando = input(false);
  readonly errorServidor = input<ErrorFormulario>(SIN_ERRORES);
  readonly guardar = output<AgrupacionRequest>();
  readonly cancelar = output<void>();

  readonly limites = LIMITES_AGRUPACION;
  readonly erroresGenerales = signal<readonly string[]>([]);

  readonly form = this.fb.group({
    nombre: this.fb.control('', [textoRequerido, longitudMaxima(LIMITES_AGRUPACION.nombreMax)]),
    descripcion: this.fb.control('', [
      textoRequerido,
      longitudMaxima(LIMITES_AGRUPACION.descripcionMax),
    ]),
  });

  constructor() {
    effect(() => {
      const error = this.errorServidor();
      untracked(() => {
        const sinCampo = aplicarErroresServidor(this.form, error.campos);
        this.erroresGenerales.set(error.general ? [error.general, ...sinCampo] : sinCampo);
      });
    });
  }

  ngOnInit(): void {
    const agrupacion = this.agrupacion();
    if (agrupacion) {
      this.form.patchValue({ nombre: agrupacion.nombre, descripcion: agrupacion.descripcion });
    }
  }

  /** Mensajes de error visibles del campo indicado. */
  errores(campo: keyof typeof this.form.controls, etiqueta: string): string[] {
    return mensajesVisibles(this.form.controls[campo], etiqueta);
  }

  /** Emite el request con textos recortados si el formulario es válido. */
  enviar(): void {
    if (this.form.invalid || this.enviando()) {
      this.form.markAllAsTouched();
      return;
    }
    const valores = this.form.getRawValue();
    this.guardar.emit({
      nombre: recortar(valores.nombre),
      descripcion: recortar(valores.descripcion),
    });
  }
}
