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
import { EstadoProyectoFormComponent } from '../../components/estado-proyecto-form/estado-proyecto-form.component';
import { EstadoProyectoDto, EstadoProyectoRequest } from '../../models/estado-proyecto.model';
import { EstadosProyectoService } from '../../services/estados-proyecto.service';
import { ordenSugerido } from '../../utils/acciones-estado';

/** Ruta del listado de estados. */
const RUTA_LISTADO = '/configuracion/estados-proyecto';

/** Mensaje al intentar editar el estado de sistema por URL (RN-09). */
export const MENSAJE_ESTADO_SISTEMA = 'El estado de sistema no se puede modificar.';

/**
 * P7 – Alta (`/configuracion/estados-proyecto/nuevo`) y edición
 * (`/configuracion/estados-proyecto/:id/editar`) de estado de proyecto.
 *
 * Creado: 2026-10-09
 * Modificado: 2026-10-09 - Creación inicial (RF-001)
 */
@Component({
  selector: 'app-estado-proyecto-form-page',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [EstadoProyectoFormComponent, NoEncontradoComponent],
  template: `
    <header class="encabezado-pagina">
      <h1>{{ esEdicion() ? 'Editar estado de proyecto' : 'Nuevo estado de proyecto' }}</h1>
    </header>
    @if (noEncontrado()) {
      <app-no-encontrado rutaVolver="/configuracion/estados-proyecto" />
    } @else if (errorCarga(); as mensaje) {
      <p class="alerta alerta-error" role="alert">{{ mensaje }}</p>
    } @else if (listo()) {
      <section class="panel">
        <app-estado-proyecto-form
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
export class EstadoProyectoFormPage implements OnInit {
  private readonly estadosService = inject(EstadosProyectoService);
  private readonly notificacion = inject(NotificacionService);
  private readonly router = inject(Router);
  private readonly destroyRef = inject(DestroyRef);

  /** Parámetro de ruta `:id` (ausente en alta). */
  readonly id = input<string>();

  readonly estadoId = computed(() => parsearId(this.id()));
  readonly esEdicion = computed(() => this.id() !== undefined);
  readonly estado = signal<EstadoProyectoDto | null>(null);
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
  guardar(request: EstadoProyectoRequest): void {
    const id = this.estadoId();
    this.enviando.set(true);
    this.errorServidor.set(SIN_ERRORES);
    const envio$: Observable<EstadoProyectoDto> =
      id === null ? this.estadosService.crear(request) : this.estadosService.editar(id, request);
    envio$.pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: () => {
        this.notificacion.exito('Estado guardado.');
        this.volver();
      },
      error: (error: unknown) => {
        this.enviando.set(false);
        this.errorServidor.set(interpretarErrorFormulario(error));
      },
    });
  }

  /** Vuelve al listado de estados. */
  volver(): void {
    void this.router.navigate([RUTA_LISTADO]);
  }

  /** Alta: calcula el orden sugerido a partir de los estados existentes. */
  private cargarOrdenSugerido(): void {
    this.estadosService
      .listar()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (estados) => {
          this.orden.set(ordenSugerido(estados));
          this.listo.set(true);
        },
        // Sin la lista se puede igualmente cargar el estado sin sugerencia.
        error: () => this.listo.set(true),
      });
  }

  /** Edición: carga el estado; el de sistema no es editable (redirige al listado). */
  private cargarEstado(id: number): void {
    this.estadosService
      .obtener(id)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (estado) => {
          if (estado.esSistema) {
            this.notificacion.info(MENSAJE_ESTADO_SISTEMA);
            this.volver();
            return;
          }
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
