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
import { takeUntilDestroyed, toObservable } from '@angular/core/rxjs-interop';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { EMPTY, catchError, of, switchMap, tap } from 'rxjs';
import { Paginado } from '../../../../core/models/paginado.model';
import { NoEncontradoComponent } from '../../../../shared/components/no-encontrado/no-encontrado.component';
import { PaginadorComponent } from '../../../../shared/components/paginador/paginador.component';
import { esNoEncontrado, mensajeErrorAccion } from '../../../../shared/forms/errores-servidor';
import { parsearId } from '../../../../shared/utils/ids';
import { EstadoIncidenciaDto } from '../../../configuracion-incidencias/models/estado-incidencia.model';
import { PrioridadIncidenciaDto } from '../../../configuracion-incidencias/models/prioridad-incidencia.model';
import { TipoIncidenciaDto } from '../../../configuracion-incidencias/models/tipo-incidencia.model';
import { EstadosIncidenciaService } from '../../../configuracion-incidencias/services/estados-incidencia.service';
import { PrioridadesIncidenciaService } from '../../../configuracion-incidencias/services/prioridades-incidencia.service';
import { TiposIncidenciaService } from '../../../configuracion-incidencias/services/tipos-incidencia.service';
import { PersonaDto } from '../../../personas/models/persona.model';
import { PersonasService } from '../../../personas/services/personas.service';
import { AgrupacionDto } from '../../../proyectos/models/agrupacion.model';
import { ProyectoDto } from '../../../proyectos/models/proyecto.model';
import { AgrupacionesService } from '../../../proyectos/services/agrupaciones.service';
import { ProyectosService } from '../../../proyectos/services/proyectos.service';
import {
  CambioFiltrosIncidencias,
  IncidenciasFiltrosComponent,
} from '../../components/incidencias-filtros/incidencias-filtros.component';
import { IncidenciasTablaComponent } from '../../components/incidencias-tabla/incidencias-tabla.component';
import {
  FILTROS_INCIDENCIAS_POR_DEFECTO,
  FiltrosIncidencias,
  IncidenciaResumenDto,
  OrdenIncidencias,
} from '../../models/incidencia.model';
import { IncidenciasListadoEstadoService } from '../../services/incidencias-listado-estado.service';
import { IncidenciasService } from '../../services/incidencias.service';
import {
  parsearFiltrosIncidencias,
  serializarFiltrosIncidencias,
  siguienteOrdenIncidencias,
} from '../../utils/filtros-incidencias';
import { MENSAJE_BANNER_ARCHIVADO } from '../../utils/proyecto-archivado';

/**
 * I1 – Listado de incidencias de un proyecto (`/proyectos/:id/incidencias`). Filtros, página y
 * orden viven en los query params (vinculados a inputs); cada cambio vuelve a consultar la API.
 * Proyecto archivado: banner y sin "Nueva incidencia" (RN-36).
 *
 * Creado: 2026-10-09
 * Modificado: 2026-10-09 - Creación inicial (RF-002)
 */
