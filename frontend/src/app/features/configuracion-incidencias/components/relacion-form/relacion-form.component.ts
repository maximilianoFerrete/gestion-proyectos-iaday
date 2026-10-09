import {
  ChangeDetectionStrategy,
  Component,
  computed,
  effect,
  inject,
  input,
  output,
  signal,
  untracked,
} from '@angular/core';
import { takeUntilDestroyed, toSignal } from '@angular/core/rxjs-interop';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { CampoErrorComponent } from '../../../../shared/components/campo-error/campo-error.component';
import {
  ErrorFormulario,
  SIN_ERRORES,
  aplicarErroresServidor,
} from '../../../../shared/forms/errores-servidor';
import { mensajesVisibles } from '../../../../shared/forms/mensajes-error';
import {
  CrearRelacionTipoIncidenciaRequest,
  RelacionTipoIncidenciaDto,
} from '../../models/relacion-tipo-incidencia.model';
import { TipoIncidenciaDto } from '../../models/tipo-incidencia.model';
import { tiposHijoElegibles, tiposPadreElegibles } from '../../utils/reglas-configuracion';

/**
 * Formulario "Nueva relación" (C2, RN-08): Tipo padre (activos) y Tipo hijo (activos, de nivel
 * mayor que el del padre y sin relación existente). La confirmación la pide la página.
 *
 * Creado: 2026-10-09
 * Modificado: 2026-10-09 - Creación inicial (RF-002)
 */
@Component({
  selector: 'app-relacion-form',
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
      <div class="fila-campos">
        <div class="campo">
          <label for="relacion-padre">Tipo padre <span aria-hidden="true">*</span></label>
          <select
            id="relacion-padre"
            formControlName="tipoPadreId"
            required
            [attr.aria-invalid]="errores('tipoPadreId', 'El tipo padre').length > 0"
            aria-describedby="relacion-padre-error"
          >
            <option [ngValue]="null" disabled>Seleccioná un tipo</option>
            @for (tipo of tiposPadre(); track tipo.id) {
              <option [ngValue]="tipo.id">{{ tipo.nombre }} (nivel {{ tipo.nivel.numero }})</option>
            }
          </select>
          <app-campo-error
            [mensajes]="errores('tipoPadreId', 'El tipo padre')"
            idError="relacion-padre-error"
          />
        </div>
        <div class="campo">
          <label for="relacion-hijo">Tipo hijo <span aria-hidden="true">*</span></label>
          <select
            id="relacion-hijo"
            formControlName="tipoHijoId"
            required
            [attr.aria-invalid]="errores('tipoHijoId', 'El tipo hijo').length > 0"
            aria-describedby="relacion-hijo-ayuda relacion-hijo-error"
          >
            <option [ngValue]="null" disabled>Seleccioná un tipo</option>
            @for (tipo of tiposHijo(); track tipo.id) {
              <option [ngValue]="tipo.id">{{ tipo.nombre }} (nivel {{ tipo.nivel.numero }})</option>
            }
          </select>
          <small id="relacion-hijo-ayuda" class="ayuda">
            Solo tipos de un nivel mayor que el del padre y sin relación existente.
          </small>
          <app-campo-error
            [mensajes]="errores('tipoHijoId', 'El tipo hijo')"
            idError="relacion-hijo-error"
          />
        </div>
      </div>
      <div class="acciones">
        <button type="button" class="btn" (click)="cancelar.emit()">Cancelar</button>
        <button type="submit" class="btn btn-primario" [disabled]="form.invalid || enviando()">
          {{ enviando() ? 'Guardando...' : 'Crear relación' }}
        </button>
      </div>
    </form>
  `,
})
export class RelacionFormComponent {
  private readonly fb = inject(NonNullableFormBuilder);

  readonly tipos = input.required<readonly TipoIncidenciaDto[]>();
  readonly relaciones = input.required<readonly RelacionTipoIncidenciaDto[]>();
  readonly enviando = input(false);
  readonly errorServidor = input<ErrorFormulario>(SIN_ERRORES);
  readonly guardar = output<CrearRelacionTipoIncidenciaRequest>();
  readonly cancelar = output<void>();

  readonly erroresGenerales = signal<readonly string[]>([]);

  readonly form = this.fb.group({
    tipoPadreId: this.fb.control<number | null>(null, [Validators.required]),
    tipoHijoId: this.fb.control<number | null>(null, [Validators.required]),
  });

  private readonly padreElegido = toSignal(this.form.controls.tipoPadreId.valueChanges, {
    initialValue: null,
  });

  readonly tiposPadre = computed(() => tiposPadreElegibles(this.tipos()));
  readonly tiposHijo = computed(() =>
    tiposHijoElegibles(this.tipos(), this.relaciones(), this.padreElegido()),
  );

  constructor() {
    effect(() => {
      const error = this.errorServidor();
      untracked(() => {
        const sinCampo = aplicarErroresServidor(this.form, error.campos);
        this.erroresGenerales.set(error.general ? [error.general, ...sinCampo] : sinCampo);
      });
    });
    // Al cambiar el padre, el hijo elegido puede dejar de ser válido.
    this.form.controls.tipoPadreId.valueChanges
      .pipe(takeUntilDestroyed())
      .subscribe(() => this.form.controls.tipoHijoId.setValue(null));
  }

  /** Mensajes de error visibles del campo indicado. */
  errores(campo: keyof typeof this.form.controls, etiqueta: string): string[] {
    return mensajesVisibles(this.form.controls[campo], etiqueta);
  }

  /** Emite el request si el formulario es válido. */
  enviar(): void {
    const { tipoPadreId, tipoHijoId } = this.form.getRawValue();
    if (this.form.invalid || this.enviando() || tipoPadreId === null || tipoHijoId === null) {
      this.form.markAllAsTouched();
      return;
    }
    this.guardar.emit({ tipoPadreId, tipoHijoId });
  }
}
