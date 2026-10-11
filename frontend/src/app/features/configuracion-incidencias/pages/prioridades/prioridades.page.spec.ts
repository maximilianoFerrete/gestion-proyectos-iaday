import { signal } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { of } from 'rxjs';
import { PERMISOS } from '../../../../core/models/permisos.model';
import { UsuarioActualService } from '../../../../core/services/usuario-actual.service';
import { PrioridadIncidenciaDto } from '../../models/prioridad-incidencia.model';
import { PrioridadesIncidenciaService } from '../../services/prioridades-incidencia.service';
import { PrioridadesPage } from './prioridades.page';

/** Prioridad de prueba. */
function prioridad(
  id: number,
  nombre: string,
  esPredeterminada: boolean,
  activo: boolean,
): PrioridadIncidenciaDto {
  return {
    id,
    nombre,
    descripcion: null,
    orden: id * 10,
    esPredeterminada,
    activo,
    cantidadIncidencias: 0,
  };
}

const PRIORIDADES = [
  prioridad(1, 'Baja', false, true),
  prioridad(2, 'Media', true, true),
  prioridad(3, 'Vieja', false, false),
];

/** Tests de PrioridadesPage (C5, RN-17, CA-02, CA-11). */
describe('PrioridadesPage', () => {
  const permisos = signal<ReadonlySet<string>>(new Set());

  /** Crea la página y la renderiza. */
  async function crear(): Promise<{
    raiz: HTMLElement;
    fixture: ComponentFixture<PrioridadesPage>;
  }> {
    const fixture = TestBed.createComponent(PrioridadesPage);
    await fixture.whenStable();
    return { raiz: fixture.nativeElement as HTMLElement, fixture };
  }

  /** Busca un control por aria-label. */
  function porEtiqueta(raiz: HTMLElement, etiqueta: string): Element | null {
    return raiz.querySelector(`[aria-label="${etiqueta}"]`);
  }

  beforeEach(() => {
    permisos.set(new Set());
    TestBed.configureTestingModule({
      imports: [PrioridadesPage],
      providers: [
        provideRouter([]),
        { provide: PrioridadesIncidenciaService, useValue: { listar: () => of(PRIORIDADES) } },
        {
          provide: UsuarioActualService,
          useValue: { tienePermiso: (p: string) => permisos().has(p) },
        },
      ],
    });
  });

  // RN-17, CA-11: la predeterminada no tiene Desactivar ni "Marcar como predeterminada"
  it('la predeterminada no ofrece Desactivar ni Marcar como predeterminada', async () => {
    permisos.set(new Set([PERMISOS.configuracionIncidencias]));

    const { raiz } = await crear();

    expect(porEtiqueta(raiz, 'Desactivar prioridad Media')).toBeNull();
    expect(porEtiqueta(raiz, 'Marcar como predeterminada la prioridad Media')).toBeNull();
    expect(porEtiqueta(raiz, 'Desactivar prioridad Baja')).not.toBeNull();
    expect(porEtiqueta(raiz, 'Marcar como predeterminada la prioridad Baja')).not.toBeNull();
  });

  // RN-17: una inactiva no ofrece "Marcar como predeterminada" pero sí Activar
  it('una inactiva no ofrece Marcar como predeterminada pero sí Activar', async () => {
    permisos.set(new Set([PERMISOS.configuracionIncidencias]));

    const { raiz } = await crear();

    expect(porEtiqueta(raiz, 'Marcar como predeterminada la prioridad Vieja')).toBeNull();
    expect(porEtiqueta(raiz, 'Activar prioridad Vieja')).not.toBeNull();
  });

  // CA-02, RN-01: sin permiso no hay acciones de alta ni edición
  it('sin permiso no muestra Nueva prioridad ni acciones por fila', async () => {
    const { raiz } = await crear();

    expect(raiz.textContent).toContain('Media');
    expect(raiz.textContent).not.toContain('Nueva prioridad');
    expect(raiz.querySelectorAll('tbody button, tbody a').length).toBe(0);
  });
});
