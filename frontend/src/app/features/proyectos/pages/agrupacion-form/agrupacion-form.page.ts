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
import { AgrupacionFormComponent } from '../../components/agrupacion-form/agrupacion-form.component';
import { AgrupacionDto, AgrupacionRequest } from '../../models/agrupacion.model';
import { ProyectoDto } from '../../models/proyecto.model';
import { AgrupacionesService } from '../../services/agrupaciones.service';
import { ProyectosService } from '../../services/proyectos.service';

/** Mensaje al intentar modificar agrupaciones de un proyecto archivado (P5, CA-24). */
export const MENSAJE_PROYECTO_ARCHIVADO_NO_MODIFICABLE =
  'El proyecto está archivado y no se puede modificar.';

/** Datos cargados para el formulario. */
interface DatosAgrupacionForm {
  readonly proyecto: ProyectoDto;
  readonly agrupacion: AgrupacionDto | null;
}

/**
 * P5 – Alta (`/proyectos/:id/agrupaciones/nueva`) y edición
 * (`/proyectos/:id/agrupaciones/:agrupacionId/editar`) de agrupación.
 *
 * Creado: 2026-10-09
 * Modificado: 2026-10-09 - Creación inicial (RF-001)
 */
@Component({
  selector: 'app-agrupacion-form-page',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [AgrupacionFormComponent, NoEncontradoComponent],
  template: `
    @if (noEncontrado()) {
      <app-no-encontrado
        [rutaVolver]="proyectoId() === null ? '/proyectos' : '/proyectos/' + proyectoId()"
        [textoVolver]="proyectoId() === null ? 'Volver al listado' : 'Volver al proyecto'"
      />
    } @else if (errorCarga(); as mensaje) {
      <p class="alerta alerta-error" role="alert">{{ mensaje }}</p>
    } @else if (datos(); as d) {
      <header class="encabezado-pagina">
        <div>
          <p class="subtitulo">{{ d.proyecto.codigo }} – {{ d.proyecto.nombre }}</p>
          <h1>{{ d.agrupacion ? 'Editar agrupación' : 'Nueva agrupación' }}</h1>
        </div>
      </header>
      <section class="panel">
        <app-agrupacion-form
          [agrupacion]="d.agrupacion"
          [enviando]="enviando()"
          [errorServidor]="errorServidor()"
          (guardar)="guardar(d.proyecto.id, $event)"
          (cancelar)="volverAlProyecto()"
        />
      </section>
    } @else {
      <p>Cargando...</p>
    }
  `,
})
export class AgrupacionFormPage implements OnInit {
  private readonly proyectosService = inject(ProyectosService);
  private readonly agrupacionesService = inject(AgrupacionesService);
  private readonly notificacion = inject(NotificacionService);
  private readonly router = inject(Router);
  private readonly destroyRef = inject(DestroyRef);

  /** Parámetro de ruta `:id` del proyecto. */
  readonly id = input.required<string>();
  /** Parámetro de ruta `:agrupacionId` (ausente en alta). */
  readonly agrupacionId = input<string>();

  readonly proyectoId = computed(() => parsearId(this.id()));
  readonly idAgrupacion = computed(() => parsearId(this.agrupacionId()));
  readonly esEdicion = computed(() => this.agrupacionId() !== undefined);
  readonly datos = signal<DatosAgrupacionForm | null>(null);
  readonly noEncontrado = signal(false);
  readonly errorCarga = signal<string | null>(null);
  readonly enviando = signal(false);
  readonly errorServidor = signal<ErrorFormulario>(SIN_ERRORES);

  ngOnInit(): void {
    const proyectoId = this.proyectoId();
    const agrupacionId = this.idAgrupacion();
    if (proyectoId === null || (this.esEdicion() && agrupacionId === null)) {
      this.noEncontrado.set(true);
      return;
    }
    this.cargar(proyectoId, agrupacionId);
  }

  /** Envía el alta o la edición; éxito → toast y vuelta al detalle del proyecto. */
  guardar(proyectoId: number, request: AgrupacionRequest): void {
    const agrupacionId = this.idAgrupacion();
    this.enviando.set(true);
    this.errorServidor.set(SIN_ERRORES);
    const envio$: Observable<AgrupacionDto> =
      agrupacionId === null
        ? this.agrupacionesService.crear(proyectoId, request)
        : this.agrupacionesService.editar(proyectoId, agrupacionId, request);
    envio$.pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: () => {
        this.notificacion.exito('Agrupación guardada.');
        this.volverAlProyecto();
      },
      error: (error: unknown) => {
        this.enviando.set(false);
        this.errorServidor.set(
          interpretarErrorFormulario(error, { AGRUPACION_NOMBRE_DUPLICADO: 'nombre' }),
        );
      },
    });
  }

  /** Navega al detalle del proyecto. */
  volverAlProyecto(): void {
    const proyectoId = this.proyectoId();
    void this.router.navigate(proyectoId === null ? ['/proyectos'] : ['/proyectos', proyectoId]);
  }

  /** Carga el proyecto (y la agrupación en edición); redirige si está archivado. */
  private cargar(proyectoId: number, agrupacionId: number | null): void {
    const agrupacion$: Observable<AgrupacionDto | null> =
      agrupacionId === null ? of(null) : this.agrupacionesService.obtener(proyectoId, agrupacionId);
    forkJoin({ proyecto: this.proyectosService.obtener(proyectoId), agrupacion: agrupacion$ })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (datos) => {
          if (datos.proyecto.archivado) {
            this.notificacion.info(MENSAJE_PROYECTO_ARCHIVADO_NO_MODIFICABLE);
            this.volverAlProyecto();
            return;
          }
          this.datos.set(datos);
        },
        error: (error: unknown) => {
          if (esNoEncontrado(error)) {
            this.noEncontrado.set(true);
          } else {
            this.errorCarga.set(mensajeErrorAccion(error) ?? 'No se pudo cargar la información.');
          }
        },
      });
  }
}
