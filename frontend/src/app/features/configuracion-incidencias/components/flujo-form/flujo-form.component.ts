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
import { toSignal } from '@angular/core/rxjs-interop';
import {
  FormArray,
  FormControl,
  FormGroup,
  NonNullableFormBuilder,
  ReactiveFormsModule,
} from '@angular/forms';
import { CampoErrorComponent } from '../../../../shared/components/campo-error/campo-error.component';
import {
  ErrorFormulario,
  SIN_ERRORES,
  aplicarErroresServidor,
} from '../../../../shared/forms/errores-servidor';
import { ERROR_SERVIDOR, mensajesVisibles } from '../../../../shared/forms/mensajes-error';
import {
  longitudMaxima,
  recortar,
  recortarOpcional,
  textoRequerido,
} from '../../../../shared/validators/texto.validators';
import {
  CategoriaEstadoIncidencia,
  EstadoIncidenciaDto,
  NOMBRES_CATEGORIA_ESTADO,
} from '../../models/estado-incidencia.model';
import {
  EstadoFlujoRequest,
  FlujoIncidenciaDto,
  FlujoIncidenciaRequest,
  LIMITES_FLUJO,
} from '../../models/flujo-incidencia.model';
import {
  MENSAJES_COMPOSICION_FLUJO,
  composicionFlujoValida,
} from '../../validators/composicion-flujo.validator';

/** Controles de un estado dentro del flujo. */
type EstadoFlujoForm = FormGroup<{
  estadoId: FormControl<number>;
  esInicial: FormControl<boolean>;
  esFinal: FormControl<boolean>;
}>;

/** Datos visibles de un estado (nombre, si está activo y su categoría de seguimiento). */
interface InfoEstado {
  readonly nombre: string;
  readonly activo: boolean;
  readonly categoria: CategoriaEstadoIncidencia | null;
}

/** Ayuda de los estados con categoría (RF-005 "Cambios en C4", RN-18). */
export function ayudaEstadoConCategoria(categoria: CategoriaEstadoIncidencia): string {
  return `Estado de categoría ${NOMBRES_CATEGORIA_ESTADO[categoria]}: no puede ser inicial ni final`;
}

/**
 * Formulario de flujo de trabajo (C4, RN-13/RN-14): Nombre, Descripción y lista ordenable de
 * estados (agregar activos no incluidos, quitar, subir/bajar), radio "Inicial" (uno; no en
 * inactivos) y check "Final" (deshabilitado en el inicial). Los estados con categoría de
 * seguimiento no pueden ser iniciales ni finales (RF-005 RN-18).
 *
 * Creado: 2026-10-09
 * Modificado: 2026-10-10 - Inicial/Final deshabilitados en estados con categoría (RF-005)
 */
