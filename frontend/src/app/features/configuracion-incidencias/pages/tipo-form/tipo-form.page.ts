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
import { Observable, forkJoin, of } from 'rxjs';
import { NotificacionService } from '../../../../core/services/notificacion.service';
import { NoEncontradoComponent } from '../../../../shared/components/no-encontrado/no-encontrado.component';
import {
  ErrorFormulario,
  SIN_ERRORES,
  esNoEncontrado,
  interpretarErrorFormulario,
  mensajeErrorAccion,
} from '../../../../shared/forms/errores-servidor';
import { parsearId } from '../../../../shared/utils/ids';
import { TipoFormComponent } from '../../components/tipo-form/tipo-form.component';
import { FlujoIncidenciaDto } from '../../models/flujo-incidencia.model';
import { NivelIncidenciaDto } from '../../models/nivel-incidencia.model';
import { CrearTipoIncidenciaRequest, TipoIncidenciaDto } from '../../models/tipo-incidencia.model';
import { FlujosIncidenciaService } from '../../services/flujos-incidencia.service';
import { NivelesIncidenciaService } from '../../services/niveles-incidencia.service';
import { TiposIncidenciaService } from '../../services/tipos-incidencia.service';

/** Ruta del listado de niveles y tipos (C1). */
const RUTA_TIPOS = '/configuracion/incidencias/tipos';

/** Datos para mostrar el formulario. */
interface DatosTipoForm {
  readonly niveles: NivelIncidenciaDto[];
  readonly flujos: FlujoIncidenciaDto[];
  readonly tipo: TipoIncidenciaDto | null;
}

/**
 * C1 – Alta (`/configuracion/incidencias/tipos/nuevo?nivelId=`) y edición
 * (`/configuracion/incidencias/tipos/:id/editar`) de tipo de incidencia.
 * `TIPO_FLUJO_INCOMPATIBLE` se muestra arriba del formulario.
 *
 * Creado: 2026-10-09
 * Modificado: 2026-10-09 - Creación inicial (RF-002)
 */
@Component({
  selector: 'app-tipo-form-page',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [TipoFormComponent, NoEncontradoComponent],
  template: `
    <header class="encabezado-pagina">
      <h1>{{ esEdicion() ? 'Editar tipo de incidencia' : 'Nuevo tipo de incidencia' }}</h1>
    </header>
    @if (noEncontrado()) {
      <app-no-encontrado rutaVolver="/configuracion/incidencias/tipos" />
    } @else if (errorCarga(); as mensaje) {
      <p class="alerta alerta-error" role="alert">{{ mensaje }}</p>
    } @else if (datos(); as d) {
      <section class="panel">
        <app-tipo-form
          [tipo]="d.tipo"
          [nivelIdInicial]="idNivel()"
          [niveles]="d.niveles"
          [flujos]="d.flujos"
          [enviando]="enviando()"
          [errorServidor]="errorServidor()"
          (guardar)="guardar($event)"
          (cancelar)="volver()"
        />
      </section>
    } @else {
      <p>Cargando...</p>
    }
  `,
})
export class TipoFormPage implements OnInit {
  private readonly tiposService = inject(TiposIncidenciaService);
  private readonly nivelesService = inject(NivelesIncidenciaService);
  private readonly flujosService = inject(FlujosIncidenciaService);
  private readonly notificacion = inject(NotificacionService);
  private readonly router = inject(Router);
  private readonly destroyRef = inject(DestroyRef);

  /** Parámetro de ruta `:id` (ausente en alta). */
  readonly id = input<string>();
  /** Query param `nivelId` (alta desde "Nuevo tipo" de un nivel). */
  readonly nivelId = input<string>();

  readonly tipoId = computed(() => parsearId(this.id()));
  readonly idNivel = computed(() => parsearId(this.nivelId()));
  readonly esEdicion = computed(() => this.id() !== undefined);
  readonly datos = signal<DatosTipoForm | null>(null);
  readonly noEncontrado = signal(false);
  readonly errorCarga = signal<string | null>(null);
  readonly enviando = signal(false);
  readonly errorServidor = signal<ErrorFormulario>(SIN_ERRORES);

  ngOnInit(): void {
    const id = this.tipoId();
    if (this.esEdicion() && id === null) {
      this.noEncontrado.set(true);
      return;
    }
    const tipo$: Observable<TipoIncidenciaDto | null> =
      id === null ? of(null) : this.tiposService.obtener(id);
    forkJoin({
      niveles: this.nivelesService.listar(),
      flujos: this.flujosService.listar(),
      tipo: tipo$,
    })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (datos) => this.datos.set(datos),
        error: (error: unknown) => {
          if (esNoEncontrado(error)) {
            this.noEncontrado.set(true);
          } else {
            this.errorCarga.set(mensajeErrorAccion(error) ?? 'No se pudo cargar la información.');
          }
        },
      });
  }

  /** POST o PUT (sin `nivelId`, RN-06); éxito → toast y vuelta a C1. */
  guardar(request: CrearTipoIncidenciaRequest): void {
    const id = this.tipoId();
    this.enviando.set(true);
    this.errorServidor.set(SIN_ERRORES);
    const envio$: Observable<TipoIncidenciaDto> =
      id === null
        ? this.tiposService.crear(request)
        : this.tiposService.editar(id, {
            nombre: request.nombre,
            descripcion: request.descripcion,
            flujoId: request.flujoId,
          });
    envio$.pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: () => {
        this.notificacion.exito('Tipo guardado.');
        this.volver();
      },
      error: (error: unknown) => {
        this.enviando.set(false);
        this.errorServidor.set(
          interpretarErrorFormulario(error, {
            TIPO_NOMBRE_DUPLICADO: 'nombre',
            NIVEL_INVALIDO: 'nivelId',
            FLUJO_INVALIDO: 'flujoId',
          }),
        );
      },
    });
  }

  /** Vuelve a C1. */
  volver(): void {
    void this.router.navigate([RUTA_TIPOS]);
  }
}
