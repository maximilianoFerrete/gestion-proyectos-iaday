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
import { takeUntilDestroyed, toSignal } from '@angular/core/rxjs-interop';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { debounceTime, distinctUntilChanged } from 'rxjs';
import { CampoErrorComponent } from '../../../../shared/components/campo-error/campo-error.component';
import {
  ErrorFormulario,
  SIN_ERRORES,
  aplicarErroresServidor,
} from '../../../../shared/forms/errores-servidor';
import { mensajesVisibles, traducirErrores } from '../../../../shared/forms/mensajes-error';
import { formatearFecha } from '../../../../shared/utils/fechas';
import {
  longitudMaxima,
  recortar,
  textoRequerido,
} from '../../../../shared/validators/texto.validators';
import { FlujoIncidenciaDto } from '../../../configuracion-incidencias/models/flujo-incidencia.model';
import { PrioridadIncidenciaDto } from '../../../configuracion-incidencias/models/prioridad-incidencia.model';
import { RelacionTipoIncidenciaDto } from '../../../configuracion-incidencias/models/relacion-tipo-incidencia.model';
import { TipoIncidenciaDto } from '../../../configuracion-incidencias/models/tipo-incidencia.model';
import { PersonaDto } from '../../../personas/models/persona.model';
import { AgrupacionDto } from '../../../proyectos/models/agrupacion.model';
import {
  IncidenciaDto,
  IncidenciaResumenDto,
  LIMITES_INCIDENCIA,
} from '../../models/incidencia.model';
import { ValoresIncidenciaForm } from '../../utils/incidencia-request';
import {
  estadoInicialDelFlujo,
  flujoDelTipo,
  idsTiposPadrePermitidos,
  opcionesEstadoFlujo,
  opcionesPrioridad,
  opcionesResponsableIncidencia,
  opcionesTipo,
  prioridadPredeterminadaId,
  tiposElegiblesAlta,
} from '../../utils/opciones-incidencia';
import {
  PadreSeleccionado,
  candidatosPadre,
  padreActualDe,
  padreDesdeResumen,
} from '../../utils/padre-incidencia';
import {
  ERROR_VENCIMIENTO_POSTERIOR_AL_PADRE,
  mensajeVencimientoPosteriorAlPadre,
  vencimientoNoPosteriorAlPadre,
} from '../../validators/vencimiento-no-posterior-al-padre.validator';
import { IncidenciasAfectadasComponent } from '../incidencias-afectadas/incidencias-afectadas.component';

/** Modo del formulario de incidencia. */
export type ModoIncidenciaForm = 'alta' | 'edicion';

/** Búsqueda de candidatos a padre que resuelve la página (HTTP fuera del componente). */
export interface BusquedaPadre {
  readonly texto: string;
  /** Tipos de padre admitidos (vacío = sin filtro de tipo). */
  readonly tipoIds: readonly number[];
}

/** Debounce del buscador de padre. */
export const DEBOUNCE_BUSQUEDA_PADRE_MS = 300;

/**
 * Formulario de incidencia (I4 alta / I5 edición). Padre con buscador; Tipo limitado a los hijos
 * permitidos del tipo del padre (alta) o en solo lectura (edición); Estado solo en edición;
 * Agrupación deshabilitada si hay padre (se hereda, RN-28); Vencimiento validado contra el del
 * padre (RN-31).
 *
 * Creado: 2026-10-09
 * Modificado: 2026-10-09 - Creación inicial (RF-002)
 */
@Component({
  selector: 'app-incidencia-form',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [ReactiveFormsModule, CampoErrorComponent, IncidenciasAfectadasComponent],
  templateUrl: './incidencia-form.component.html',
})
export class IncidenciaFormComponent implements OnInit {
  private readonly fb = inject(NonNullableFormBuilder);