@Component({
  selector: 'app-flujo-form',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [ReactiveFormsModule, CampoErrorComponent],
  template: `
    <form class="formulario" [formGroup]="form" (ngSubmit)="enviar()" novalidate>
      @if (erroresGenerales().length > 0) {
        <div class="alerta alerta-error" role="alert">
          @for (mensaje of erroresGenerales(); track $index) {
            <p>{{ mensaje }}</p>
          }
        </div>
      }
      @if (avisoCompartido(); as aviso) {
        <p class="aviso" role="status">{{ aviso }}</p>
      }
      <div class="campo">
        <label for="flujo-nombre">Nombre <span aria-hidden="true">*</span></label>
        <input
          id="flujo-nombre"
          type="text"
          formControlName="nombre"
          required
          [attr.aria-invalid]="errores('nombre', 'El nombre').length > 0"
          aria-describedby="flujo-nombre-error"
        />
        <app-campo-error [mensajes]="errores('nombre', 'El nombre')" idError="flujo-nombre-error" />
      </div>
      <div class="campo">
        <label for="flujo-descripcion">Descripción</label>
        <textarea
          id="flujo-descripcion"
          rows="2"
          formControlName="descripcion"
          [attr.aria-invalid]="errores('descripcion', 'La descripción').length > 0"
          aria-describedby="flujo-descripcion-contador flujo-descripcion-error"
        ></textarea>
        <small id="flujo-descripcion-contador" class="contador">
          {{ form.controls.descripcion.value.length }} / {{ limites.descripcionMax }}
        </small>
        <app-campo-error
          [mensajes]="errores('descripcion', 'La descripción')"
          idError="flujo-descripcion-error"
        />
      </div>

      <fieldset class="campo" aria-describedby="flujo-estados-error">
        <legend>Estados del flujo <span aria-hidden="true">*</span></legend>
        @if (form.controls.estados.length === 0) {
          <p class="vacio">El flujo no tiene estados.</p>
        } @else {
          <ol class="lista-estados-flujo" formArrayName="estados">
            @for (
              grupo of form.controls.estados.controls;
              track grupo.controls.estadoId.value;
              let i = $index, primero = $first, ultimo = $last
            ) {
              @let info = infoEstado(grupo.controls.estadoId.value);
              <li [formGroupName]="i">
                <span class="estado-flujo-nombre">
                  {{ i + 1 }}. {{ info.nombre }}
                  @if (!info.activo) {
                    <span class="texto-secundario">(inactivo)</span>
                  }
                </span>
                <label class="campo-check">
                  <input
                    type="radio"
                    name="flujo-inicial"
                    [checked]="grupo.controls.esInicial.value"
                    [disabled]="!info.activo || info.categoria !== null"
                    [attr.aria-describedby]="
                      info.categoria !== null ? 'flujo-estado-categoria-' + i : null
                    "
                    (change)="marcarInicial(i)"
                  />
                  Inicial
                </label>
                <label class="campo-check">
                  <input
                    type="checkbox"
                    formControlName="esFinal"
                    [attr.aria-describedby]="
                      info.categoria !== null ? 'flujo-estado-categoria-' + i : null
                    "
                  />
                  Final
                </label>
                @if (info.categoria !== null) {
                  <small class="ayuda" [id]="'flujo-estado-categoria-' + i">
                    {{ ayudaCategoria(info.categoria) }}
                  </small>
                }
                <span class="acciones-fila">
                  <button
                    type="button"
                    class="btn btn-chico"
                    [disabled]="primero"
                    [attr.aria-label]="'Subir ' + info.nombre"
                    (click)="mover(i, -1)"
                  >
                    ▲
                  </button>
                  <button
                    type="button"
                    class="btn btn-chico"
                    [disabled]="ultimo"
                    [attr.aria-label]="'Bajar ' + info.nombre"
                    (click)="mover(i, 1)"
                  >
                    ▼
                  </button>
                  <button
                    type="button"
                    class="btn btn-chico btn-peligro"
                    [attr.aria-label]="'Quitar ' + info.nombre"
                    (click)="quitar(i)"
                  >
                    Quitar
                  </button>
                </span>
              </li>
            }
          </ol>
        }
        <app-campo-error [mensajes]="erroresEstados()" idError="flujo-estados-error" />
        <div class="fila-campos">
          <div class="campo">
            <label for="flujo-agregar-estado">Agregar estado</label>
            <select id="flujo-agregar-estado" [formControl]="estadoAgregar">
              <option [ngValue]="null">Seleccioná un estado</option>
              @for (estado of estadosAgregables(); track estado.id) {
                <option [ngValue]="estado.id">{{ estado.nombre }}</option>
              }
            </select>
          </div>
          <div class="campo campo-accion">
            <button
              type="button"
              class="btn"
              [disabled]="estadoAgregar.value === null"
              (click)="agregar()"
            >
              Agregar
            </button>
          </div>
        </div>
      </fieldset>

      <div class="acciones">
        <button type="button" class="btn" (click)="cancelar.emit()">Cancelar</button>
        <button type="submit" class="btn btn-primario" [disabled]="form.invalid || enviando()">
          {{ enviando() ? 'Guardando...' : 'Guardar' }}
        </button>
      </div>
    </form>
  `,
})
export class FlujoFormComponent implements OnInit {
  private readonly fb = inject(NonNullableFormBuilder);

