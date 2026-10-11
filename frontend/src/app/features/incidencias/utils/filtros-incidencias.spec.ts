import { FILTROS_INCIDENCIAS_POR_DEFECTO } from '../models/incidencia.model';
import {
  parsearFiltrosIncidencias,
  serializarFiltrosIncidencias,
  siguienteOrdenIncidencias,
} from './filtros-incidencias';

/** Tests de la conversión query params ↔ filtros del listado de incidencias (I1, CA-19). */
describe('filtros-incidencias', () => {
  // Contrato GET incidencias: sin params → defaults (página 1, 20, numero desc)
  it('parsearFiltrosIncidencias sin params devuelve los defaults', () => {
    expect(parsearFiltrosIncidencias({})).toEqual({
      ...FILTROS_INCIDENCIAS_POR_DEFECTO,
      texto: undefined,
      estadoId: undefined,
      prioridadId: undefined,
      responsableId: undefined,
      agrupacionId: undefined,
    });
    expect(FILTROS_INCIDENCIAS_POR_DEFECTO).toMatchObject({
      pagina: 1,
      tamanioPagina: 20,
      ordenarPor: 'numero',
      direccion: 'desc',
    });
  });

  // CA-19: tipoId repetible (sin duplicados ni inválidos)
  it('parsearFiltrosIncidencias lee tipoId repetido sin duplicados ni inválidos', () => {
    expect(parsearFiltrosIncidencias({ tipoId: ['1', '2', '1', 'x'] }).tipoIds).toEqual([1, 2]);
  });

  // CA-19: "Sin responsable"/"Sin agrupación" tienen prioridad sobre el id (excluyentes)
  it('parsearFiltrosIncidencias con sinX=true descarta el id correspondiente', () => {
    const filtros = parsearFiltrosIncidencias({
      responsableId: '2',
      sinResponsable: 'true',
      agrupacionId: '8',
      sinAgrupacion: 'true',
    });

    expect(filtros).toMatchObject({
      sinResponsable: true,
      responsableId: undefined,
      sinAgrupacion: true,
      agrupacionId: undefined,
    });
  });

  // CA-19: valores fuera de rango o fuera de la lista blanca vuelven al default
  it('parsearFiltrosIncidencias corrige tamaño > 100 y orden desconocido', () => {
    const filtros = parsearFiltrosIncidencias({
      tamanioPagina: '101',
      ordenarPor: 'foo',
      direccion: 'x',
    });

    expect(filtros).toMatchObject({ tamanioPagina: 20, ordenarPor: 'numero', direccion: 'desc' });
  });

  // CA-19: "Sin agrupación" se serializa como sinAgrupacion=true; defaults omitidos
  it('serializarFiltrosIncidencias emite sinAgrupacion y tipoId repetido, sin defaults', () => {
    const params = serializarFiltrosIncidencias({
      ...FILTROS_INCIDENCIAS_POR_DEFECTO,
      tipoIds: [1, 2],
      sinAgrupacion: true,
      agrupacionId: 8,
    });

    expect(params).toEqual({ tipoId: ['1', '2'], sinAgrupacion: 'true' });
  });

  // I1: clic en la misma columna alterna la dirección; en otra empieza ascendente
  it('siguienteOrdenIncidencias alterna la dirección o empieza ascendente', () => {
    expect(siguienteOrdenIncidencias({ ordenarPor: 'titulo', direccion: 'asc' }, 'titulo')).toEqual(
      {
        ordenarPor: 'titulo',
        direccion: 'desc',
      },
    );
    expect(
      siguienteOrdenIncidencias({ ordenarPor: 'numero', direccion: 'desc' }, 'estado'),
    ).toEqual({
      ordenarPor: 'estado',
      direccion: 'asc',
    });
  });
});
