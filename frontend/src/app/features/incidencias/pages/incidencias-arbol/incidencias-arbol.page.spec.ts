import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { of } from 'rxjs';
import { ProyectoDto } from '../../../proyectos/models/proyecto.model';
import { ProyectosService } from '../../../proyectos/services/proyectos.service';
import { NodoArbolIncidenciaDto } from '../../models/incidencia.model';
import { IncidenciasService } from '../../services/incidencias.service';
import { IncidenciasArbolPage } from './incidencias-arbol.page';

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
  cantidadAgrupaciones: 0,
  cantidadIncidencias: 4,
  fechaCreacion: '2026-10-09T13:05:00Z',
  fechaModificacion: null,
};

/** Nodo plano del árbol. */
function nodo(id: number, padreId: number | null): NodoArbolIncidenciaDto {
  return {
    id,
    codigo: `PRJ-0001-${id}`,
    titulo: `Incidencia ${id}`,
    padreId,
    tipo: { id: 1, nombre: 'Épica', activo: true, nivelNumero: 1 },
    estado: { id: 1, nombre: 'Por hacer', activo: true, esFinal: false, categoria: null },
    prioridad: { id: 2, nombre: 'Media', orden: 20, activo: true },
    responsable: null,
    fechaVencimiento: null,
    vencida: false,
    agrupacion: null,
  };
}

/** Épica 3 → HU 15 → Subtarea 16, y la raíz 10 sin hijas (lista desordenada). */
const NODOS = [nodo(16, 15), nodo(10, null), nodo(15, 3), nodo(3, null)];

/** Tests de IncidenciasArbolPage + ArbolIncidenciasComponent (I2, CA-34). */
describe('IncidenciasArbolPage', () => {
  let fixture: ComponentFixture<IncidenciasArbolPage>;
  const incidencias = { arbol: vi.fn() };

  /** Crea la página con los nodos indicados y la renderiza. */
  async function crear(nodos: NodoArbolIncidenciaDto[], proyecto = PROYECTO): Promise<HTMLElement> {
    incidencias.arbol.mockReturnValue(of(nodos));
    TestBed.overrideProvider(ProyectosService, { useValue: { obtener: () => of(proyecto) } });
    fixture = TestBed.createComponent(IncidenciasArbolPage);
    fixture.componentRef.setInput('id', '1');
    await fixture.whenStable();
    return fixture.nativeElement as HTMLElement;
  }

  /** Códigos visibles en el árbol, en orden de aparición. */
  function codigosVisibles(raiz: HTMLElement): string[] {
    return Array.from(raiz.querySelectorAll('.arbol a')).map((a) => a.textContent?.trim() ?? '');
  }

  /** Hace clic en un botón por texto o aria-label y espera el render. */
  async function clic(raiz: HTMLElement, texto: string): Promise<void> {
    Array.from(raiz.querySelectorAll<HTMLButtonElement>('button'))
      .find((b) => b.textContent?.trim() === texto || b.getAttribute('aria-label') === texto)
      ?.click();
    await fixture.whenStable();
  }

  beforeEach(() => {
    vi.clearAllMocks();
    TestBed.configureTestingModule({
      imports: [IncidenciasArbolPage],
      providers: [
        provideRouter([]),
        { provide: IncidenciasService, useValue: incidencias },
        { provide: ProyectosService, useValue: { obtener: () => of(PROYECTO) } },
      ],
    });
  });

  // CA-34: raíces por número con sus hijas visibles (raíces expandidas al abrir)
  it('al abrir muestra las raíces por número y sus hijas directas', async () => {
    const raiz = await crear(NODOS);

    expect(codigosVisibles(raiz)).toEqual(['PRJ-0001-3', 'PRJ-0001-15', 'PRJ-0001-10']);
  });

  // CA-34: expandir y colapsar un nodo
  it('expandir un nodo muestra sus hijas y colapsarlo las oculta', async () => {
    const raiz = await crear(NODOS);

    await clic(raiz, 'Expandir PRJ-0001-15');
    expect(codigosVisibles(raiz)).toContain('PRJ-0001-16');

    await clic(raiz, 'Colapsar PRJ-0001-15');
    expect(codigosVisibles(raiz)).not.toContain('PRJ-0001-16');
  });

  // CA-34: "Expandir todo" y "Colapsar todo"
  it('Expandir todo muestra todos los niveles y Colapsar todo deja solo las raíces', async () => {
    const raiz = await crear(NODOS);

    await clic(raiz, 'Expandir todo');
    expect(codigosVisibles(raiz)).toEqual([
      'PRJ-0001-3',
      'PRJ-0001-15',
      'PRJ-0001-16',
      'PRJ-0001-10',
    ]);

    await clic(raiz, 'Colapsar todo');
    expect(codigosVisibles(raiz)).toEqual(['PRJ-0001-3', 'PRJ-0001-10']);
  });

  // CA-34: cada código enlaza al detalle
  it('cada código enlaza al detalle de la incidencia', async () => {
    const raiz = await crear(NODOS);

    const enlace = Array.from(raiz.querySelectorAll('.arbol a')).find(
      (a) => a.textContent?.trim() === 'PRJ-0001-15',
    );
    expect(enlace?.getAttribute('href')).toBe('/proyectos/1/incidencias/15');
  });

  // I2: proyecto sin incidencias
  it('sin incidencias muestra "El proyecto no tiene incidencias."', async () => {
    const raiz = await crear([]);

    expect(raiz.textContent).toContain('El proyecto no tiene incidencias.');
  });

  // CA-21: el árbol de un proyecto archivado se consulta con el banner
  it('proyecto archivado muestra el banner y el árbol', async () => {
    const raiz = await crear(NODOS, { ...PROYECTO, archivado: true });

    expect(raiz.querySelector('.banner-archivado')?.textContent).toContain(
      'Proyecto archivado: solo lectura.',
    );
    expect(codigosVisibles(raiz)).toContain('PRJ-0001-3');
  });
});
