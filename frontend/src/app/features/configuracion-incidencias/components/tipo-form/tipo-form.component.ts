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
import {
  longitudMaxima,
  recortar,
  recortarOpcional,
  textoRequerido,
} from '../../../../shared/validators/texto.validators';
import { FlujoIncidenciaDto } from '../../models/flujo-incidencia.model';
import { NivelIncidenciaDto } from '../../models/nivel-incidencia.model';
import {
  CrearTipoIncidenciaRequest,
  LIMITES_TIPO,
  TipoIncidenciaDto,
} from '../../models/tipo-incidencia.model';

/**
 * Formulario de tipo de incidencia (C1, RN-05/RN-06): Nombre (1–50), Descripción (máx. 250),
 * Nivel (solo lectura en edición) y Flujo. Errores de negocio sin campo arriba del form.
 *
 * Creado: 2026-10-09
 * Modificado: 2026-10-09 - Creación inicial (RF-002)
 */
@Component({
  selector: 'app-tipo-form',
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
        <label for="tipo-nombre">Nombre <span aria-hidden="true">*</span></label>
        <input
          id="tipo-nombre"
          type="text"
          formControlName="nombre"
          required
          [attr.aria-invalid]="errores('nombre', 'El nombre').length > 0"
          aria-describedby="tipo-nombre-error"
        />
        <app-campo-error [mensajes]="errores('nombre', 'El nombre')" idError="tipo-nombre-error" />
      </div>
      <div class="campo">
        <label for="tipo-descripcion">Descripción</label>
        <textarea
          id="tipo-descripcion"
          rows="3"
          formControlName="descripcion"
          [attr.aria-invalid]="errores('descripcion', 'La descripción').length > 0"
          aria-describedby="tipo-descripcion-contador tipo-descripcion-error"
        ></textarea>
        <small id="tipo-descripcion-contador" class="contador">
          {{ form.controls.descripcion.value.length }} / {{ limites.descripcionMax }}
        </small>
        <app-campo-error
          [mensajes]="errores('descripcion', 'La descripción')"
          idError="tipo-descripcion-error"
        />
      </div>
      <div class="fila-campos">
        <div class="campo">
          <label for="tipo-nivel">Nivel <span aria-hidden="true">*</span></label>
          <select
            id="tipo-nivel"
            formControlName="nivelId"
            required
            [attr.aria-invalid]="errores('nivelId', 'El nivel').length > 0"
            aria-describedby="tipo-nivel-ayuda tipo-nivel-error"
          >
            <option [ngValue]="null" disabled>Seleccioná un nivel</option>
            @for (nivel of niveles(); track nivel.id) {
              <option [ngValue]="nivel.id">Nivel {{ nivel.numero }} – {{ nivel.nombre }}</option>
            }
          </select>
          @if (esEdicion()) {
            <small id="tipo-nivel-ayuda" class="ayuda">El nivel no se puede cambiar</small>
          }
          <app-campo-error [mensajes]="errores('nivelId', 'El nivel')" idError="tipo-nivel-error" />
        </div>
        <div class="campo">
          <label for="tipo-flujo">Flujo <span aria-hidden="true">*</span></label>
          <select
            id="tipo-flujo"
            formControlName="flujoId"
            required
            [attr.aria-invalid]="errores('flujoId', 'El flujo').length > 0"
            aria-describedby="tipo-flujo-error"
          >
            <option [ngValue]="null" disabled>Seleccioná un flujo</option>
            @for (flujo of flujos(); track flujo.id) {
              <option [ngValue]="flujo.id">{{ flujo.nombre }}</option>
            }
          </select>
          <app-campo-error [mensajes]="errores('flujoId', 'El flujo')" idError="tipo-flujo-error" />
        </div>
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
export class TipoFormComponent implements OnInit {
  private readonly fb = inject(NonNullableFormBuilder);

  /** Tipo a editar (null en alta). */
  readonly tipo = input<TipoIncidenciaDto | null>(null);
  /** Nivel preseleccionado en el alta (`?nivelId=`). */
  readonly nivelIdInicial = input<number | null>(null);
  readonly niveles = input.required<readonly NivelIncidenciaDto[]>();
  readonly flujos = input.required<readonly FlujoIncidenciaDto[]>();
  readonly enviando = input(false);
  readonly errorServidor = input<ErrorFormulario>(SIN_ERRORES);
  /** En edición `nivelId` es el actual (la página lo descarta al armar el PUT). */
  readonly guardar = output<CrearTipoIncidenciaRequest>();
  readonly cancelar = output<void>();

  readonly limites = LIMITES_TIPO;
  readonly erroresGenerales = signal<readonly string[]>([]);
  readonly esEdicion = computed(() => this.tipo() !== null);

  readonly form = this.fb.group({
    nombre: this.fb.control('', [textoRequerido, longitudMaxima(LIMITES_TIPO.nombreMax)]),
    descripcion: this.fb.control('', [longitudMaxima(LIMITES_TIPO.descripcionMax)]),
    nivelId: this.fb.control<number | null>(null, [Validators.required]),
    flujoId: this.fb.control<number | null>(null, [Validators.required]),
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
    const tipo = this.tipo();
    if (tipo) {
      this.form.patchValue({
        nombre: tipo.nombre,
        descripcion: tipo.descripcion ?? '',
        nivelId: tipo.nivel.id,
        flujoId: tipo.flujo.id,
      });
      this.form.controls.nivelId.disable();
    } else {
      this.form.patchValue({ nivelId: this.nivelIdInicial() });
    }
  }

  /** Mensajes de error visibles del campo indicado. */
  errores(campo: keyof typeof this.form.controls, etiqueta: string): string[] {
    return mensajesVisibles(this.form.controls[campo], etiqueta);
  }

  /** Emite el request (descripción vacía → null) si el formulario es válido. */
  enviar(): void {
    const v = this.form.getRawValue();
    if (this.form.invalid || this.enviando() || v.nivelId === null || v.flujoId === null) {
      this.form.markAllAsTouched();
      return;
    }
    this.guardar.emit({
      nombre: recortar(v.nombre),
      descripcion: recortarOpcional(v.descripcion),
      nivelId: v.nivelId,
      flujoId: v.flujoId,
    });
  }
}
