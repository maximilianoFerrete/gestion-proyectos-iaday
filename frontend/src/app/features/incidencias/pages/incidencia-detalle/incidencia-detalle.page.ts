import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  computed,
  effect,
  inject,
  input,
  signal,
  untracked,
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { RouterLink } from '@angular/router';
import { forkJoin } from 'rxjs';
import { PERMISOS } from '../../../../core/models/permisos.model';
import { NotificacionService } from '../../../../core/services/notificacion.service';
import { UsuarioActualService } from '../../../../core/services/usuario-actual.service';
import { NoEncontradoComponent } from '../../../../shared/components/no-encontrado/no-encontrado.component';
import {
  ErrorFormulario,
  SIN_ERRORES,
  esNoEncontrado,
  interpretarErrorFormulario,
  mensajeErrorAccion,
} from '../../../../shared/forms/errores-servidor';
import { parsearId } from '../../../../shared/utils/ids';
import { FlujoIncidenciaDto } from '../../../configuracion-incidencias/models/flujo-incidencia.model';
import { TipoIncidenciaDto } from '../../../configuracion-incidencias/models/tipo-incidencia.model';
import { FlujosIncidenciaService } from '../../../configuracion-incidencias/services/flujos-incidencia.service';
import { TiposIncidenciaService } from '../../../configuracion-incidencias/services/tipos-incidencia.service';
import { CambiarTipoDialogComponent } from '../../components/cambiar-tipo-dialog/cambiar-tipo-dialog.component';
import { IncidenciaDatosComponent } from '../../components/incidencia-datos/incidencia-datos.component';
import { IncidenciaHijasComponent } from '../../components/incidencia-hijas/incidencia-hijas.component';
import { CambiarTipoIncidenciaRequest, IncidenciaDto } from '../../models/incidencia.model';
import { IncidenciasListadoEstadoService } from '../../services/incidencias-listado-estado.service';
import { IncidenciasService } from '../../services/incidencias.service';
import { incidenciasDelError } from '../../utils/incidencias-afectadas';
import { MENSAJE_BANNER_ARCHIVADO } from '../../utils/proyecto-archivado';

/**
 * I3 – Detalle de incidencia (`/proyectos/:id/incidencias/:incidenciaId`) con padre, hijas y
 * acciones Editar, Crear hija (si hay tipos de hija permitidos) y Cambiar tipo (con permiso
 * `Incidencias.CambiarTipo`). Proyecto archivado: banner y sin acciones (RN-36).
 *
 * Creado: 2026-10-09
 * Modificado: 2026-10-09 - Creación inicial (RF-002)
 */
@Component({
  selector: 'app-incidencia-detalle-page',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    RouterLink,
    NoEncontradoComponent,
    IncidenciaDatosComponent,
    IncidenciaHijasComponent,
    CambiarTipoDialogComponent,
  ],
  template: `
    @if (noEncontrado()) {
      <app-no-encontrado [rutaVolver]="rutaListado()" [queryParamsVolver]="queryParamsListado()" />
    } @else if (errorCarga(); as mensaje) {
      <p class="alerta alerta-error" role="alert">{{ mensaje }}</p>
    } @else if (incidencia(); as i) {
      <header class="encabezado-pagina">
        <div>
          <p class="subtitulo">{{ i.proyecto.codigo }} – {{ i.proyecto.nombre }}</p>
          <h1>{{ i.codigo }} – {{ i.titulo }}</h1>
        </div>
        <div class="acciones">
          <a class="btn" [routerLink]="rutaListado()" [queryParams]="queryParamsListado()"
            >Volver</a
          >
          @if (!i.proyecto.archivado) {
            <a
              class="btn"
              [routerLink]="['/proyectos', i.proyecto.id, 'incidencias', i.id, 'editar']"
            >
              Editar
            </a>
            @if (i.tiposHijaPermitidos.length > 0) {
              <a
                class="btn"
                [routerLink]="['/proyectos', i.proyecto.id, 'incidencias', 'nueva']"
                [queryParams]="{ padreId: i.id }"
              >
                Crear hija
              </a>
            }
            @if (puedeCambiarTipo()) {
              <button type="button" class="btn" (click)="abrirCambioTipo()">Cambiar tipo</button>
            }
          }
        </div>
      </header>
      @if (i.proyecto.archivado) {
        <div class="banner-archivado" role="status">{{ mensajeArchivado }}</div>
      }
      <app-incidencia-datos [incidencia]="i" />
      <app-incidencia-hijas [proyectoId]="i.proyecto.id" [hijas]="i.hijas" />
      <app-cambiar-tipo-dialog
        [abierto]="dialogoAbierto()"
        [incidencia]="i"
        [tipos]="tipos()"
        [flujos]="flujos()"
        [cargando]="cargandoCatalogos()"
        [enviando]="enviando()"
        [errorServidor]="errorCambioTipo()"
        [incidenciasAfectadas]="incidenciasAfectadas()"
        (confirmar)="cambiarTipo(i, $event)"
        (cancelar)="cerrarCambioTipo()"
      />
    } @else {
      <p>Cargando...</p>
    }
  `,
})
export class IncidenciaDetallePage {
  private readonly incidenciasService = inject(IncidenciasService);
  private readonly tiposService = inject(TiposIncidenciaService);
  private readonly flujosService = inject(FlujosIncidenciaService);
  private readonly notificacion = inject(NotificacionService);
  private readonly usuarioActual = inject(UsuarioActualService);
  private readonly listadoEstado = inject(IncidenciasListadoEstadoService);
  private readonly destroyRef = inject(DestroyRef);

