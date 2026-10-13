import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  computed,
  inject,
  input,
  signal,
} from '@angular/core';
import { takeUntilDestroyed, toObservable } from '@angular/core/rxjs-interop';
import { ActivatedRoute, Router } from '@angular/router';
import { catchError, of, switchMap, tap } from 'rxjs';
import { PERMISOS } from '../../../../core/models/permisos.model';
import { UsuarioActualService } from '../../../../core/services/usuario-actual.service';
import { PaginadorComponent } from '../../../../shared/components/paginador/paginador.component';
import { mensajeErrorAccion } from '../../../../shared/forms/errores-servidor';
import { EstadoIncidenciaDto } from '../../../configuracion-incidencias/models/estado-incidencia.model';
import { NivelIncidenciaDto } from '../../../configuracion-incidencias/models/nivel-incidencia.model';
import { TipoIncidenciaDto } from '../../../configuracion-incidencias/models/tipo-incidencia.model';
import { EstadosIncidenciaService } from '../../../configuracion-incidencias/services/estados-incidencia.service';
import { NivelesIncidenciaService } from '../../../configuracion-incidencias/services/niveles-incidencia.service';
import { TiposIncidenciaService } from '../../../configuracion-incidencias/services/tipos-incidencia.service';
import { FiltroAlcanceComponent } from '../../components/filtro-alcance/filtro-alcance.component';
import {
  CambioFiltrosTableroGeneral,
  FiltrosTableroGeneralComponent,
} from '../../components/filtros-tablero-general/filtros-tablero-general.component';
import { IndicadoresComponent } from '../../components/indicadores/indicadores.component';
import { LeyendaAlcanceComponent } from '../../components/leyenda-alcance/leyenda-alcance.component';
import {
  CargaAgrupaciones,
  TablaProyectosSeguimientoComponent,
} from '../../components/tabla-proyectos-seguimiento/tabla-proyectos-seguimiento.component';
import {
  AlcanceConsulta,
  CONSULTA_TABLERO_PROYECTOS_POR_DEFECTO,
  ConsultaTableroProyectos,
  OrdenTableroProyectos,
  TableroProyectosDto,
} from '../../models/seguimiento.model';
import { SeguimientoService } from '../../services/seguimiento.service';
import {
  parsearConsultaTableroGeneral,
  serializarConsultaTableroGeneral,
  siguienteOrdenTableroGeneral,
} from '../../utils/filtros-seguimiento';

/**
 * T1 – Tablero general (`/seguimiento`, RF-005): indicadores totales de todos los proyectos que
 * cumplen el filtro (solo informativos, DA-21), tabla por proyecto con orden y paginación en el
 * servidor y filas de agrupaciones expandibles (cargadas una vez por proyecto y alcance).
 * Filtros, alcance, página y orden viven en los query params.
 *
 * Creado: 2026-10-10
 * Modificado: 2026-10-10 - Creación inicial (RF-005)
 */
