import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  OnInit,
  computed,
  inject,
  input,
  signal,
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Router } from '@angular/router';
import { Observable } from 'rxjs';
import { NotificacionService } from '../../../../core/services/notificacion.service';
import { UsuarioActualService } from '../../../../core/services/usuario-actual.service';
import { NoEncontradoComponent } from '../../../../shared/components/no-encontrado/no-encontrado.component';
import {
  ErrorFormulario,
  SIN_ERRORES,
  esNoEncontrado,
  interpretarErrorFormulario,
  mensajeErrorAccion,
} from '../../../../shared/forms/errores-servidor';
import { parsearId } from '../../../../shared/utils/ids';
import { PersonaFormComponent } from '../../components/persona-form/persona-form.component';
import { PersonaDto, PersonaRequest } from '../../models/persona.model';
import { PersonasService } from '../../services/personas.service';

/**
 * P6 – Alta (`/personas/nueva`) y edición (`/personas/:id/editar`) de persona.
 *
 * Creado: 2026-10-09
 * Modificado: 2026-10-09 - Creación inicial (RF-001)
 */
@Component({
  selector: 'app-persona-form-page',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [PersonaFormComponent, NoEncontradoComponent],
  template: `
    <header class="encabezado-pagina">
      <h1>{{ esEdicion() ? 'Editar persona' : 'Nueva persona' }}</h1>
    </header>
    @if (noEncontrado()) {
      <app-no-encontrado rutaVolver="/personas" />
    } @else if (errorCarga(); as mensaje) {
      <p class="alerta alerta-error" role="alert">{{ mensaje }}</p>
    } @else if (!esEdicion() || persona()) {
      <section class="panel">
        <app-persona-form
          [persona]="persona()"
          [puedeGestionarAdmin]="esAdmin()"
          [enviando]="enviando()"
          [errorServidor]="errorServidor()"
          (guardar)="guardar($event)"
          (cancelar)="volver()"
        />
      </section>
    } @else {
      <p>Cargando...</p>
    }
  `,
})
export class PersonaFormPage implements OnInit {
  private readonly personasService = inject(PersonasService);
  private readonly usuarioActual = inject(UsuarioActualService);
  private readonly notificacion = inject(NotificacionService);
  private readonly router = inject(Router);
  private readonly destroyRef = inject(DestroyRef);

  /** Parámetro de ruta `:id` (ausente en alta). */
  readonly id = input<string>();

  readonly personaId = computed(() => parsearId(this.id()));
  readonly esEdicion = computed(() => this.id() !== undefined);
  readonly esAdmin = this.usuarioActual.esAdmin;
  readonly persona = signal<PersonaDto | null>(null);
  readonly noEncontrado = signal(false);
  readonly errorCarga = signal<string | null>(null);
  readonly enviando = signal(false);
  readonly errorServidor = signal<ErrorFormulario>(SIN_ERRORES);

  ngOnInit(): void {
    const id = this.personaId();
    if (!this.esEdicion()) {
      return;
    }
    if (id === null) {
      this.noEncontrado.set(true);
      return;
    }
    this.personasService
      .obtener(id)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (persona) => this.persona.set(persona),
        error: (error: unknown) => {
          if (esNoEncontrado(error)) {
            this.noEncontrado.set(true);
          } else {
            this.errorCarga.set(mensajeErrorAccion(error) ?? 'No se pudo cargar la persona.');
          }
        },
      });
  }

  /** POST o PUT; éxito → toast, refresco del selector de usuario y vuelta al listado. */
  guardar(request: PersonaRequest): void {
    const id = this.personaId();
    this.enviando.set(true);
    this.errorServidor.set(SIN_ERRORES);
    const envio$: Observable<PersonaDto> =
      id === null ? this.personasService.crear(request) : this.personasService.editar(id, request);
    envio$.pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (persona) => {
        this.notificacion.exito('Persona guardada.');
        this.actualizarUsuarioActual(persona);
        this.volver();
      },
      error: (error: unknown) => {
        this.enviando.set(false);
        this.errorServidor.set(
          interpretarErrorFormulario(error, { PERSONA_EMAIL_DUPLICADO: 'email' }),
        );
      },
    });
  }

  /** Vuelve al listado de personas. */
  volver(): void {
    void this.router.navigate(['/personas']);
  }

  /** Refresca el selector y, si se editó al usuario actual, revalida su rol. */
  private actualizarUsuarioActual(persona: PersonaDto): void {
    this.usuarioActual.recargarPersonas();
    if (persona.id === this.usuarioActual.personaId()) {
      this.usuarioActual.verificar();
    }
  }
}
