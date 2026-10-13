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
import { ActivatedRoute, Params, Router, RouterLink } from '@angular/router';
import { EMPTY, Observable, catchError, map, of, switchMap, tap } from 'rxjs';
import { MENSAJES_GENERICOS } from '../../../../core/interceptors/convertir-error';
import { esErrorApi } from '../../../../core/models/error-api.model';
import { PERMISOS } from '../../../../core/models/permisos.model';
import { NotificacionService } from '../../../../core/services/notificacion.service';
import { UsuarioActualService } from '../../../../core/services/usuario-actual.service';
import { NoEncontradoComponent } from '../../../../shared/components/no-encontrado/no-encontrado.component';
import { mensajeErrorAccion } from '../../../../shared/forms/errores-servidor';
import { parsearId } from '../../../../shared/utils/ids';
import { EstadoIncidenciaDto } from '../../../configuracion-incidencias/models/estado-incidencia.model';
import { NivelIncidenciaDto } from '../../../configuracion-incidencias/models/nivel-incidencia.model';
import { TipoIncidenciaDto } from '../../../configuracion-incidencias/models/tipo-incidencia.model';
import { EstadosIncidenciaService } from '../../../configuracion-incidencias/services/estados-incidencia.service';
import { NivelesIncidenciaService } from '../../../configuracion-incidencias/services/niveles-incidencia.service';
import { TiposIncidenciaService } from '../../../configuracion-incidencias/services/tipos-incidencia.service';
import { ArbolIncidenciasComponent } from '../../../incidencias/components/arbol-incidencias/arbol-incidencias.component';
import { IncidenciasService } from '../../../incidencias/services/incidencias.service';
import {
  NodoArbol,
  alternarId,
  construirArbol,
  idsConHijos,
  idsExpandidosHastaNivel,
  idsExpandidosIniciales,
} from '../../../incidencias/utils/arbol-incidencias';
import { MENSAJE_BANNER_ARCHIVADO } from '../../../incidencias/utils/proyecto-archivado';
import { AgrupacionDto } from '../../../proyectos/models/agrupacion.model';
import { AgrupacionesService } from '../../../proyectos/services/agrupaciones.service';
import { ConfigurarColumnasDialogComponent } from '../../components/configurar-columnas-dialog/configurar-columnas-dialog.component';
import { ConteoPorEstadoComponent } from '../../components/conteo-por-estado/conteo-por-estado.component';
import { FiltroAlcanceComponent } from '../../components/filtro-alcance/filtro-alcance.component';
import { FiltrosTableroComponent } from '../../components/filtros-tablero/filtros-tablero.component';
import { IndicadoresComponent } from '../../components/indicadores/indicadores.component';
import { LeyendaAlcanceComponent } from '../../components/leyenda-alcance/leyenda-alcance.component';
import {
  AmbitoElegido,
  SelectorAmbitoComponent,
} from '../../components/selector-ambito/selector-ambito.component';
import { TablaAgrupacionesSeguimientoComponent } from '../../components/tabla-agrupaciones-seguimiento/tabla-agrupaciones-seguimiento.component';
import { TablaTableroComponent } from '../../components/tabla-tablero/tabla-tablero.component';
import {
  AlcanceConsulta,
  ConfiguracionTableroRequest,
  FiltrosTablero,
  SIN_FILTROS_TABLERO,
  SeguimientoDto,
  TableroIncidenciasDto,
} from '../../models/seguimiento.model';
import { SeguimientoService } from '../../services/seguimiento.service';
import { esAlcanceCompleto, tipoIdsEnAlcance } from '../../utils/alcance';
import {
  DestinoResumen,
  FiltrosSeguimiento,
  VistaSeguimiento,
  aQueryParams,
  conAmbito,
  filtrosSeguimientoDesdeQueryParams,
  hayFiltrosTablero,
  navegacionDesdeResumen,
} from '../../utils/filtros-seguimiento';
import { ClaveTarjetaIndicador } from '../../utils/indicadores';

/** Mensaje de 404 `AGRUPACION_NO_ENCONTRADA` en T2 (RF-005 "Pantallas"). */
export const MENSAJE_AGRUPACION_NO_EXISTE = 'La agrupación no existe en este proyecto.';

