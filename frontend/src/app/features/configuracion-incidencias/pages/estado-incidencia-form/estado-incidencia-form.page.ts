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
import { Observable } from 'rxjs';
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
import { EstadoIncidenciaFormComponent } from '../../components/estado-incidencia-form/estado-incidencia-form.component';
import { EstadoIncidenciaDto, EstadoIncidenciaRequest } from '../../models/estado-incidencia.model';
import { EstadosIncidenciaService } from '../../services/estados-incidencia.service';
import { ordenSugeridoEstado } from '../../utils/reglas-configuracion';

/** Ruta del listado de estados (C3). */
const RUTA_ESTADOS = '/configuracion/incidencias/estados';

/**
 * C3 – Alta (`/configuracion/incidencias/estados/nuevo`, orden sugerido = mayor + 10) y edición
 * (`/configuracion/incidencias/estados/:id/editar`) de estado de incidencia, con categoría de
 * seguimiento (RF-005). 409 `ESTADO_CATEGORIA_INCOMPATIBLE` arriba del formulario.
 *
 * Creado: 2026-10-09
 * Modificado: 2026-10-10 - Formulario con categoría de seguimiento (RF-005)
 */
@Component({
  selector: 'app-estado-incidencia-form-page',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [EstadoIncidenciaFormComponent, NoEncontradoComponent],
  template: `
    <header class="encabezado-pagina">
      <h1>{{ esEdicion() ? 'Editar estado de incidencia' : 'Nuevo estado de incidencia' }}</h1>
    </header>
    @if (noEncontrado()) {
      <app-no-encontrado rutaVolver="/configuracion/incidencias/estados" />
    } @else if (errorCarga(); as mensaje) {
      <p class="alerta alerta-error" role="alert">{{ mensaje }}</p>
    } @else if (listo()) {
      <section class="panel">
        <app-estado-incidencia-form
          [estado]="estado()"
          [ordenSugerido]="orden()"
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
export class EstadoIncidenciaFormPage implements OnInit {
  private readonly estadosService = inject(EstadosIncidenciaService);
  private readonly notificacion = inject(NotificacionService);
  private readonly router = inject(Router);
  private readonly destroyRef = inject(DestroyRef);

  /** Parámetro de ruta `:id` (ausente en alta). */
  readonly id = input<string>();

  readonly estadoId = computed(() => parsearId(this.id()));
  readonly esEdicion = computed(() => this.id() !== undefined);
  readonly estado = signal<EstadoIncidenciaDto | null>(null);
  readonly orden = signal<number | null>(null);
  readonly listo = signal(false);
  readonly noEncontrado = signal(false);
  readonly errorCarga = signal<string | null>(null);
  readonly enviando = signal(false);
  readonly errorServidor = signal<ErrorFormulario>(SIN_ERRORES);

  ngOnInit(): void {
    const id = this.estadoId();
    if (this.esEdicion() && id === null) {
      this.noEncontrado.set(true);
      return;
    }
    if (id === null) {
      this.cargarOrdenSugerido();
    } else {
      this.cargarEstado(id);
    }
  }

  /** POST o PUT; éxito → toast y vuelta al listado. */
  guardar(request: EstadoIncidenciaRequest): void {
    const id = this.estadoId();
    this.enviando.set(true);
    this.errorServidor.set(SIN_ERRORES);
    const envio$: Observable<EstadoIncidenciaDto> =
      id === null ? this.estadosService.crear(request) : this.estadosService.editar(id, request);
    envio$.pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: () => {
        this.notificacion.exito('Estado guardado.');
        this.volver();
      },
      error: (error: unknown) => {
        this.enviando.set(false);
        this.errorServidor.set(
          interpretarErrorFormulario(error, { ESTADO_INCIDENCIA_NOMBRE_DUPLICADO: 'nombre' }),
        );
      },
    });
  }

  /** Vuelve al listado. */
  volver(): void {
    void this.router.navigate([RUTA_ESTADOS]);
  }

  /** Alta: orden sugerido a partir de los estados existentes. */
  private cargarOrdenSugerido(): void {
    this.estadosService
      .listar()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (estados) => {
          this.orden.set(ordenSugeridoEstado(estados));
          this.listo.set(true);
        },
        // Sin la lista igualmente se puede cargar el estado, sin sugerencia.
        error: () => this.listo.set(true),
      });
  }

  /** Edición: carga el estado. */
  private cargarEstado(id: number): void {
    this.estadosService
      .obtener(id)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (estado) => {
          this.estado.set(estado);
          this.listo.set(true);
        },
        error: (error: unknown) => {
          if (esNoEncontrado(error)) {
            this.noEncontrado.set(true);
          } else {
            this.errorCarga.set(mensajeErrorAccion(error) ?? 'No se pudo cargar el estado.');
          }
        },
      });
  }
}