  readonly modo = input.required<ModoIncidenciaForm>();
  readonly proyectoId = input.required<number>();
  /** Incidencia a editar (null en alta). */
  readonly incidencia = input<IncidenciaDto | null>(null);
  /** Padre preseleccionado en el alta (`?padreId=`). */
  readonly padreInicial = input<PadreSeleccionado | null>(null);
  /** Todos los tipos (en alta se ofrecen solo los activos). */
  readonly tipos = input.required<readonly TipoIncidenciaDto[]>();
  readonly relaciones = input.required<readonly RelacionTipoIncidenciaDto[]>();
  readonly flujos = input.required<readonly FlujoIncidenciaDto[]>();
  /** Todas las prioridades (se ofrecen las activas y la actual). */
  readonly prioridades = input.required<readonly PrioridadIncidenciaDto[]>();
  /** Personas activas. */
  readonly personas = input.required<readonly PersonaDto[]>();
  readonly agrupaciones = input.required<readonly AgrupacionDto[]>();
  /** Resultados de la última búsqueda de padre. */
  readonly resultadosPadre = input<readonly IncidenciaResumenDto[]>([]);
  readonly buscandoPadre = input(false);
  readonly enviando = input(false);
  readonly errorServidor = input<ErrorFormulario>(SIN_ERRORES);
  /** Códigos de la extensión `incidencias` del último error (p. ej. hijas). */
  readonly incidenciasAfectadas = input<readonly string[]>([]);
  readonly buscarPadre = output<BusquedaPadre>();
  readonly guardar = output<ValoresIncidenciaForm>();
  readonly cancelar = output<void>();

  readonly limites = LIMITES_INCIDENCIA;
  readonly erroresGenerales = signal<readonly string[]>([]);
  /** Padre elegido (null = sin padre). */
  readonly padre = signal<PadreSeleccionado | null>(null);

  readonly form = this.fb.group({
    padreId: this.fb.control<number | null>(null),
    tipoId: this.fb.control<number | null>(null, [Validators.required]),
    titulo: this.fb.control('', [textoRequerido, longitudMaxima(LIMITES_INCIDENCIA.tituloMax)]),
    descripcion: this.fb.control('', [
      textoRequerido,
      longitudMaxima(LIMITES_INCIDENCIA.descripcionMax),
    ]),
    estadoId: this.fb.control<number | null>(null, [Validators.required]),
    prioridadId: this.fb.control<number | null>(null, [Validators.required]),
    responsableId: this.fb.control<number | null>(null),
    fechaVencimiento: this.fb.control('', [
      vencimientoNoPosteriorAlPadre(() => this.padre()?.fechaVencimiento ?? null),
    ]),
    agrupacionId: this.fb.control<number | null>(null),
  });
  /** Texto del buscador de padre (fuera del grupo: no se envía). */
  readonly busquedaPadre = this.fb.control('');

  private readonly tipoElegido = toSignal(this.form.controls.tipoId.valueChanges, {
    initialValue: null,
  });
  private readonly agrupacionElegida = toSignal(this.form.controls.agrupacionId.valueChanges, {
    initialValue: null,
  });

