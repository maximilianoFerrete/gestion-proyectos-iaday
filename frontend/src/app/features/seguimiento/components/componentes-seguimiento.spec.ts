import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { EstadoIncidenciaDto } from '../../configuracion-incidencias/models/estado-incidencia.model';
import { NivelIncidenciaDto } from '../../configuracion-incidencias/models/nivel-incidencia.model';
import { TipoIncidenciaDto } from '../../configuracion-incidencias/models/tipo-incidencia.model';
import {
  AlcanceConsulta,
  AlcanceSeguimientoDto,
  ConteoEstadoDto,
  FilaProyectoSeguimientoDto,
  IndicadoresDto,
} from '../models/seguimiento.model';
import { ConteoPorEstadoComponent } from './conteo-por-estado/conteo-por-estado.component';
import { FiltroAlcanceComponent } from './filtro-alcance/filtro-alcance.component';
import { LeyendaAlcanceComponent } from './leyenda-alcance/leyenda-alcance.component';
import { TablaProyectosSeguimientoComponent } from './tabla-proyectos-seguimiento/tabla-proyectos-seguimiento.component';

const CREADO = '2026-10-09T13:00:00Z';
const ALCANCE: AlcanceSeguimientoDto = { todosLosNiveles: true, niveles: [], todosLosTipos: true, tipos: [] };

/** Estado del catálogo con la categoría indicada. */
function estado(id: number, nombre: string, categoria: 'BLOQUEADA' | 'EN_ESPERA' | null): EstadoIncidenciaDto {
  return { id, nombre, descripcion: null, orden: id * 10, activo: true, cantidadIncidencias: 0, flujos: [], categoria };
}

/** Nivel del catálogo. */
function nivel(id: number): NivelIncidenciaDto {
  return { id, numero: id, nombre: `Nivel ${id}`, descripcion: null, cantidadTipos: 1, fechaCreacion: CREADO, fechaModificacion: null };
}

/** Tipo del catálogo. */
function tipo(id: number, nombre: string, nivelId: number): TipoIncidenciaDto {
  return {
    id,
    nombre,
    descripcion: null,
    nivel: { id: nivelId, numero: nivelId, nombre: `Nivel ${nivelId}` },
    flujo: { id: 1, nombre: 'Flujo general' },
    activo: true,
    cantidadIncidencias: 0,
    fechaCreacion: CREADO,
    fechaModificacion: null,
  };
}

const INDICADORES: IndicadoresDto = {
  total: 0,
  abiertas: 0,
  finalizadas: 0,
  vencidas: 0,
  bloqueadas: 0,
  enEspera: 0,
  sinResponsable: 0,
  porEstado: [],
};

