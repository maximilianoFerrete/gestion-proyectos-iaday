import {
  ChangeDetectionStrategy,
  Component,
  effect,
  inject,
  input,
  output,
  untracked,
} from '@angular/core';
import { NonNullableFormBuilder, ReactiveFormsModule } from '@angular/forms';
import { DialogoComponent } from '../../../../shared/components/dialogo/dialogo.component';
import {
  ConfiguracionTableroDto,
  ConfiguracionTableroRequest,
} from '../../models/seguimiento.model';
import { COLUMNAS_OBLIGATORIAS, TITULOS_COLUMNA_TABLERO } from '../../utils/tablero';

/** Texto del diálogo (RF-005 RN-30). */
export const TEXTO_ALCANCE_CONFIGURACION =
  'La configuración aplica al tablero de todo el proyecto y a todos los usuarios.';

/**
 * Diálogo "Columnas" del Tablero (RF-005 DA-15, RN-29 a RN-31): Tipo, Nro y Título marcados y
 * deshabilitados ("obligatoria"); Estado, Estimación y Responsable editables. "Restablecer"
 * marca los tres; "Guardar" emite los tres booleanos. El error (401/403/409) se muestra dentro.
 *
 * Creado: 2026-10-10
 * Modificado: 2026-10-10 - Creación inicial (RF-005)
 */
@Component({
  selector: 'app-configurar-columnas-dialog',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [ReactiveFormsModule, DialogoComponent],
  template: `
    <app-dialogo
      [abierto]="abierto()"
      titulo="Columnas del tablero"
      idTitulo="configurar-columnas-titulo"
      (cerrar)="cancelar.emit()"
    >
      @if (abierto()) {
        <form class="formulario" [formGroup]="form" (ngSubmit)="enviar()" novalidate>
          @if (error(); as mensaje) {
            <div class="alerta alerta-error" role="alert">
              <p>{{ mensaje }}</p>
            </div>
          }
          <fieldset class="grupo-checks">
            <legend>Columnas visibles</legend>
            @for (columna of obligatorias; track columna) {
              <label class="campo-check">
                <input type="checkbox" [checked]="true" disabled />
                {{ titulos[columna] }} <span class="texto-secundario">(obligatoria)</span>
              </label>
            }
            <label class="campo-check">
              <input type="checkbox" formControlName="mostrarEstado" />
              {{ titulos.estado }}
            </label>
            <label class="campo-check">
              <input type="checkbox" formControlName="mostrarEstimacion" />
              {{ titulos.estimacion }}
            </label>
            <label class="campo-check">
              <input type="checkbox" formControlName="mostrarResponsable" />
              {{ titulos.responsable }}
            </label>
          </fieldset>
          <p class="ayuda">{{ textoAlcance }}</p>
          <div class="acciones">
            <button type="button" class="btn" [disabled]="enviando()" (click)="restablecer()">
              Restablecer
            </button>
            <button type="button" class="btn" (click)="cancelar.emit()">Cancelar</button>
            <button type="submit" class="btn btn-primario" [disabled]="enviando()">
              {{ enviando() ? 'Guardando...' : 'Guardar' }}
            </button>
          </div>
        </form>
      }
    </app-dialogo>
  `,
})
export class ConfigurarColumnasDialogComponent {
  private readonly fb = inject(NonNullableFormBuilder);

  readonly abierto = input.required<boolean>();
  /** Configuración vigente (null = por defecto, todas visibles). */
  readonly configuracion = input<ConfiguracionTableroDto | null>(null);
  readonly enviando = input(false);
  /** Mensaje de error del último guardado (`detail`). */
  readonly error = input<string | null>(null);
  readonly guardar = output<ConfiguracionTableroRequest>();
  readonly cancelar = output<void>();

  protected readonly obligatorias = COLUMNAS_OBLIGATORIAS;
  protected readonly titulos = TITULOS_COLUMNA_TABLERO;
  protected readonly textoAlcance = TEXTO_ALCANCE_CONFIGURACION;

  readonly form = this.fb.group({
    mostrarEstado: this.fb.control(true),
    mostrarEstimacion: this.fb.control(true),
    mostrarResponsable: this.fb.control(true),
  });

  constructor() {
    effect(() => {
      if (this.abierto()) {
        untracked(() => this.cargar(this.configuracion()));
      }
    });
  }

  /** Marca las tres columnas configurables. */
  restablecer(): void {
    this.form.setValue({ mostrarEstado: true, mostrarEstimacion: true, mostrarResponsable: true });
  }

  /** Emite los tres valores (el PUT es un reemplazo completo, RN-31). */
  enviar(): void {
    if (this.enviando()) {
      return;
    }
    this.guardar.emit(this.form.getRawValue());
  }

  /** Carga en el formulario la configuración vigente al abrir. */
  private cargar(configuracion: ConfiguracionTableroDto | null): void {
    this.form.setValue({
      mostrarEstado: configuracion?.mostrarEstado ?? true,
      mostrarEstimacion: configuracion?.mostrarEstimacion ?? true,
      mostrarResponsable: configuracion?.mostrarResponsable ?? true,
    });
  }
}