  /** Flujo a editar (null en alta). */
  readonly flujo = input<FlujoIncidenciaDto | null>(null);
  /** Catálogo completo de estados (incluidos inactivos). */
  readonly estados = input.required<readonly EstadoIncidenciaDto[]>();
  readonly enviando = input(false);
  readonly errorServidor = input<ErrorFormulario>(SIN_ERRORES);
  readonly guardar = output<FlujoIncidenciaRequest>();
  readonly cancelar = output<void>();

  readonly limites = LIMITES_FLUJO;
  protected readonly ayudaCategoria = ayudaEstadoConCategoria;
  readonly erroresGenerales = signal<readonly string[]>([]);

  readonly form = this.fb.group({
    nombre: this.fb.control('', [textoRequerido, longitudMaxima(LIMITES_FLUJO.nombreMax)]),
    descripcion: this.fb.control('', [longitudMaxima(LIMITES_FLUJO.descripcionMax)]),
    estados: this.fb.array<EstadoFlujoForm>([], { validators: [composicionFlujoValida] }),
  });
  /** Estado elegido para agregar (fuera del grupo: no se envía). */
  readonly estadoAgregar = new FormControl<number | null>(null);

  private readonly idsIncluidos = toSignal(this.form.controls.estados.valueChanges, {
    initialValue: [],
  });

  /** Estados activos no incluidos en el flujo (RN-11: inactivos no se agregan). */
  readonly estadosAgregables = computed(() => {
    const incluidos = new Set(this.idsIncluidos().map((e) => e.estadoId));
    return this.estados().filter((e) => e.activo && !incluidos.has(e.id));
  });
  /** Aviso si el flujo lo comparten varios tipos. */
  readonly avisoCompartido = computed(() => {
    const tipos = this.flujo()?.tipos ?? [];
    return tipos.length > 1
      ? `Este flujo lo usan los tipos: ${tipos.map((t) => t.nombre).join(', ')}.`
      : null;
  });

  constructor() {
    effect(() => {
      const error = this.errorServidor();
      untracked(() => {
        const sinCampo = aplicarErroresServidor(this.form, error.campos);
        this.erroresGenerales.set(error.general ? [error.general, ...sinCampo] : sinCampo);
      });
    });
  }

  ngOnInit(): void {
    const flujo = this.flujo();
    if (!flujo) {
      return;
    }
    this.form.patchValue({ nombre: flujo.nombre, descripcion: flujo.descripcion ?? '' });
    for (const estado of [...flujo.estados].sort((a, b) => a.orden - b.orden)) {
      this.form.controls.estados.push(this.crearGrupo(estado));
    }
    this.sincronizarFinalDelInicial();
  }

  /** Mensajes de error visibles del campo indicado. */
  errores(campo: 'nombre' | 'descripcion', etiqueta: string): string[] {
    return mensajesVisibles(this.form.controls[campo], etiqueta);
  }

  /** Errores de la composición (validador de grupo y errores `estados` del backend). */
  erroresEstados(): string[] {
    const control = this.form.controls.estados;
    if (!(control.touched || control.dirty)) {
      return [];
    }
    return Object.entries(control.errors ?? {}).map(([clave, detalle]) =>
      clave === ERROR_SERVIDOR && typeof detalle === 'string'
        ? detalle
        : (MENSAJES_COMPOSICION_FLUJO[clave] ?? 'La composición del flujo no es válida.'),
    );
  }

  /** Nombre y estado activo de un estado (catálogo o, si no figura, datos del flujo). */
  infoEstado(estadoId: number): InfoEstado {
    const delCatalogo = this.estados().find((e) => e.id === estadoId);
    if (delCatalogo) {
      return {
        nombre: delCatalogo.nombre,
        activo: delCatalogo.activo,
        categoria: delCatalogo.categoria ?? null,
      };
    }
    const delFlujo = this.flujo()?.estados.find((e) => e.estadoId === estadoId);
    return {
      nombre: delFlujo?.nombre ?? `Estado ${estadoId}`,
      activo: delFlujo?.activo ?? false,
      categoria: null,
    };
  }

