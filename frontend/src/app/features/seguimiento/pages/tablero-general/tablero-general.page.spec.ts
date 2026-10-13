import { ComponentFixture, TestBed } from '@angular/core/testing';
import { By } from '@angular/platform-browser';
import { Router, provideRouter } from '@angular/router';
import { of } from 'rxjs';
import { UsuarioActualService } from '../../../../core/services/usuario-actual.service';
import { EstadosIncidenciaService } from '../../../configuracion-incidencias/services/estados-incidencia.service';
import { NivelesIncidenciaService } from '../../../configuracion-incidencias/services/niveles-incidencia.service';
import { TiposIncidenciaService } from '../../../configuracion-incidencias/services/tipos-incidencia.service';
import { IndicadoresComponent } from '../../components/indicadores/indicadores.component';
import { IndicadoresDto, TableroProyectosDto } from '../../models/seguimiento.model';
import { SeguimientoService } from '../../services/seguimiento.service';
import { TableroGeneralPage } from './tablero-general.page';

const INDICADORES: IndicadoresDto = {
  total: 42,
  abiertas: 30,
  finalizadas: 12,
  vencidas: 5,
  bloqueadas: 2,
  enEspera: 0,
  sinResponsable: 7,
  porEstado: [],
};

const TABLERO: TableroProyectosDto = {
  fechaReferencia: '2026-10-10',
  alcance: { todosLosNiveles: true, niveles: [], todosLosTipos: true, tipos: [] },
  totales: INDICADORES,
  proyectos: {
    items: [
      {
        proyecto: {
          id: 1,
          codigo: 'PRJ-0001',
          nombre: 'Portal clientes',
          archivado: false,
          estado: { id: 3, nombre: 'En curso', esFinal: false, activo: true, esSistema: false },
          responsable: { id: 2, nombre: 'Ana Gómez', activo: true },
          fechaObjetivo: '2026-12-15',
        },
        cantidadAgrupaciones: 2,
        indicadores: INDICADORES,
      },
    ],
    pagina: 1,
    tamanioPagina: 20,
    total: 1,
  },
};

/** Tests de TableroGeneralPage (T1, RF-005 CA-01, CA-50, DA-21). */
describe('TableroGeneralPage', () => {
  let fixture: ComponentFixture<TableroGeneralPage>;
  let router: Router;
  const seguimiento = { tableroProyectos: vi.fn(), seguimiento: vi.fn() };

  /** Crea la página con los query params indicados (inputs vinculados). */
  async function crear(params: Record<string, string> = {}): Promise<HTMLElement> {
    fixture = TestBed.createComponent(TableroGeneralPage);
    for (const [clave, valor] of Object.entries(params)) {
      fixture.componentRef.setInput(clave, valor);
    }
    await fixture.whenStable();
    return fixture.nativeElement as HTMLElement;
  }

  /** Query params de la última navegación. */
  function ultimosQueryParams(): unknown {
    const llamadas = vi.mocked(router.navigate).mock.calls;
    return (llamadas[llamadas.length - 1][1] as { queryParams: unknown }).queryParams;
  }

  beforeEach(() => {
    vi.clearAllMocks();
    seguimiento.tableroProyectos.mockReturnValue(of(TABLERO));
    TestBed.configureTestingModule({
      imports: [TableroGeneralPage],
      providers: [
        provideRouter([]),
        { provide: SeguimientoService, useValue: seguimiento },
        { provide: NivelesIncidenciaService, useValue: { listar: () => of([]) } },
        { provide: TiposIncidenciaService, useValue: { listar: () => of([]) } },
        { provide: EstadosIncidenciaService, useValue: { listar: () => of([]) } },
        { provide: UsuarioActualService, useValue: { tienePermiso: () => false } },
      ],
    });
    router = TestBed.inject(Router);
    vi.spyOn(router, 'navigate').mockResolvedValue(true);
  });

  // CA-01, RN-02: por defecto pide el tablero excluyendo archivados, página 1, orden código desc
  it('sin query params pide el tablero con archivados=excluir y los defaults', async () => {
    await crear();

    expect(seguimiento.tableroProyectos).toHaveBeenCalledWith(
      expect.objectContaining({
        archivados: 'excluir',
        pagina: 1,
        tamanioPagina: 20,
        ordenarPor: 'codigo',
        direccion: 'desc',
      }),
    );
  });

  // DA-21, CA-50: T1 pasa clicable = false a IndicadoresComponent
  it('muestra los totales con IndicadoresComponent no clicable', async () => {
    const raiz = await crear();

    const indicadores = fixture.debugElement.query(By.directive(IndicadoresComponent));
    expect((indicadores.componentInstance as IndicadoresComponent).clicable()).toBe(false);
    expect(raiz.querySelectorAll('.tarjetas-indicadores button').length).toBe(0);
  });

  // DA-21, CA-50: clic en cualquier tarjeta no navega ni vuelve a pedir el tablero
  it('clic en las tarjetas de totales no navega ni cambia los filtros', async () => {
    const raiz = await crear();

    raiz.querySelectorAll<HTMLElement>('.tarjeta-indicador').forEach((t) => t.click());
    await fixture.whenStable();

    expect(router.navigate).not.toHaveBeenCalled();
    expect(seguimiento.tableroProyectos).toHaveBeenCalledTimes(1);
  });

  // CA-01: cambiar archivados vuelve a la página 1
  it('cambiar Archivados navega a la página 1 con el filtro', async () => {
    await crear({ pagina: '3' });

    fixture.componentInstance.cambiarFiltros({ texto: undefined, archivados: 'incluir' });

    expect(ultimosQueryParams()).toEqual({ archivados: 'incluir' });
  });

  // CA-01, RN-06: cambiar el alcance vuelve a la página 1 y lo guarda en la URL
  it('cambiar el alcance navega a la página 1 con nivelId repetido', async () => {
    await crear({ pagina: '2' });

    fixture.componentInstance.cambiarAlcance({ nivelIds: [2, 3], tipoIds: [] });

    expect(ultimosQueryParams()).toEqual({ nivelId: ['2', '3'] });
  });

  // CA-04: ordenar por vencidas desde los encabezados
  it('ordenar por vencidas navega con ordenarPor y dirección asc', async () => {
    await crear();

    fixture.componentInstance.ordenar('vencidas');

    expect(ultimosQueryParams()).toEqual({ ordenarPor: 'vencidas', direccion: 'asc' });
  });

  // CA-06: expandir un proyecto pide su seguimiento con el mismo alcance
  it('cargarAgrupaciones pide el seguimiento del proyecto con el alcance vigente', async () => {
    seguimiento.seguimiento.mockReturnValue(of({ agrupaciones: [] }));
    await crear({ nivelId: '2' });

    fixture.componentInstance.cargarAgrupaciones(1);

    expect(seguimiento.seguimiento).toHaveBeenCalledWith(1, { nivelIds: [2], tipoIds: [] });
  });
});