  readonly esEdicion = computed(() => this.modo() === 'edicion');
  readonly opcionesTipo = computed(() =>
    opcionesTipo(
      tiposElegiblesAlta(this.tipos(), this.relaciones(), this.padre()?.tipo.id ?? null),
    ),
  );
  /** Nombre del estado inicial del flujo del tipo elegido (alta). */
  readonly nombreEstadoInicial = computed(
    () =>
      estadoInicialDelFlujo(flujoDelTipo(this.tipoElegido(), this.tipos(), this.flujos()))
        ?.nombre ?? null,
  );
  readonly opcionesEstado = computed(() => {
    const incidencia = this.incidencia();
    if (!incidencia) {
      return [];
    }
    const flujo = flujoDelTipo(incidencia.tipo.id, this.tipos(), this.flujos());
    return opcionesEstadoFlujo(flujo, incidencia.estado);
  });
  readonly opcionesPrioridad = computed(() =>
    opcionesPrioridad(this.prioridades(), this.incidencia()?.prioridad ?? null),
  );
  readonly opcionesResponsable = computed(() =>
    opcionesResponsableIncidencia(this.personas(), this.incidencia()?.responsable ?? null),
  );
  /** Tipos de padre admitidos para el tipo de la incidencia (edición). */
  readonly tiposPadreIds = computed(() => {
    const incidencia = this.incidencia();
    return incidencia ? idsTiposPadrePermitidos(this.relaciones(), incidencia.tipo.id) : [];
  });
  /** En edición, el tipo no admite ningún padre. */
  readonly sinPadresPosibles = computed(
    () => this.esEdicion() && this.tiposPadreIds().length === 0,
  );
  readonly candidatos = computed(() =>
    candidatosPadre(this.resultadosPadre(), this.incidencia()?.id ?? null),
  );
  /** Aviso de I5 al cambiar de padre (la jerarquía toma la agrupación del nuevo padre). */
  readonly avisoCambioPadre = computed(() => {
    const incidencia = this.incidencia();
    const padre = this.padre();
    if (!incidencia || !padre || padre.id === incidencia.padre?.id) {
      return null;
    }
    const hijas = incidencia.hijas.length;
    return hijas > 0
      ? `La incidencia y sus ${hijas} hijas tomarán la agrupación del nuevo padre.`
      : 'La incidencia tomará la agrupación del nuevo padre.';
  });
  /** Aviso de I5 al cambiar la agrupación de una incidencia sin padre con hijas. */
  readonly avisoAgrupacion = computed(() => {
    const incidencia = this.incidencia();
    if (!incidencia || this.padre() || incidencia.hijas.length === 0) {
      return false;
    }
    return this.agrupacionElegida() !== (incidencia.agrupacion?.id ?? null);
  });

  constructor() {
    effect(() => {
      const error = this.errorServidor();
      untracked(() => this.mostrarErrorServidor(error));
    });
    this.busquedaPadre.valueChanges
      .pipe(debounceTime(DEBOUNCE_BUSQUEDA_PADRE_MS), distinctUntilChanged(), takeUntilDestroyed())
      .subscribe((texto) => this.solicitarBusqueda(texto));
  }

  ngOnInit(): void {
    const incidencia = this.incidencia();
    if (this.esEdicion() && incidencia) {
      this.inicializarEdicion(incidencia);
    } else {
      this.inicializarAlta();
    }
  }

  /** Mensajes de error visibles del campo indicado. */
  errores(campo: keyof typeof this.form.controls, etiqueta: string): string[] {
    return mensajesVisibles(this.form.controls[campo], etiqueta);
  }

  /** Errores de Vencimiento; el de RN-31 se muestra siempre (puede surgir al cambiar el padre). */
  erroresVencimiento(): string[] {
    const control = this.form.controls.fechaVencimiento;
    const { [ERROR_VENCIMIENTO_POSTERIOR_AL_PADRE]: errorPadre, ...otros } = control.errors ?? {};
    const fechaPadre = this.padre()?.fechaVencimiento ?? null;
    const mensajes =
      errorPadre && fechaPadre ? [mensajeVencimientoPosteriorAlPadre(fechaPadre)] : [];
    const interactuado = control.touched || control.dirty;
    return interactuado ? [...mensajes, ...traducirErrores(otros, 'El vencimiento')] : mensajes;
  }

  /** Fecha `dd/MM/yyyy` para mostrar. */
  fecha(valor: string | null): string {
    return valor ? formatearFecha(valor) : 'sin vencimiento';
  }

  /** Elige un candidato de la búsqueda como padre. */
  elegirCandidato(candidato: IncidenciaResumenDto): void {
    this.seleccionarPadre(padreDesdeResumen(candidato));
  }

  /**
   * Cambia el padre: hereda/deshabilita la agrupación (RN-28), limpia el tipo si deja de ser
   * válido (alta) y revalida el vencimiento (RN-31).
   */
  seleccionarPadre(padre: PadreSeleccionado | null): void {
    this.padre.set(padre);
    this.form.controls.padreId.setValue(padre?.id ?? null);
    this.form.controls.padreId.markAsDirty();
    this.aplicarAgrupacionHeredada(padre);
    if (!this.esEdicion()) {
      this.limpiarTipoSiNoValido(padre);
    }
    this.form.controls.fechaVencimiento.updateValueAndValidity();
    this.busquedaPadre.setValue('', { emitEvent: false });
    this.buscarPadre.emit({ texto: '', tipoIds: [] });
  }

