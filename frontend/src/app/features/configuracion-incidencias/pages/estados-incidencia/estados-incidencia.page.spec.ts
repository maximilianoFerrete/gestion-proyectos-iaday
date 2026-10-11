import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { of } from 'rxjs';
import { PERMISOS } from '../../../../core/models/permisos.model';
import { UsuarioActualService } from '../../../../core/services/usuario-actual.service';
import { EstadoIncidenciaDto } from '../../models/estado-incidencia.model';
import { EstadosIncidenciaService } from '../../services/estados-incidencia.service';
import { EstadosIncidenciaPage } from './estados-incidencia.page';

/** Estado de prueba con su marca de inicial en el flujo general. */
function estado(
  id: number,
  nombre: string,
  esInicial: boolean,
  activo = true,
): EstadoIncidenciaDto {
  return {
    id,
    nombre,
    descripcion: null,
    orden: id * 10,
    activo,
    cantidadIncidencias: 0,
    flujos: [{ id: 1, nombre: 'Flujo general', esInicial, esFinal: false }],
  };
}

const ESTADOS = [
  estado(1, 'Por hacer', true),
  estado(2, 'En progreso', false),
  estado(4, 'Viejo', false, false),
];

/** Tests de EstadosIncidenciaPage (C3, RN-11, CA-02, CA-10). */
describe('EstadosIncidenciaPage', () => {
  const permisos = signal<ReadonlySet<string>>(new Set());

  /** Crea la página y la renderiza. */
  async function crear(): Promise<HTMLElement> {
    const fixture = TestBed.createComponent(EstadosIncidenciaPage);
    await fixture.whenStable();
    return fixture.nativeElement as HTMLElement;
  }

  beforeEach(() => {
    permisos.set(new Set());
    TestBed.configureTestingModule({
      imports: [EstadosIncidenciaPage],
      providers: [
        provideRouter([]),
        { provide: EstadosIncidenciaService, useValue: { listar: () => of(ESTADOS) } },
        {
          provide: UsuarioActualService,
          useValue: { tienePermiso: (p: string) => permisos().has(p) },
        },
      ],
    });
  });

  // RN-11, CA-10: sin Desactivar en el estado inicial de algún flujo
  it('el estado inicial de un flujo no ofrece Desactivar; los demás activos sí', async () => {
    permisos.set(new Set([PERMISOS.configuracionIncidencias]));

    const raiz = await crear();

    expect(raiz.querySelector('[aria-label="Desactivar estado Por hacer"]')).toBeNull();
    expect(raiz.querySelector('[aria-label="Desactivar estado En progreso"]')).not.toBeNull();
    expect(raiz.querySelector('[aria-label="Activar estado Viejo"]')).not.toBeNull();
  });

  // CA-02, RN-01: sin permiso no hay acciones de alta ni edición
  it('sin permiso no muestra Nuevo estado ni acciones por fila', async () => {
    const raiz = await crear();

    expect(raiz.textContent).toContain('Por hacer');
    expect(raiz.textContent).not.toContain('Nuevo estado');
    expect(raiz.querySelectorAll('tbody button, tbody a').length).toBe(0);
  });
});
