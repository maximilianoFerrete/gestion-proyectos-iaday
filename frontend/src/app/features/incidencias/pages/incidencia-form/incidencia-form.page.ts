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
import { Router } from '@angular/router';
import { Observable, Subject, catchError, forkJoin, map, of, switchMap, tap } from 'rxjs';
import { NotificacionService } from '../../../../core/services/notificacion.service';
import { NoEncontradoComponent } from '../../../../shared/components/no-encontrado/no-encontrado.component';
import {
  CamposPorCodigo,
  ErrorFormulario,
  SIN_ERRORES,
  esNoEncontrado,
  interpretarErrorFormulario,
  mensajeErrorAccion,
} from '../../../../shared/forms/errores-servidor';
import { parsearId } from '../../../../shared/utils/ids';
import { FlujoIncidenciaDto } from '../../../configuracion-incidencias/models/flujo-incidencia.model';
import { PrioridadIncidenciaDto } from '../../../configuracion-incidencias/models/prioridad-incidencia.model';
import { RelacionTipoIncidenciaDto } from '../../../configuracion-incidencias/models/relacion-tipo-incidencia.model';
import { TipoIncidenciaDto } from '../../../configuracion-incidencias/models/tipo-incidencia.model';
import { FlujosIncidenciaService } from '../../../configuracion-incidencias/services/flujos-incidencia.service';
import { PrioridadesIncidenciaService } from '../../../configuracion-incidencias/services/prioridades-incidencia.service';
import { RelacionesTiposIncidenciaService } from '../../../configuracion-incidencias/services/relaciones-tipos-incidencia.service';
import { TiposIncidenciaService } from '../../../configuracion-incidencias/services/tipos-incidencia.service';
import { PersonaDto } from '../../../personas/models/persona.model';
import { PersonasService } from '../../../personas/services/personas.service';
import { AgrupacionDto } from '../../../proyectos/models/agrupacion.model';
import { ProyectoDto } from '../../../proyectos/models/proyecto.model';
import { AgrupacionesService } from '../../../proyectos/services/agrupaciones.service';
import { ProyectosService } from '../../../proyectos/services/proyectos.service';
import {
  BusquedaPadre,
  IncidenciaFormComponent,
  ModoIncidenciaForm,
} from '../../components/incidencia-form/incidencia-form.component';
import {
  IncidenciaDto,
  IncidenciaResumenDto,
  LIMITES_INCIDENCIA,
} from '../../models/incidencia.model';
import { IncidenciasService } from '../../services/incidencias.service';
import {
  ValoresIncidenciaForm,
  aCrearIncidenciaRequest,
  aEditarIncidenciaRequest,
} from '../../utils/incidencia-request';
import { incidenciasDelError } from '../../utils/incidencias-afectadas';
import { PadreSeleccionado, padreDesdeIncidencia } from '../../utils/padre-incidencia';
import { MENSAJE_PROYECTO_ARCHIVADO_INCIDENCIAS } from '../../utils/proyecto-archivado';

/** Errores de negocio que se muestran junto a un campo del formulario (spec "Pantallas"). */
const CAMPOS_POR_CODIGO: CamposPorCodigo = {
  TIPO_INVALIDO: 'tipoId',
  RELACION_NO_PERMITIDA: 'tipoId',
  PADRE_INVALIDO: 'padreId',
  RELACION_CIRCULAR: 'padreId',
  AGRUPACION_INVALIDA: 'agrupacionId',
  AGRUPACION_HEREDADA: 'agrupacionId',
  VENCIMIENTO_POSTERIOR_AL_PADRE: 'fechaVencimiento',
  VENCIMIENTO_ANTERIOR_A_HIJAS: 'fechaVencimiento',
  ESTADO_INCIDENCIA_INVALIDO: 'estadoId',
  PRIORIDAD_INVALIDA: 'prioridadId',
  RESPONSABLE_INVALIDO: 'responsableId',
};

