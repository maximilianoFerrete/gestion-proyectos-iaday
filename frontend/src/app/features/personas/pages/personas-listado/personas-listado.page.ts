import { ChangeDetectionStrategy, Component, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { NonNullableFormBuilder, ReactiveFormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import {
  Observable,
  Subject,
  catchError,
  debounceTime,
  filter,
  finalize,
  merge,
  of,
  switchMap,
} from 'rxjs';
import { ConfirmacionService } from '../../../../core/services/confirmacion.service';
import { NotificacionService } from '../../../../core/services/notificacion.service';
import { UsuarioActualService } from '../../../../core/services/usuario-actual.service';
import { mensajeErrorAccion } from '../../../../shared/forms/errores-servidor';
import { PersonasTablaComponent } from '../../components/personas-tabla/personas-tabla.component';
import { FiltrosPersonas, PersonaDto } from '../../models/persona.model';
import { PersonasService } from '../../services/personas.service';

/** Debounce del filtro de texto. */
const DEBOUNCE_TEXTO_MS = 300;

/**
 * P6 – Listado de personas con filtro de texto, "Incluir inactivas" y acciones de
 * desactivación / reactivación.
 *
 * Creado: 2026-10-09
 * Modificado: 2026-10-09 - Creación inicial (RF-001)
 */
@Component({
  selector: 'app-personas-listado-page',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [ReactiveFormsModule, RouterLink, PersonasTablaComponent],
  template: `
    <header class="encabezado-pagina">
      <h1>Personas</h1>
      <a class="btn btn-primario" routerLink="/personas/nueva">Nueva persona</a>
    </header>

    <form class="filtros" [formGroup]="filtros" role="search" (ngSubmit)="recargar()">
      <div class="campo">
        <label for="personas-texto">Texto (nombre o email)</label>
        <input id="personas-texto" type="search" formControlName="texto" />
      </div>
      <div class="campo campo-check">
        <input id="personas-inactivas" type="checkbox" formControlName="incluirInactivas" />
        <label for="personas-inactivas">Incluir inactivas</label>
      </div>
    </form>

    @if (error(); as mensaje) {
      <p class="alerta alerta-error" role="alert">{{ mensaje }}</p>
    }

    <section class="panel" [attr.aria-busy]="cargando()">
      @if (cargando() && personas() === null) {
        <p>Cargando...</p>
      } @else if (personas(); as lista) {
        <app-personas-tabla
          [personas]="lista"
          [deshabilitado]="procesando()"
          (desactivar)="desactivar($event)"
          (activar)="activar($event)"
        />
      }
    </section>
  `,
})
export class PersonasListadoPage {
  private readonly personasService = inject(PersonasService);
  private readonly confirmacion = inject(ConfirmacionService);
  private readonly notificacion = inject(NotificacionService);
  private readonly usuarioActual = inject(UsuarioActualService);
  private readonly destroyRef = inject(DestroyRef);
  private readonly recarga$ = new Subject<void>();

  readonly filtros = inject(NonNullableFormBuilder).group({
    texto: '',
    incluirInactivas: false,
  });
  readonly personas = signal<readonly PersonaDto[] | null>(null);
  readonly cargando = signal(false);
  readonly procesando = signal(false);
  readonly error = signal<string | null>(null);

  constructor() {
    merge(
      of(undefined),
      this.recarga$,
      this.filtros.controls.texto.valueChanges.pipe(debounceTime(DEBOUNCE_TEXTO_MS)),
      this.filtros.controls.incluirInactivas.valueChanges,
    )
      .pipe(
        switchMap(() => this.buscar(this.valoresFiltros())),
        takeUntilDestroyed(),
      )
      .subscribe((personas) => {
        this.cargando.set(false);
        if (personas) {
          this.personas.set(personas);
        }
      });
  }

  /** Vuelve a consultar el listado con los filtros actuales. */
  recargar(): void {
    this.recarga$.next();
  }

  /** Pide confirmación y desactiva la persona. */
  desactivar(persona: PersonaDto): void {
    this.confirmacion
      .confirmar({
        titulo: 'Desactivar persona',
        mensaje: `¿Desactivar a ${persona.nombre}? No podrá asignarse como responsable.`,
        textoConfirmar: 'Desactivar',
        peligro: true,
      })
      .pipe(
        filter((confirmado) => confirmado),
        switchMap(() => this.ejecutar(this.personasService.desactivar(persona.id))),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe((actualizada) => {
        if (actualizada) {
          this.notificacion.exito('Persona desactivada.');
          this.alCambiarPersona(actualizada);
        }
      });
  }

  /** Reactiva la persona. */
  activar(persona: PersonaDto): void {
    this.ejecutar(this.personasService.activar(persona.id))
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((actualizada) => {
        if (actualizada) {
          this.notificacion.exito('Persona activada.');
          this.alCambiarPersona(actualizada);
        }
      });
  }

  /** Filtros actuales para la API. */
  private valoresFiltros(): FiltrosPersonas {
    const { texto, incluirInactivas } = this.filtros.getRawValue();
    return { texto: texto.trim() || undefined, incluirInactivas };
  }

  /** Consulta la API; ante error lo muestra y devuelve null. */
  private buscar(filtros: FiltrosPersonas): Observable<PersonaDto[] | null> {
    this.cargando.set(true);
    this.error.set(null);
    return this.personasService.listar(filtros).pipe(
      catchError((error: unknown) => {
        this.error.set(mensajeErrorAccion(error));
        return of(null);
      }),
    );
  }

  /** Ejecuta una acción mostrando su error como toast (p. ej. 409 PERSONA_ULTIMO_ADMIN). */
  private ejecutar(accion$: Observable<PersonaDto>): Observable<PersonaDto | null> {
    this.procesando.set(true);
    return accion$.pipe(
      catchError((error: unknown) => {
        const mensaje = mensajeErrorAccion(error);
        if (mensaje) {
          this.notificacion.error(mensaje);
        }
        return of(null);
      }),
      finalize(() => this.procesando.set(false)),
    );
  }

  /** Recarga listado y selector; si se desactivó al usuario actual, limpia la selección. */
  private alCambiarPersona(persona: PersonaDto): void {
    if (!persona.activo && persona.id === this.usuarioActual.personaId()) {
      this.usuarioActual.limpiar();
    }
    this.usuarioActual.recargarPersonas();
    this.recargar();
  }
}
