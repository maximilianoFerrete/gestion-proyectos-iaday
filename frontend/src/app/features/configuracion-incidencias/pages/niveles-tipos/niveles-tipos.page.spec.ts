import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { of } from 'rxjs';
import { PERMISOS } from '../../../../core/models/permisos.model';
import { UsuarioActualService } from '../../../../core/services/usuario-actual.service';
import { NivelIncidenciaDto } from '../../models/nivel-incidencia.model';
import { TipoIncidenciaDto } from '../../models/tipo-incidencia.model';
import { NivelesIncidenciaService } from '../../services/niveles-incidencia.service';
import { TiposIncidenciaService } from '../../services/tipos-incidencia.service';
import { NivelesTiposPage } from './niveles-tipos.page';

const NIVEL: NivelIncidenciaDto = {
  id: 2,
  numero: 2,
  nombre: 'Nivel 2',
  descripcion: null,
  cantidadTipos: 1,
  fechaCreacion: '2026-10-09T13:00:00Z',
  fechaModificacion: null,
};

const HU: TipoIncidenciaDto = {
  id: 2,
  nombre: 'Historia de Usuario',
  descripcion: null,
  nivel: { id: 2, numero: 2, nombre: 'Nivel 2' },
  flujo: { id: 1, nombre: 'Flujo general' },
  activo: true,
  cantidadIncidencias: 12,
  fechaCreacion: '2026-10-09T13:00:00Z',
  fechaModificacion: null,
};

/** Tests de NivelesTiposPage (C1, RN-01, CA-02). */
describe('NivelesTiposPage', () => {
  const permisos = signal<ReadonlySet<string>>(new Set());

  /** Textos de los botones y enlaces visibles (acciones). */
  function acciones(raiz: HTMLElement): string[] {
    return Array.from(raiz.querySelectorAll('button, a')).map((e) => e.textContent?.trim() ?? '');
  }

  /** Crea la página y la renderiza. */
  async function crear(): Promise<HTMLElement> {
    const fixture = TestBed.createComponent(NivelesTiposPage);
    await fixture.whenStable();
    return fixture.nativeElement as HTMLElement;
  }

  beforeEach(() => {
    permisos.set(new Set());
    TestBed.configureTestingModule({
      imports: [NivelesTiposPage],
      providers: [
        provideRouter([]),
        { provide: NivelesIncidenciaService, useValue: { listar: () => of([NIVEL]) } },
        { provide: TiposIncidenciaService, useValue: { listar: () => of([HU]) } },
        {
          provide: UsuarioActualService,
          useValue: { tienePermiso: (p: string) => permisos().has(p) },
        },
      ],
    });
  });

  // CA-02, RN-01: sin permiso se consulta pero no hay acciones de alta ni edición
  it('sin permiso muestra niveles y tipos sin acciones de alta ni edición', async () => {
    const raiz = await crear();

    expect(raiz.textContent).toContain('Historia de Usuario');
    expect(acciones(raiz)).not.toContain('Nuevo nivel');
    expect(acciones(raiz)).not.toContain('Nuevo tipo');
    expect(acciones(raiz)).not.toContain('Editar nivel');
    expect(acciones(raiz)).not.toContain('Desactivar');
  });

  // RN-01: con permiso se ofrecen las acciones
  it('con permiso muestra Nuevo nivel, Editar nivel y Nuevo tipo', async () => {
    permisos.set(new Set([PERMISOS.configuracionIncidencias]));

    const raiz = await crear();

    expect(acciones(raiz)).toEqual(
      expect.arrayContaining(['Nuevo nivel', 'Editar nivel', 'Nuevo tipo']),
    );
  });
});
