import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  OnInit,
  computed,
  inject,
  signal,
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { RouterLink } from '@angular/router';
import { Observable, finalize } from 'rxjs';
import { PERMISOS } from '../../../../core/models/permisos.model';
import { UsuarioActualService } from '../../../../core/services/usuario-actual.service';
import { mensajeErrorAccion } from '../../../../shared/forms/errores-servidor';
import { PrioridadesTablaComponent } from '../../components/prioridades-tabla/prioridades-tabla.component';
import { PrioridadIncidenciaDto } from '../../models/prioridad-incidencia.model';
import { EjecutorAccionesService } from '../../services/ejecutor-acciones.service';
import { PrioridadesIncidenciaService } from '../../services/prioridades-incidencia.service';
import { mensajeDesactivarPrioridad } from '../../utils/reglas-configuracion';

/**
 * C5 – Prioridades (`/configuracion/incidencias/prioridades`). Consultable por todos; Nueva,
 * Editar, Marcar como predeterminada, Desactivar y Activar solo con permiso (RN-01, RN-17).
 *
 * Creado: 2026-10-09
 * Modificado: 2026-10-09 - Creación inicial (RF-002)
 */
@Component({
  selector: 'app-prioridades-page',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterLink, PrioridadesTablaComponent],
  template: `
    <header class="encabezado-pagina">
      <h1>Prioridades de incidencia</h1>
      @if (puedeEditar()) {
        <a class="btn btn-primario" routerLink="/configuracion/incidencias/prioridades/nueva">
          Nueva prioridad
        </a>
      }
    </header>
    @if (error(); as mensaje) {
      <p class="alerta alerta-error" role="alert">{{ mensaje }}</p>
    }
    <section class="panel" [attr.aria-busy]="prioridades() === null">
      @if (prioridades(); as lista) {
        <app-prioridades-tabla
          [prioridades]="lista"
          [puedeEditar]="puedeEditar()"
          [deshabilitado]="procesando()"
          (marcarPredeterminada)="marcarPredeterminada($event)"
          (desactivar)="desactivar($event)"
          (activar)="activar($event)"
        />
      } @else if (!error()) {
        <p>Cargando...</p>
      }
    </section>
  `,
})
export class PrioridadesPage implements OnInit {
  private readonly prioridadesService = inject(PrioridadesIncidenciaService);
  private readonly ejecutor = inject(EjecutorAccionesService);
  private readonly usuarioActual = inject(UsuarioActualService);
  private readonly destroyRef = inject(DestroyRef);

  readonly puedeEditar = computed(() =>
    this.usuarioActual.tienePermiso(PERMISOS.configuracionIncidencias),
  );
  readonly prioridades = signal<readonly PrioridadIncidenciaDto[] | null>(null);
  readonly procesando = signal(false);
  readonly error = signal<string | null>(null);

  ngOnInit(): void {
    this.cargar();
  }

  /** Confirma y marca la prioridad como predeterminada (desmarca la anterior, RN-17). */
  marcarPredeterminada(prioridad: PrioridadIncidenciaDto): void {
    this.alTerminar(
      this.ejecutor.confirmarYEjecutar(
        {
          titulo: 'Marcar como predeterminada',
          mensaje: `Las nuevas incidencias sin prioridad se crearán con ${prioridad.nombre}.`,
          textoConfirmar: 'Marcar como predeterminada',
        },
        () => this.prioridadesService.marcarPredeterminada(prioridad.id),
        `La prioridad ${prioridad.nombre} es ahora la predeterminada.`,
      ),
    );
  }

  /** Confirma y desactiva la prioridad. */
  desactivar(prioridad: PrioridadIncidenciaDto): void {
    this.alTerminar(
      this.ejecutor.confirmarYEjecutar(
        {
          titulo: 'Desactivar prioridad',
          mensaje: mensajeDesactivarPrioridad(prioridad),
          textoConfirmar: 'Desactivar',
          peligro: true,
        },
        () => this.prioridadesService.desactivar(prioridad.id),
        'Prioridad desactivada.',
      ),
    );
  }

  /** Reactiva la prioridad. */
  activar(prioridad: PrioridadIncidenciaDto): void {
    this.alTerminar(
      this.ejecutor.ejecutar(this.prioridadesService.activar(prioridad.id), 'Prioridad activada.'),
    );
  }

  /** Marca la acción en curso y recarga al terminar (también tras un 409). */
  private alTerminar(accion$: Observable<boolean>): void {
    this.procesando.set(true);
    accion$
      .pipe(
        finalize(() => this.procesando.set(false)),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe(() => this.cargar());
  }

  /** Carga todas las prioridades (incluidas inactivas). */
  private cargar(): void {
    this.prioridadesService
      .listar()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (prioridades) => {
          this.error.set(null);
          this.prioridades.set(prioridades);
        },
        error: (error: unknown) =>
          this.error.set(mensajeErrorAccion(error) ?? 'No se pudieron cargar las prioridades.'),
      });
  }
}
