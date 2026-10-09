import { FILTROS_PROYECTOS_POR_DEFECTO, FiltrosProyectos } from '../models/proyecto.model';
import { parsearFiltros, serializarFiltros, siguienteOrden } from './filtros-proyectos';

/** Tests de parsearFiltros / serializarFiltros / siguienteOrden (P1, CA-25 a CA-27). */
describe('filtros-proyectos', () => {
  // CA-25: sin query params → "No archivados", página 1, 20, codigo desc
  it('parsearFiltros sin parámetros devuelve los defaults del contrato', () => {
    expect(parsearFiltros({})).toEqual({
      ...FILTROS_PROYECTOS_POR_DEFECTO,
      texto: undefined,
      estadoId: undefined,
      responsableId: undefined,
    });
  });

  // CA-26: valores válidos se respetan; el texto se recorta
  it('parsearFiltros con valores válidos los convierte', () => {
    const filtros = parsearFiltros({
      texto: '  prj-0003 ',
      estadoId: '3',
      responsableId: '2',
      archivados: 'solo',
      pagina: '4',
      tamanioPagina: '100',
      ordenarPor: 'fechaObjetivo',
      direccion: 'asc',
    });

    expect(filtros).toEqual({
      texto: 'prj-0003',
      estadoId: 3,
      responsableId: 2,
      archivados: 'solo',
      pagina: 4,
      tamanioPagina: 100,
      ordenarPor: 'fechaObjetivo',
      direccion: 'asc',
    });
  });

  // CA-27: valores inválidos en la URL se reemplazan por los defaults
  it('parsearFiltros con valores inválidos usa los defaults', () => {
    const filtros = parsearFiltros({
      estadoId: 'x',
      archivados: 'x',
      pagina: '0',
      tamanioPagina: '500',
      ordenarPor: 'foo',
      direccion: 'arriba',
    });

    expect(filtros).toEqual({
      ...FILTROS_PROYECTOS_POR_DEFECTO,
      texto: undefined,
      estadoId: undefined,
      responsableId: undefined,
    });
  });

  // Texto: máximo 150 caracteres (filtro del listado)
  it('parsearFiltros trunca el texto a 150 caracteres', () => {
    expect(parsearFiltros({ texto: 'a'.repeat(200) }).texto?.length).toBe(150);
  });

  // P1: URL limpia, solo valores distintos del default
  it('serializarFiltros omite los valores por defecto', () => {
    expect(serializarFiltros(FILTROS_PROYECTOS_POR_DEFECTO)).toEqual({});
  });

  // P1: los filtros se reflejan en query params
  it('serializarFiltros incluye los filtros definidos y no default', () => {
    const filtros: FiltrosProyectos = {
      ...FILTROS_PROYECTOS_POR_DEFECTO,
      texto: 'portal',
      estadoId: 3,
      archivados: 'incluir',
      pagina: 2,
    };

    expect(serializarFiltros(filtros)).toEqual({
      texto: 'portal',
      estadoId: '3',
      archivados: 'incluir',
      pagina: '2',
    });
  });

  // P1: ida y vuelta conserva los filtros
  it('parsearFiltros(serializarFiltros(f)) devuelve los mismos filtros', () => {
    const filtros: FiltrosProyectos = {
      texto: 'portal',
      estadoId: 3,
      responsableId: 2,
      archivados: 'solo',
      pagina: 3,
      tamanioPagina: 50,
      ordenarPor: 'responsable',
      direccion: 'asc',
    };

    expect(parsearFiltros(serializarFiltros(filtros))).toEqual(filtros);
  });

  // P1: ordenar por la misma columna alterna la dirección
  it('siguienteOrden en la columna actual alterna la dirección', () => {
    expect(siguienteOrden({ ordenarPor: 'nombre', direccion: 'asc' }, 'nombre')).toEqual({
      ordenarPor: 'nombre',
      direccion: 'desc',
    });
  });

  // P1: ordenar por otra columna empieza ascendente
  it('siguienteOrden en otra columna empieza ascendente', () => {
    expect(siguienteOrden({ ordenarPor: 'codigo', direccion: 'desc' }, 'estado')).toEqual({
      ordenarPor: 'estado',
      direccion: 'asc',
    });
  });
});
