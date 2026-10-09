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
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { CampoErrorComponent } from '../../../../shared/components/campo-error/campo-error.component';
import {
  ErrorFormulario,
  SIN_ERRORES,
  aplicarErroresServidor,
} from '../../../../shared/forms/errores-servidor';
import { mensajesVisibles } from '../../../../shared/forms/mensajes-error';
import { entero } from '../../../../shared/validators/numero.validators';
import {
  longitudMaxima,
  recortar,
  recortarOpcional,
  textoRequerido,
} from '../../../../shared/validators/texto.validators';

/** Valores del formulario (mismo shape que `EstadoIncidenciaRequest` y `PrioridadIncidenciaRequest`). */
export interface ValoresNombreOrden {
  readonly nombre: string;
  readonly descripcion: string | null;
  readonly orden: number;
}

/** Límites que aplica el formulario. */
export interface LimitesNombreOrden {
  readonly nombreMax: number;
  readonly descripcionMax: number;
  readonly ordenMin: number;
  readonly ordenMax: number;
}

/**
 * Formulario Nombre / Descripción / Orden compartido por Estados de incidencia (C3, RN-10) y
 * Prioridades (C5, RN-17); ambos tienen las mismas reglas (50 / 250 / 1–9999).
 *
 * Creado: 2026-10-09
 * Modificado: 2026-10-09 - Creación inicial (RF-002)
 */
@Component({
  selector: 'app-nombre-orden-form',
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
        <label [for]="prefijo() + '-nombre'">Nombre <span aria-hidden="true">*</span></label>
        <input
          [id]="prefijo() + '-nombre'"
          type="text"
          formControlName="nombre"
          required
          [attr.aria-invalid]="errores('nombre', 'El nombre').length > 0"
          [attr.aria-describedby]="prefijo() + '-nombre-error'"
        />
        <app-campo-error
          [mensajes]="errores('nombre', 'El nombre')"
          [idError]="prefijo() + '-nombre-error'"
        />
      </div>
      <div class="campo">
        <label [for]="prefijo() + '-descripcion'">Descripción</label>
        <textarea
          [id]="prefijo() + '-descripcion'"
          rows="3"
          formControlName="descripcion"
          [attr.aria-invalid]="errores('descripcion', 'La descripción').length > 0"
          [attr.aria-describedby]="
            prefijo() + '-descripcion-contador ' + prefijo() + '-descripcion-error'
          "
        ></textarea>
        <small [id]="prefijo() + '-descripcion-contador'" class="contador">
          {{ form.controls.descripcion.value.length }} / {{ limites().descripcionMax }}
        </small>
        <app-campo-error
          [mensajes]="errores('descripcion', 'La descripción')"
          [idError]="prefijo() + '-descripcion-error'"
        />
      </div>
      <div class="campo campo-corto">
        <label [for]="prefijo() + '-orden'">Orden <span aria-hidden="true">*</span></label>
        <input
          [id]="prefijo() + '-orden'"
          type="number"
          formControlName="orden"
          required
          step="1"
          [min]="limites().ordenMin"
          [max]="limites().ordenMax"
          [attr.aria-invalid]="errores('orden', 'El orden').length > 0"
          [attr.aria-describedby]="prefijo() + '-orden-error'"
        />
        <app-campo-error
          [mensajes]="errores('orden', 'El orden')"
          [idError]="prefijo() + '-orden-error'"
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
export class NombreOrdenFormComponent implements OnInit {
  private readonly fb = inject(NonNullableFormBuilder);

  /** Prefijo de los ids de los campos (únicos en la página). */
  readonly prefijo = input.required<string>();
  readonly limites = input.required<LimitesNombreOrden>();
  /** Valores iniciales (edición) o null (alta). */
  readonly valores = input<ValoresNombreOrden | null>(null);
  /** Orden sugerido en el alta. */
  readonly ordenSugerido = input<number | null>(null);
  readonly enviando = input(false);
  readonly errorServidor = input<ErrorFormulario>(SIN_ERRORES);
  readonly guardar = output<ValoresNombreOrden>();
  readonly cancelar = output<void>();

  readonly erroresGenerales = signal<readonly string[]>([]);

  readonly form = this.fb.group({
    nombre: this.fb.control('', [textoRequerido]),
    descripcion: this.fb.control(''),
    orden: this.fb.control<number | null>(null, [Validators.required, entero]),
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
    const limites = this.limites();
    this.form.controls.nombre.addValidators(longitudMaxima(limites.nombreMax));
    this.form.controls.descripcion.addValidators(longitudMaxima(limites.descripcionMax));
    this.form.controls.orden.addValidators([
      Validators.min(limites.ordenMin),
      Validators.max(limites.ordenMax),
    ]);
    const valores = this.valores();
    this.form.reset({
      nombre: valores?.nombre ?? '',
      descripcion: valores?.descripcion ?? '',
      orden: valores?.orden ?? this.ordenSugerido(),
    });
  }

  /** Mensajes de error visibles del campo indicado. */
  errores(campo: keyof typeof this.form.controls, etiqueta: string): string[] {
    return mensajesVisibles(this.form.controls[campo], etiqueta);
  }

  /** Emite los valores (descripción vacía → null) si el formulario es válido. */
  enviar(): void {
    const v = this.form.getRawValue();
    if (this.form.invalid || this.enviando() || v.orden === null) {
      this.form.markAllAsTouched();
      return;
    }
    this.guardar.emit({
      nombre: recortar(v.nombre),
      descripcion: recortarOpcional(v.descripcion),
      orden: v.orden,
    });
  }
}