/** Pestañas de T2 (DA-16: Resumen · Tablero · Jerarquía). */
export const PESTANIAS_SEGUIMIENTO: readonly {
  readonly vista: VistaSeguimiento;
  readonly titulo: string;
}[] = [
  { vista: 'resumen', titulo: 'Resumen' },
  { vista: 'tablero', titulo: 'Tablero' },
  { vista: 'jerarquia', titulo: 'Jerarquía' },
];

/** Resultado de un error de carga del Resumen. */
type ErrorResumen =
  | { readonly tipo: 'proyecto' }
  | { readonly tipo: 'agrupacion' }
  | { readonly tipo: 'mensaje'; readonly mensaje: string };

/** Igualdad estructural para las claves de carga (evita recargas sin cambios reales). */
function mismaClave<T>(a: T, b: T): boolean {
  return JSON.stringify(a) === JSON.stringify(b);
}

/** Contador del tablero: "N incidencias" y "(filtradas)" si hay filtros (RF-005 CA-24). */
export function textoContadorTablero(total: number, filtrado: boolean): string {
  const base = total === 1 ? '1 incidencia' : `${total} incidencias`;
  return filtrado ? `${base} (filtradas)` : base;
}

/** Mensaje de error del guardado de columnas (dentro del diálogo, también 401/403/409). */
export function mensajeErrorConfiguracion(error: unknown): string {
  if (!esErrorApi(error)) {
    return MENSAJES_GENERICOS.inesperado;
  }
  const porCampo = Object.values(error.errores).flat();
  return porCampo.length > 0 ? porCampo.join(' ') : error.detalle;
}

/**
 * T2 – Seguimiento de proyecto o agrupación (`/proyectos/:id/seguimiento`, RF-005). Ámbito,
 * alcance, vista y filtros del tablero viven en la URL (RN-06, RN-21); el orden de la grilla no
 * (RN-33). Vistas Resumen (indicadores y conteo clicables → Tablero filtrado, RN-24), Tablero
 * (grilla con filtros y columnas configurables con permiso) y Jerarquía (árbol del ámbito con
 * "expandir hasta nivel N", marcas y nodos fuera del alcance atenuados).
 *
 * Creado: 2026-10-10
 * Modificado: 2026-10-10 - Creación inicial (RF-005)
 */