/** En edición el tipo es de solo lectura: `RELACION_NO_PERMITIDA` surge del padre elegido. */
const CAMPOS_POR_CODIGO_EDICION: CamposPorCodigo = {
  ...CAMPOS_POR_CODIGO,
  RELACION_NO_PERMITIDA: 'padreId',
};

/** Datos necesarios para mostrar el formulario. */
interface DatosFormularioIncidencia {
  readonly proyecto: ProyectoDto;
  readonly incidencia: IncidenciaDto | null;
  readonly padreInicial: PadreSeleccionado | null;
  readonly tipos: TipoIncidenciaDto[];
  readonly relaciones: RelacionTipoIncidenciaDto[];
  readonly flujos: FlujoIncidenciaDto[];
  readonly prioridades: PrioridadIncidenciaDto[];
  readonly personas: PersonaDto[];
  readonly agrupaciones: AgrupacionDto[];
}

/**
 * I4 / I5 – Alta (`/proyectos/:id/incidencias/nueva[?padreId=]`) y edición
 * (`/proyectos/:id/incidencias/:incidenciaId/editar`) de incidencia. Proyecto archivado:
 * redirige con toast (RN-36). Resuelve la búsqueda de padre que pide el formulario.
 *
 * Creado: 2026-10-09
 * Modificado: 2026-10-09 - Creación inicial (RF-002)
 */
@Component({
  selector: 'app-incidencia-form-page',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [IncidenciaFormComponent, NoEncontradoComponent],
  template: `
    @if (noEncontrado()) {
      <app-no-encontrado [rutaVolver]="rutaListado()" />
    } @else if (errorCarga(); as mensaje) {
      <p class="alerta alerta-error" role="alert">{{ mensaje }}</p>
    } @else if (datos(); as d) {
      <header class="encabezado-pagina">
        <div>
          <p class="subtitulo">{{ d.proyecto.codigo }} – {{ d.proyecto.nombre }}</p>
          <h1>{{ modo() === 'alta' ? 'Nueva incidencia' : 'Editar incidencia' }}</h1>
        </div>
      </header>
      <section class="panel">
        <app-incidencia-form
          [modo]="modo()"
          [proyectoId]="d.proyecto.id"
          [incidencia]="d.incidencia"
          [padreInicial]="d.padreInicial"
          [tipos]="d.tipos"
          [relaciones]="d.relaciones"
          [flujos]="d.flujos"
          [prioridades]="d.prioridades"
          [personas]="d.personas"
          [agrupaciones]="d.agrupaciones"
          [resultadosPadre]="resultadosPadre()"
          [buscandoPadre]="buscandoPadre()"
          [enviando]="enviando()"
          [errorServidor]="errorServidor()"
          [incidenciasAfectadas]="incidenciasAfectadas()"
          (buscarPadre)="busquedas.next($event)"
          (guardar)="guardar(d.proyecto.id, $event)"
          (cancelar)="cancelar()"
        />
      </section>
    } @else {
      <p>Cargando...</p>
    }
  `,
})
export class IncidenciaFormPage implements OnInit {
  private readonly incidenciasService = inject(IncidenciasService);
  private readonly proyectosService = inject(ProyectosService);
  private readonly agrupacionesService = inject(AgrupacionesService);
  private readonly tiposService = inject(TiposIncidenciaService);
  private readonly relacionesService = inject(RelacionesTiposIncidenciaService);
  private readonly flujosService = inject(FlujosIncidenciaService);
  private readonly prioridadesService = inject(PrioridadesIncidenciaService);
  private readonly personasService = inject(PersonasService);
  private readonly notificacion = inject(NotificacionService);
  private readonly router = inject(Router);
  private readonly destroyRef = inject(DestroyRef);

