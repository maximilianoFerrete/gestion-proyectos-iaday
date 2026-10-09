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
import { EstadosIncidenciaTablaComponent } from '../../components/estados-incidencia-tabla/estados-incidencia-tabla.component';
import { EstadoIncidenciaDto } from '../../models/estado-incidencia.model';
import { EjecutorAccionesService } from '../../services/ejecutor-acciones.service';
import { EstadosIncidenciaService } from '../../services/estados-incidencia.service';
import { mensajeDesactivarEstado } from '../../utils/reglas-configuracion';

/**
 * C3 – Estados de incidencia (`/configuracion/incidencias/estados`). Consultable por todos;
 * Nuevo, Editar, Desactivar y Activar solo con permiso (RN-01).
 *
 * Creado: 2026-10-09
 * Modificado: 2026-10-09 - Creación inicial (RF-002)
 */
@Component({
  selector: 'app-estados-incidencia-page',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterLink, EstadosIncidenciaTablaComponent],
  template: `
    <header class="encabezado-pagina">
      <h1>Estados de incidencia</h1>
      @if (puedeEditar()) {
        <a class="btn btn-primario" routerLink="/configuracion/incidencias/estados/nuevo">
          Nuevo estado
        </a>
      }
    </header>
    @if (error(); as mensaje) {
      <p class="alerta alerta-error" role="alert">{{ mensaje }}</p>
    }
    <section class="panel" [attr.aria-busy]="estados() === null">
      @if (estados(); as lista) {
        <app-estados-incidencia-tabla
          [estados]="lista"
          [puedeEditar]="puedeEditar()"
          [deshabilitado]="procesando()"
          (desactivar)="desactivar($event)"
          (activar)="activar($event)"
        />
      } @else if (!error()) {
        <p>Cargando...</p>
      }
    </section>
  `,
})
export class EstadosIncidenciaPage implements OnInit {
  private readonly estadosService = inject(EstadosIncidenciaService);
  private readonly ejecutor = inject(EjecutorAccionesService);
  private readonly usuarioActual = inject(UsuarioActualService);
  private readonly destroyRef = inject(DestroyRef);

  readonly puedeEditar = computed(() =>
    this.usuarioActual.tienePermiso(PERMISOS.configuracionIncidencias),
  );
  readonly estados = signal<readonly EstadoIncidenciaDto[] | null>(null);
  readonly procesando = signal(false);
  readonly error = signal<string | null>(null);

  ngOnInit(): void {
    this.cargar();
  }

  /** Confirma (con la cantidad de incidencias) y desactiva el estado (RN-11). */
  desactivar(estado: EstadoIncidenciaDto): void {
    this.alTerminar(
      this.ejecutor.confirmarYEjecutar(
        {
          titulo: 'Desactivar estado',
          mensaje: mensajeDesactivarEstado(estado),
          textoConfirmar: 'Desactivar',
          peligro: true,
        },
        () => this.estadosService.desactivar(estado.id),
        'Estado desactivado.',
      ),
    );
  }

  /** Reactiva el estado. */
  activar(estado: EstadoIncidenciaDto): void {
    this.alTerminar(
      this.ejecutor.ejecutar(this.estadosService.activar(estado.id), 'Estado activado.'),
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

  /** Carga todos los estados (incluidos inactivos). */
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
}
