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
import { EstadoProyectoDto } from '../../../estados-proyecto/models/estado-proyecto.model';
import { EstadosProyectoService } from '../../../estados-proyecto/services/estados-proyecto.service';
import { PersonaDto } from '../../../personas/models/persona.model';
import { PersonasService } from '../../../personas/services/personas.service';
import {
  ModoProyectoForm,
  ProyectoFormComponent,
  ValoresProyectoForm,
} from '../../components/proyecto-form/proyecto-form.component';
import { ProyectoDto } from '../../models/proyecto.model';
import { ProyectosService } from '../../services/proyectos.service';
import { aCrearProyectoRequest, aEditarProyectoRequest } from '../../utils/proyecto-request';

/** Mensaje al intentar editar un proyecto archivado (P4, CA-24). */
export const MENSAJE_PROYECTO_ARCHIVADO_NO_EDITABLE =
  'El proyecto está archivado y no se puede editar.';

/** Datos necesarios para mostrar el formulario. */
interface DatosFormularioProyecto {
  readonly personas: PersonaDto[];
  readonly estados: EstadoProyectoDto[];
  readonly proyecto: ProyectoDto | null;
}

/**
 * P2 / P4 – Alta (`/proyectos/nuevo`) y edición (`/proyectos/:id/editar`) de proyecto.
 * El modo se deduce de la presencia del parámetro `id`.
 *
 * Creado: 2026-10-09
 * Modificado: 2026-10-09 - Creación inicial (RF-001)
 */
@Component({
  selector: 'app-proyecto-form-page',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [ProyectoFormComponent, NoEncontradoComponent],
  template: `
    <header class="encabezado-pagina">
      <h1>{{ modo() === 'alta' ? 'Nuevo proyecto' : 'Editar proyecto' }}</h1>
    </header>
    @if (noEncontrado()) {
      <app-no-encontrado rutaVolver="/proyectos" />
    } @else if (errorCarga(); as mensaje) {
      <p class="alerta alerta-error" role="alert">{{ mensaje }}</p>
    } @else if (datos(); as d) {
      <section class="panel">
        <app-proyecto-form
          [modo]="modo()"
          [proyecto]="d.proyecto"
          [personas]="d.personas"
          [estados]="d.estados"
          [enviando]="enviando()"
          [errorServidor]="errorServidor()"
          (guardar)="guardar($event)"
          (cancelar)="cancelar()"
        />
      </section>
    } @else {
      <p>Cargando...</p>
    }
  `,
})
export class ProyectoFormPage implements OnInit {
  private readonly proyectosService = inject(ProyectosService);
  private readonly personasService = inject(PersonasService);
  private readonly estadosService = inject(EstadosProyectoService);
  private readonly notificacion = inject(NotificacionService);
  private readonly router = inject(Router);
  private readonly destroyRef = inject(DestroyRef);

  /** Parámetro de ruta `:id` (ausente en alta). */
  readonly id = input<string>();

  readonly proyectoId = computed(() => parsearId(this.id()));
  readonly modo = computed<ModoProyectoForm>(() => (this.id() === undefined ? 'alta' : 'edicion'));
  readonly datos = signal<DatosFormularioProyecto | null>(null);
  readonly noEncontrado = signal(false);
  readonly errorCarga = signal<string | null>(null);
  readonly enviando = signal(false);
  readonly errorServidor = signal<ErrorFormulario>(SIN_ERRORES);

  ngOnInit(): void {
    if (this.modo() === 'edicion' && this.proyectoId() === null) {
      this.noEncontrado.set(true);
      return;
    }
    this.cargar();
  }

  /** Envía el alta o la edición según el modo. */
  guardar(valores: ValoresProyectoForm): void {
    const id = this.proyectoId();
    this.enviando.set(true);
    this.errorServidor.set(SIN_ERRORES);
    if (this.modo() === 'alta' || id === null) {
      this.enviarAlta(valores);
    } else {
      this.enviarEdicion(id, valores);
    }
  }

  /** Vuelve al listado (alta) o al detalle (edición). */
  cancelar(): void {
    const id = this.proyectoId();
    void this.router.navigate(id === null ? ['/proyectos'] : ['/proyectos', id]);
  }

  /** Carga personas activas, estados activos y, en edición, el proyecto. */
  private cargar(): void {
    const id = this.proyectoId();
    const proyecto$: Observable<ProyectoDto | null> =
      id === null ? of(null) : this.proyectosService.obtener(id);
    forkJoin({
      personas: this.personasService.listar(),
      estados: this.estadosService.listar({ soloActivos: true }),
      proyecto: proyecto$,
    })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (datos) => this.alCargar(datos),
        error: (error: unknown) => this.alFallarCarga(error),
      });
  }

  /** Muestra el formulario o redirige si el proyecto está archivado (CA-24). */
  private alCargar(datos: DatosFormularioProyecto): void {
    if (datos.proyecto?.archivado) {
      this.notificacion.info(MENSAJE_PROYECTO_ARCHIVADO_NO_EDITABLE);
      void this.router.navigate(['/proyectos', datos.proyecto.id]);
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
  private enviarAlta(valores: ValoresProyectoForm): void {
    this.proyectosService
      .crear(aCrearProyectoRequest(valores))
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (proyecto) => {
          this.notificacion.exito(`Proyecto ${proyecto.codigo} creado.`);
          void this.router.navigate(['/proyectos', proyecto.id]);
        },
        error: (error: unknown) => this.alFallarEnvio(error),
      });
  }

  /** PUT de la edición; éxito → toast y navegación al detalle. */
  private enviarEdicion(id: number, valores: ValoresProyectoForm): void {
    const request = aEditarProyectoRequest(valores);
    if (request === null) {
      this.enviando.set(false);
      return;
    }
    this.proyectosService
      .editar(id, request)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.notificacion.exito('Proyecto actualizado.');
          void this.router.navigate(['/proyectos', id]);
        },
        error: (error: unknown) => this.alFallarEnvio(error),
      });
  }

  /** Muestra los errores del envío en el formulario. */
  private alFallarEnvio(error: unknown): void {
    this.enviando.set(false);
    this.errorServidor.set(interpretarErrorFormulario(error));
  }
}
