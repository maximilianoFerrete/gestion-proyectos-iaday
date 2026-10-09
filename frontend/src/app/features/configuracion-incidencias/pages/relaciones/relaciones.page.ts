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
import { FormsModule } from '@angular/forms';
import { filter, forkJoin, switchMap } from 'rxjs';
import { PERMISOS } from '../../../../core/models/permisos.model';
import { ConfirmacionService } from '../../../../core/services/confirmacion.service';
import { NotificacionService } from '../../../../core/services/notificacion.service';
import { UsuarioActualService } from '../../../../core/services/usuario-actual.service';
import {
  ErrorFormulario,
  SIN_ERRORES,
  interpretarErrorFormulario,
  mensajeErrorAccion,
} from '../../../../shared/forms/errores-servidor';
import { RelacionFormComponent } from '../../components/relacion-form/relacion-form.component';
import { RelacionesTablaComponent } from '../../components/relaciones-tabla/relaciones-tabla.component';
import {
  CrearRelacionTipoIncidenciaRequest,
  RelacionTipoIncidenciaDto,
} from '../../models/relacion-tipo-incidencia.model';
import { TipoIncidenciaDto } from '../../models/tipo-incidencia.model';
import { RelacionesTiposIncidenciaService } from '../../services/relaciones-tipos-incidencia.service';
import { TiposIncidenciaService } from '../../services/tipos-incidencia.service';
import { filtrarRelaciones, mensajeCrearRelacion } from '../../utils/reglas-configuracion';

/**
 * C2 – Relaciones permitidas entre tipos (`/configuracion/incidencias/relaciones`). Tabla con
 * filtros por tipo padre / tipo hijo y "Nueva relación" (con permiso). Sin editar ni eliminar.
 *
 * Creado: 2026-10-09
 * Modificado: 2026-10-09 - Creación inicial (RF-002)
 */
@Component({
  selector: 'app-relaciones-page',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [FormsModule, RelacionFormComponent, RelacionesTablaComponent],
  template: `
    <header class="encabezado-pagina">
      <h1>Relaciones de tipos</h1>
      @if (puedeEditar() && !formularioAbierto()) {
        <button type="button" class="btn btn-primario" (click)="abrirFormulario()">
          Nueva relación
        </button>
      }
    </header>
    <p class="ayuda">Las relaciones no se pueden modificar ni eliminar una vez creadas.</p>

    @if (formularioAbierto()) {
      <section class="panel" aria-labelledby="titulo-nueva-relacion">
        <h2 id="titulo-nueva-relacion">Nueva relación</h2>
        <app-relacion-form
          [tipos]="tipos()"
          [relaciones]="relaciones()"
          [enviando]="enviando()"
          [errorServidor]="errorServidor()"
          (guardar)="crear($event)"
          (cancelar)="formularioAbierto.set(false)"
        />
      </section>
    }

    @if (error(); as mensaje) {
      <p class="alerta alerta-error" role="alert">{{ mensaje }}</p>
    }

    <div class="filtros" role="search">
      <div class="campo">
        <label for="filtro-relacion-padre">Tipo padre</label>
        <select
          id="filtro-relacion-padre"
          [ngModel]="filtroPadre()"
          (ngModelChange)="filtroPadre.set($event)"
        >
          <option [ngValue]="null">Todos</option>
          @for (tipo of tipos(); track tipo.id) {
            <option [ngValue]="tipo.id">{{ tipo.nombre }}</option>
          }
        </select>
      </div>
      <div class="campo">
        <label for="filtro-relacion-hijo">Tipo hijo</label>
        <select
          id="filtro-relacion-hijo"
          [ngModel]="filtroHijo()"
          (ngModelChange)="filtroHijo.set($event)"
        >
          <option [ngValue]="null">Todos</option>
          @for (tipo of tipos(); track tipo.id) {
            <option [ngValue]="tipo.id">{{ tipo.nombre }}</option>
          }
        </select>
      </div>
    </div>

    <section class="panel" [attr.aria-busy]="!cargado()">
      @if (cargado()) {
        <app-relaciones-tabla [relaciones]="relacionesFiltradas()" />
      } @else if (!error()) {
        <p>Cargando...</p>
      }
    </section>
  `,
})
export class RelacionesPage implements OnInit {
  private readonly relacionesService = inject(RelacionesTiposIncidenciaService);
  private readonly tiposService = inject(TiposIncidenciaService);
  private readonly confirmacion = inject(ConfirmacionService);
  private readonly notificacion = inject(NotificacionService);
  private readonly usuarioActual = inject(UsuarioActualService);
  private readonly destroyRef = inject(DestroyRef);

  readonly puedeEditar = computed(() =>
    this.usuarioActual.tienePermiso(PERMISOS.configuracionIncidencias),
  );
  readonly relaciones = signal<readonly RelacionTipoIncidenciaDto[]>([]);
  readonly tipos = signal<readonly TipoIncidenciaDto[]>([]);
  readonly filtroPadre = signal<number | null>(null);
  readonly filtroHijo = signal<number | null>(null);
  readonly relacionesFiltradas = computed(() =>
    filtrarRelaciones(this.relaciones(), {
      tipoPadreId: this.filtroPadre(),
      tipoHijoId: this.filtroHijo(),
    }),
  );
  readonly cargado = signal(false);
  readonly error = signal<string | null>(null);
  readonly formularioAbierto = signal(false);
  readonly enviando = signal(false);
  readonly errorServidor = signal<ErrorFormulario>(SIN_ERRORES);

  ngOnInit(): void {
    this.cargar();
  }

  /** Muestra el formulario "Nueva relación". */
  abrirFormulario(): void {
    this.errorServidor.set(SIN_ERRORES);
    this.formularioAbierto.set(true);
  }

  /** Confirma ("no podrá modificarse ni eliminarse") y crea la relación. */
  crear(request: CrearRelacionTipoIncidenciaRequest): void {
    const padre = this.nombreTipo(request.tipoPadreId);
    const hijo = this.nombreTipo(request.tipoHijoId);
    this.confirmacion
      .confirmar({
        titulo: 'Nueva relación',
        mensaje: mensajeCrearRelacion(padre, hijo),
        textoConfirmar: 'Crear relación',
      })
      .pipe(
        filter((confirmado) => confirmado),
        switchMap(() => {
          this.enviando.set(true);
          this.errorServidor.set(SIN_ERRORES);
          return this.relacionesService.crear(request);
        }),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({
        next: () => {
          this.enviando.set(false);
          this.formularioAbierto.set(false);
          this.notificacion.exito('Relación creada.');
          this.cargar();
        },
        error: (error: unknown) => {
          this.enviando.set(false);
          this.errorServidor.set(interpretarErrorFormulario(error));
        },
      });
  }

  /** Nombre de un tipo por id (para la confirmación). */
  private nombreTipo(id: number): string {
    return this.tipos().find((t) => t.id === id)?.nombre ?? String(id);
  }

  /** Carga todas las relaciones y todos los tipos (los filtros se aplican en memoria). */
  private cargar(): void {
    forkJoin({ relaciones: this.relacionesService.listar(), tipos: this.tiposService.listar() })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: ({ relaciones, tipos }) => {
          this.error.set(null);
          this.relaciones.set(relaciones);
          this.tipos.set(tipos);
          this.cargado.set(true);
        },
        error: (error: unknown) =>
          this.error.set(mensajeErrorAccion(error) ?? 'No se pudieron cargar las relaciones.'),
      });
  }
}