@Component({
  selector: 'app-incidencias-listado-page',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    RouterLink,
    NoEncontradoComponent,
    IncidenciasFiltrosComponent,
    IncidenciasTablaComponent,
    PaginadorComponent,
  ],
  template: `
    @if (noEncontrado()) {
      <app-no-encontrado rutaVolver="/proyectos" />
    } @else {
      <header class="encabezado-pagina">
        <div>
          @if (proyecto(); as p) {
            <p class="subtitulo">{{ p.codigo }} – {{ p.nombre }}</p>
          }
          <h1>Incidencias</h1>
        </div>
        <div class="acciones">
          <a class="btn" [routerLink]="['/proyectos', proyectoId()]">Volver al proyecto</a>
          <a class="btn" [routerLink]="['/proyectos', proyectoId(), 'incidencias', 'arbol']">
            Ver árbol
          </a>
          @if (proyecto() && !archivado()) {
            <a
              class="btn btn-primario"
              [routerLink]="['/proyectos', proyectoId(), 'incidencias', 'nueva']"
            >
              Nueva incidencia
            </a>
          }
        </div>
      </header>

      @if (archivado()) {
        <div class="banner-archivado" role="status">{{ mensajeArchivado }}</div>
      }

      <app-incidencias-filtros
        [filtros]="filtros()"
        [tipos]="tipos()"
        [estados]="estados()"
        [prioridades]="prioridades()"
        [personas]="personas()"
        [agrupaciones]="agrupaciones()"
        (cambio)="cambiarFiltros($event)"
        (limpiar)="limpiarFiltros()"
      />

      @if (error(); as mensaje) {
        <p class="alerta alerta-error" role="alert">{{ mensaje }}</p>
      }

      <section class="panel" [attr.aria-busy]="cargando()">
        @if (cargando() && !resultado()) {
          <p>Cargando...</p>
        } @else if (resultado(); as pagina) {
          <app-incidencias-tabla
            [proyectoId]="proyectoId() ?? 0"
            [incidencias]="pagina.items"
            [ordenarPor]="filtros().ordenarPor"
            [direccion]="filtros().direccion"
            (ordenar)="ordenar($event)"
            (seleccionar)="verDetalle($event)"
          />
          @if (pagina.total > 0) {
            <app-paginador
              [pagina]="pagina.pagina"
              [tamanioPagina]="pagina.tamanioPagina"
              [total]="pagina.total"
              (cambioPagina)="irAPagina($event)"
            />
          }
        }
      </section>
    }
  `,
})
export class IncidenciasListadoPage implements OnInit {
  private readonly incidenciasService = inject(IncidenciasService);
  private readonly proyectosService = inject(ProyectosService);
  private readonly agrupacionesService = inject(AgrupacionesService);
  private readonly tiposService = inject(TiposIncidenciaService);
  private readonly estadosService = inject(EstadosIncidenciaService);
  private readonly prioridadesService = inject(PrioridadesIncidenciaService);
  private readonly personasService = inject(PersonasService);
  private readonly listadoEstado = inject(IncidenciasListadoEstadoService);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);
  private readonly destroyRef = inject(DestroyRef);

  /** Parámetro de ruta `:id` del proyecto. */
  readonly id = input.required<string>();
  // Query params vinculados por withComponentInputBinding (`tipoId` puede repetirse).
  readonly texto = input<string>();
  readonly tipoId = input<string | readonly string[]>();
  readonly estadoId = input<string>();
  readonly prioridadId = input<string>();
  readonly responsableId = input<string>();
  readonly sinResponsable = input<string>();
  readonly agrupacionId = input<string>();
  readonly sinAgrupacion = input<string>();
  readonly pagina = input<string>();
  readonly tamanioPagina = input<string>();
  readonly ordenarPor = input<string>();
  readonly direccion = input<string>();

  protected readonly mensajeArchivado = MENSAJE_BANNER_ARCHIVADO;

  readonly proyectoId = computed(() => parsearId(this.id()));
  /** Filtros efectivos (valores inválidos de la URL → defaults del contrato). */
  readonly filtros = computed<FiltrosIncidencias>(() =>
    parsearFiltrosIncidencias({
      texto: this.texto(),
      tipoId: this.tipoId(),
      estadoId: this.estadoId(),
      prioridadId: this.prioridadId(),
      responsableId: this.responsableId(),
      sinResponsable: this.sinResponsable(),
      agrupacionId: this.agrupacionId(),
      sinAgrupacion: this.sinAgrupacion(),
      pagina: this.pagina(),
      tamanioPagina: this.tamanioPagina(),
      ordenarPor: this.ordenarPor(),
      direccion: this.direccion(),
    }),
  );

  readonly proyecto = signal<ProyectoDto | null>(null);
  readonly archivado = computed(() => this.proyecto()?.archivado ?? false);
  readonly tipos = signal<readonly TipoIncidenciaDto[]>([]);
  readonly estados = signal<readonly EstadoIncidenciaDto[]>([]);
  readonly prioridades = signal<readonly PrioridadIncidenciaDto[]>([]);
  readonly personas = signal<readonly PersonaDto[]>([]);
  readonly agrupaciones = signal<readonly AgrupacionDto[]>([]);
  readonly resultado = signal<Paginado<IncidenciaResumenDto> | null>(null);
  readonly cargando = signal(false);
  readonly error = signal<string | null>(null);
  readonly noEncontrado = signal(false);

  constructor() {
    toObservable(this.filtros)
      .pipe(
        tap((filtros) => this.antesDeCargar(filtros)),
        switchMap((filtros) => {
          const proyectoId = this.proyectoId();
          if (proyectoId === null) {
            return EMPTY;
          }
          return this.incidenciasService.listar(proyectoId, filtros).pipe(
            catchError((error: unknown) => {
              this.alFallarListado(error);
              return of(null);
            }),
          );
        }),
        takeUntilDestroyed(),
      )
      .subscribe((resultado) => {
        this.cargando.set(false);
        if (resultado) {
          this.resultado.set(resultado);
        }
      });
  }

  ngOnInit(): void {
    const proyectoId = this.proyectoId();
    if (proyectoId === null) {
      this.noEncontrado.set(true);
      return;
    }
    this.cargarProyecto(proyectoId);
    this.cargarCatalogos(proyectoId);
  }

  /** Aplica filtros nuevos volviendo a la página 1. */
  cambiarFiltros(cambio: CambioFiltrosIncidencias): void {
    this.navegar({ ...this.filtros(), ...cambio, pagina: 1 });
  }

  /** Restablece los filtros por defecto. */
  limpiarFiltros(): void {
    this.navegar(FILTROS_INCIDENCIAS_POR_DEFECTO);
  }

  /** Ordena por la columna indicada (alterna la dirección si ya era la actual). */
  ordenar(columna: OrdenIncidencias): void {
    this.navegar({
      ...this.filtros(),
      ...siguienteOrdenIncidencias(this.filtros(), columna),
      pagina: 1,
    });
  }

  /** Cambia de página. */
  irAPagina(pagina: number): void {
    this.navegar({ ...this.filtros(), pagina });
  }

  /** Navega al detalle de la incidencia. */
  verDetalle(incidenciaId: number): void {
    void this.router.navigate(['/proyectos', this.proyectoId(), 'incidencias', incidenciaId]);
  }

  /** Marca la carga en curso y recuerda los query params para "Volver". */
  private antesDeCargar(filtros: FiltrosIncidencias): void {
    const proyectoId = this.proyectoId();
    this.cargando.set(proyectoId !== null);
    this.error.set(null);
    if (proyectoId !== null) {
      this.listadoEstado.guardar(proyectoId, serializarFiltrosIncidencias(filtros));
    }
  }

  /** 404 del proyecto → "No se encontró el recurso"; otros errores → mensaje. */
  private alFallarListado(error: unknown): void {
    if (esNoEncontrado(error)) {
      this.noEncontrado.set(true);
      return;
    }
    this.error.set(mensajeErrorAccion(error));
  }

  /** Refleja los filtros en la URL (dispara la recarga vía inputs). */
  private navegar(filtros: FiltrosIncidencias): void {
    void this.router.navigate([], {
      relativeTo: this.route,
      queryParams: serializarFiltrosIncidencias(filtros),
    });
  }

  /** Carga el proyecto para el encabezado y el modo solo lectura. */
  private cargarProyecto(proyectoId: number): void {
    this.proyectosService
      .obtener(proyectoId)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (proyecto) => this.proyecto.set(proyecto),
        error: (error: unknown) => this.alFallarListado(error),
      });
  }

  /** Carga los catálogos de los filtros (todos, incluidos inactivos). */
  private cargarCatalogos(proyectoId: number): void {
    this.tiposService.listar().subscribe({
      next: (tipos) => this.tipos.set(tipos),
      error: () => this.tipos.set([]),
    });
    this.estadosService.listar().subscribe({
      next: (estados) => this.estados.set(estados),
      error: () => this.estados.set([]),
    });
    this.prioridadesService.listar().subscribe({
      next: (prioridades) => this.prioridades.set(prioridades),
      error: () => this.prioridades.set([]),
    });
    this.personasService.listar({ incluirInactivas: true }).subscribe({
      next: (personas) => this.personas.set(personas),
      error: () => this.personas.set([]),
    });
    this.agrupacionesService.listar(proyectoId).subscribe({
      next: (agrupaciones) => this.agrupaciones.set(agrupaciones),
      error: () => this.agrupaciones.set([]),
    });
  }
}
