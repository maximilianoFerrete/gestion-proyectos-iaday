import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Router, provideRouter } from '@angular/router';
import { of } from 'rxjs';
import { EstadosIncidenciaService } from '../../../configuracion-incidencias/services/estados-incidencia.service';
import { PrioridadesIncidenciaService } from '../../../configuracion-incidencias/services/prioridades-incidencia.service';
import { TiposIncidenciaService } from '../../../configuracion-incidencias/services/tipos-incidencia.service';
import { PersonasService } from '../../../personas/services/personas.service';
import { ProyectoDto } from '../../../proyectos/models/proyecto.model';
import { AgrupacionesService } from '../../../proyectos/services/agrupaciones.service';
import { ProyectosService } from '../../../proyectos/services/proyectos.service';
import { IncidenciasService } from '../../services/incidencias.service';
import { IncidenciasListadoPage } from './incidencias-listado.page';

const PROYECTO: ProyectoDto = {
  id: 1,
  codigo: 'PRJ-0001',
  nombre: 'Portal clientes',
  descripcion: 'Nuevo portal.',
  responsable: { id: 2, nombre: 'Ana Gómez', activo: true },
  fechaInicio: '2026-10-01',
  fechaObjetivo: '2026-12-15',
  estado: { id: 1, nombre: 'En curso', esFinal: false, activo: true, esSistema: false },
  archivado: false,
  fechaArchivado: null,
  archivadoPor: null,
  estadoAlArchivar: null,
  cantidadAgrupaciones: 1,
  cantidadIncidencias: 0,
  fechaCreacion: '2026-10-09T13:05:00Z',
  fechaModificacion: null,
};

/** Tests de IncidenciasListadoPage (I1, CA-19, CA-21, CA-22). */
describe('IncidenciasListadoPage', () => {
  let fixture: ComponentFixture<IncidenciasListadoPage>;
  const incidencias = { listar: vi.fn() };
  const proyectos = { obtener: vi.fn() };

  /** Crea la página con los query params indicados como inputs y la renderiza. */
  async function crear(
    proyecto: ProyectoDto,
    queryParams: Record<string, string> = {},
  ): Promise<HTMLElement> {
    proyectos.obtener.mockReturnValue(of(proyecto));
    fixture = TestBed.createComponent(IncidenciasListadoPage);
    fixture.componentRef.setInput('id', '1');
    for (const [clave, valor] of Object.entries(queryParams)) {
      fixture.componentRef.setInput(clave, valor);
    }
    await fixture.whenStable();
    return fixture.nativeElement as HTMLElement;
  }

  /** Elige en un select la opción con el texto indicado y dispara `change`. */
  function elegir(raiz: HTMLElement, id: string, texto: string): void {
    const select = raiz.querySelector<HTMLSelectElement>(`#${id}`)!;
    const opcion = Array.from(select.options).find((o) => o.textContent?.trim() === texto)!;
    select.value = opcion.value;
    select.dispatchEvent(new Event('change'));
  }

  beforeEach(() => {
    vi.clearAllMocks();
    incidencias.listar.mockReturnValue(of({ items: [], pagina: 1, tamanioPagina: 20, total: 0 }));
    TestBed.configureTestingModule({
      imports: [IncidenciasListadoPage],
      providers: [
        provideRouter([]),
        { provide: IncidenciasService, useValue: incidencias },
        { provide: ProyectosService, useValue: proyectos },
        {
          provide: AgrupacionesService,
          useValue: {
            listar: () =>
              of([
                {
                  id: 8,
                  proyectoId: 1,
                  nombre: 'Módulo pagos',
                  descripcion: 'Pagos.',
                  cantidadIncidencias: 0,
                  fechaCreacion: '2026-10-09T13:10:00Z',
                  fechaModificacion: null,
                },
              ]),
          },
        },
        { provide: TiposIncidenciaService, useValue: { listar: () => of([]) } },
        { provide: EstadosIncidenciaService, useValue: { listar: () => of([]) } },
        { provide: PrioridadesIncidenciaService, useValue: { listar: () => of([]) } },
        { provide: PersonasService, useValue: { listar: () => of([]) } },
      ],
    });
  });

  // CA-19: "Sin agrupación" → query param sinAgrupacion=true y vuelta a página 1
  it('elegir "Sin agrupación" navega con sinAgrupacion=true y sin página (vuelve a la 1)', async () => {
    const raiz = await crear(PROYECTO, { pagina: '3' });
    const navegar = vi.spyOn(TestBed.inject(Router), 'navigate').mockResolvedValue(true);

    elegir(raiz, 'filtro-inc-agrupacion', 'Sin agrupación');

    expect(navegar).toHaveBeenCalledTimes(1);
    const extras = navegar.mock.calls[0][1];
    expect(extras?.queryParams).toEqual({ sinAgrupacion: 'true' });
  });

  // CA-19: "Sin responsable" → sinResponsable=true
  it('elegir "Sin responsable" navega con sinResponsable=true', async () => {
    const raiz = await crear(PROYECTO);
    const navegar = vi.spyOn(TestBed.inject(Router), 'navigate').mockResolvedValue(true);

    elegir(raiz, 'filtro-inc-responsable', 'Sin responsable');

    expect(navegar.mock.calls[0][1]?.queryParams).toEqual({ sinResponsable: 'true' });
  });

  // CA-19: los query params de la URL llegan al servicio como filtros
  it('los query params sinAgrupacion y estadoId se envían al servicio', async () => {
    await crear(PROYECTO, { sinAgrupacion: 'true', estadoId: '2' });

    expect(incidencias.listar).toHaveBeenLastCalledWith(
      1,
      expect.objectContaining({ sinAgrupacion: true, estadoId: 2, pagina: 1 }),
    );
  });

  // CA-19: "Limpiar filtros" vuelve a los defaults
  it('Limpiar filtros navega sin query params', async () => {
    const raiz = await crear(PROYECTO, { sinAgrupacion: 'true', pagina: '2' });
    const navegar = vi.spyOn(TestBed.inject(Router), 'navigate').mockResolvedValue(true);

    Array.from(raiz.querySelectorAll<HTMLButtonElement>('button'))
      .find((b) => b.textContent?.trim() === 'Limpiar filtros')
      ?.click();

    expect(navegar.mock.calls[0][1]?.queryParams).toEqual({});
  });

  // CA-21, RN-36: proyecto archivado → banner y sin "Nueva incidencia"
  it('proyecto archivado muestra el banner y oculta Nueva incidencia', async () => {
    const raiz = await crear({ ...PROYECTO, archivado: true });

    expect(raiz.querySelector('.banner-archivado')?.textContent).toContain(
      'Proyecto archivado: solo lectura.',
    );
    expect(raiz.textContent).not.toContain('Nueva incidencia');
  });

  // I1: proyecto activo muestra "Nueva incidencia"; vacío con el mensaje del contrato
  it('proyecto activo sin resultados muestra Nueva incidencia y el mensaje de vacío', async () => {
    const raiz = await crear(PROYECTO);

    expect(raiz.textContent).toContain('Nueva incidencia');
    expect(raiz.textContent).toContain('No hay incidencias que coincidan con los filtros.');
  });

  // CA-22: no hay acción de eliminar incidencias
  it('no ofrece una acción Eliminar', async () => {
    const raiz = await crear(PROYECTO);

    const eliminar = Array.from(raiz.querySelectorAll('a, button')).find(
      (e) => e.textContent?.trim() === 'Eliminar',
    );
    expect(eliminar).toBeUndefined();
  });
});
