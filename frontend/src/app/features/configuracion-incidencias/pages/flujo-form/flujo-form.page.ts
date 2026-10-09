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
import { FlujoFormComponent } from '../../components/flujo-form/flujo-form.component';
import { EstadoIncidenciaDto } from '../../models/estado-incidencia.model';
import { FlujoIncidenciaDto, FlujoIncidenciaRequest } from '../../models/flujo-incidencia.model';
import { EstadosIncidenciaService } from '../../services/estados-incidencia.service';
import { FlujosIncidenciaService } from '../../services/flujos-incidencia.service';

/** Ruta del listado de flujos (C4). */
const RUTA_FLUJOS = '/configuracion/incidencias/flujos';

/** Datos para mostrar el formulario. */
interface DatosFlujoForm {
  readonly estados: EstadoIncidenciaDto[];
  readonly flujo: FlujoIncidenciaDto | null;
}

/**
 * C4 – Alta (`/configuracion/incidencias/flujos/nuevo`) y edición
 * (`/configuracion/incidencias/flujos/:id/editar`) de flujo. `FLUJO_ESTADO_EN_USO` arriba del form.
 *
 * Creado: 2026-10-09
 * Modificado: 2026-10-09 - Creación inicial (RF-002)
 */
@Component({
  selector: 'app-flujo-form-page',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [FlujoFormComponent, NoEncontradoComponent],
  template: `
    <header class="encabezado-pagina">
      <h1>{{ esEdicion() ? 'Editar flujo de trabajo' : 'Nuevo flujo de trabajo' }}</h1>
    </header>
    @if (noEncontrado()) {
      <app-no-encontrado rutaVolver="/configuracion/incidencias/flujos" />
    } @else if (errorCarga(); as mensaje) {
      <p class="alerta alerta-error" role="alert">{{ mensaje }}</p>
    } @else if (datos(); as d) {
      <section class="panel">
        <app-flujo-form
          [flujo]="d.flujo"
          [estados]="d.estados"
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
export class FlujoFormPage implements OnInit {
  private readonly flujosService = inject(FlujosIncidenciaService);
  private readonly estadosService = inject(EstadosIncidenciaService);
  private readonly notificacion = inject(NotificacionService);
  private readonly router = inject(Router);
  private readonly destroyRef = inject(DestroyRef);

  /** Parámetro de ruta `:id` (ausente en alta). */
  readonly id = input<string>();

  readonly flujoId = computed(() => parsearId(this.id()));
  readonly esEdicion = computed(() => this.id() !== undefined);
  readonly datos = signal<DatosFlujoForm | null>(null);
  readonly noEncontrado = signal(false);
  readonly errorCarga = signal<string | null>(null);
  readonly enviando = signal(false);
  readonly errorServidor = signal<ErrorFormulario>(SIN_ERRORES);

  ngOnInit(): void {
    const id = this.flujoId();
    if (this.esEdicion() && id === null) {
      this.noEncontrado.set(true);
      return;
    }
    const flujo$: Observable<FlujoIncidenciaDto | null> =
      id === null ? of(null) : this.flujosService.obtener(id);
    forkJoin({ estados: this.estadosService.listar(), flujo: flujo$ })
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

  /** POST o PUT (reemplaza la composición); éxito → toast y vuelta al listado. */
  guardar(request: FlujoIncidenciaRequest): void {
    const id = this.flujoId();
    this.enviando.set(true);
    this.errorServidor.set(SIN_ERRORES);
    const envio$: Observable<FlujoIncidenciaDto> =
      id === null ? this.flujosService.crear(request) : this.flujosService.editar(id, request);
    envio$.pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: () => {
        this.notificacion.exito('Flujo guardado.');
        this.volver();
      },
      error: (error: unknown) => {
        this.enviando.set(false);
        this.errorServidor.set(
          interpretarErrorFormulario(error, {
            FLUJO_NOMBRE_DUPLICADO: 'nombre',
            ESTADO_INCIDENCIA_INVALIDO: 'estados',
          }),
        );
      },
    });
  }

  /** Vuelve al listado. */
  volver(): void {
    void this.router.navigate([RUTA_FLUJOS]);
  }
}
