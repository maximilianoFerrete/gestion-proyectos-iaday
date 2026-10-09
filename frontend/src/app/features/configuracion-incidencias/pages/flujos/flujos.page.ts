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
import { RouterLink } from '@angular/router';
import { PERMISOS } from '../../../../core/models/permisos.model';
import { UsuarioActualService } from '../../../../core/services/usuario-actual.service';
import { mensajeErrorAccion } from '../../../../shared/forms/errores-servidor';
import { FlujosTablaComponent } from '../../components/flujos-tabla/flujos-tabla.component';
import { FlujoIncidenciaDto } from '../../models/flujo-incidencia.model';
import { FlujosIncidenciaService } from '../../services/flujos-incidencia.service';

/**
 * C4 – Flujos de trabajo (`/configuracion/incidencias/flujos`). Consultable por todos; Nuevo y
 * Editar solo con permiso (RN-01). Los flujos no se eliminan (DA-14).
 *
 * Creado: 2026-10-09
 * Modificado: 2026-10-09 - Creación inicial (RF-002)
 */
@Component({
  selector: 'app-flujos-page',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterLink, FlujosTablaComponent],
  template: `
    <header class="encabezado-pagina">
      <h1>Flujos de trabajo</h1>
      @if (puedeEditar()) {
        <a class="btn btn-primario" routerLink="/configuracion/incidencias/flujos/nuevo">
          Nuevo flujo
        </a>
      }
    </header>
    @if (error(); as mensaje) {
      <p class="alerta alerta-error" role="alert">{{ mensaje }}</p>
    }
    <section class="panel" [attr.aria-busy]="flujos() === null">
      @if (flujos(); as lista) {
        <app-flujos-tabla [flujos]="lista" [puedeEditar]="puedeEditar()" />
      } @else if (!error()) {
        <p>Cargando...</p>
      }
    </section>
  `,
})
export class FlujosPage implements OnInit {
  private readonly flujosService = inject(FlujosIncidenciaService);
  private readonly usuarioActual = inject(UsuarioActualService);
  private readonly destroyRef = inject(DestroyRef);

  readonly puedeEditar = computed(() =>
    this.usuarioActual.tienePermiso(PERMISOS.configuracionIncidencias),
  );
  readonly flujos = signal<readonly FlujoIncidenciaDto[] | null>(null);
  readonly error = signal<string | null>(null);

  ngOnInit(): void {
    this.flujosService
      .listar()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (flujos) => this.flujos.set(flujos),
        error: (error: unknown) =>
          this.error.set(mensajeErrorAccion(error) ?? 'No se pudieron cargar los flujos.'),
      });
  }
}
