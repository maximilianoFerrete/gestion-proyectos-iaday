import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { NodoArbolIncidenciaDto } from '../../models/incidencia.model';
import { construirArbol } from '../../utils/arbol-incidencias';
import { ArbolIncidenciasComponent, marcasNodo } from './arbol-incidencias.component';

/** Nodo del árbol. */
function nodo(
  id: number,
  padreId: number | null,
  extra: Partial<NodoArbolIncidenciaDto> = {},
): NodoArbolIncidenciaDto {
  return {
    id,
    codigo: `PRJ-0001-${id}`,
    titulo: `Incidencia ${id}`,
    padreId,
    tipo: { id: 1, nombre: 'Épica', activo: true, nivelNumero: 1 },
    estado: { id: 1, nombre: 'Por hacer', activo: true, esFinal: false, categoria: null },
    prioridad: { id: 2, nombre: 'Media', orden: 20, activo: true },
    responsable: { id: 2, nombre: 'Ana', activo: true },
    fechaVencimiento: null,
    vencida: false,
    agrupacion: null,
    ...extra,
  };
}

const HU = { id: 2, nombre: 'Historia de Usuario', activo: true, nivelNumero: 2 };

/** Tests de ArbolIncidenciasComponent modificado por RF-005 (CA-18, CA-45, RN-36). */
describe('ArbolIncidenciasComponent (RF-005)', () => {
  let fixture: ComponentFixture<ArbolIncidenciasComponent>;

  /** Crea el árbol con todos los nodos expandidos. */
  async function crear(
    nodos: NodoArbolIncidenciaDto[],
    opciones: { tipoIdsEnAlcance?: ReadonlySet<number> | null; mostrarMarcas?: boolean } = {},
  ): Promise<HTMLElement> {
    fixture = TestBed.createComponent(ArbolIncidenciasComponent);
    fixture.componentRef.setInput('proyectoId', 1);
    fixture.componentRef.setInput('arbol', construirArbol(nodos));
    fixture.componentRef.setInput('expandidos', new Set(nodos.map((n) => n.id)));
    fixture.componentRef.setInput('tipoIdsEnAlcance', opciones.tipoIdsEnAlcance ?? null);
    fixture.componentRef.setInput('mostrarMarcas', opciones.mostrarMarcas ?? false);
    await fixture.whenStable();
    return fixture.nativeElement as HTMLElement;
  }

  /** Nodo renderizado del código indicado. */
  function nodoDe(raiz: HTMLElement, codigo: string): HTMLElement {
    return Array.from(raiz.querySelectorAll<HTMLElement>('.arbol-nodo')).find((n) =>
      n.textContent?.includes(codigo),
    )!;
  }

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [ArbolIncidenciasComponent],
      providers: [provideRouter([])],
    });
  });

  // RN-36, CA-45: la alerta de vencimiento sigue a `vencida` del nodo
  it('marca el vencimiento con alerta solo si el nodo viene con vencida = true', async () => {
    const raiz = await crear([
      nodo(3, null, { fechaVencimiento: '2000-01-01', vencida: false }),
      nodo(4, null, { fechaVencimiento: '2999-12-31', vencida: true }),
    ]);

    expect(nodoDe(raiz, 'PRJ-0001-3').querySelector('.texto-alerta')).toBeNull();
    expect(nodoDe(raiz, 'PRJ-0001-4').querySelector('.texto-alerta')).not.toBeNull();
  });

  // CA-18, RN-07: los nodos fuera del alcance se muestran atenuados
  it('atenúa los nodos cuyo tipo no está en el alcance', async () => {
    const raiz = await crear([nodo(3, null), nodo(15, 3, { tipo: HU })], {
      tipoIdsEnAlcance: new Set([HU.id]),
    });

    expect(nodoDe(raiz, 'PRJ-0001-3').classList.contains('arbol-nodo-atenuado')).toBe(true);
    expect(nodoDe(raiz, 'PRJ-0001-15').classList.contains('arbol-nodo-atenuado')).toBe(false);
  });

  // CA-18: sin alcance (null) no se atenúa ningún nodo
  it('sin alcance no atenúa ningún nodo', async () => {
    const raiz = await crear([nodo(3, null), nodo(15, 3, { tipo: HU })]);

    expect(raiz.querySelectorAll('.arbol-nodo-atenuado').length).toBe(0);
  });

  // "Vista Jerarquía": marcas de vencida, bloqueada y sin responsable
  it('con mostrarMarcas muestra las marcas de indicadores del nodo', async () => {
    const raiz = await crear(
      [
        nodo(3, null, {
          vencida: true,
          responsable: null,
          estado: { id: 4, nombre: 'Bloqueada', activo: true, esFinal: false, categoria: 'BLOQUEADA' },
        }),
      ],
      { mostrarMarcas: true },
    );

    const marcas = Array.from(nodoDe(raiz, 'PRJ-0001-3').querySelectorAll('.marca-indicador')).map(
      (m) => m.getAttribute('title'),
    );
    expect(marcas).toEqual(['Vencida', 'Bloqueada', 'Sin responsable']);
  });

  // RN-12 a RN-14: un nodo finalizado no lleva marcas de bloqueada, en espera ni sin responsable
  it('marcasNodo no marca bloqueada ni sin responsable en un estado final', () => {
    const final = nodo(3, null, {
      responsable: null,
      estado: { id: 4, nombre: 'Bloqueada', activo: true, esFinal: true, categoria: 'BLOQUEADA' },
    });

    expect(marcasNodo(final)).toEqual([]);
  });

  // RN-13: un nodo abierto en estado EN_ESPERA lleva la marca "En espera"
  it('marcasNodo marca En espera para un estado de categoría EN_ESPERA abierto', () => {
    const enEspera = nodo(3, null, {
      estado: { id: 5, nombre: 'Esperando', activo: true, esFinal: false, categoria: 'EN_ESPERA' },
    });

    expect(marcasNodo(enEspera).map((m) => m.clave)).toEqual(['en-espera']);
  });
});