@Component({
  selector: 'app-tablero-general-page',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    FiltrosTableroGeneralComponent,
    FiltroAlcanceComponent,
    LeyendaAlcanceComponent,
    IndicadoresComponent,
    TablaProyectosSeguimientoComponent,
    PaginadorComponent,
  ],
  template: `
    <header class="encabezado-pagina">
      <h1>Tablero de seguimiento</h1>
    </header>

    <app-filtros-tablero-general
      [consulta]="consulta()"
      (cambio)="cambiarFiltros($event)"
      (limpiar)="limpiarFiltros()"
    />
    <app-filtro-alcance
      [niveles]="niveles()"
      [tipos]="tipos()"
      [nivelIds]="consulta().nivelIds"
      [tipoIds]="consulta().tipoIds"
      (cambio)="cambiarAlcance($event)"
    />

    @if (error(); as mensaje) {
      <p class="alerta alerta-error" role="alert">{{ mensaje }}</p>
    }

    <section class="panel" [attr.aria-busy]="cargando()">
      @if (cargando() && !resultado()) {
        <p>Cargando...</p>
      } @else if (resultado(); as r) {
        <app-leyenda-alcance
          [alcance]="r.alcance"
          [fechaReferencia]="r.fechaReferencia"
          [estados]="estados()"
          [puedeConfigurarEstados]="puedeConfigurarEstados()"
        />
        <app-indicadores
          [indicadores]="r.totales"
          [alcance]="r.alcance"
          [fechaReferencia]="r.fechaReferencia"
          [clicable]="false"
        />
        <app-tabla-proyectos-seguimiento
          [filas]="r.proyectos.items"
          [ordenarPor]="consulta().ordenarPor"
          [direccion]="consulta().direccion"
          [nivelIds]="consulta().nivelIds"
          [tipoIds]="consulta().tipoIds"
          [agrupaciones]="agrupaciones()"
          (ordenar)="ordenar($event)"
          (cargarAgrupaciones)="cargarAgrupaciones($event)"
        />
        @if (r.proyectos.total > 0) {
          <app-paginador
            [pagina]="r.proyectos.pagina"
            [tamanioPagina]="r.proyectos.tamanioPagina"
            [total]="r.proyectos.total"
            (cambioPagina)="irAPagina($event)"
          />
        }
      }
    </section>
  `,
})
export class TableroGeneralPage {
  private readonly seguimientoService = inject(SeguimientoService);
  private readonly nivelesService = inject(NivelesIncidenciaService);
  private readonly tiposService = inject(TiposIncidenciaService);
  private readonly estadosService = inject(EstadosIncidenciaService);
  private readonly usuarioActual = inject(UsuarioActualService);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);
  private readonly destroyRef = inject(DestroyRef);

  // Query params vinculados por withComponentInputBinding (`nivelId`/`tipoId` repetibles).
  readonly texto = input<string>();
  readonly archivados = input<string>();
  readonly nivelId = input<string | readonly string[]>();
  readonly tipoId = input<string | readonly string[]>();
  readonly pagina = input<string>();
  readonly tamanioPagina = input<string>();
  readonly ordenarPor = input<string>();
  readonly direccion = input<string>();

  /** Consulta efectiva (valores inválidos de la URL → defaults del contrato). */
  readonly consulta = computed<ConsultaTableroProyectos>(() =>
    parsearConsultaTableroGeneral({
      texto: this.texto(),
      archivados: this.archivados(),
      nivelId: this.nivelId(),
      tipoId: this.tipoId(),
      pagina: this.pagina(),
      tamanioPagina: this.tamanioPagina(),
      ordenarPor: this.ordenarPor(),
      direccion: this.direccion(),
    }),
  );

  readonly puedeConfigurarEstados = computed(() =>
    this.usuarioActual.tienePermiso(PERMISOS.configuracionIncidencias),
  );
  readonly niveles = signal<readonly NivelIncidenciaDto[]>([]);
  readonly tipos = signal<readonly TipoIncidenciaDto[]>([]);
  readonly estados = signal<readonly EstadoIncidenciaDto[]>([]);
  readonly resultado = signal<TableroProyectosDto | null>(null);
  readonly cargando = signal(false);
  readonly error = signal<string | null>(null);
  /** Agrupaciones por proyecto expandido (se reinicia al cambiar la consulta). */
  readonly agrupaciones = signal<ReadonlyMap<number, CargaAgrupaciones>>(new Map());

  constructor() {
    this.cargarCatalogos();
    toObservable(this.consulta)
      .pipe(
        tap(() => this.antesDeCargar()),
        switchMap((consulta) =>
          this.seguimientoService.tableroProyectos(consulta).pipe(
            catchError((error: unknown) => {
              this.error.set(mensajeErrorAccion(error));
              return of(null);
            }),
          ),
        ),
        takeUntilDestroyed(),
      )
      .subscribe((resultado) => {
        this.cargando.set(false);
        if (resultado) {
          this.resultado.set(resultado);
        }
      });
  }

  /** Aplica texto / archivados volviendo a la página 1. */
  cambiarFiltros(cambio: CambioFiltrosTableroGeneral): void {
    this.navegar({ ...this.consulta(), ...cambio, pagina: 1 });
  }

  /** Aplica el alcance volviendo a la página 1. */
  cambiarAlcance(alcance: AlcanceConsulta): void {
    this.navegar({ ...this.consulta(), ...alcance, pagina: 1 });
  }

  /** Restablece los filtros y el alcance por defecto. */
  limpiarFiltros(): void {
    this.navegar(CONSULTA_TABLERO_PROYECTOS_POR_DEFECTO);
  }

  /** Ordena por la columna indicada (alterna la dirección si ya era la actual). */
  ordenar(columna: OrdenTableroProyectos): void {
    this.navegar({
      ...this.consulta(),
      ...siguienteOrdenTableroGeneral(this.consulta(), columna),
      pagina: 1,
    });
  }

  /** Cambia de página. */
  irAPagina(pagina: number): void {
    this.navegar({ ...this.consulta(), pagina });
  }

  /** Carga el seguimiento del proyecto con el mismo alcance (filas de agrupaciones, CA-06). */
  cargarAgrupaciones(proyectoId: number): void {
    const { nivelIds, tipoIds } = this.consulta();
    this.ponerAgrupaciones(proyectoId, { estado: 'cargando' });
    this.seguimientoService
      .seguimiento(proyectoId, { nivelIds, tipoIds })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (seguimiento) =>
          this.ponerAgrupaciones(proyectoId, { estado: 'ok', filas: seguimiento.agrupaciones }),
        error: (error: unknown) =>
          this.ponerAgrupaciones(proyectoId, {
            estado: 'error',
            mensaje: mensajeErrorAccion(error) ?? 'No se pudieron cargar las agrupaciones.',
          }),
      });
  }

  /** Marca la carga en curso y descarta las agrupaciones de la consulta anterior. */
  private antesDeCargar(): void {
    this.cargando.set(true);
    this.error.set(null);
    this.agrupaciones.set(new Map());
  }

  /** Actualiza el estado de carga de las agrupaciones de un proyecto. */
  private ponerAgrupaciones(proyectoId: number, carga: CargaAgrupaciones): void {
    this.agrupaciones.update((actual) => new Map(actual).set(proyectoId, carga));
  }

  /** Refleja la consulta en la URL (dispara la recarga vía inputs). */
  private navegar(consulta: ConsultaTableroProyectos): void {
    void this.router.navigate([], {
      relativeTo: this.route,
      queryParams: serializarConsultaTableroGeneral(consulta),
    });
  }

  /** Carga niveles y tipos (filtro de alcance) y estados (leyenda de categorías). */
  private cargarCatalogos(): void {
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
  }
}