@Component({
  selector: 'app-seguimiento-proyecto-page',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    RouterLink,
    NoEncontradoComponent,
    SelectorAmbitoComponent,
    FiltroAlcanceComponent,
    LeyendaAlcanceComponent,
    IndicadoresComponent,
    ConteoPorEstadoComponent,
    TablaAgrupacionesSeguimientoComponent,
    FiltrosTableroComponent,
    TablaTableroComponent,
    ConfigurarColumnasDialogComponent,
    ArbolIncidenciasComponent,
  ],
  template: `
    @if (errorResumen()?.tipo === 'proyecto') {
      <app-no-encontrado rutaVolver="/seguimiento" textoVolver="Volver al tablero general" />
    } @else {
      <header class="encabezado-pagina">
        <div>
          @if (proyecto(); as p) {
            <p class="subtitulo">
              <a [routerLink]="['/proyectos', p.id]">{{ p.codigo }}</a> – {{ p.nombre }}
            </p>
          }
          <h1>Seguimiento</h1>
        </div>
        <div class="acciones">
          <a class="btn" routerLink="/seguimiento">Volver al tablero general</a>
          <a class="btn" [routerLink]="['/proyectos', proyectoId()]">Ver proyecto</a>
        </div>
      </header>

      @if (archivado()) {
        <div class="banner-archivado" role="status">{{ mensajeArchivado }}</div>
      }

      <section class="panel panel-ambito">
        <div class="filtros">
          <app-selector-ambito
            [agrupaciones]="agrupaciones()"
            [agrupacionId]="filtros().agrupacionId"
            [sinAgrupacion]="filtros().sinAgrupacion"
            (cambio)="cambiarAmbito($event)"
          />
        </div>
        @if (resumen()?.agrupacion; as agrupacion) {
          <div class="detalle-agrupacion">
            <h2>{{ agrupacion.nombre }}</h2>
            @if (agrupacion.descripcion) {
              <p class="texto-multilinea">{{ agrupacion.descripcion }}</p>
            }
            @if (!archivado()) {
              <a
                class="btn btn-chico"
                [routerLink]="['/proyectos', proyectoId(), 'agrupaciones', agrupacion.id, 'editar']"
              >
                Editar agrupación
              </a>
            }
          </div>
        } @else if (filtros().sinAgrupacion) {
          <p class="texto-secundario">Incidencias del proyecto sin agrupación.</p>
        }
        <app-filtro-alcance
          [niveles]="niveles()"
          [tipos]="tipos()"
          [nivelIds]="filtros().nivelIds"
          [tipoIds]="filtros().tipoIds"
          (cambio)="cambiarAlcance($event)"
        />
      </section>

      @if (errorResumen(); as error) {
        @if (error.tipo === 'agrupacion') {
          <div class="alerta alerta-error" role="alert">
            <p>{{ mensajeAgrupacionNoExiste }}</p>
            <button type="button" class="btn btn-chico" (click)="todoElProyecto()">
              Todo el proyecto
            </button>
          </div>
        } @else if (error.tipo === 'mensaje') {
          <p class="alerta alerta-error" role="alert">{{ error.mensaje }}</p>
        }
      } @else {
        <nav class="pestanias" aria-label="Vistas del seguimiento">
          @for (pestania of pestanias; track pestania.vista) {
            <a
              class="pestania"
              [class.pestania-activa]="filtros().vista === pestania.vista"
              [attr.aria-current]="filtros().vista === pestania.vista ? 'page' : null"
              [routerLink]="['/proyectos', proyectoId(), 'seguimiento']"
              [queryParams]="queryParamsVista(pestania.vista)"
              >{{ pestania.titulo }}</a
            >
          }
        </nav>

        @if (resumen(); as r) {
          <app-leyenda-alcance
            [alcance]="r.alcance"
            [fechaReferencia]="r.fechaReferencia"
            [estados]="estados()"
            [puedeConfigurarEstados]="puedeConfigurarEstados()"
            [notaAtenuados]="filtros().vista === 'jerarquia' && !alcanceCompleto()"
          />
        }

        @switch (filtros().vista) {
          @case ('tablero') {
            <section
              class="panel"
              aria-labelledby="titulo-vista-tablero"
              [attr.aria-busy]="cargandoTablero()"
            >
              <h2 id="titulo-vista-tablero" class="sr-only">Tablero</h2>
              <app-filtros-tablero
                [filtros]="filtros()"
                [estados]="estados()"
                [estadoAplicado]="tablero()?.filtros?.estado ?? null"
                (cambio)="cambiarFiltrosTablero($event)"
              />
              @if (errorTablero(); as mensaje) {
                <div class="alerta alerta-error" role="alert">
                  <p>{{ mensaje }}</p>
                  @if (hayFiltrosTablero()) {
                    <button type="button" class="btn btn-chico" (click)="limpiarFiltrosTablero()">
                      Limpiar filtros
                    </button>
                  }
                </div>
              } @else if (tablero(); as t) {
                <div class="encabezado-seccion">
                  <p class="contador-tablero" aria-live="polite">
                    {{ contador(t.total) }}
                  </p>
                  @if (puedeConfigurarColumnas()) {
                    <button type="button" class="btn" (click)="abrirColumnas()">Columnas</button>
                  }
                </div>
                @if (t.items.length === 0) {
                  @if (hayFiltrosTablero()) {
                    <p class="vacio">No hay incidencias que coincidan con los filtros.</p>
                    <button type="button" class="btn" (click)="limpiarFiltrosTablero()">
                      Limpiar filtros
                    </button>
                  } @else {
                    <p class="vacio">No hay incidencias en este ámbito y alcance.</p>
                  }
                } @else {
                  <app-tabla-tablero
                    [proyectoId]="proyectoId() ?? 0"
                    [items]="t.items"
                    [configuracion]="t.configuracion"
                    (seleccionar)="verIncidencia($event)"
                  />
                }
                <app-configurar-columnas-dialog
                  [abierto]="dialogoColumnas()"
                  [configuracion]="t.configuracion"
                  [enviando]="guardandoColumnas()"
                  [error]="errorColumnas()"
                  (guardar)="guardarColumnas($event)"
                  (cancelar)="cerrarColumnas()"
                />
              } @else {
                <p>Cargando...</p>
              }
            </section>
          }
          @case ('jerarquia') {
            <section
              class="panel"
              aria-labelledby="titulo-vista-jerarquia"
              [attr.aria-busy]="arbol() === null && !errorArbol()"
            >
              <h2 id="titulo-vista-jerarquia" class="sr-only">Jerarquía</h2>
              @if (errorArbol(); as mensaje) {
                <p class="alerta alerta-error" role="alert">{{ mensaje }}</p>
              } @else if (arbol(); as nodos) {
                @if (nodos.length === 0) {
                  <p class="vacio">No hay incidencias en este ámbito.</p>
                } @else {
                  <div class="acciones acciones-jerarquia">
                    <button type="button" class="btn btn-chico" (click)="expandirTodo()">
                      Expandir todo
                    </button>
                    <button type="button" class="btn btn-chico" (click)="colapsarTodo()">
                      Colapsar todo
                    </button>
                    @if (numerosNivel().length > 0) {
                      <span class="grupo-botones" role="group" aria-label="Expandir hasta nivel">
                        <span>Expandir hasta nivel:</span>
                        @for (numero of numerosNivel(); track numero) {
                          <button
                            type="button"
                            class="btn btn-chico"
                            [attr.aria-label]="'Expandir hasta nivel ' + numero"
                            (click)="expandirHastaNivel(numero)"
                          >
                            {{ numero }}
                          </button>
                        }
                      </span>
                    }
                  </div>
                  <app-arbol-incidencias
                    [proyectoId]="proyectoId() ?? 0"
                    [arbol]="nodos"
                    [expandidos]="expandidos()"
                    [tipoIdsEnAlcance]="tiposEnAlcance()"
                    [mostrarMarcas]="true"
                    (alternar)="alternarNodo($event)"
                  />
                }
              } @else {
                <p>Cargando...</p>
              }
            </section>
          }
          @default {
            <section
              class="panel"
              aria-labelledby="titulo-vista-resumen"
              [attr.aria-busy]="cargandoResumen()"
            >
              <h2 id="titulo-vista-resumen" class="sr-only">Resumen</h2>
              @if (resumen(); as r) {
                <app-indicadores
                  [indicadores]="r.indicadores"
                  [alcance]="r.alcance"
                  [fechaReferencia]="r.fechaReferencia"
                  [clicable]="true"
                  (indicadorSeleccionado)="irDesdeTarjeta($event)"
                />
                <h3>Cantidad por estado</h3>
                <app-conteo-por-estado
                  [porEstado]="r.indicadores.porEstado"
                  [clicable]="true"
                  (seleccionar)="irDesdeResumen({ estadoId: $event })"
                />
                @if (r.ambito === 'proyecto') {
                  <h3>Por agrupación</h3>
                  <app-tabla-agrupaciones-seguimiento
                    [proyectoId]="r.proyecto.id"
                    [filas]="r.agrupaciones"
                    [nivelIds]="filtros().nivelIds"
                    [tipoIds]="filtros().tipoIds"
                  />
                }
              } @else {
                <p>Cargando...</p>
              }
            </section>
          }
        }
      }
    }
  `,
})
export class SeguimientoProyectoPage implements OnInit {
  private readonly seguimientoService = inject(SeguimientoService);
  private readonly incidenciasService = inject(IncidenciasService);
  private readonly agrupacionesService = inject(AgrupacionesService);
  private readonly nivelesService = inject(NivelesIncidenciaService);
  private readonly tiposService = inject(TiposIncidenciaService);
  private readonly estadosService = inject(EstadosIncidenciaService);
  private readonly usuarioActual = inject(UsuarioActualService);
  private readonly notificacion = inject(NotificacionService);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);
  private readonly destroyRef = inject(DestroyRef);

  /** Parámetro de ruta `:id` del proyecto. */
  readonly id = input.required<string>();
  // Query params vinculados por withComponentInputBinding (`nivelId`/`tipoId` repetibles).
  readonly agrupacionId = input<string>();
  readonly sinAgrupacion = input<string>();
  readonly nivelId = input<string | readonly string[]>();
  readonly tipoId = input<string | readonly string[]>();
  readonly vista = input<string>();
  readonly indicador = input<string>();
  readonly estadoId = input<string>();
  readonly texto = input<string>();

  protected readonly mensajeArchivado = MENSAJE_BANNER_ARCHIVADO;
  protected readonly mensajeAgrupacionNoExiste = MENSAJE_AGRUPACION_NO_EXISTE;
  protected readonly pestanias = PESTANIAS_SEGUIMIENTO;

  readonly proyectoId = computed(() => parsearId(this.id()));
  /** Filtros efectivos de la URL (vista desconocida → resumen, CA-49). */
  readonly filtros = computed<FiltrosSeguimiento>(() =>
    filtrosSeguimientoDesdeQueryParams({
      agrupacionId: this.agrupacionId(),
      sinAgrupacion: this.sinAgrupacion(),
      nivelId: this.nivelId(),
      tipoId: this.tipoId(),
      vista: this.vista(),
      indicador: this.indicador(),
      estadoId: this.estadoId(),
      texto: this.texto(),
    }),
  );

  // Catálogos.
  readonly niveles = signal<readonly NivelIncidenciaDto[]>([]);
  readonly tipos = signal<readonly TipoIncidenciaDto[]>([]);
  readonly estados = signal<readonly EstadoIncidenciaDto[]>([]);
  readonly agrupaciones = signal<readonly AgrupacionDto[]>([]);

  // Resumen (siempre cargado: encabezado, ámbito y leyenda).
  readonly resumen = signal<SeguimientoDto | null>(null);
  readonly cargandoResumen = signal(false);
  readonly errorResumen = signal<ErrorResumen | null>(null);

  // Tablero.
  readonly tablero = signal<TableroIncidenciasDto | null>(null);
  readonly cargandoTablero = signal(false);
  readonly errorTablero = signal<string | null>(null);
  readonly dialogoColumnas = signal(false);
  readonly guardandoColumnas = signal(false);
  readonly errorColumnas = signal<string | null>(null);

  // Jerarquía.
  readonly arbol = signal<readonly NodoArbol[] | null>(null);
  readonly expandidos = signal<ReadonlySet<number>>(new Set());
  readonly errorArbol = signal<string | null>(null);

  readonly proyecto = computed(() => this.resumen()?.proyecto ?? this.tablero()?.proyecto ?? null);
  readonly archivado = computed(() => this.proyecto()?.archivado ?? false);
  readonly puedeConfigurarEstados = computed(() =>
    this.usuarioActual.tienePermiso(PERMISOS.configuracionIncidencias),
  );
  /** Botón "Columnas": con permiso y proyecto no archivado (CA-22, CA-34). */
  readonly puedeConfigurarColumnas = computed(
    () =>
      this.usuarioActual.tienePermiso(PERMISOS.proyectosConfigurarTablero) &&
      this.proyecto() !== null &&
      !this.archivado(),
  );
  readonly hayFiltrosTablero = computed(() => hayFiltrosTablero(this.filtros()));
  readonly alcanceCompleto = computed(() => {
    const resumen = this.resumen();
    return resumen === null || esAlcanceCompleto(resumen.alcance);
  });
  readonly tiposEnAlcance = computed(() => tipoIdsEnAlcance(this.resumen()?.alcance ?? null));
  /** Números de nivel configurados (botones "Expandir hasta nivel", RN-22). */
  readonly numerosNivel = computed(() =>
    [...new Set(this.niveles().map((n) => n.numero))].sort((a, b) => a - b),
  );

  /** Clave del resumen: proyecto, ámbito y alcance (los filtros del tablero no lo afectan). */
  private readonly claveResumen = computed(
    () => {
      const f = this.filtros();
      return {
        proyectoId: this.proyectoId(),
        agrupacionId: f.agrupacionId,
        sinAgrupacion: f.sinAgrupacion,
        nivelIds: f.nivelIds,
        tipoIds: f.tipoIds,
      };
    },
    { equal: mismaClave },
  );

  /** Clave del tablero (solo en la vista Tablero): ámbito, alcance y filtros. */
  private readonly claveTablero = computed(
    () => {
      const f = this.filtros();
      if (f.vista !== 'tablero') {
        return null;
      }
      return {
        ...this.claveResumen(),
        indicador: f.indicador,
        estadoId: f.estadoId,
        texto: f.texto,
      };
    },
    { equal: mismaClave },
  );

  /** Clave del árbol (solo en la vista Jerarquía): proyecto y ámbito. */
  private readonly claveArbol = computed(
    () => {
      const f = this.filtros();
      if (f.vista !== 'jerarquia') {
        return null;
      }
      return {
        proyectoId: this.proyectoId(),
        agrupacionId: f.agrupacionId,
        sinAgrupacion: f.sinAgrupacion,
      };
    },
    { equal: mismaClave },
  );

  constructor() {
    this.suscribirResumen();
    this.suscribirTablero();
    this.suscribirArbol();
  }

  ngOnInit(): void {
    const proyectoId = this.proyectoId();
    if (proyectoId === null) {
      this.errorResumen.set({ tipo: 'proyecto' });
      return;
    }
    this.cargarCatalogos(proyectoId);
  }

  /** Query params de una pestaña: conserva ámbito, alcance y filtros del tablero. */
  queryParamsVista(vista: VistaSeguimiento): Params {
    return aQueryParams({ ...this.filtros(), vista });
  }

  /** Contador de la grilla. */
  contador(total: number): string {
    return textoContadorTablero(total, this.hayFiltrosTablero());
  }

  /** Cambia el ámbito conservando alcance, vista y filtros del tablero. */
  cambiarAmbito(ambito: AmbitoElegido): void {
    this.navegar(conAmbito(this.filtros(), ambito.agrupacionId, ambito.sinAgrupacion));
  }

  /** Vuelve al ámbito "Todo el proyecto" (404 de agrupación). */
  todoElProyecto(): void {
    this.navegar(conAmbito(this.filtros(), null, false));
  }

  /** Cambia el alcance (niveles y tipos). */
  cambiarAlcance(alcance: AlcanceConsulta): void {
    this.navegar({ ...this.filtros(), nivelIds: alcance.nivelIds, tipoIds: alcance.tipoIds });
  }

  /** Aplica los filtros del tablero (vuelve a pedirlo y restablece el orden, RN-33). */
  cambiarFiltrosTablero(filtros: FiltrosTablero): void {
    this.navegar({ ...this.filtros(), ...filtros });
  }

  /** Quita indicador, estado y texto conservando ámbito y alcance (CA-48). */
  limpiarFiltrosTablero(): void {
    this.navegar({ ...this.filtros(), ...SIN_FILTROS_TABLERO });
  }

  /** Clic en una tarjeta del Resumen (RN-24). */
  irDesdeTarjeta(clave: ClaveTarjetaIndicador): void {
    this.irDesdeResumen(clave === 'total' ? { total: true } : { indicador: clave });
  }

  /** Navega del Resumen al Tablero filtrado conservando ámbito y alcance (RN-24). */
  irDesdeResumen(destino: DestinoResumen): void {
    void this.router.navigate([], {
      relativeTo: this.route,
      queryParams: navegacionDesdeResumen(this.filtros(), destino),
    });
  }

  /** Abre el detalle de la incidencia (I3). */
  verIncidencia(incidenciaId: number): void {
    void this.router.navigate(['/proyectos', this.proyectoId(), 'incidencias', incidenciaId]);
  }

  /** Abre el diálogo "Columnas". */
  abrirColumnas(): void {
    this.errorColumnas.set(null);
    this.guardandoColumnas.set(false);
    this.dialogoColumnas.set(true);
  }

  /** Cierra el diálogo sin cambios. */
  cerrarColumnas(): void {
    this.dialogoColumnas.set(false);
    this.guardandoColumnas.set(false);
  }

  /** PUT de la configuración; éxito → toast y la grilla la aplica sin recargar datos. */
  guardarColumnas(request: ConfiguracionTableroRequest): void {
    const proyectoId = this.proyectoId();
    if (proyectoId === null) {
      return;
    }
    this.guardandoColumnas.set(true);
    this.errorColumnas.set(null);
    this.seguimientoService
      .guardarConfiguracionTablero(proyectoId, request)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (configuracion) => {
          this.tablero.update((t) => (t === null ? t : { ...t, configuracion }));
          this.cerrarColumnas();
          this.notificacion.exito('Configuración del tablero guardada.');
        },
        error: (error: unknown) => {
          this.guardandoColumnas.set(false);
          this.errorColumnas.set(mensajeErrorConfiguracion(error));
        },
      });
  }

  /** Expande o colapsa un nodo de la jerarquía. */
  alternarNodo(id: number): void {
    this.expandidos.update((actuales) => alternarId(actuales, id));
  }

  /** Expande todos los nodos con hijos. */
  expandirTodo(): void {
    this.expandidos.set(idsConHijos(this.arbol() ?? []));
  }

  /** Colapsa todos los nodos (quedan las raíces). */
  colapsarTodo(): void {
    this.expandidos.set(new Set());
  }

  /** Expande los nodos de nivel menor que N y colapsa el resto (RN-22). */
  expandirHastaNivel(nivel: number): void {
    this.expandidos.set(idsExpandidosHastaNivel(this.arbol() ?? [], nivel));
  }

  /** Refleja los filtros en la URL (dispara las recargas vía inputs). */
  private navegar(filtros: FiltrosSeguimiento): void {
    void this.router.navigate([], { relativeTo: this.route, queryParams: aQueryParams(filtros) });
  }

  /** Carga el resumen cada vez que cambian proyecto, ámbito o alcance. */
  private suscribirResumen(): void {
    toObservable(this.claveResumen)
      .pipe(
        switchMap((clave) => {
          if (clave.proyectoId === null) {
            return EMPTY;
          }
          this.cargandoResumen.set(true);
          this.errorResumen.set(null);
          this.resumen.set(null);
          return this.seguimientoService
            .seguimiento(clave.proyectoId, clave)
            .pipe(catchError((error: unknown) => this.alFallarResumen(error)));
        }),
        takeUntilDestroyed(),
      )
      .subscribe((resumen) => {
        this.cargandoResumen.set(false);
        if (resumen) {
          this.resumen.set(resumen);
        }
      });
  }

  /** Carga el tablero en la vista Tablero cuando cambian ámbito, alcance o filtros. */
  private suscribirTablero(): void {
    toObservable(this.claveTablero)
      .pipe(
        tap(() => {
          this.tablero.set(null);
          this.errorTablero.set(null);
          this.dialogoColumnas.set(false);
        }),
        switchMap((clave): Observable<TableroIncidenciasDto | null> => {
          if (clave === null || clave.proyectoId === null) {
            return of(null);
          }
          this.cargandoTablero.set(true);
          return this.seguimientoService.tablero(clave.proyectoId, clave).pipe(
            catchError((error: unknown) => {
              // 404 de proyecto/agrupación ya lo informa el resumen.
              this.errorTablero.set(
                esErrorApi(error) && error.status === 404 ? null : mensajeErrorAccion(error),
              );
              return of(null);
            }),
          );
        }),
        takeUntilDestroyed(),
      )
      .subscribe((tablero) => {
        this.cargandoTablero.set(false);
        if (tablero) {
          this.tablero.set(tablero);
        }
      });
  }

  /** Carga el árbol del ámbito en la vista Jerarquía. */
  private suscribirArbol(): void {
    toObservable(this.claveArbol)
      .pipe(
        tap(() => {
          this.arbol.set(null);
          this.errorArbol.set(null);
        }),
        switchMap((clave): Observable<NodoArbol[] | null> => {
          if (clave === null || clave.proyectoId === null) {
            return of(null);
          }
          return this.incidenciasService.arbol(clave.proyectoId, clave).pipe(
            map((nodos) => construirArbol(nodos)),
            catchError((error: unknown) => {
              this.errorArbol.set(
                esErrorApi(error) && error.status === 404
                  ? null
                  : (mensajeErrorAccion(error) ?? 'No se pudo cargar la jerarquía.'),
              );
              return of(null);
            }),
          );
        }),
        takeUntilDestroyed(),
      )
      .subscribe((arbol) => {
        if (arbol) {
          this.arbol.set(arbol);
          this.expandidos.set(idsExpandidosIniciales(arbol));
        }
      });
  }

  /** 404 de proyecto → "No se encontró el recurso"; de agrupación → mensaje propio. */
  private alFallarResumen(error: unknown): Observable<null> {
    if (esErrorApi(error) && error.status === 404) {
      this.errorResumen.set(
        error.tieneCodigo('AGRUPACION_NO_ENCONTRADA')
          ? { tipo: 'agrupacion' }
          : { tipo: 'proyecto' },
      );
    } else {
      this.errorResumen.set({
        tipo: 'mensaje',
        mensaje: mensajeErrorAccion(error) ?? 'No se pudo cargar el seguimiento.',
      });
    }
    return of(null);
  }

  /** Carga niveles, tipos, estados y agrupaciones del proyecto. */
  private cargarCatalogos(proyectoId: number): void {
    this.nivelesService.listar().subscribe({
      next: (niveles) => this.niveles.set(niveles),
      error: () => this.niveles.set([]),
    });
    this.tiposService.listar().subscribe({
      next: (tipos) => this.tipos.set(tipos),
      error: () => this.tipos.set([]),
    });
    this.estadosService.listar().subscribe({
      next: (estados) => this.estados.set(estados),
      error: () => this.estados.set([]),
    });
    this.agrupacionesService.listar(proyectoId).subscribe({
      next: (agrupaciones) => this.agrupaciones.set(agrupaciones),
      error: () => this.agrupaciones.set([]),
    });
  }
}
