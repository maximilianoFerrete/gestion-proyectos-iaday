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
import { emailValido } from '../../../../shared/validators/email.validators';
import {
  longitudMaxima,
  recortar,
  textoRequerido,
} from '../../../../shared/validators/texto.validators';
import { LIMITES_PERSONA, PersonaDto, PersonaRequest } from '../../models/persona.model';

/**
 * Formulario de persona (P6): Nombre (1–150), Email (máx. 254, formato) y Administrador.
 * El checkbox Administrador se deshabilita si el usuario actual no es admin (RN-05, CA-03).
 *
 * Creado: 2026-10-09
 * Modificado: 2026-10-09 - Email validado con la regex del backend (RN-32)
 */
@Component({
  selector: 'app-persona-form',
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
        <label for="persona-nombre">Nombre <span aria-hidden="true">*</span></label>
        <input
          id="persona-nombre"
          type="text"
          formControlName="nombre"
          required
          autocomplete="name"
          [attr.aria-invalid]="errores('nombre', 'El nombre').length > 0"
          aria-describedby="persona-nombre-error"
        />
        <app-campo-error
          [mensajes]="errores('nombre', 'El nombre')"
          idError="persona-nombre-error"
        />
      </div>
      <div class="campo">
        <label for="persona-email">Email <span aria-hidden="true">*</span></label>
        <input
          id="persona-email"
          type="email"
          formControlName="email"
          required
          autocomplete="email"
          [attr.aria-invalid]="errores('email', 'El email').length > 0"
          aria-describedby="persona-email-error"
        />
        <app-campo-error [mensajes]="errores('email', 'El email')" idError="persona-email-error" />
      </div>
      <div class="campo campo-check">
        <input
          id="persona-admin"
          type="checkbox"
          formControlName="esAdmin"
          aria-describedby="persona-admin-ayuda persona-admin-error"
        />
        <label for="persona-admin">Administrador</label>
        @if (!puedeGestionarAdmin()) {
          <small id="persona-admin-ayuda" class="ayuda">
            Solo un administrador puede cambiar este valor
          </small>
        }
        <app-campo-error
          [mensajes]="errores('esAdmin', 'Administrador')"
          idError="persona-admin-error"
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
export class PersonaFormComponent implements OnInit {
  private readonly fb = inject(NonNullableFormBuilder);

  /** Persona a editar (null en alta). */
  readonly persona = input<PersonaDto | null>(null);
  /** true si el usuario actual es administrador. */
  readonly puedeGestionarAdmin = input(false);
  readonly enviando = input(false);
  readonly errorServidor = input<ErrorFormulario>(SIN_ERRORES);
  readonly guardar = output<PersonaRequest>();
  readonly cancelar = output<void>();

  readonly erroresGenerales = signal<readonly string[]>([]);

  readonly form = this.fb.group({
    nombre: this.fb.control('', [textoRequerido, longitudMaxima(LIMITES_PERSONA.nombreMax)]),
    email: this.fb.control('', [
      textoRequerido,
      longitudMaxima(LIMITES_PERSONA.emailMax),
      emailValido,
    ]),
    esAdmin: this.fb.control(false),
  });

  constructor() {
    effect(() => {
      const habilitado = this.puedeGestionarAdmin();
      untracked(() => this.habilitarAdmin(habilitado));
    });
    effect(() => {
      const error = this.errorServidor();
      untracked(() => {
        const sinCampo = aplicarErroresServidor(this.form, error.campos);
        this.erroresGenerales.set(error.general ? [error.general, ...sinCampo] : sinCampo);
      });
    });
  }

  ngOnInit(): void {
    const persona = this.persona();
    if (persona) {
      this.form.patchValue({
        nombre: persona.nombre,
        email: persona.email,
        esAdmin: persona.esAdmin,
      });
    }
  }

  /** Mensajes de error visibles del campo indicado. */
  errores(campo: keyof typeof this.form.controls, etiqueta: string): string[] {
    return mensajesVisibles(this.form.controls[campo], etiqueta);
  }

  /** Emite el request (textos recortados; el backend normaliza el email a minúsculas). */
  enviar(): void {
    if (this.form.invalid || this.enviando()) {
      this.form.markAllAsTouched();
      return;
    }
    const valores = this.form.getRawValue();
    this.guardar.emit({
      nombre: recortar(valores.nombre),
      email: recortar(valores.email),
      esAdmin: valores.esAdmin,
    });
  }

  /** Habilita o deshabilita el checkbox Administrador conservando su valor. */
  private habilitarAdmin(habilitado: boolean): void {
    const control = this.form.controls.esAdmin;
    if (habilitado) {
      control.enable({ emitEvent: false });
    } else {
      control.disable({ emitEvent: false });
    }
  }
}