  /** Emite los valores recortados si el formulario es válido. */
  enviar(): void {
    if (this.form.invalid || this.enviando()) {
      this.form.markAllAsTouched();
      return;
    }
    const v = this.form.getRawValue();
    const padreId = this.padre()?.id ?? null;
    this.guardar.emit({
      tipoId: this.esEdicion() ? null : v.tipoId,
      titulo: recortar(v.titulo),
      descripcion: recortar(v.descripcion),
      estadoId: this.esEdicion() ? v.estadoId : null,
      prioridadId: v.prioridadId,
      responsableId: v.responsableId,
      fechaVencimiento: v.fechaVencimiento.length > 0 ? v.fechaVencimiento : null,
      agrupacionId: padreId === null ? v.agrupacionId : null,
      padreId,
    });
  }

  /** Alta: sin estado; prioridad predeterminada; padre preseleccionado si vino en la URL. */
  private inicializarAlta(): void {
    this.form.controls.estadoId.disable();
    this.form.patchValue({ prioridadId: prioridadPredeterminadaId(this.prioridades()) });
    const padre = this.padreInicial();
    if (padre) {
      this.padre.set(padre);
      this.form.controls.padreId.setValue(padre.id);
      this.aplicarAgrupacionHeredada(padre);
    }
  }

  /** Edición: tipo en solo lectura y valores actuales (incluido el padre). */
  private inicializarEdicion(incidencia: IncidenciaDto): void {
    this.form.controls.tipoId.disable();
    const padre = padreActualDe(incidencia);
    this.padre.set(padre);
    this.form.patchValue({
      padreId: padre?.id ?? null,
      tipoId: incidencia.tipo.id,
      titulo: incidencia.titulo,
      descripcion: incidencia.descripcion,
      estadoId: incidencia.estado.id,
      prioridadId: incidencia.prioridad.id,
      responsableId: incidencia.responsable?.id ?? null,
      fechaVencimiento: incidencia.fechaVencimiento ?? '',
      agrupacionId: incidencia.agrupacion?.id ?? null,
    });
    this.aplicarAgrupacionHeredada(padre);
  }

  /** Con padre: agrupación = la del padre y deshabilitada; sin padre: editable (conserva valor). */
  private aplicarAgrupacionHeredada(padre: PadreSeleccionado | null): void {
    const control = this.form.controls.agrupacionId;
    if (padre) {
      control.setValue(padre.agrupacion?.id ?? null);
      control.disable();
    } else {
      control.enable();
    }
  }

  /** Alta: si el tipo elegido no es hijo permitido del tipo del nuevo padre, lo limpia. */
  private limpiarTipoSiNoValido(padre: PadreSeleccionado | null): void {
    const tipoId = this.form.controls.tipoId.value;
    const validos = tiposElegiblesAlta(this.tipos(), this.relaciones(), padre?.tipo.id ?? null);
    if (tipoId !== null && !validos.some((t) => t.id === tipoId)) {
      this.form.controls.tipoId.setValue(null);
    }
  }

  /** Pide a la página buscar candidatos (en edición, solo de tipos que admiten la relación). */
  private solicitarBusqueda(texto: string): void {
    const recortado = texto.trim();
    if (this.sinPadresPosibles()) {
      return;
    }
    this.buscarPadre.emit({
      texto: recortado,
      tipoIds: this.esEdicion() ? this.tiposPadreIds() : [],
    });
  }

  /** Aplica en los controles los errores del backend y calcula los generales. */
  private mostrarErrorServidor(error: ErrorFormulario): void {
    const sinCampo = aplicarErroresServidor(this.form, error.campos);
    this.erroresGenerales.set(error.general ? [error.general, ...sinCampo] : sinCampo);
  }
}