  /** Agrega el estado elegido al final; si es el primero (y sin categoría), queda como inicial. */
  agregar(): void {
    const estadoId = this.estadoAgregar.value;
    if (estadoId === null) {
      return;
    }
    const esPrimero =
      this.form.controls.estados.length === 0 && this.infoEstado(estadoId).categoria === null;
    this.form.controls.estados.push(
      this.crearGrupo({ estadoId, esInicial: esPrimero, esFinal: false }),
    );
    this.estadoAgregar.setValue(null);
    this.marcarCambio();
  }

  /** Quita el estado de la posición indicada. */
  quitar(indice: number): void {
    this.form.controls.estados.removeAt(indice);
    this.marcarCambio();
  }

  /** Mueve el estado una posición arriba (-1) o abajo (+1). */
  mover(indice: number, delta: -1 | 1): void {
    const estados = this.form.controls.estados;
    const destino = indice + delta;
    if (destino < 0 || destino >= estados.length) {
      return;
    }
    const grupo = estados.at(indice);
    estados.removeAt(indice, { emitEvent: false });
    estados.insert(destino, grupo);
    this.marcarCambio();
  }

  /** Marca como inicial el estado indicado (exactamente uno, RN-14; nunca uno con categoría). */
  marcarInicial(indice: number): void {
    const grupo = this.form.controls.estados.at(indice);
    if (!grupo || this.infoEstado(grupo.controls.estadoId.value).categoria !== null) {
      return;
    }
    this.form.controls.estados.controls.forEach((grupo, i) =>
      grupo.controls.esInicial.setValue(i === indice, { emitEvent: false }),
    );
    this.sincronizarFinalDelInicial();
    this.marcarCambio();
  }

  /** Emite el request (orden = posición) si el formulario es válido. */
  enviar(): void {
    if (this.form.invalid || this.enviando()) {
      this.form.markAllAsTouched();
      return;
    }
    const v = this.form.getRawValue();
    this.guardar.emit({
      nombre: recortar(v.nombre),
      descripcion: recortarOpcional(v.descripcion),
      estados: v.estados.map((e): EstadoFlujoRequest => ({
        estadoId: e.estadoId,
        esInicial: e.esInicial,
        esFinal: e.esInicial ? false : e.esFinal,
      })),
    });
  }

  /** Crea los controles de un estado del flujo. */
  private crearGrupo(estado: EstadoFlujoRequest): EstadoFlujoForm {
    return this.fb.group({
      estadoId: this.fb.control(estado.estadoId),
      esInicial: this.fb.control(estado.esInicial),
      esFinal: this.fb.control(estado.esFinal),
    });
  }

  /**
   * El check "Final" se deshabilita en el estado inicial (RN-14) y en los estados con categoría
   * (RF-005 RN-18). En el inicial se desmarca; en los de categoría se conserva el valor para que
   * el backend informe el 409 si un flujo existente ya lo tuviera marcado.
   */
  private sincronizarFinalDelInicial(): void {
    for (const grupo of this.form.controls.estados.controls) {
      const final = grupo.controls.esFinal;
      const conCategoria = this.infoEstado(grupo.controls.estadoId.value).categoria !== null;
      if (grupo.controls.esInicial.value) {
        final.setValue(false, { emitEvent: false });
        final.disable({ emitEvent: false });
      } else if (conCategoria) {
        final.disable({ emitEvent: false });
      } else if (final.disabled) {
        final.enable({ emitEvent: false });
      }
    }
  }

  /** Revalida la composición y la marca como modificada (para mostrar errores). */
  private marcarCambio(): void {
    this.sincronizarFinalDelInicial();
    const estados = this.form.controls.estados;
    estados.markAsDirty();
    estados.updateValueAndValidity();
  }
}
