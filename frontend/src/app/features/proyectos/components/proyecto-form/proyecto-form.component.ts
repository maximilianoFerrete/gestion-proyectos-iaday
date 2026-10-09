import {
  ChangeDetectionStrategy,
  Component,
  OnInit,
  computed,
  effect,
  inject,
  input,
  output,
  signal,
  untracked,
} from '@angular/core';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { CampoErrorComponent } from '../../../../shared/components/campo-error/campo-error.component';
import {
  ErrorFormulario,
  SIN_ERRORES,
  aplicarErroresServidor,
} from '../../../../shared/forms/errores-servidor';
import { mensajesVisibles } from '../../../../shared/forms/mensajes-error';
import {
  longitudMaxima,
  recortar,
  textoRequerido,
} from '../../../../shared/validators/texto.validators';
import { EstadoProyectoDto } from '../../../estados-proyecto/models/estado-proyecto.model';
import { PersonaDto } from '../../../personas/models/persona.model';
import { LIMITES_PROYECTO, ProyectoDto } from '../../models/proyecto.model';
import { opcionesEstado, opcionesResponsable } from '../../utils/opciones-proyecto';
import {
  ERROR_FECHA_OBJETIVO_ANTERIOR,
  MENSAJE_FECHA_OBJETIVO_ANTERIOR,
  fechaObjetivoNoAnterior,
} from '../../validators/fecha-objetivo-no-anterior.validator';

/** Modo del formulario de proyecto. */
export type ModoProyectoForm = 'alta' | 'edicion';

/** Valores emitidos al guardar (textos ya recortados). `estadoId` solo en edición. */
export interface ValoresProyectoForm {
  readonly nombre: string;
  readonly descripcion: string;
  readonly responsableId: number;
  readonly fechaInicio: string;
  readonly fechaObjetivo: string;
  readonly estadoId: number | null;
}

/**
 * Formulario de proyecto (P2 alta / P4 edición). En alta no hay campo Estado y se informa el
 * estado inicial; en edición muestra Código (solo lectura) y Estado.
 *
 * Creado: 2026-10-09
 * Modificado: 2026-10-09 - Creación inicial (RF-001)
 */
@Component({
  selector: 'app-proyecto-form',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [ReactiveFormsModule, CampoErrorComponent],
  templateUrl: './proyecto-form.component.html',
})
export class ProyectoFormComponent implements OnInit {
  private readonly fb = inject(NonNullableFormBuilder);

  readonly modo = input.required<ModoProyectoForm>();
  /** Proyecto a editar (null en alta). */
  readonly proyecto = input<ProyectoDto | null>(null);
  /** Personas activas. */
  readonly personas = input.required<readonly PersonaDto[]>();
  /** Estados activos (en alta solo se usa para el nombre del inicial). */
  readonly estados = input.required<readonly EstadoProyectoDto[]>();
  readonly enviando = input(false);
  readonly errorServidor = input<ErrorFormulario>(SIN_ERRORES);
  readonly guardar = output<ValoresProyectoForm>();
  readonly cancelar = output<void>();

  readonly limites = LIMITES_PROYECTO;
  readonly mensajeFechas = MENSAJE_FECHA_OBJETIVO_ANTERIOR;

  readonly form = this.fb.group(
    {
      nombre: this.fb.control('', [textoRequerido, longitudMaxima(LIMITES_PROYECTO.nombreMax)]),
      descripcion: this.fb.control('', [
        textoRequerido,
        longitudMaxima(LIMITES_PROYECTO.descripcionMax),
      ]),
      responsableId: this.fb.control<number | null>(null, [Validators.required]),
      fechaInicio: this.fb.control('', [Validators.required]),
      fechaObjetivo: this.fb.control('', [Validators.required]),
      estadoId: this.fb.control<number | null>(null, [Validators.required]),
    },
    { validators: [fechaObjetivoNoAnterior()] },
  );

  readonly esEdicion = computed(() => this.modo() === 'edicion');
  readonly opcionesResponsable = computed(() =>
    opcionesResponsable(this.personas(), this.proyecto()?.responsable ?? null),
  );
  readonly opcionesEstado = computed(() =>
    opcionesEstado(this.estados(), this.proyecto()?.estado ?? null),
  );
  readonly nombreEstadoInicial = computed(
    () => this.estados().find((e) => e.esInicial)?.nombre ?? null,
  );
  /** Mensajes generales (arriba del form): `detail` de negocio y errores sin campo. */
  readonly erroresGenerales = signal<readonly string[]>([]);

  constructor() {
    effect(() => {
      const error = this.errorServidor();
      untracked(() => this.mostrarErrorServidor(error));
    });
  }

  ngOnInit(): void {
    if (this.modo() === 'alta') {
      this.form.controls.estadoId.disable();
    }
    const proyecto = this.proyecto();
    if (proyecto) {
      this.form.patchValue({
        nombre: proyecto.nombre,
        descripcion: proyecto.descripcion,
        responsableId: proyecto.responsable.id,
        fechaInicio: proyecto.fechaInicio,
        fechaObjetivo: proyecto.fechaObjetivo,
        estadoId: proyecto.estado.id,
      });
    }
  }

  /** Mensajes de error visibles del campo indicado. */
  errores(campo: keyof typeof this.form.controls, etiqueta: string): string[] {
    return mensajesVisibles(this.form.controls[campo], etiqueta);
  }

  /** Errores de Fecha objetivo, incluido el validador de grupo (RN-16). */
  erroresFechaObjetivo(): string[] {
    const mensajes = this.errores('fechaObjetivo', 'La fecha objetivo');
    const { fechaInicio, fechaObjetivo } = this.form.controls;
    const interactuado =
      fechaInicio.touched || fechaInicio.dirty || fechaObjetivo.touched || fechaObjetivo.dirty;
    if (interactuado && this.form.hasError(ERROR_FECHA_OBJETIVO_ANTERIOR)) {
      mensajes.push(MENSAJE_FECHA_OBJETIVO_ANTERIOR);
    }
    return mensajes;
  }

  /** Emite los valores recortados si el formulario es válido. */
  enviar(): void {
    if (this.form.invalid || this.enviando()) {
      this.form.markAllAsTouched();
      return;
    }
    const valores = this.form.getRawValue();
    if (valores.responsableId === null) {
      return;
    }
    this.guardar.emit({
      nombre: recortar(valores.nombre),
      descripcion: recortar(valores.descripcion),
      responsableId: valores.responsableId,
      fechaInicio: valores.fechaInicio,
      fechaObjetivo: valores.fechaObjetivo,
      estadoId: this.esEdicion() ? valores.estadoId : null,
    });
  }

  /** Aplica en los controles los errores del backend y calcula los generales. */
  private mostrarErrorServidor(error: ErrorFormulario): void {
    const sinCampo = aplicarErroresServidor(this.form, error.campos);
    this.erroresGenerales.set(error.general ? [error.general, ...sinCampo] : sinCampo);
  }
}
