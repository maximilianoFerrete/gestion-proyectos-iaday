import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { of } from 'rxjs';
import { PERMISOS } from '../../../../core/models/permisos.model';
import { UsuarioActualService } from '../../../../core/services/usuario-actual.service';
import { FlujoIncidenciaDto } from '../../models/flujo-incidencia.model';
import { FlujosIncidenciaService } from '../../services/flujos-incidencia.service';
import { FlujosPage } from './flujos.page';

const GENERAL: FlujoIncidenciaDto = {
  id: 1,
  nombre: 'Flujo general',
  descripcion: null,
  estados: [
    { estadoId: 1, nombre: 'Por hacer', activo: true, orden: 1, esInicial: true, esFinal: false },
    { estadoId: 3, nombre: 'Hecho', activo: true, orden: 2, esInicial: false, esFinal: true },
  ],
  tipos: [{ id: 1, nombre: 'Épica', activo: true }],
  fechaCreacion: '2026-10-09T13:00:00Z',
  fechaModificacion: null,
};

/** Tests de FlujosPage (C4, RN-01, CA-02). */
describe('FlujosPage', () => {
  const permisos = signal<ReadonlySet<string>>(new Set());

  /** Crea la página y la renderiza. */
  async function crear(): Promise<HTMLElement> {
    const fixture = TestBed.createComponent(FlujosPage);
    await fixture.whenStable();
    return fixture.nativeElement as HTMLElement;
  }

  beforeEach(() => {
    permisos.set(new Set());
    TestBed.configureTestingModule({
      imports: [FlujosPage],
      providers: [
        provideRouter([]),
        { provide: FlujosIncidenciaService, useValue: { listar: () => of([GENERAL]) } },
        {
          provide: UsuarioActualService,
          useValue: { tienePermiso: (p: string) => permisos().has(p) },
        },
      ],
    });
  });

  // CA-02, RN-01: sin permiso no hay Nuevo flujo ni Editar
  it('sin permiso muestra los flujos sin Nuevo flujo ni Editar', async () => {
    const raiz = await crear();

    expect(raiz.textContent).toContain('Flujo general');
    expect(raiz.textContent).not.toContain('Nuevo flujo');
    expect(raiz.querySelector('[aria-label="Editar flujo Flujo general"]')).toBeNull();
  });

  // RN-01: con permiso se ofrecen Nuevo flujo y Editar
  it('con permiso muestra Nuevo flujo y Editar', async () => {
    permisos.set(new Set([PERMISOS.configuracionIncidencias]));

    const raiz = await crear();

    expect(raiz.textContent).toContain('Nuevo flujo');
    expect(raiz.querySelector('[aria-label="Editar flujo Flujo general"]')).not.toBeNull();
  });
});
