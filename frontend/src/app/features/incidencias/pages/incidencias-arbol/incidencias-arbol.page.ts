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
import { forkJoin } from 'rxjs';
import { NoEncontradoComponent } from '../../../../shared/components/no-encontrado/no-encontrado.component';
import { esNoEncontrado, mensajeErrorAccion } from '../../../../shared/forms/errores-servidor';
import { parsearId } from '../../../../shared/utils/ids';
import { ProyectoDto } from '../../../proyectos/models/proyecto.model';
import { ProyectosService } from '../../../proyectos/services/proyectos.service';
import { ArbolIncidenciasComponent } from '../../components/arbol-incidencias/arbol-incidencias.component';
import { IncidenciasListadoEstadoService } from '../../services/incidencias-listado-estado.service';
import { IncidenciasService } from '../../services/incidencias.service';
import {
  NodoArbol,
  alternarId,
  construirArbol,
  idsConHijos,
  idsExpandidosIniciales,
} from '../../utils/arbol-incidencias';
import { MENSAJE_BANNER_ARCHIVADO } from '../../utils/proyecto-archivado';

/**
 * I2 – Árbol de incidencias del proyecto (`/proyectos/:id/incidencias/arbol`). Al abrir se ven
 * las raíces expandidas; "Expandir todo" / "Colapsar todo".
 *
 * Creado: 2026-10-09
 * Modificado: 2026-10-09 - Creación inicial (RF-002)
 */
@Component({
  selector: 'app-incidencias-arbol-page',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterLink, NoEncontradoComponent, ArbolIncidenciasComponent],
  template: `
    @if (noEncontrado()) {
      <app-no-encontrado rutaVolver="/proyectos" />
    } @else if (errorCarga(); as mensaje) {
      <p class="alerta alerta-error" role="alert">{{ mensaje }}</p>
    } @else if (proyecto(); as p) {
      <header class="encabezado-pagina">
        <div>
          <p class="subtitulo">{{ p.codigo }} – {{ p.nombre }}</p>
          <h1>Árbol de incidencias</h1>
        </div>
        <div class="acciones">
          <a class="btn" [routerLink]="['/proyectos', p.id]">Volver al proyecto</a>
          <a
            class="btn"
            [routerLink]="['/proyectos', p.id, 'incidencias']"
            [queryParams]="queryParamsListado()"
          >
            Ver listado
          </a>
        </div>
      </header>
      @if (p.archivado) {
        <div class="banner-archivado" role="status">{{ mensajeArchivado }}</div>
      }
      <section class="panel">
        @if (arbol().length === 0) {
          <p class="vacio">El proyecto no tiene incidencias.</p>
        } @else {
          <div class="acciones">
            <button type="button" class="btn btn-chico" (click)="expandirTodo()">
              Expandir todo
            </button>
            <button type="button" class="btn btn-chico" (click)="colapsarTodo()">
              Colapsar todo
            </button>
          </div>
          <app-arbol-incidencias
            [proyectoId]="p.id"
            [arbol]="arbol()"
            [expandidos]="expandidos()"
            (alternar)="alternar($event)"
          />
        }
      </section>
    } @else {
      <p>Cargando...</p>
    }
  `,
})
export class IncidenciasArbolPage implements OnInit {
  private readonly incidenciasService = inject(IncidenciasService);
  private readonly proyectosService = inject(ProyectosService);
  private readonly listadoEstado = inject(IncidenciasListadoEstadoService);
  private readonly destroyRef = inject(DestroyRef);

  /** Parámetro de ruta `:id` del proyecto. */
  readonly id = input.required<string>();

  protected readonly mensajeArchivado = MENSAJE_BANNER_ARCHIVADO;

  readonly proyectoId = computed(() => parsearId(this.id()));
  readonly queryParamsListado = computed(() => this.listadoEstado.queryParams(this.proyectoId()));
  readonly proyecto = signal<ProyectoDto | null>(null);
  readonly arbol = signal<readonly NodoArbol[]>([]);
  readonly expandidos = signal<ReadonlySet<number>>(new Set());
  readonly noEncontrado = signal(false);
  readonly errorCarga = signal<string | null>(null);

  ngOnInit(): void {
    const proyectoId = this.proyectoId();
    if (proyectoId === null) {
      this.noEncontrado.set(true);
      return;
    }
    forkJoin({
      proyecto: this.proyectosService.obtener(proyectoId),
      nodos: this.incidenciasService.arbol(proyectoId),
    })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: ({ proyecto, nodos }) => {
          const arbol = construirArbol(nodos);
          this.arbol.set(arbol);
          this.expandidos.set(idsExpandidosIniciales(arbol));
          this.proyecto.set(proyecto);
        },
        error: (error: unknown) => {
          if (esNoEncontrado(error)) {
            this.noEncontrado.set(true);
          } else {
            this.errorCarga.set(mensajeErrorAccion(error) ?? 'No se pudo cargar el árbol.');
          }
        },
      });
  }

  /** Expande o colapsa un nodo. */
  alternar(id: number): void {
    this.expandidos.update((actuales) => alternarId(actuales, id));
  }

  /** Expande todos los nodos con hijos. */
  expandirTodo(): void {
    this.expandidos.set(idsConHijos(this.arbol()));
  }

  /** Colapsa todos los nodos. */
  colapsarTodo(): void {
    this.expandidos.set(new Set());
  }
}
