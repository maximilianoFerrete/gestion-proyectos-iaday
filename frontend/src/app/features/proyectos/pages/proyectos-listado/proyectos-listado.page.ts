import { ChangeDetectionStrategy, Component, computed, inject, input, signal } from '@angular/core';
import { takeUntilDestroyed, toObservable } from '@angular/core/rxjs-interop';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { catchError, of, switchMap, tap } from 'rxjs';
import { Paginado } from '../../../../core/models/paginado.model';
import { PaginadorComponent } from '../../../../shared/components/paginador/paginador.component';
import { mensajeErrorAccion } from '../../../../shared/forms/errores-servidor';
import { EstadoProyectoDto } from '../../../estados-proyecto/models/estado-proyecto.model';
import { EstadosProyectoService } from '../../../estados-proyecto/services/estados-proyecto.service';
import { PersonaDto } from '../../../personas/models/persona.model';
import { PersonasService } from '../../../personas/services/personas.service';
import {
  CambioFiltrosProyectos,
  ProyectosFiltrosComponent,
} from '../../components/proyectos-filtros/proyectos-filtros.component';
import { ProyectosTablaComponent } from '../../components/proyectos-tabla/proyectos-tabla.component';
import {
  FILTROS_PROYECTOS_POR_DEFECTO,
  FiltrosProyectos,
  OrdenProyectos,
  ProyectoResumenDto,
} from '../../models/proyecto.model';
import { ProyectosListadoEstadoService } from '../../services/proyectos-listado-estado.service';
import { ProyectosService } from '../../services/proyectos.service';
import { parsearFiltros, serializarFiltros, siguienteOrden } from '../../utils/filtros-proyectos';

/**
 * P1 – Listado de proyectos. Los filtros, página y orden viven en los query params de la URL
 * (vinculados a inputs) y cada cambio vuelve a consultar la API.
 *
 * Creado: 2026-10-09
 * Modificado: 2026-10-09 - Creación inicial (RF-001)
 */
@Component({
  selector: 'app-proyectos-listado-page',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterLink, ProyectosFiltrosComponent, ProyectosTablaComponent, PaginadorComponent],
  template: `
    <header class="encabezado-pagina">
      <h1>Proyectos</h1>
      <a class="btn btn-primario" routerLink="/proyectos/nuevo">Nuevo proyecto</a>
    </header>

    <app-proyectos-filtros
      [filtros]="filtros()"
      [estados]="estados()"
      [personas]="personas()"
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
        <app-proyectos-tabla
          [proyectos]="pagina.items"
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
  `,
})
export class ProyectosListadoPage {
  private readonly proyectosService = inject(ProyectosService);
  private readonly estadosService = inject(EstadosProyectoService);
  private readonly personasService = inject(PersonasService);
  private readonly listadoEstado = inject(ProyectosListadoEstadoService);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);

  // Query params vinculados por withComponentInputBinding.
  readonly texto = input<string>();
  readonly estadoId = input<string>();
  readonly responsableId = input<string>();
  readonly archivados = input<string>();
  readonly pagina = input<string>();
  readonly tamanioPagina = input<string>();
  readonly ordenarPor = input<string>();
  readonly direccion = input<string>();

  /** Filtros efectivos (valores inválidos de la URL → defaults del contrato). */
  readonly filtros = computed<FiltrosProyectos>(() =>
    parsearFiltros({
      texto: this.texto(),
      estadoId: this.estadoId(),
      responsableId: this.responsableId(),
      archivados: this.archivados(),
      pagina: this.pagina(),
      tamanioPagina: this.tamanioPagina(),
      ordenarPor: this.ordenarPor(),
      direccion: this.direccion(),
    }),
  );

  readonly estados = signal<readonly EstadoProyectoDto[]>([]);
  readonly personas = signal<readonly PersonaDto[]>([]);
  readonly resultado = signal<Paginado<ProyectoResumenDto> | null>(null);
  readonly cargando = signal(false);
  readonly error = signal<string | null>(null);

  constructor() {
    this.cargarCatalogos();
    toObservable(this.filtros)
      .pipe(
        tap((filtros) => this.antesDeCargar(filtros)),
        switchMap((filtros) =>
          this.proyectosService.listar(filtros).pipe(
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

  /** Aplica filtros nuevos volviendo a la página 1. */
  cambiarFiltros(cambio: CambioFiltrosProyectos): void {
    this.navegar({ ...this.filtros(), ...cambio, pagina: 1 });
  }

  /** Restablece los filtros por defecto. */
  limpiarFiltros(): void {
    this.navegar(FILTROS_PROYECTOS_POR_DEFECTO);
  }

  /** Ordena por la columna indicada (alterna la dirección si ya era la actual). */
  ordenar(columna: OrdenProyectos): void {
    this.navegar({ ...this.filtros(), ...siguienteOrden(this.filtros(), columna), pagina: 1 });
  }

  /** Cambia de página. */
  irAPagina(pagina: number): void {
    this.navegar({ ...this.filtros(), pagina });
  }

  /** Navega al detalle del proyecto. */
  verDetalle(id: number): void {
    void this.router.navigate(['/proyectos', id]);
  }

  /** Marca la carga en curso y recuerda los query params para "Volver". */
  private antesDeCargar(filtros: FiltrosProyectos): void {
    this.cargando.set(true);
    this.error.set(null);
    this.listadoEstado.guardar(serializarFiltros(filtros));
  }

  /** Refleja los filtros en la URL (dispara la recarga vía inputs). */
  private navegar(filtros: FiltrosProyectos): void {
    void this.router.navigate([], {
      relativeTo: this.route,
      queryParams: serializarFiltros(filtros),
    });
  }

  /** Carga estados (todos) y personas (incluidas inactivas) para los selects de filtro. */
  private cargarCatalogos(): void {
    this.estadosService.listar().subscribe({
      next: (estados) => this.estados.set(estados),
      error: () => this.estados.set([]),
    });
    this.personasService.listar({ incluirInactivas: true }).subscribe({
      next: (personas) => this.personas.set(personas),
      error: () => this.personas.set([]),
    });
  }
}