/** Tests de componentes de T1/T2: leyenda, conteo por estado, filtro de alcance y tabla de proyectos. */
describe('Componentes de seguimiento (RF-005)', () => {
  beforeEach(() => {
    TestBed.configureTestingModule({ providers: [provideRouter([])] });
  });

  describe('LeyendaAlcanceComponent', () => {
    /** Renderiza la leyenda con el catálogo de estados indicado. */
    async function crear(estados: EstadoIncidenciaDto[], puedeConfigurar = false): Promise<HTMLElement> {
      const fixture = TestBed.createComponent(LeyendaAlcanceComponent);
      fixture.componentRef.setInput('alcance', ALCANCE);
      fixture.componentRef.setInput('fechaReferencia', '2026-10-10');
      fixture.componentRef.setInput('estados', estados);
      fixture.componentRef.setInput('puedeConfigurarEstados', puedeConfigurar);
      await fixture.whenStable();
      return fixture.nativeElement as HTMLElement;
    }

    // CA-12: sin estados EN_ESPERA aparece el aviso (con la semilla S004)
    it('sin estados EN_ESPERA muestra el aviso de En espera y no el de Bloqueada', async () => {
      const raiz = await crear([estado(1, 'Por hacer', null), estado(4, 'Bloqueada', 'BLOQUEADA')]);

      const avisos = Array.from(raiz.querySelectorAll('.aviso')).map((a) => a.textContent?.trim());
      expect(avisos).toEqual([
        'No hay estados configurados como En espera; el indicador siempre será 0.',
      ]);
    });

    // CA-12: con permiso de configuración el aviso enlaza a C3
    it('con permiso el aviso incluye el enlace a C3', async () => {
      const raiz = await crear([estado(1, 'Por hacer', null)], true);

      expect(raiz.querySelector('.aviso a')?.getAttribute('href')).toBe('/configuracion/incidencias/estados');
    });

    // CA-14, RN-23: leyenda general y "Datos al dd/MM/yyyy"
    it('muestra la leyenda de alcance y la fecha de referencia', async () => {
      const raiz = await crear([]);

      expect(raiz.textContent).toContain('Se contabilizan: todos los niveles y tipos de incidencia.');
      expect(raiz.textContent).toContain('Datos al 10/10/2026.');
    });
  });

  describe('ConteoPorEstadoComponent', () => {
    const POR_ESTADO: ConteoEstadoDto[] = [
      { estado: { id: 2, nombre: 'En progreso', orden: 20, activo: true, categoria: null, esFinal: false }, cantidad: 3 },
      { estado: { id: 3, nombre: 'Hecho', orden: 30, activo: true, categoria: null, esFinal: true }, cantidad: 1 },
    ];

    // "ConteoPorEstadoComponent": filas en el orden recibido; clic emite el estadoId
    it('muestra las filas en el orden recibido y el clic emite el estadoId', async () => {
      const fixture = TestBed.createComponent(ConteoPorEstadoComponent);
      fixture.componentRef.setInput('porEstado', POR_ESTADO);
      const emitidos: number[] = [];
      fixture.componentInstance.seleccionar.subscribe((id) => emitidos.push(id));
      await fixture.whenStable();
      const raiz = fixture.nativeElement as HTMLElement;

      const estados = Array.from(raiz.querySelectorAll('tbody tr td:first-child')).map((td) => td.textContent?.trim());
      raiz.querySelectorAll<HTMLButtonElement>('tbody button')[1].click();

      expect(estados).toEqual(['En progreso', 'Hecho']);
      expect(emitidos).toEqual([3]);
    });
  });

  describe('FiltroAlcanceComponent', () => {
    const TIPOS = [tipo(1, 'Épica', 1), tipo(3, 'Subtarea', 3), tipo(5, 'Bug', 3)];

    /** Renderiza el filtro con la selección indicada y captura las emisiones. */
    async function crear(nivelIds: number[], tipoIds: number[]): Promise<[HTMLElement, AlcanceConsulta[]]> {
      const fixture = TestBed.createComponent(FiltroAlcanceComponent);
      fixture.componentRef.setInput('niveles', [nivel(1), nivel(2), nivel(3)]);
      fixture.componentRef.setInput('tipos', TIPOS);
      fixture.componentRef.setInput('nivelIds', nivelIds);
      fixture.componentRef.setInput('tipoIds', tipoIds);
      const emitidos: AlcanceConsulta[] = [];
      fixture.componentInstance.cambio.subscribe((a) => emitidos.push(a));
      await fixture.whenStable();
      return [fixture.nativeElement as HTMLElement, emitidos];
    }

    // RN-04: al elegir el nivel 3 los tipos se limitan a ese nivel
    it('con el nivel 3 elegido ofrece solo los tipos del nivel 3', async () => {
      const [raiz] = await crear([3], []);

      const legends = Array.from(raiz.querySelectorAll('legend')).map((l) => l.textContent?.trim());
      expect(legends).toEqual(['Niveles', 'Tipos de nivel 3']);
    });

    // CA-16: al elegir nivel 1 se quitan los tipos de nivel 3 elegidos (sin intersección vacía)
    it('al agregar el nivel 1 con Subtarea elegida quita Subtarea de los tipos', async () => {
      const [raiz, emitidos] = await crear([], [3]);

      raiz.querySelectorAll<HTMLInputElement>('fieldset:first-of-type input')[0].click();

      expect(emitidos).toEqual([{ nivelIds: [1], tipoIds: [] }]);
    });

    // CA-16, RN-05: un nivel sin tipos no se puede elegir
    it('deshabilita los niveles sin tipos', async () => {
      const [raiz] = await crear([], []);

      const nivel2 = raiz.querySelectorAll<HTMLInputElement>('fieldset:first-of-type input')[1];
      expect(nivel2.disabled).toBe(true);
    });
  });

  describe('TablaProyectosSeguimientoComponent', () => {
    const FILA: FilaProyectoSeguimientoDto = {
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
      indicadores: { ...INDICADORES, vencidas: 3 },
    };

    // CA-06: expandir pide las agrupaciones una sola vez
    it('expandir emite cargarAgrupaciones solo la primera vez', async () => {
      const fixture = TestBed.createComponent(TablaProyectosSeguimientoComponent);
      fixture.componentRef.setInput('filas', [FILA]);
      fixture.componentRef.setInput('ordenarPor', 'codigo');
      fixture.componentRef.setInput('direccion', 'desc');
      const pedidos: number[] = [];
      fixture.componentInstance.cargarAgrupaciones.subscribe((id) => {
        pedidos.push(id);
        fixture.componentRef.setInput('agrupaciones', new Map([[id, { estado: 'cargando' }]]));
      });
      await fixture.whenStable();
      const boton = (fixture.nativeElement as HTMLElement).querySelector<HTMLButtonElement>(
        '[aria-label="Ver agrupaciones de PRJ-0001"]',
      )!;

      boton.click();
      await fixture.whenStable();
      fixture.componentInstance.alternar(1);
      fixture.componentInstance.alternar(1);

      expect(pedidos).toEqual([1]);
    });

    // CA-06: filas de agrupaciones con enlaces a T2 por ámbito y "Sin agrupación"; código → P3
    it('muestra las agrupaciones con enlaces a T2 y el código enlaza a P3', async () => {
      const fixture = TestBed.createComponent(TablaProyectosSeguimientoComponent);
      fixture.componentRef.setInput('filas', [FILA]);
      fixture.componentRef.setInput('ordenarPor', 'codigo');
      fixture.componentRef.setInput('direccion', 'desc');
      fixture.componentRef.setInput('nivelIds', [2]);
      fixture.componentRef.setInput(
        'agrupaciones',
        new Map([
          [
            1,
            {
              estado: 'ok',
              filas: [
                { agrupacion: { id: 8, nombre: 'Pagos' }, indicadores: INDICADORES },
                { agrupacion: null, indicadores: INDICADORES },
              ],
            },
          ],
        ]),
      );
      await fixture.whenStable();
      fixture.componentInstance.alternar(1);
      await fixture.whenStable();
      const raiz = fixture.nativeElement as HTMLElement;

      const enlaces = Array.from(raiz.querySelectorAll<HTMLAnchorElement>('.fila-agrupacion a')).map((a) => [
        a.textContent?.trim(),
        a.getAttribute('href'),
      ]);
      expect(enlaces).toEqual([
        ['Pagos', '/proyectos/1/seguimiento?agrupacionId=8&nivelId=2'],
        ['Sin agrupación', '/proyectos/1/seguimiento?sinAgrupacion=true&nivelId=2'],
      ]);
      expect(raiz.querySelector('tbody a')?.getAttribute('href')).toBe('/proyectos/1');
    });

    // CA-04: los encabezados ordenables emiten ordenarPor
    it('clic en el encabezado Vencidas emite ordenar("vencidas")', async () => {
      const fixture = TestBed.createComponent(TablaProyectosSeguimientoComponent);
      fixture.componentRef.setInput('filas', [FILA]);
      fixture.componentRef.setInput('ordenarPor', 'codigo');
      fixture.componentRef.setInput('direccion', 'desc');
      const emitidos: string[] = [];
      fixture.componentInstance.ordenar.subscribe((o) => emitidos.push(o));
      await fixture.whenStable();

      Array.from((fixture.nativeElement as HTMLElement).querySelectorAll<HTMLButtonElement>('thead button'))
        .find((b) => b.textContent?.includes('Vencidas'))!
        .click();

      expect(emitidos).toEqual(['vencidas']);
    });
  });
});
