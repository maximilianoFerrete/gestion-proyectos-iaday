import { signal } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { of } from 'rxjs';
import { PERMISOS } from '../../../../core/models/permisos.model';
import { ConfirmacionService } from '../../../../core/services/confirmacion.service';
import { UsuarioActualService } from '../../../../core/services/usuario-actual.service';
import { RelacionTipoIncidenciaDto } from '../../models/relacion-tipo-incidencia.model';
import { TipoIncidenciaDto } from '../../models/tipo-incidencia.model';
import { RelacionesTiposIncidenciaService } from '../../services/relaciones-tipos-incidencia.service';
import { TiposIncidenciaService } from '../../services/tipos-incidencia.service';
import { RelacionesPage } from './relaciones.page';

/** Tipo de prueba en el nivel indicado. */
function tipo(id: number, nombre: string, nivel: number): TipoIncidenciaDto {
  return {
    id,
    nombre,
    descripcion: null,
    nivel: { id: nivel, numero: nivel, nombre: `Nivel ${nivel}` },
    flujo: { id: 1, nombre: 'Flujo general' },
    activo: true,
    cantidadIncidencias: 0,
    fechaCreacion: '2026-10-09T13:00:00Z',
    fechaModificacion: null,
  };
}

const EPICA = tipo(1, 'Épica', 1);
const HU = tipo(2, 'Historia de Usuario', 2);
const SUBTAREA = tipo(3, 'Subtarea', 3);
const BUG = tipo(5, 'Bug', 3);
const SPIKE = tipo(6, 'Spike', 3);

/** Relación existente padre → hijo. */
function relacion(padre: TipoIncidenciaDto, hijo: TipoIncidenciaDto): RelacionTipoIncidenciaDto {
  return {
    id: padre.id * 10 + hijo.id,
    tipoPadre: {
      id: padre.id,
      nombre: padre.nombre,
      activo: true,
      nivelNumero: padre.nivel.numero,
    },
    tipoHijo: { id: hijo.id, nombre: hijo.nombre, activo: true, nivelNumero: hijo.nivel.numero },
    fechaCreacion: '2026-10-09T13:00:00Z',
  };
}

/** Tests de RelacionesPage (C2, CA-02, CA-05, CA-06). */
describe('RelacionesPage', () => {
  let fixture: ComponentFixture<RelacionesPage>;
  const relaciones = { listar: vi.fn(), crear: vi.fn() };
  const permisos = signal<ReadonlySet<string>>(new Set());

  /** Crea la página y la renderiza. */
  async function crear(): Promise<HTMLElement> {
    fixture = TestBed.createComponent(RelacionesPage);
    await fixture.whenStable();
    return fixture.nativeElement as HTMLElement;
  }

  /** Busca un botón o enlace por su texto visible. */
  function accion(raiz: HTMLElement, texto: string): HTMLElement | undefined {
    return Array.from(raiz.querySelectorAll<HTMLElement>('a, button')).find(
      (e) => e.textContent?.trim() === texto,
    );
  }

  /** Textos de las opciones habilitadas de un select. */
  function opciones(raiz: HTMLElement, id: string): string[] {
    return Array.from(
      raiz.querySelectorAll<HTMLOptionElement>(`#${id} option:not([disabled])`),
    ).map((o) => o.textContent?.trim() ?? '');
  }

  beforeEach(() => {
    vi.clearAllMocks();
    permisos.set(new Set());
    relaciones.listar.mockReturnValue(of([relacion(EPICA, HU), relacion(HU, SUBTAREA)]));
    TestBed.configureTestingModule({
      imports: [RelacionesPage],
      providers: [
        provideRouter([]),
        { provide: RelacionesTiposIncidenciaService, useValue: relaciones },
        {
          provide: TiposIncidenciaService,
          useValue: { listar: () => of([EPICA, HU, SUBTAREA, BUG, SPIKE]) },
        },
        {
          provide: UsuarioActualService,
          useValue: { tienePermiso: (p: string) => permisos().has(p) },
        },
      ],
    });
  });

  // RN-09, CA-06: sin acciones de editar ni eliminar y con la leyenda
  it('no ofrece editar ni eliminar relaciones y muestra la leyenda', async () => {
    permisos.set(new Set([PERMISOS.configuracionIncidencias]));

    const raiz = await crear();

    expect(raiz.textContent).toContain(
      'Las relaciones no se pueden modificar ni eliminar una vez creadas.',
    );
    expect(raiz.textContent).toContain('Historia de Usuario (2)');
    expect(accion(raiz, 'Editar')).toBeUndefined();
    expect(accion(raiz, 'Eliminar')).toBeUndefined();
  });

  // CA-02, RN-01: sin permiso no hay "Nueva relación"
  it('sin permiso de configuración no muestra Nueva relación', async () => {
    const raiz = await crear();

    expect(accion(raiz, 'Nueva relación')).toBeUndefined();
  });

  // RN-08, CA-05: el select de hijo solo ofrece niveles mayores sin relación existente
  it('el select de tipo hijo ofrece solo tipos de nivel mayor sin relación existente', async () => {
    permisos.set(new Set([PERMISOS.configuracionIncidencias]));
    const raiz = await crear();
    accion(raiz, 'Nueva relación')?.click();
    await fixture.whenStable();

    const padre = raiz.querySelector<HTMLSelectElement>('#relacion-padre')!;
    padre.value = Array.from(padre.options).find((o) =>
      o.textContent?.includes('Historia de Usuario'),
    )!.value;
    padre.dispatchEvent(new Event('change'));
    await fixture.whenStable();

    expect(opciones(raiz, 'relacion-hijo')).toEqual(['Bug (nivel 3)', 'Spike (nivel 3)']);
  });

  // RN-09, C2: la creación pide confirmar que la relación será inmutable
  it('crear pide confirmación con el aviso de inmutabilidad y luego llama a crear', async () => {
    permisos.set(new Set([PERMISOS.configuracionIncidencias]));
    relaciones.crear.mockReturnValue(of(relacion(HU, SPIKE)));
    await crear();
    const confirmacion = TestBed.inject(ConfirmacionService);

    fixture.componentInstance.crear({ tipoPadreId: HU.id, tipoHijoId: SPIKE.id });
    expect(confirmacion.solicitud()?.mensaje).toBe(
      'La relación Historia de Usuario → Spike no podrá modificarse ni eliminarse.',
    );
    confirmacion.responder(true);
    await fixture.whenStable();

    expect(relaciones.crear).toHaveBeenCalledWith({ tipoPadreId: HU.id, tipoHijoId: SPIKE.id });
  });
});
