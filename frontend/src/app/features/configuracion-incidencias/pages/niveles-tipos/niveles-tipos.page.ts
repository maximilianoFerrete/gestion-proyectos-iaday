import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  OnInit,
  computed,
  inject,
  signal,
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Observable, finalize, forkJoin } from 'rxjs';
import { PERMISOS } from '../../../../core/models/permisos.model';
import { NotificacionService } from '../../../../core/services/notificacion.service';
import { UsuarioActualService } from '../../../../core/services/usuario-actual.service';
import {
  ErrorFormulario,
  SIN_ERRORES,
  interpretarErrorFormulario,
  mensajeErrorAccion,
} from '../../../../shared/forms/errores-servidor';
import { NivelFormDialogComponent } from '../../components/nivel-form-dialog/nivel-form-dialog.component';
import { NivelesTiposComponent } from '../../components/niveles-tipos/niveles-tipos.component';
import {
  CrearNivelIncidenciaRequest,
  NivelIncidenciaDto,
} from '../../models/nivel-incidencia.model';
import { TipoIncidenciaDto } from '../../models/tipo-incidencia.model';
import { EjecutorAccionesService } from '../../services/ejecutor-acciones.service';
import { NivelesIncidenciaService } from '../../services/niveles-incidencia.service';
import { TiposIncidenciaService } from '../../services/tipos-incidencia.service';
import { agruparTiposPorNivel, mensajeDesactivarTipo } from '../../utils/reglas-configuracion';

/**
 * C1 – Niveles y tipos (`/configuracion/incidencias/tipos`). Consultable por todos; alta y
 * edición de niveles (diálogo) y tipos, y activar/desactivar, solo con permiso (RN-01).
 *
 * Creado: 2026-10-09
 * Modificado: 2026-10-09 - Creación inicial (RF-002)
 */
@Component({
  selector: 'app-niveles-tipos-page',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [NivelesTiposComponent, NivelFormDialogComponent],
  template: `
    <header class="encabezado-pagina">
      <h1>Niveles y tipos de incidencia</h1>
      @if (puedeEditar()) {
        <button type="button" class="btn btn-primario" (click)="abrirNivel(null)">
          Nuevo nivel
        </button>
      }
    </header>
    @if (error(); as mensaje) {
      <p class="alerta alerta-error" role="alert">{{ mensaje }}</p>
    }
    @if (cargado()) {
      <app-niveles-tipos
        [bloques]="bloques()"
        [puedeEditar]="puedeEditar()"
        [deshabilitado]="procesando()"
        (editarNivel)="abrirNivel($event)"
        (desactivar)="desactivarTipo($event)"
        (activar)="activarTipo($event)"
      />
    } @else if (!error()) {
      <p>Cargando...</p>
    }
    <app-nivel-form-dialog
      [abierto]="dialogoAbierto()"
      [nivel]="nivelEditado()"
      [enviando]="enviando()"
      [errorServidor]="errorNivel()"
      (guardar)="guardarNivel($event)"
      (cancelar)="cerrarNivel()"
    />
  `,
})
export class NivelesTiposPage implements OnInit {
  private readonly nivelesService = inject(NivelesIncidenciaService);
  private readonly tiposService = inject(TiposIncidenciaService);
  private readonly ejecutor = inject(EjecutorAccionesService);
  private readonly notificacion = inject(NotificacionService);
  private readonly usuarioActual = inject(UsuarioActualService);
  private readonly destroyRef = inject(DestroyRef);

  readonly puedeEditar = computed(() =>
    this.usuarioActual.tienePermiso(PERMISOS.configuracionIncidencias),
  );
  readonly niveles = signal<readonly NivelIncidenciaDto[]>([]);
  readonly tipos = signal<readonly TipoIncidenciaDto[]>([]);
  readonly bloques = computed(() => agruparTiposPorNivel(this.niveles(), this.tipos()));
  readonly cargado = signal(false);
  readonly error = signal<string | null>(null);
  readonly procesando = signal(false);
  // Diálogo de nivel.
  readonly dialogoAbierto = signal(false);
  readonly nivelEditado = signal<NivelIncidenciaDto | null>(null);
  readonly enviando = signal(false);
  readonly errorNivel = signal<ErrorFormulario>(SIN_ERRORES);

  ngOnInit(): void {
    this.cargar();
  }

  /** Abre el diálogo de nivel (null = alta). */
  abrirNivel(nivel: NivelIncidenciaDto | null): void {
    this.nivelEditado.set(nivel);
    this.errorNivel.set(SIN_ERRORES);
    this.enviando.set(false);
    this.dialogoAbierto.set(true);
  }

  /** Cierra el diálogo de nivel. */
  cerrarNivel(): void {
    this.dialogoAbierto.set(false);
  }

  /** POST o PUT del nivel; éxito → toast, cierre y recarga. */
  guardarNivel(request: CrearNivelIncidenciaRequest): void {
    const nivel = this.nivelEditado();
    this.enviando.set(true);
    this.errorNivel.set(SIN_ERRORES);
    const envio$: Observable<NivelIncidenciaDto> = nivel
      ? this.nivelesService.editar(nivel.id, {
          nombre: request.nombre,
          descripcion: request.descripcion,
        })
      : this.nivelesService.crear(request);
    envio$.pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: () => {
        this.enviando.set(false);
        this.dialogoAbierto.set(false);
        this.notificacion.exito('Nivel guardado.');
        this.cargar();
      },
      error: (error: unknown) => {
        this.enviando.set(false);
        this.errorNivel.set(
          interpretarErrorFormulario(error, {
            NIVEL_NUMERO_DUPLICADO: 'numero',
            NIVEL_NOMBRE_DUPLICADO: 'nombre',
          }),
        );
      },
    });
  }

  /** Confirma y desactiva el tipo (RN-07). */
  desactivarTipo(tipo: TipoIncidenciaDto): void {
    this.procesando.set(true);
    this.ejecutor
      .confirmarYEjecutar(
        {
          titulo: 'Desactivar tipo',
          mensaje: mensajeDesactivarTipo(tipo),
          textoConfirmar: 'Desactivar',
          peligro: true,
        },
        () => this.tiposService.desactivar(tipo.id),
        'Tipo desactivado.',
      )
      .pipe(
        finalize(() => this.procesando.set(false)),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe(() => this.cargar());
  }

  /** Reactiva el tipo. */
  activarTipo(tipo: TipoIncidenciaDto): void {
    this.procesando.set(true);
    this.ejecutor
      .ejecutar(this.tiposService.activar(tipo.id), 'Tipo activado.')
      .pipe(
        finalize(() => this.procesando.set(false)),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe(() => this.cargar());
  }

  /** Carga niveles y todos los tipos. */
  private cargar(): void {
    forkJoin({ niveles: this.nivelesService.listar(), tipos: this.tiposService.listar() })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: ({ niveles, tipos }) => {
          this.error.set(null);
          this.niveles.set(niveles);
          this.tipos.set(tipos);
          this.cargado.set(true);
        },
        error: (error: unknown) =>
          this.error.set(mensajeErrorAccion(error) ?? 'No se pudieron cargar los niveles y tipos.'),
      });
  }
}
