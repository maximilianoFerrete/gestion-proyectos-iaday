import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { RouterLink } from '@angular/router';
import { NivelIncidenciaDto } from '../../models/nivel-incidencia.model';
import { TipoIncidenciaDto } from '../../models/tipo-incidencia.model';
import { BloqueNivel } from '../../utils/reglas-configuracion';

/**
 * Bloques "Nivel N – Nombre" con sus tipos (C1). Las acciones solo se muestran con permiso.
 *
 * Creado: 2026-10-09
 * Modificado: 2026-10-09 - Creación inicial (RF-002)
 */
@Component({
  selector: 'app-niveles-tipos',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterLink],
  template: `
    @if (bloques().length === 0) {
      <p class="vacio">No hay niveles configurados.</p>
    }
    @for (bloque of bloques(); track bloque.nivel.id) {
      <section class="panel bloque-nivel" [attr.aria-labelledby]="'nivel-' + bloque.nivel.id">
        <header class="encabezado-seccion">
          <div>
            <h2 [id]="'nivel-' + bloque.nivel.id">
              Nivel {{ bloque.nivel.numero }} – {{ bloque.nivel.nombre }}
            </h2>
            @if (bloque.nivel.descripcion) {
              <p class="texto-secundario">{{ bloque.nivel.descripcion }}</p>
            }
          </div>
          @if (puedeEditar()) {
            <div class="acciones-fila">
              <button
                type="button"
                class="btn btn-chico"
                [attr.aria-label]="'Editar nivel ' + bloque.nivel.nombre"
                (click)="editarNivel.emit(bloque.nivel)"
              >
                Editar nivel
              </button>
              <a
                class="btn btn-chico btn-primario"
                routerLink="/configuracion/incidencias/tipos/nuevo"
                [queryParams]="{ nivelId: bloque.nivel.id }"
                [attr.aria-label]="'Nuevo tipo en ' + bloque.nivel.nombre"
              >
                Nuevo tipo
              </a>
            </div>
          }
        </header>
        @if (bloque.tipos.length === 0) {
          <p class="vacio">El nivel no tiene tipos.</p>
        } @else {
          <table class="tabla">
            <caption class="sr-only">
              Tipos del nivel
              {{
                bloque.nivel.numero
              }}
            </caption>
            <thead>
              <tr>
                <th scope="col">Nombre</th>
                <th scope="col">Descripción</th>
                <th scope="col">Flujo</th>
                <th scope="col">Activo</th>
                <th scope="col">Incidencias</th>
                @if (puedeEditar()) {
                  <th scope="col"><span class="sr-only">Acciones</span></th>
                }
              </tr>
            </thead>
            <tbody>
              @for (tipo of bloque.tipos; track tipo.id) {
                <tr [class.fila-inactiva]="!tipo.activo">
                  <td>{{ tipo.nombre }}</td>
                  <td>{{ tipo.descripcion ?? '' }}</td>
                  <td>{{ tipo.flujo.nombre }}</td>
                  <td>{{ tipo.activo ? 'Sí' : 'No' }}</td>
                  <td>{{ tipo.cantidadIncidencias }}</td>
                  @if (puedeEditar()) {
                    <td class="acciones-fila">
                      <a
                        class="btn btn-chico"
                        [routerLink]="['/configuracion/incidencias/tipos', tipo.id, 'editar']"
                        [attr.aria-label]="'Editar tipo ' + tipo.nombre"
                      >
                        Editar tipo
                      </a>
                      @if (tipo.activo) {
                        <button
                          type="button"
                          class="btn btn-chico btn-peligro"
                          [disabled]="deshabilitado()"
                          [attr.aria-label]="'Desactivar tipo ' + tipo.nombre"
                          (click)="desactivar.emit(tipo)"
                        >
                          Desactivar
                        </button>
                      } @else {
                        <button
                          type="button"
                          class="btn btn-chico"
                          [disabled]="deshabilitado()"
                          [attr.aria-label]="'Activar tipo ' + tipo.nombre"
                          (click)="activar.emit(tipo)"
                        >
                          Activar
                        </button>
                      }
                    </td>
                  }
                </tr>
              }
            </tbody>
          </table>
        }
      </section>
    }
  `,
})
export class NivelesTiposComponent {
  readonly bloques = input.required<readonly BloqueNivel[]>();
  /** Con permiso `Configuracion.Incidencias`. */
  readonly puedeEditar = input(false);
  readonly deshabilitado = input(false);
  readonly editarNivel = output<NivelIncidenciaDto>();
  readonly desactivar = output<TipoIncidenciaDto>();
  readonly activar = output<TipoIncidenciaDto>();
}
