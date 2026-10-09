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
import { NoEncontradoComponent } from '../../../../shared/components/no-encontrado/no-encontrado.component';
import {
  ErrorFormulario,
  SIN_ERRORES,
  esNoEncontrado,
  interpretarErrorFormulario,
  mensajeErrorAccion,
} from '../../../../shared/forms/errores-servidor';
import { parsearId } from '../../../../shared/utils/ids';
import {
  NombreOrdenFormComponent,
  ValoresNombreOrden,
} from '../../components/nombre-orden-form/nombre-orden-form.component';
import { LIMITES_PRIORIDAD, PrioridadIncidenciaDto } from '../../models/prioridad-incidencia.model';
import { PrioridadesIncidenciaService } from '../../services/prioridades-incidencia.service';

/** Ruta del listado de prioridades (C5). */
const RUTA_PRIORIDADES = '/configuracion/incidencias/prioridades';

/**
 * C5 – Alta (`/configuracion/incidencias/prioridades/nueva`) y edición
 * (`/configuracion/incidencias/prioridades/:id/editar`) de prioridad.
 *
 * Creado: 2026-10-09
 * Modificado: 2026-10-09 - Creación inicial (RF-002)
 */
@Component({
  selector: 'app-prioridad-form-page',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [NombreOrdenFormComponent, NoEncontradoComponent],
  template: `
    <header class="encabezado-pagina">
      <h1>{{ esEdicion() ? 'Editar prioridad' : 'Nueva prioridad' }}</h1>
    </header>
    @if (noEncontrado()) {
      <app-no-encontrado rutaVolver="/configuracion/incidencias/prioridades" />
    } @else if (errorCarga(); as mensaje) {
      <p class="alerta alerta-error" role="alert">{{ mensaje }}</p>
    } @else if (listo()) {
      <section class="panel">
        <app-nombre-orden-form
          prefijo="prioridad"
          [limites]="limites"
          [valores]="prioridad()"
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
export class PrioridadFormPage implements OnInit {
  private readonly prioridadesService = inject(PrioridadesIncidenciaService);
  private readonly notificacion = inject(NotificacionService);
  private readonly router = inject(Router);
  private readonly destroyRef = inject(DestroyRef);

  /** Parámetro de ruta `:id` (ausente en alta). */
  readonly id = input<string>();

  readonly limites = LIMITES_PRIORIDAD;
  readonly prioridadId = computed(() => parsearId(this.id()));
  readonly esEdicion = computed(() => this.id() !== undefined);
  readonly prioridad = signal<PrioridadIncidenciaDto | null>(null);
  readonly listo = signal(false);
  readonly noEncontrado = signal(false);
  readonly errorCarga = signal<string | null>(null);
  readonly enviando = signal(false);
  readonly errorServidor = signal<ErrorFormulario>(SIN_ERRORES);

  ngOnInit(): void {
    const id = this.prioridadId();
    if (this.esEdicion() && id === null) {
      this.noEncontrado.set(true);
      return;
    }
    if (id === null) {
      this.listo.set(true);
      return;
    }
    this.prioridadesService
      .obtener(id)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (prioridad) => {
          this.prioridad.set(prioridad);
          this.listo.set(true);
        },
        error: (error: unknown) => {
          if (esNoEncontrado(error)) {
            this.noEncontrado.set(true);
          } else {
            this.errorCarga.set(mensajeErrorAccion(error) ?? 'No se pudo cargar la prioridad.');
          }
        },
      });
  }

  /** POST o PUT; éxito → toast y vuelta al listado. */
  guardar(request: ValoresNombreOrden): void {
    const id = this.prioridadId();
    this.enviando.set(true);
    this.errorServidor.set(SIN_ERRORES);
    const envio$: Observable<PrioridadIncidenciaDto> =
      id === null
        ? this.prioridadesService.crear(request)
        : this.prioridadesService.editar(id, request);
    envio$.pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: () => {
        this.notificacion.exito('Prioridad guardada.');
        this.volver();
      },
      error: (error: unknown) => {
        this.enviando.set(false);
        this.errorServidor.set(
          interpretarErrorFormulario(error, { PRIORIDAD_NOMBRE_DUPLICADO: 'nombre' }),
        );
      },
    });
  }

  /** Vuelve al listado. */
  volver(): void {
    void this.router.navigate([RUTA_PRIORIDADES]);
  }
}