  /** Parámetro de ruta `:id` del proyecto. */
  readonly id = input.required<string>();
  /** Parámetro de ruta `:incidenciaId` (solo en edición). */
  readonly incidenciaId = input<string>();
  /** Query param `padreId` (alta desde "Crear hija"). */
  readonly padreId = input<string>();

  readonly proyectoId = computed(() => parsearId(this.id()));
  readonly idIncidencia = computed(() => parsearId(this.incidenciaId()));
  readonly idPadre = computed(() => parsearId(this.padreId()));
  readonly modo = computed<ModoIncidenciaForm>(() =>
    this.incidenciaId() === undefined ? 'alta' : 'edicion',
  );
  readonly rutaListado = computed(() => {
    const proyectoId = this.proyectoId();
    return proyectoId === null ? '/proyectos' : `/proyectos/${proyectoId}/incidencias`;
  });

  readonly datos = signal<DatosFormularioIncidencia | null>(null);
  readonly noEncontrado = signal(false);
  readonly errorCarga = signal<string | null>(null);
  readonly enviando = signal(false);
  readonly errorServidor = signal<ErrorFormulario>(SIN_ERRORES);
  readonly incidenciasAfectadas = signal<readonly string[]>([]);
  readonly resultadosPadre = signal<readonly IncidenciaResumenDto[]>([]);
  readonly buscandoPadre = signal(false);
  /** Búsquedas de padre pedidas por el formulario (se cancela la anterior). */
  readonly busquedas = new Subject<BusquedaPadre>();

  constructor() {
    this.busquedas
      .pipe(
        tap((busqueda) => this.buscandoPadre.set(busqueda.texto.length > 0)),
        switchMap((busqueda) => this.buscarCandidatos(busqueda)),
        takeUntilDestroyed(),
      )
      .subscribe((resultados) => {
        this.buscandoPadre.set(false);
        this.resultadosPadre.set(resultados);
      });
  }

  ngOnInit(): void {
    const proyectoId = this.proyectoId();
    const incidenciaId = this.idIncidencia();
    if (proyectoId === null || (this.modo() === 'edicion' && incidenciaId === null)) {
      this.noEncontrado.set(true);
      return;
    }
    this.cargar(proyectoId, incidenciaId);
  }

  /** Envía el alta o la edición según el modo. */
  guardar(proyectoId: number, valores: ValoresIncidenciaForm): void {
    const incidenciaId = this.idIncidencia();
    this.enviando.set(true);
    this.errorServidor.set(SIN_ERRORES);
    this.incidenciasAfectadas.set([]);
    if (this.modo() === 'alta' || incidenciaId === null) {
      this.enviarAlta(proyectoId, valores);
    } else {
      this.enviarEdicion(proyectoId, incidenciaId, valores);
    }
  }

  /** Alta: al padre si vino `padreId`, si no al listado. Edición: al detalle. */
  cancelar(): void {
    const proyectoId = this.proyectoId();
    const destino = this.idIncidencia() ?? this.idPadre();
    void this.router.navigate(
      destino === null
        ? ['/proyectos', proyectoId, 'incidencias']
        : ['/proyectos', proyectoId, 'incidencias', destino],
    );
  }

  /** Busca candidatos a padre por texto (y tipos admitidos); texto vacío → sin resultados. */
  private buscarCandidatos(busqueda: BusquedaPadre): Observable<IncidenciaResumenDto[]> {
    const proyectoId = this.proyectoId();
    if (proyectoId === null || busqueda.texto.length === 0) {
      return of([]);
    }
    return this.incidenciasService
      .listar(proyectoId, {
        texto: busqueda.texto,
        tipoIds: busqueda.tipoIds,
        tamanioPagina: LIMITES_INCIDENCIA.resultadosBusquedaPadre,
      })
      .pipe(
        map((pagina) => [...pagina.items]),
        catchError(() => of([])),
      );
  }

