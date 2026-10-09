import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  OnInit,
  inject,
  signal,
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { RouterLink } from '@angular/router';
import { Observable, catchError, filter, finalize, of, switchMap } from 'rxjs';
import { ConfirmacionService } from '../../../../core/services/confirmacion.service';
import { NotificacionService } from '../../../../core/services/notificacion.service';
import { mensajeErrorAccion } from '../../../../shared/forms/errores-servidor';
import { EstadosTablaComponent } from '../../components/estados-tabla/estados-tabla.component';
import { EstadoProyectoDto } from '../../models/estado-proyecto.model';
import { EstadosProyectoService } from '../../services/estados-proyecto.service';
import { mensajeDesactivar } from '../../utils/acciones-estado';

/**
 * P7 – Listado de estados de proyecto con acciones marcar inicial, desactivar y activar.
 *
 * Creado: 2026-10-09
 * Modificado: 2026-10-09 - Creación inicial (RF-001)
 */
@Component({
  selector: 'app-estados-proyecto-listado-page',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterLink, EstadosTablaComponent],
  template: `
    <header class="encabezado-pagina">
      <h1>Estados de proyecto</h1>
      <a class="btn btn-primario" routerLink="/configuracion/estados-proyecto/nuevo"
        >Nuevo estado</a
      >
    </header>
    @if (error(); as mensaje) {
      <p class="alerta alerta-error" role="alert">{{ mensaje }}</p>
    }
    <section class="panel" [attr.aria-busy]="estados() === null">
      @if (estados(); as lista) {
        <app-estados-tabla
          [estados]="lista"
          [deshabilitado]="procesando()"
          (marcarInicial)="marcarInicial($event)"
          (desactivar)="desactivar($event)"
          (activar)="activar($event)"
        />
      } @else if (!error()) {
        <p>Cargando...</p>
      }
    </section>
  `,
})
export class EstadosProyectoListadoPage implements OnInit {
  private readonly estadosService = inject(EstadosProyectoService);
  private readonly confirmacion = inject(ConfirmacionService);
  private readonly notificacion = inject(NotificacionService);
  private readonly destroyRef = inject(DestroyRef);

  readonly estados = signal<readonly EstadoProyectoDto[] | null>(null);
  readonly procesando = signal(false);
  readonly error = signal<string | null>(null);

  ngOnInit(): void {
    this.cargar();
  }

  /** Confirma y marca el estado como inicial (RN-10). */
  marcarInicial(estado: EstadoProyectoDto): void {
    this.confirmarYEjecutar(
      'Marcar como inicial',
      `Los nuevos proyectos se crearán en estado ${estado.nombre}.`,
      () => this.estadosService.marcarInicial(estado.id),
      `El estado ${estado.nombre} es ahora el inicial.`,
    );
  }

  /** Confirma y desactiva el estado (RN-13). */
  desactivar(estado: EstadoProyectoDto): void {
    this.confirmarYEjecutar(
      'Desactivar estado',
      mensajeDesactivar(estado),
      () => this.estadosService.desactivar(estado.id),
      'Estado desactivado.',
    );
  }

  /** Reactiva el estado. */
  activar(estado: EstadoProyectoDto): void {
    this.ejecutar(this.estadosService.activar(estado.id), 'Estado activado.')
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe();
  }

  /** Carga todos los estados (incluido el de sistema). */
  private cargar(): void {
    this.estadosService
      .listar()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (estados) => {
          this.error.set(null);
          this.estados.set(estados);
        },
        error: (error: unknown) =>
          this.error.set(mensajeErrorAccion(error) ?? 'No se pudieron cargar los estados.'),
      });
  }

  /** Pide confirmación y ejecuta la acción. */
  private confirmarYEjecutar(
    titulo: string,
    mensaje: string,
    accion: () => Observable<EstadoProyectoDto>,
    mensajeExito: string,
  ): void {
    this.confirmacion
      .confirmar({ titulo, mensaje, textoConfirmar: titulo })
      .pipe(
        filter((confirmado) => confirmado),
        switchMap(() => this.ejecutar(accion(), mensajeExito)),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe();
  }

  /** Ejecuta una acción: éxito → toast y recarga; error (p. ej. 409) → toast con `detail`. */
  private ejecutar(
    accion$: Observable<EstadoProyectoDto>,
    mensajeExito: string,
  ): Observable<EstadoProyectoDto | null> {
    this.procesando.set(true);
    return accion$.pipe(
      switchMap((estado) => {
        this.notificacion.exito(mensajeExito);
        this.cargar();
        return of(estado);
      }),
      catchError((error: unknown) => {
        const mensaje = mensajeErrorAccion(error);
        if (mensaje) {
          this.notificacion.error(mensaje);
        }
        this.cargar();
        return of(null);
      }),
      finalize(() => this.procesando.set(false)),
    );
  }
}
