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
  CATEGORIAS_ESTADO_INCIDENCIA,
  CategoriaEstadoIncidencia,
  EstadoIncidenciaDto,
  EstadoIncidenciaRequest,
  LIMITES_ESTADO_INCIDENCIA,
  NOMBRES_CATEGORIA_ESTADO,
} from '../../models/estado-incidencia.model';
import { esInicialOFinalEnAlgunFlujo } from '../../utils/reglas-configuracion';

/** Ayuda del select de categoría (RF-005 "Cambios en C3"). */
export const AYUDA_CATEGORIA_ESTADO =
  'Se usa en el tablero de seguimiento. Un estado con categoría no puede ser inicial ni final ' +
  'de un flujo.';

/**
 * Formulario de estado de incidencia (C3): Nombre, Descripción, Orden (RN-10) y Categoría de
 * seguimiento (RF-005 RN-17). Si el estado es inicial o final en algún flujo, el select de
 * categoría queda deshabilitado (RF-005 RN-18).
 *
 * Creado: 2026-10-10
 * Modificado: 2026-10-10 - Creación inicial (RF-005; reemplaza a NombreOrdenForm en C3)
 */
@Component({
  selector: 'app-estado-incidencia-form',
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
        <label for="estado-incidencia-nombre">Nombre <span aria-hidden="true">*</span></label>
        <input
          id="estado-incidencia-nombre"
          type="text"
          formControlName="nombre"
          required
          [attr.aria-invalid]="errores('nombre', 'El nombre').length > 0"
          aria-describedby="estado-incidencia-nombre-error"
        />
        <app-campo-error
          [mensajes]="errores('nombre', 'El nombre')"
          idError="estado-incidencia-nombre-error"
        />
      </div>
      <div class="campo">
        <label for="estado-incidencia-descripcion">Descripción</label>
        <textarea
          id="estado-incidencia-descripcion"
          rows="3"
          formControlName="descripcion"
          [attr.aria-invalid]="errores('descripcion', 'La descripción').length > 0"
          aria-describedby="estado-incidencia-descripcion-contador estado-incidencia-descripcion-error"
        ></textarea>
        <small id="estado-incidencia-descripcion-contador" class="contador">
          {{ form.controls.descripcion.value.length }} / {{ limites.descripcionMax }}
        </small>
        <app-campo-error
          [mensajes]="errores('descripcion', 'La descripción')"
          idError="estado-incidencia-descripcion-error"
        />
      </div>
      <div class="campo campo-corto">
        <label for="estado-incidencia-orden">Orden <span aria-hidden="true">*</span></label>
        <input
          id="estado-incidencia-orden"
          type="number"
          formControlName="orden"
          required
          step="1"
          [min]="limites.ordenMin"
          [max]="limites.ordenMax"
          [attr.aria-invalid]="errores('orden', 'El orden').length > 0"
          aria-describedby="estado-incidencia-orden-error"
        />
        <app-campo-error
          [mensajes]="errores('orden', 'El orden')"
          idError="estado-incidencia-orden-error"
        />
      </div>
      <div class="campo">
        <label for="estado-incidencia-categoria">Categoría de seguimiento</label>
        <select
          id="estado-incidencia-categoria"
          formControlName="categoria"
          [attr.aria-invalid]="errores('categoria', 'La categoría').length > 0"
          aria-describedby="estado-incidencia-categoria-ayuda estado-incidencia-categoria-error"
        >
          <option [ngValue]="null">Ninguna</option>
          @for (categoria of categorias; track categoria) {
            <option [ngValue]="categoria">{{ nombresCategoria[categoria] }}</option>
          }
        </select>
        <small id="estado-incidencia-categoria-ayuda" class="ayuda">
          {{ ayudaCategoria }}
          @if (categoriaBloqueada()) {
            Este estado es inicial o final en algún flujo.
          }
        </small>
        <app-campo-error
          [mensajes]="errores('categoria', 'La categoría')"
          idError="estado-incidencia-categoria-error"
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
export class EstadoIncidenciaFormComponent implements OnInit {
  private readonly fb = inject(NonNullableFormBuilder);

  /** Estado a editar (null en alta). */
  readonly estado = input<EstadoIncidenciaDto | null>(null);
  /** Orden sugerido en el alta. */
  readonly ordenSugerido = input<number | null>(null);
  readonly enviando = input(false);
  readonly errorServidor = input<ErrorFormulario>(SIN_ERRORES);
  readonly guardar = output<EstadoIncidenciaRequest>();
  readonly cancelar = output<void>();

  readonly limites = LIMITES_ESTADO_INCIDENCIA;
  readonly categorias = CATEGORIAS_ESTADO_INCIDENCIA;
  readonly nombresCategoria = NOMBRES_CATEGORIA_ESTADO;
  readonly ayudaCategoria = AYUDA_CATEGORIA_ESTADO;
  readonly erroresGenerales = signal<readonly string[]>([]);

  /** El estado es inicial o final en algún flujo: no admite categoría (RF-005 RN-18). */
  readonly categoriaBloqueada = computed(() => {
    const estado = this.estado();
    return estado !== null && esInicialOFinalEnAlgunFlujo(estado);
  });

  readonly form = this.fb.group({
    nombre: this.fb.control('', [
      textoRequerido,
      longitudMaxima(LIMITES_ESTADO_INCIDENCIA.nombreMax),
    ]),
    descripcion: this.fb.control('', [longitudMaxima(LIMITES_ESTADO_INCIDENCIA.descripcionMax)]),
    orden: this.fb.control<number | null>(null, [
      Validators.required,
      entero,
      Validators.min(LIMITES_ESTADO_INCIDENCIA.ordenMin),
      Validators.max(LIMITES_ESTADO_INCIDENCIA.ordenMax),
    ]),
    categoria: this.fb.control<CategoriaEstadoIncidencia | null>(null),
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
    this.form.reset({
      nombre: estado?.nombre ?? '',
      descripcion: estado?.descripcion ?? '',
      orden: estado?.orden ?? this.ordenSugerido(),
      categoria: estado?.categoria ?? null,
    });
    if (this.categoriaBloqueada()) {
      this.form.controls.categoria.disable();
    }
  }

  /** Mensajes de error visibles del campo indicado. */
  errores(campo: keyof typeof this.form.controls, etiqueta: string): string[] {
    return mensajesVisibles(this.form.controls[campo], etiqueta);
  }

  /** Emite el request (descripción vacía → null) si el formulario es válido. */
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
      categoria: v.categoria,
    });
  }
}
