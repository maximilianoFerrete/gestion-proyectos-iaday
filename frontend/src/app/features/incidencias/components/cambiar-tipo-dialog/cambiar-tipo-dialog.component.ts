import {
  ChangeDetectionStrategy,
  Component,
  computed,
  effect,
  inject,
  input,
  output,
  signal,
  untracked,
} from '@angular/core';
import { takeUntilDestroyed, toSignal } from '@angular/core/rxjs-interop';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { CampoErrorComponent } from '../../../../shared/components/campo-error/campo-error.component';
import { DialogoComponent } from '../../../../shared/components/dialogo/dialogo.component';
import {
  ErrorFormulario,
  SIN_ERRORES,
  aplicarErroresServidor,
} from '../../../../shared/forms/errores-servidor';
import { mensajesVisibles } from '../../../../shared/forms/mensajes-error';
import { FlujoIncidenciaDto } from '../../../configuracion-incidencias/models/flujo-incidencia.model';
import { TipoIncidenciaDto } from '../../../configuracion-incidencias/models/tipo-incidencia.model';
import { CambiarTipoIncidenciaRequest, IncidenciaDto } from '../../models/incidencia.model';
import {
  opcionesEstadoCambio,
  resolverEstadoAlCambiarTipo,
  tiposDestinoCambio,
} from '../../utils/cambio-tipo';
import { flujoDelTipo, opcionesTipo } from '../../utils/opciones-incidencia';
import { IncidenciasAfectadasComponent } from '../incidencias-afectadas/incidencias-afectadas.component';

/**
 * Diálogo "Cambiar tipo" (I3, RN-02/RN-23): Tipo (activos, distinto del actual) y Estado
 * (estados activos del flujo del tipo elegido; preseleccionado el actual si está en el nuevo
 * flujo, si no el inicial). Muestra `TIPO_INCOMPATIBLE_CON_HIJAS` con enlaces a las hijas.
 *
 * Creado: 2026-10-09
 * Modificado: 2026-10-09 - Creación inicial (RF-002)
 */
@Component({
  selector: 'app-cambiar-tipo-dialog',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    ReactiveFormsModule,
    CampoErrorComponent,
    DialogoComponent,
    IncidenciasAfectadasComponent,
  ],
  template: `
    <app-dialogo
      [abierto]="abierto()"
      titulo="Cambiar tipo"
      idTitulo="cambiar-tipo-titulo"
      (cerrar)="cancelar.emit()"
    >
      @if (abierto()) {
        <form class="formulario" [formGroup]="form" (ngSubmit)="enviar()" novalidate>
          <p class="texto-secundario">
            {{ incidencia().codigo }} – tipo actual: {{ incidencia().tipo.nombre }}
          </p>
          @if (erroresGenerales().length > 0) {
            <div class="alerta alerta-error" role="alert">
              @for (mensaje of erroresGenerales(); track $index) {
                <p>{{ mensaje }}</p>
              }
              <app-incidencias-afectadas
                [proyectoId]="incidencia().proyecto.id"
                [codigos]="incidenciasAfectadas()"
                [conocidas]="incidencia().hijas"
              />
            </div>
          }
          @if (cargando()) {
            <p>Cargando...</p>
          } @else {
            <div class="campo">
              <label for="cambiar-tipo-tipo">Tipo <span aria-hidden="true">*</span></label>
              <select
                id="cambiar-tipo-tipo"
                formControlName="tipoId"
                required
                [attr.aria-invalid]="errores('tipoId', 'El tipo').length > 0"
                aria-describedby="cambiar-tipo-tipo-error"
              >
                <option [ngValue]="null" disabled>Seleccioná un tipo</option>
                @for (opcion of opcionesTipo(); track opcion.valor) {
                  <option [ngValue]="opcion.valor">{{ opcion.etiqueta }}</option>
                }
              </select>
              <app-campo-error
                [mensajes]="errores('tipoId', 'El tipo')"
                idError="cambiar-tipo-tipo-error"
              />
            </div>
            <div class="campo">
              <label for="cambiar-tipo-estado">Estado <span aria-hidden="true">*</span></label>
              <select
                id="cambiar-tipo-estado"
                formControlName="estadoId"
                required
                [attr.aria-invalid]="errores('estadoId', 'El estado').length > 0"
                aria-describedby="cambiar-tipo-estado-error"
              >
                <option [ngValue]="null" disabled>Seleccioná un estado</option>
                @for (opcion of opcionesEstado(); track opcion.valor) {
                  <option [ngValue]="opcion.valor">{{ opcion.etiqueta }}</option>
                }
              </select>
              <app-campo-error
                [mensajes]="errores('estadoId', 'El estado')"
                idError="cambiar-tipo-estado-error"
              />
            </div>
          }
          <div class="acciones">
            <button type="button" class="btn" (click)="cancelar.emit()">Cancelar</button>
            <button
              type="submit"
              class="btn btn-primario"
              [disabled]="form.invalid || enviando() || cargando()"
            >
              {{ enviando() ? 'Guardando...' : 'Confirmar' }}
            </button>
          </div>
        </form>
      }
    </app-dialogo>
  `,
})
export class CambiarTipoDialogComponent {
  private readonly fb = inject(NonNullableFormBuilder);

