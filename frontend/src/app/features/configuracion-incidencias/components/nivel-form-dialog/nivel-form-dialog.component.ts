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
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { CampoErrorComponent } from '../../../../shared/components/campo-error/campo-error.component';
import { DialogoComponent } from '../../../../shared/components/dialogo/dialogo.component';
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
  CrearNivelIncidenciaRequest,
  LIMITES_NIVEL,
  NivelIncidenciaDto,
} from '../../models/nivel-incidencia.model';

/**
 * Diálogo de alta/edición de nivel (C1, RN-04): Número (1–99, solo en alta), Nombre (1–50) y
 * Descripción (máx. 250). En edición el número no se envía (la página arma el request).
 *
 * Creado: 2026-10-09
 * Modificado: 2026-10-09 - Creación inicial (RF-002)
 */
@Component({
  selector: 'app-nivel-form-dialog',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [ReactiveFormsModule, CampoErrorComponent, DialogoComponent],
  template: `
    <app-dialogo
      [abierto]="abierto()"
      [titulo]="nivel() ? 'Editar nivel' : 'Nuevo nivel'"
      idTitulo="nivel-dialogo-titulo"
      (cerrar)="cancelar.emit()"
    >
      @if (abierto()) {
        <form class="formulario" [formGroup]="form" (ngSubmit)="enviar()" novalidate>
          @if (erroresGenerales().length > 0) {
            <div class="alerta alerta-error" role="alert">
              @for (mensaje of erroresGenerales(); track $index) {
                <p>{{ mensaje }}</p>
              }
            </div>
          }
          @if (esEdicion()) {
            <div class="campo campo-corto">
              <label for="nivel-numero-lectura">Número</label>
              <input id="nivel-numero-lectura" type="text" [value]="nivel()?.numero" readonly />
            </div>
          } @else {
            <div class="campo campo-corto">
              <label for="nivel-numero">Número <span aria-hidden="true">*</span></label>
              <input
                id="nivel-numero"
                type="number"
                formControlName="numero"
                required
                step="1"
                [min]="limites.numeroMin"
                [max]="limites.numeroMax"
                [attr.aria-invalid]="errores('numero', 'El número').length > 0"
                aria-describedby="nivel-numero-error"
              />
              <app-campo-error
                [mensajes]="errores('numero', 'El número')"
                idError="nivel-numero-error"
              />
            </div>
          }
          <div class="campo">
            <label for="nivel-nombre">Nombre <span aria-hidden="true">*</span></label>
            <input
              id="nivel-nombre"
              type="text"
              formControlName="nombre"
              required
              [attr.aria-invalid]="errores('nombre', 'El nombre').length > 0"
              aria-describedby="nivel-nombre-error"
            />
            <app-campo-error
              [mensajes]="errores('nombre', 'El nombre')"
              idError="nivel-nombre-error"
            />
          </div>
          <div class="campo">
            <label for="nivel-descripcion">Descripción</label>
            <textarea
              id="nivel-descripcion"
              rows="3"
              formControlName="descripcion"
              [attr.aria-invalid]="errores('descripcion', 'La descripción').length > 0"
              aria-describedby="nivel-descripcion-contador nivel-descripcion-error"
            ></textarea>
            <small id="nivel-descripcion-contador" class="contador">
              {{ form.controls.descripcion.value.length }} / {{ limites.descripcionMax }}
            </small>
            <app-campo-error
              [mensajes]="errores('descripcion', 'La descripción')"
              idError="nivel-descripcion-error"
            />
          </div>
          <div class="acciones">
            <button type="button" class="btn" (click)="cancelar.emit()">Cancelar</button>
            <button type="submit" class="btn btn-primario" [disabled]="form.invalid || enviando()">
              {{ enviando() ? 'Guardando...' : 'Guardar' }}
            </button>
          </div>
        </form>
      }
    </app-dialogo>
  `,
})
export class NivelFormDialogComponent {
  private readonly fb = inject(NonNullableFormBuilder);

  readonly abierto = input.required<boolean>();
  /** Nivel a editar (null en alta). */
  readonly nivel = input<NivelIncidenciaDto | null>(null);
  readonly enviando = input(false);
  readonly errorServidor = input<ErrorFormulario>(SIN_ERRORES);
  /** En edición `numero` es el actual (la página lo descarta al armar el PUT). */
  readonly guardar = output<CrearNivelIncidenciaRequest>();
  readonly cancelar = output<void>();

  readonly limites = LIMITES_NIVEL;
  readonly erroresGenerales = signal<readonly string[]>([]);
  readonly esEdicion = computed(() => this.nivel() !== null);

  readonly form = this.fb.group({
    numero: this.fb.control<number | null>(null, [
      Validators.required,
      entero,
      Validators.min(LIMITES_NIVEL.numeroMin),
      Validators.max(LIMITES_NIVEL.numeroMax),
    ]),
    nombre: this.fb.control('', [textoRequerido, longitudMaxima(LIMITES_NIVEL.nombreMax)]),
    descripcion: this.fb.control('', [longitudMaxima(LIMITES_NIVEL.descripcionMax)]),
  });

  constructor() {
    effect(() => {
      const abierto = this.abierto();
      const nivel = this.nivel();
      if (abierto) {
        untracked(() => this.reiniciar(nivel));
      }
    });
    effect(() => {
      const error = this.errorServidor();
      untracked(() => {
        const sinCampo = aplicarErroresServidor(this.form, error.campos);
        this.erroresGenerales.set(error.general ? [error.general, ...sinCampo] : sinCampo);
      });
    });
  }

  /** Mensajes de error visibles del campo indicado. */
  errores(campo: keyof typeof this.form.controls, etiqueta: string): string[] {
    return mensajesVisibles(this.form.controls[campo], etiqueta);
  }

  /** Emite el request (descripción vacía → null) si el formulario es válido. */
  enviar(): void {
    const valores = this.form.getRawValue();
    if (this.form.invalid || this.enviando() || valores.numero === null) {
      this.form.markAllAsTouched();
      return;
    }
    this.guardar.emit({
      numero: valores.numero,
      nombre: recortar(valores.nombre),
      descripcion: recortarOpcional(valores.descripcion),
    });
  }

  /** Carga los valores del nivel (o vacíos en alta) al abrir; el número no se edita. */
  private reiniciar(nivel: NivelIncidenciaDto | null): void {
    this.form.reset({
      numero: nivel?.numero ?? null,
      nombre: nivel?.nombre ?? '',
      descripcion: nivel?.descripcion ?? '',
    });
    if (nivel) {
      this.form.controls.numero.disable();
    } else {
      this.form.controls.numero.enable();
    }
    this.erroresGenerales.set([]);
  }
}
