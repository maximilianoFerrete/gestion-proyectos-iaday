import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Router, provideRouter } from '@angular/router';
import { of } from 'rxjs';
import { EstadosProyectoService } from '../../../estados-proyecto/services/estados-proyecto.service';
import { PersonasService } from '../../../personas/services/personas.service';
import { ProyectosService } from '../../services/proyectos.service';
import { ProyectosListadoPage } from './proyectos-listado.page';

/** Tests de ProyectosListadoPage (P1, CA-25 a CA-27). */
describe('ProyectosListadoPage', () => {
  let fixture: ComponentFixture<ProyectosListadoPage>;
  let router: Router;
  const proyectos = { listar: vi.fn() };
  const estados = { listar: vi.fn() };
  const personas = { listar: vi.fn() };

  beforeEach(() => {
    vi.clearAllMocks();
    proyectos.listar.mockReturnValue(of({ items: [], pagina: 1, tamanioPagina: 20, total: 0 }));
    estados.listar.mockReturnValue(of([]));
    personas.listar.mockReturnValue(of([]));
    TestBed.configureTestingModule({
      imports: [ProyectosListadoPage],
      providers: [
        provideRouter([]),
        { provide: ProyectosService, useValue: proyectos },
        { provide: EstadosProyectoService, useValue: estados },
        { provide: PersonasService, useValue: personas },
      ],
    });
    router = TestBed.inject(Router);
    vi.spyOn(router, 'navigate').mockResolvedValue(true);
    fixture = TestBed.createComponent(ProyectosListadoPage);
  });

  /** Último objeto de opciones pasado a router.navigate. */
  function ultimosQueryParams(): unknown {
    const llamadas = vi.mocked(router.navigate).mock.calls;
    return llamadas.at(-1)?.[1]?.queryParams;
  }

  // CA-25: sin query params el filtro Archivados es "No archivados" y se pide archivados=excluir
  it('sin query params usa "No archivados" y consulta con los defaults', async () => {
    await fixture.whenStable();
    const raiz = fixture.nativeElement as HTMLElement;

    const select = raiz.querySelector<HTMLSelectElement>('#filtro-archivados');
    expect(select?.value).toBe('excluir');
    expect(select?.selectedOptions[0]?.textContent).toBe('No archivados');
    expect(proyectos.listar).toHaveBeenCalledWith(
      expect.objectContaining({ archivados: 'excluir', pagina: 1, tamanioPagina: 20 }),
    );
  });

  // CA-25: cambiar un filtro actualiza los query params y vuelve a la página 1
  it('cambiar filtros navega con los query params nuevos y página 1', async () => {
    fixture.componentRef.setInput('pagina', '3');
    await fixture.whenStable();

    fixture.componentInstance.cambiarFiltros({ archivados: 'solo', texto: 'portal' });

    expect(ultimosQueryParams()).toEqual({ archivados: 'solo', texto: 'portal' });
  });

  // P1: "Limpiar filtros" vuelve a la URL sin parámetros
  it('limpiar filtros navega sin query params', async () => {
    fixture.componentRef.setInput('archivados', 'solo');
    await fixture.whenStable();

    fixture.componentInstance.limpiarFiltros();

    expect(ultimosQueryParams()).toEqual({});
  });

  // CA-25, CA-26: los query params de la URL se usan como filtros
  it('con query params en la URL consulta con esos filtros', async () => {
    fixture.componentRef.setInput('archivados', 'incluir');
    fixture.componentRef.setInput('estadoId', '3');
    await fixture.whenStable();

    expect(proyectos.listar).toHaveBeenLastCalledWith(
      expect.objectContaining({ archivados: 'incluir', estadoId: 3 }),
    );
  });

  // CA-27: valores inválidos en la URL no llegan a la API
  it('con tamanioPagina=500 en la URL consulta con el tamaño por defecto', async () => {
    fixture.componentRef.setInput('tamanioPagina', '500');
    await fixture.whenStable();

    expect(proyectos.listar).toHaveBeenLastCalledWith(expect.objectContaining({ tamanioPagina: 20 }));
  });

  // P1: el filtro Responsable incluye personas inactivas
  it('carga los responsables incluyendo inactivas', async () => {
    await fixture.whenStable();

    expect(personas.listar).toHaveBeenCalledWith({ incluirInactivas: true });
  });
});
