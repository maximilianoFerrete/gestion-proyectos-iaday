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
import { RouterLink } from '@angular/router';
import { filter, forkJoin, switchMap } from 'rxjs';
import { ConfirmacionService } from '../../../../core/services/confirmacion.service';
import { NotificacionService } from '../../../../core/services/notificacion.service';
import { UsuarioActualService } from '../../../../core/services/usuario-actual.service';
import { NoEncontradoComponent } from '../../../../shared/components/no-encontrado/no-encontrado.component';
import { esNoEncontrado, mensajeErrorAccion } from '../../../../shared/forms/errores-servidor';
import { parsearId } from '../../../../shared/utils/ids';
import { AgrupacionesListaComponent } from '../../components/agrupaciones-lista/agrupaciones-lista.component';
import { ProyectoDatosComponent } from '../../components/proyecto-datos/proyecto-datos.component';
import { AgrupacionDto } from '../../models/agrupacion.model';
import { ProyectoDto } from '../../models/proyecto.model';
import { AgrupacionesService } from '../../services/agrupaciones.service';
import { ProyectosListadoEstadoService } from '../../services/proyectos-listado-estado.service';
import { ProyectosService } from '../../services/proyectos.service';

/**
 * Mensaje del diálogo de eliminar agrupación (RF-002 "Cambios en P3"): informa la cantidad de
 * incidencias que quedarán sin agrupación; con 0 incidencias, el texto de RF-001.
 */
export function mensajeEliminarAgrupacion(agrupacion: AgrupacionDto): string {
  const cantidad = agrupacion.cantidadIncidencias ?? 0;
  if (cantidad === 0) {
    return (
      `¿Eliminar la agrupación ${agrupacion.nombre}? ` +
      'Las incidencias asociadas no se eliminan: quedarán sin agrupación.'
    );
  }
  return (
    `¿Eliminar la agrupación ${agrupacion.nombre}? Las ${cantidad} incidencias asociadas no se ` +
    'eliminan: quedarán sin agrupación (incluidas sus jerarquías).'
  );
}

/**
 * P3 – Detalle de proyecto con sus agrupaciones. Archivar solo visible para administradores;
 * proyecto archivado: banner y sin acciones de modificación (RN-22). RF-002: botones Incidencias
 * y Árbol, y cantidad de incidencias en el diálogo de eliminar agrupación. RF-005: botón
 * Seguimiento y acción Seguimiento por agrupación.
 *
 * Creado: 2026-10-09
 * Modificado: 2026-10-10 - Accesos al seguimiento del proyecto y de cada agrupación (RF-005)
 */
@Component({
  selector: 'app-proyecto-detalle-page',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterLink, NoEncontradoComponent, ProyectoDatosComponent, AgrupacionesListaComponent],
  template: `
    @if (noEncontrado()) {
      <app-no-encontrado rutaVolver="/proyectos" [queryParamsVolver]="queryParamsListado()" />
    } @else if (errorCarga(); as mensaje) {
      <p class="alerta alerta-error" role="alert">{{ mensaje }}</p>
      <a class="btn" routerLink="/proyectos" [queryParams]="queryParamsListado()">Volver</a>
    } @else if (proyecto(); as p) {
      <header class="encabezado-pagina">
        <h1>{{ p.codigo }} – {{ p.nombre }}</h1>
        <div class="acciones">
          <a class="btn" routerLink="/proyectos" [queryParams]="queryParamsListado()">Volver</a>
          <a class="btn" [routerLink]="['/proyectos', p.id, 'incidencias']">Incidencias</a>
          <a class="btn" [routerLink]="['/proyectos', p.id, 'incidencias', 'arbol']">Árbol</a>
          <a class="btn" [routerLink]="['/proyectos', p.id, 'seguimiento']">Seguimiento</a>
          @if (!p.archivado) {
            <a class="btn" [routerLink]="['/proyectos', p.id, 'editar']">Editar</a>
            @if (esAdmin()) {
              <button
                type="button"
                class="btn btn-peligro"
                [disabled]="procesando()"
                (click)="archivar(p)"
              >
                Archivar
              </button>
            }
          }
        </div>
      </header>
      <section class="panel">
        <app-proyecto-datos [proyecto]="p" />
      </section>
      <app-agrupaciones-lista
        [proyectoId]="p.id"
        [agrupaciones]="agrupaciones()"
        [soloLectura]="p.archivado"
        (eliminar)="eliminarAgrupacion(p, $event)"
      />
    } @else {
      <p>Cargando...</p>
    }
  `,
})
export class ProyectoDetallePage implements OnInit {
  private readonly proyectosService = inject(ProyectosService);
  private readonly agrupacionesService = inject(AgrupacionesService);
  private readonly confirmacion = inject(ConfirmacionService);
  private readonly notificacion = inject(NotificacionService);
  private readonly usuarioActual = inject(UsuarioActualService);
  private readonly listadoEstado = inject(ProyectosListadoEstadoService);
  private readonly destroyRef = inject(DestroyRef);

