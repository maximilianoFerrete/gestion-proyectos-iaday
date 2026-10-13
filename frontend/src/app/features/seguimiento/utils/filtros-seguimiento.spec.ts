import {
  FILTROS_SEGUIMIENTO_POR_DEFECTO,
  FiltrosSeguimiento,
  aQueryParams,
  filtrosSeguimientoDesdeQueryParams,
  hayFiltrosTablero,
  navegacionDesdeResumen,
  parsearConsultaTableroGeneral,
  serializarConsultaTableroGeneral,
  siguienteOrdenTableroGeneral,
} from './filtros-seguimiento';

/** Filtros de T2 con ámbito agrupación, alcance nivel 2 y filtros del tablero. */
const COMPLETOS: FiltrosSeguimiento = {
  agrupacionId: 8,
  sinAgrupacion: false,
  nivelIds: [2, 3],
  tipoIds: [5],
  vista: 'tablero',
  indicador: 'vencidas',
  estadoId: 4,
  texto: 'pago',
};

/** Tests de la conversión filtros ↔ query params de T1/T2 y de la navegación del Resumen. */
describe('utils/filtros-seguimiento', () => {
  describe('filtrosSeguimientoDesdeQueryParams / aQueryParams', () => {
    // RN-06, RN-21: ida y vuelta sin pérdida (nivelId múltiples, vista, ámbito y filtros)
    it('ida y vuelta conserva ámbito, alcance, vista y filtros del tablero', () => {
      const params = aQueryParams(COMPLETOS);

      expect(filtrosSeguimientoDesdeQueryParams(params)).toEqual(COMPLETOS);
    });

    // RN-06, RN-33: los query params no incluyen el orden de la grilla y usan nombres de la API
    it('serializa con los nombres de la URL y sin orden del tablero', () => {
      expect(aQueryParams(COMPLETOS)).toEqual({
        agrupacionId: '8',
        nivelId: ['2', '3'],
        tipoId: ['5'],
        vista: 'tablero',
        indicador: 'vencidas',
        estadoId: '4',
        texto: 'pago',
      });
    });

    // RN-06: los valores por defecto no se escriben en la URL
    it('con los valores por defecto devuelve query params vacíos', () => {
      expect(aQueryParams(FILTROS_SEGUIMIENTO_POR_DEFECTO)).toEqual({});
    });

    // RN-01: sinAgrupacion gana sobre agrupacionId
    it('con sinAgrupacion=true descarta agrupacionId', () => {
      const filtros = filtrosSeguimientoDesdeQueryParams({ agrupacionId: '8', sinAgrupacion: 'true' });

      expect([filtros.agrupacionId, filtros.sinAgrupacion]).toEqual([null, true]);
    });

    // CA-49, DA-16: vista=lista o un valor desconocido se tratan como resumen
    it.each(['lista', 'foo', undefined])('con vista=%s usa la vista resumen', (vista) => {
      expect(filtrosSeguimientoDesdeQueryParams({ vista }).vista).toBe('resumen');
    });

    // CA-26: indicador sin distinguir mayúsculas; fuera de lista se ignora
    it('normaliza el indicador a minúsculas e ignora valores fuera de lista', () => {
      expect(filtrosSeguimientoDesdeQueryParams({ indicador: 'VENCIDAS' }).indicador).toBe(
        'vencidas',
      );
      expect(filtrosSeguimientoDesdeQueryParams({ indicador: 'foo' }).indicador).toBeNull();
    });

    // RN-21: texto vacío o solo espacios = sin filtro; ids inválidos se descartan
    it('trata el texto vacío como sin filtro y descarta ids no numéricos', () => {
      const filtros = filtrosSeguimientoDesdeQueryParams({
        texto: '   ',
        nivelId: ['2', 'x', '2'],
        estadoId: 'abc',
      });

      expect(filtros.texto).toBeNull();
      expect(filtros.nivelIds).toEqual([2]);
      expect(filtros.estadoId).toBeNull();
    });
  });

  describe('navegacionDesdeResumen', () => {
    // RN-24, CA-24: tarjeta de indicador → vista tablero con indicador, sin estadoId ni texto
    it('con un indicador abre el tablero con ese indicador conservando ámbito y alcance', () => {
      const params = navegacionDesdeResumen({ ...COMPLETOS, vista: 'resumen' }, {
        indicador: 'bloqueadas',
      });

      expect(params).toEqual({
        agrupacionId: '8',
        nivelId: ['2', '3'],
        tipoId: ['5'],
        vista: 'tablero',
        indicador: 'bloqueadas',
      });
    });

    // RN-24, CA-24: estado del conteo → vista tablero con estadoId, sin indicador ni texto
    it('con un estado abre el tablero con estadoId y sin indicador ni texto', () => {
      const params = navegacionDesdeResumen(COMPLETOS, { estadoId: 2 });

      expect(params).toEqual({
        agrupacionId: '8',
        nivelId: ['2', '3'],
        tipoId: ['5'],
        vista: 'tablero',
        estadoId: '2',
      });
    });

    // RN-24: tarjeta "Total" → vista tablero sin filtros del tablero
    it('con total abre el tablero sin filtros del tablero', () => {
      const params = navegacionDesdeResumen(COMPLETOS, { total: true });

      expect(params).toEqual({
        agrupacionId: '8',
        nivelId: ['2', '3'],
        tipoId: ['5'],
        vista: 'tablero',
      });
    });
  });

  describe('hayFiltrosTablero', () => {
    // CA-48: cualquiera de los tres filtros cuenta como filtro activo
    it('es true con algún filtro y false sin ninguno', () => {
      expect(hayFiltrosTablero({ indicador: null, estadoId: null, texto: 'pago' })).toBe(true);
      expect(hayFiltrosTablero({ indicador: null, estadoId: null, texto: null })).toBe(false);
    });
  });

  describe('T1 – consulta del tablero general', () => {
    // CA-01: por defecto excluye archivados, página 1 de 20, orden código desc
    it('sin query params aplica los defaults del contrato', () => {
      expect(parsearConsultaTableroGeneral({})).toEqual({
        texto: undefined,
        archivados: 'excluir',
        nivelIds: [],
        tipoIds: [],
        pagina: 1,
        tamanioPagina: 20,
        ordenarPor: 'codigo',
        direccion: 'desc',
      });
    });

    // CA-05, RN-28: valores inválidos de la URL vuelven al default
    it('con valores inválidos usa los defaults', () => {
      const consulta = parsearConsultaTableroGeneral({
        archivados: 'x',
        tamanioPagina: '101',
        pagina: '0',
        ordenarPor: 'foo',
      });

      expect([consulta.archivados, consulta.tamanioPagina, consulta.pagina, consulta.ordenarPor]).toEqual(
        ['excluir', 20, 1, 'codigo'],
      );
    });

    // CA-01: ida y vuelta de la consulta con alcance repetido
    it('ida y vuelta conserva la consulta y omite los defaults', () => {
      const consulta = parsearConsultaTableroGeneral({
        texto: 'portal',
        archivados: 'incluir',
        nivelId: ['1', '2'],
        pagina: '2',
        ordenarPor: 'vencidas',
      });

      const params = serializarConsultaTableroGeneral(consulta);

      expect(params).toEqual({
        texto: 'portal',
        archivados: 'incluir',
        nivelId: ['1', '2'],
        pagina: '2',
        ordenarPor: 'vencidas',
      });
    });

    // CA-04: clic en la misma columna alterna; en otra empieza asc
    it('siguienteOrdenTableroGeneral alterna la dirección o empieza en asc', () => {
      expect(
        siguienteOrdenTableroGeneral({ ordenarPor: 'vencidas', direccion: 'asc' }, 'vencidas'),
      ).toEqual({ ordenarPor: 'vencidas', direccion: 'desc' });
      expect(
        siguienteOrdenTableroGeneral({ ordenarPor: 'codigo', direccion: 'desc' }, 'vencidas'),
      ).toEqual({ ordenarPor: 'vencidas', direccion: 'asc' });
    });
  });
});