  /** Parámetro de ruta `:id` del proyecto. */
  readonly id = input.required<string>();
  /** Parámetro de ruta `:incidenciaId`. */
  readonly incidenciaId = input.required<string>();

  protected readonly mensajeArchivado = MENSAJE_BANNER_ARCHIVADO;

  readonly proyectoId = computed(() => parsearId(this.id()));
  readonly idIncidencia = computed(() => parsearId(this.incidenciaId()));
  readonly rutaListado = computed(() => {
    const proyectoId = this.proyectoId();
    return proyectoId === null ? '/proyectos' : `/proyectos/${proyectoId}/incidencias`;
  });
  readonly queryParamsListado = computed(() => this.listadoEstado.queryParams(this.proyectoId()));
  readonly puedeCambiarTipo = computed(() =>
    this.usuarioActual.tienePermiso(PERMISOS.incidenciasCambiarTipo),
  );

  readonly incidencia = signal<IncidenciaDto | null>(null);
  readonly noEncontrado = signal(false);
  readonly errorCarga = signal<string | null>(null);
  // Diálogo "Cambiar tipo".
  readonly dialogoAbierto = signal(false);
  readonly tipos = signal<readonly TipoIncidenciaDto[]>([]);
  readonly flujos = signal<readonly FlujoIncidenciaDto[]>([]);
  readonly cargandoCatalogos = signal(false);
  readonly enviando = signal(false);
  readonly errorCambioTipo = signal<ErrorFormulario>(SIN_ERRORES);
  readonly incidenciasAfectadas = signal<readonly string[]>([]);

  constructor() {
    // Navegar a otra incidencia (padre/hija) reutiliza el componente: recarga al cambiar la ruta.
    effect(() => {
      const proyectoId = this.proyectoId();
      const incidenciaId = this.idIncidencia();
      untracked(() => this.reiniciarYCargar(proyectoId, incidenciaId));
    });
  }

  /** Abre el diálogo y carga tipos activos y flujos. */
  abrirCambioTipo(): void {
    this.errorCambioTipo.set(SIN_ERRORES);
    this.incidenciasAfectadas.set([]);
    this.dialogoAbierto.set(true);
    this.cargandoCatalogos.set(true);
    forkJoin({
      tipos: this.tiposService.listar({ soloActivos: true }),
      flujos: this.flujosService.listar(),
    })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: ({ tipos, flujos }) => {
          this.tipos.set(tipos);
          this.flujos.set(flujos);
          this.cargandoCatalogos.set(false);
        },
        error: (error: unknown) => {
          this.cargandoCatalogos.set(false);
          this.errorCambioTipo.set(interpretarErrorFormulario(error));
        },
      });
  }

  /** Cierra el diálogo sin cambios. */
  cerrarCambioTipo(): void {
    this.dialogoAbierto.set(false);
    this.enviando.set(false);
  }

  /** POST `cambiar-tipo`; éxito → toast "Tipo actualizado." y recarga. */
  cambiarTipo(incidencia: IncidenciaDto, request: CambiarTipoIncidenciaRequest): void {
    this.enviando.set(true);
    this.errorCambioTipo.set(SIN_ERRORES);
    this.incidenciasAfectadas.set([]);
    this.incidenciasService
      .cambiarTipo(incidencia.proyecto.id, incidencia.id, request)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (actualizada) => {
          this.cerrarCambioTipo();
          this.notificacion.exito('Tipo actualizado.');
          this.incidencia.set(actualizada);
          this.cargar(incidencia.proyecto.id, incidencia.id);
        },
        error: (error: unknown) => {
          this.enviando.set(false);
          this.incidenciasAfectadas.set(incidenciasDelError(error));
          this.errorCambioTipo.set(
            interpretarErrorFormulario(error, {
              TIPO_INVALIDO: 'tipoId',
              RELACION_NO_PERMITIDA: 'tipoId',
              ESTADO_INCIDENCIA_INVALIDO: 'estadoId',
            }),
          );
        },
      });
  }

  /** Limpia el estado de la incidencia anterior y carga la indicada por la ruta. */
  private reiniciarYCargar(proyectoId: number | null, incidenciaId: number | null): void {
    this.dialogoAbierto.set(false);
    this.incidencia.set(null);
    this.noEncontrado.set(false);
    this.errorCarga.set(null);
    if (proyectoId === null || incidenciaId === null) {
      this.noEncontrado.set(true);
      return;
    }
    this.cargar(proyectoId, incidenciaId);
  }

  /** Carga el detalle de la incidencia. */
  private cargar(proyectoId: number, incidenciaId: number): void {
    this.incidenciasService
      .obtener(proyectoId, incidenciaId)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (incidencia) => this.incidencia.set(incidencia),
        error: (error: unknown) => {
          if (esNoEncontrado(error)) {
            this.noEncontrado.set(true);
          } else {
            this.errorCarga.set(mensajeErrorAccion(error) ?? 'No se pudo cargar la incidencia.');
          }
        },
      });
  }
}