  /** Parámetro de ruta `:id`. */
  readonly id = input.required<string>();

  readonly proyectoId = computed(() => parsearId(this.id()));
  readonly esAdmin = this.usuarioActual.esAdmin;
  readonly queryParamsListado = this.listadoEstado.queryParams;
  readonly proyecto = signal<ProyectoDto | null>(null);
  readonly agrupaciones = signal<readonly AgrupacionDto[]>([]);
  readonly noEncontrado = signal(false);
  readonly errorCarga = signal<string | null>(null);
  readonly procesando = signal(false);

  ngOnInit(): void {
    this.cargar();
  }

  /** Pide confirmación y archiva el proyecto (solo admin; el backend valida igual). */
  archivar(proyecto: ProyectoDto): void {
    this.confirmacion
      .confirmar({
        titulo: 'Archivar proyecto',
        mensaje:
          `¿Archivar el proyecto ${proyecto.codigo} – ${proyecto.nombre}? ` +
          'El proyecto quedará en solo lectura y no podrá desarchivarse.',
        textoConfirmar: 'Archivar',
        peligro: true,
      })
      .pipe(
        filter((confirmado) => confirmado),
        switchMap(() => {
          this.procesando.set(true);
          return this.proyectosService.archivar(proyecto.id);
        }),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({
        next: () => {
          this.procesando.set(false);
          this.notificacion.exito('Proyecto archivado.');
          this.cargar();
        },
        error: (error: unknown) => this.alFallarAccion(error),
      });
  }

  /** Pide confirmación y elimina la agrupación; luego recarga la lista. */
  eliminarAgrupacion(proyecto: ProyectoDto, agrupacion: AgrupacionDto): void {
    this.confirmacion
      .confirmar({
        titulo: 'Eliminar agrupación',
        mensaje: mensajeEliminarAgrupacion(agrupacion),
        textoConfirmar: 'Eliminar',
        peligro: true,
      })
      .pipe(
        filter((confirmado) => confirmado),
        switchMap(() => this.agrupacionesService.eliminar(proyecto.id, agrupacion.id)),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({
        next: () => {
          this.notificacion.exito('Agrupación eliminada.');
          this.cargarAgrupaciones(proyecto.id);
        },
        error: (error: unknown) => this.alFallarAccion(error),
      });
  }

  /** Carga el proyecto y sus agrupaciones. */
  private cargar(): void {
    const id = this.proyectoId();
    if (id === null) {
      this.noEncontrado.set(true);
      return;
    }
    forkJoin({
      proyecto: this.proyectosService.obtener(id),
      agrupaciones: this.agrupacionesService.listar(id),
    })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: ({ proyecto, agrupaciones }) => {
          this.proyecto.set(proyecto);
          this.agrupaciones.set(agrupaciones);
        },
        error: (error: unknown) => {
          if (esNoEncontrado(error)) {
            this.noEncontrado.set(true);
          } else {
            this.errorCarga.set(mensajeErrorAccion(error) ?? 'No se pudo cargar el proyecto.');
          }
        },
      });
  }

  /** Recarga solo la lista de agrupaciones. */
  private cargarAgrupaciones(proyectoId: number): void {
    this.agrupacionesService
      .listar(proyectoId)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (agrupaciones) => this.agrupaciones.set(agrupaciones),
        error: (error: unknown) => this.alFallarAccion(error),
      });
  }

  /** Muestra el error de una acción y recarga para reflejar el estado real (p. ej. 409). */
  private alFallarAccion(error: unknown): void {
    this.procesando.set(false);
    const mensaje = mensajeErrorAccion(error);
    if (mensaje) {
      this.notificacion.error(mensaje);
    }
    this.cargar();
  }
}