  /** Carga proyecto, catálogos, la incidencia (edición) y el padre preseleccionado (alta). */
  private cargar(proyectoId: number, incidenciaId: number | null): void {
    const padreId = this.idPadre();
    const incidencia$: Observable<IncidenciaDto | null> =
      incidenciaId === null ? of(null) : this.incidenciasService.obtener(proyectoId, incidenciaId);
    // Un padre inexistente en `?padreId=` no impide el alta: se abre sin padre.
    const padre$: Observable<PadreSeleccionado | null> =
      incidenciaId !== null || padreId === null
        ? of(null)
        : this.incidenciasService.obtener(proyectoId, padreId).pipe(
            map(padreDesdeIncidencia),
            catchError(() => of(null)),
          );
    forkJoin({
      proyecto: this.proyectosService.obtener(proyectoId),
      incidencia: incidencia$,
      padreInicial: padre$,
      tipos: this.tiposService.listar(),
      relaciones: this.relacionesService.listar(),
      flujos: this.flujosService.listar(),
      prioridades: this.prioridadesService.listar(),
      personas: this.personasService.listar(),
      agrupaciones: this.agrupacionesService.listar(proyectoId),
    })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (datos) => this.alCargar(datos),
        error: (error: unknown) => this.alFallarCarga(error),
      });
  }

  /** Muestra el formulario o redirige si el proyecto está archivado (RN-36). */
  private alCargar(datos: DatosFormularioIncidencia): void {
    if (datos.proyecto.archivado) {
      this.notificacion.info(MENSAJE_PROYECTO_ARCHIVADO_INCIDENCIAS);
      const incidenciaId = this.idIncidencia();
      void this.router.navigate(
        incidenciaId === null
          ? ['/proyectos', datos.proyecto.id, 'incidencias']
          : ['/proyectos', datos.proyecto.id, 'incidencias', incidenciaId],
      );
      return;
    }
    this.datos.set(datos);
  }

  /** 404 → "No se encontró el recurso"; otros errores → mensaje genérico. */
  private alFallarCarga(error: unknown): void {
    if (esNoEncontrado(error)) {
      this.noEncontrado.set(true);
      return;
    }
    this.errorCarga.set(mensajeErrorAccion(error) ?? 'No se pudo cargar la información.');
  }

  /** POST del alta; éxito → toast con el código y navegación al detalle. */
  private enviarAlta(proyectoId: number, valores: ValoresIncidenciaForm): void {
    const request = aCrearIncidenciaRequest(valores);
    if (request === null) {
      this.enviando.set(false);
      return;
    }
    this.incidenciasService
      .crear(proyectoId, request)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (incidencia) => {
          this.notificacion.exito(`Incidencia ${incidencia.codigo} creada.`);
          void this.router.navigate(['/proyectos', proyectoId, 'incidencias', incidencia.id]);
        },
        error: (error: unknown) => this.alFallarEnvio(error, CAMPOS_POR_CODIGO),
      });
  }

  /** PUT de la edición; éxito → toast y navegación al detalle. */
  private enviarEdicion(
    proyectoId: number,
    incidenciaId: number,
    valores: ValoresIncidenciaForm,
  ): void {
    const request = aEditarIncidenciaRequest(valores);
    if (request === null) {
      this.enviando.set(false);
      return;
    }
    this.incidenciasService
      .editar(proyectoId, incidenciaId, request)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.notificacion.exito('Incidencia actualizada.');
          void this.router.navigate(['/proyectos', proyectoId, 'incidencias', incidenciaId]);
        },
        error: (error: unknown) => this.alFallarEnvio(error, CAMPOS_POR_CODIGO_EDICION),
      });
  }

  /** Muestra los errores del envío en el formulario (incluida la lista `incidencias`). */
  private alFallarEnvio(error: unknown, camposPorCodigo: CamposPorCodigo): void {
    this.enviando.set(false);
    this.incidenciasAfectadas.set(incidenciasDelError(error));
    this.errorServidor.set(interpretarErrorFormulario(error, camposPorCodigo));
  }
}