  readonly abierto = input.required<boolean>();
  readonly incidencia = input.required<IncidenciaDto>();
  /** Tipos (se filtran activos y distintos del actual). */
  readonly tipos = input.required<readonly TipoIncidenciaDto[]>();
  readonly flujos = input.required<readonly FlujoIncidenciaDto[]>();
  readonly cargando = input(false);
  readonly enviando = input(false);
  readonly errorServidor = input<ErrorFormulario>(SIN_ERRORES);
  /** Códigos de la extensión `incidencias` del último error. */
  readonly incidenciasAfectadas = input<readonly string[]>([]);
  readonly confirmar = output<CambiarTipoIncidenciaRequest>();
  readonly cancelar = output<void>();

  readonly erroresGenerales = signal<readonly string[]>([]);

  readonly form = this.fb.group({
    tipoId: this.fb.control<number | null>(null, [Validators.required]),
    estadoId: this.fb.control<number | null>(null, [Validators.required]),
  });

  private readonly tipoElegido = toSignal(this.form.controls.tipoId.valueChanges, {
    initialValue: null,
  });

  readonly opcionesTipo = computed(() =>
    opcionesTipo(tiposDestinoCambio(this.tipos(), this.incidencia().tipo.id)),
  );
  readonly flujoNuevo = computed(() =>
    flujoDelTipo(this.tipoElegido(), this.tipos(), this.flujos()),
  );
  readonly opcionesEstado = computed(() =>
    opcionesEstadoCambio(this.flujoNuevo(), this.incidencia().estado),
  );

  constructor() {
    effect(() => {
      if (this.abierto()) {
        untracked(() => this.reiniciar());
      }
    });
    effect(() => {
      const error = this.errorServidor();
      untracked(() => {
        const sinCampo = aplicarErroresServidor(this.form, error.campos);
        this.erroresGenerales.set(error.general ? [error.general, ...sinCampo] : sinCampo);
      });
    });
    this.form.controls.tipoId.valueChanges
      .pipe(takeUntilDestroyed())
      .subscribe((tipoId) => this.preseleccionarEstado(tipoId));
  }

  /** Mensajes de error visibles del campo indicado. */
  errores(campo: keyof typeof this.form.controls, etiqueta: string): string[] {
    return mensajesVisibles(this.form.controls[campo], etiqueta);
  }

  /** Emite el request si el formulario es válido. */
  enviar(): void {
    const { tipoId, estadoId } = this.form.getRawValue();
    if (this.form.invalid || this.enviando() || tipoId === null) {
      this.form.markAllAsTouched();
      return;
    }
    this.confirmar.emit({ tipoId, estadoId });
  }

  /** RN-23: preselecciona el estado actual si está en el nuevo flujo; si no, el inicial. */
  private preseleccionarEstado(tipoId: number | null): void {
    const flujo = flujoDelTipo(tipoId, this.tipos(), this.flujos());
    const estadoId = resolverEstadoAlCambiarTipo(this.incidencia().estado.id, flujo);
    this.form.controls.estadoId.setValue(estadoId);
  }

  /** Limpia el formulario y los errores al abrir el diálogo. */
  private reiniciar(): void {
    this.form.reset({ tipoId: null, estadoId: null });
    this.erroresGenerales.set([]);
  }
}
