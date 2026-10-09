import {
  ChangeDetectionStrategy,
  Component,
  OnInit,
  computed,
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
import {
  EstadoProyectoDto,
  EstadoProyectoRequest,
  LIMITES_ESTADO,
} from '../../models/estado-proyecto.model';

/**
 * Formulario de estado de proyecto (P7): Nombre (1–50), Descripción (máx. 250), Orden
 * (entero 1–9998) y Es final (deshabilitado si el estado es el inicial, RN-12).
 *
 * Creado: 2026-10-09
 * Modificado: 2026-10-09 - Creación inicial (RF-001)
 */
@Component({
  selector: 'app-estado-proyecto-form',
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
        <label for="estado-nombre">Nombre <span aria-hidden="true">*</span></label>
        <input
          id="estado-nombre"
          type="text"
          formControlName="nombre"
          required
          [attr.aria-invalid]="errores('nombre', 'El nombre').length > 0"
          aria-describedby="estado-nombre-error"
        />
        <app-campo-error
          [mensajes]="errores('nombre', 'El nombre')"
          idError="estado-nombre-error"
        />
      </div>
      <div class="campo">
        <label for="estado-descripcion">Descripción</label>
        <textarea
          id="estado-descripcion"
          rows="3"
          formControlName="descripcion"
          [attr.aria-invalid]="errores('descripcion', 'La descripción').length > 0"
          aria-describedby="estado-descripcion-contador estado-descripcion-error"
        ></textarea>
        <small id="estado-descripcion-contador" class="contador">
          {{ form.controls.descripcion.value.length }} / {{ limites.descripcionMax }}
        </small>
        <app-campo-error
          [mensajes]="errores('descripcion', 'La descripción')"
          idError="estado-descripcion-error"
        />
      </div>
      <div class="campo campo-corto">
        <label for="estado-orden">Orden <span aria-hidden="true">*</span></label>
        <input
          id="estado-orden"
          type="number"
          formControlName="orden"
          required
          step="1"
          [min]="limites.ordenMin"
          [max]="limites.ordenMax"
          [attr.aria-invalid]="errores('orden', 'El orden').length > 0"
          aria-describedby="estado-orden-error"
        />
        <app-campo-error [mensajes]="errores('orden', 'El orden')" idError="estado-orden-error" />
      </div>
      <div class="campo campo-check">
        <input
          id="estado-final"
          type="checkbox"
          formControlName="esFinal"
          aria-describedby="estado-final-ayuda"
        />
        <label for="estado-final">Es final</label>
        @if (esInicial()) {
          <small id="estado-final-ayuda" class="ayuda">El estado inicial no puede ser final</small>
        }
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
export class EstadoProyectoFormComponent implements OnInit {
  private readonly fb = inject(NonNullableFormBuilder);

  /** Estado a editar (null en alta). */
  readonly estado = input<EstadoProyectoDto | null>(null);
  /** Orden sugerido para el alta. */
  readonly ordenSugerido = input<number | null>(null);
  readonly enviando = input(false);
  readonly errorServidor = input<ErrorFormulario>(SIN_ERRORES);
  readonly guardar = output<EstadoProyectoRequest>();
  readonly cancelar = output<void>();

  readonly limites = LIMITES_ESTADO;
  readonly erroresGenerales = signal<readonly string[]>([]);
  readonly esInicial = computed(() => this.estado()?.esInicial ?? false);

  readonly form = this.fb.group({
    nombre: this.fb.control('', [textoRequerido, longitudMaxima(LIMITES_ESTADO.nombreMax)]),
    descripcion: this.fb.control('', [longitudMaxima(LIMITES_ESTADO.descripcionMax)]),
    orden: this.fb.control<number | null>(null, [
      Validators.required,
      entero,
      Validators.min(LIMITES_ESTADO.ordenMin),
      Validators.max(LIMITES_ESTADO.ordenMax),
    ]),
    esFinal: this.fb.control(false),
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
    const estado = this.estado();
    if (estado) {
      this.form.patchValue({
        nombre: estado.nombre,
        descripcion: estado.descripcion ?? '',
        orden: estado.orden,
        esFinal: estado.esFinal,
      });
    } else {
      this.form.patchValue({ orden: this.ordenSugerido() });
    }
    if (this.esInicial()) {
      this.form.controls.esFinal.disable();
    }
  }

  /** Mensajes de error visibles del campo indicado. */
  errores(campo: keyof typeof this.form.controls, etiqueta: string): string[] {
    return mensajesVisibles(this.form.controls[campo], etiqueta);
  }

  /** Emite el request (descripción vacía → null) si el formulario es válido. */
  enviar(): void {
    if (this.form.invalid || this.enviando()) {
      this.form.markAllAsTouched();
      return;
    }
    const valores = this.form.getRawValue();
    if (valores.orden === null) {
      return;
    }
    this.guardar.emit({
      nombre: recortar(valores.nombre),
      descripcion: recortarOpcional(valores.descripcion),
      orden: valores.orden,
      esFinal: valores.esFinal,
    });
  }
}
