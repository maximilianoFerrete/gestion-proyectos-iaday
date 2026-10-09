import { EstadoProyectoDto } from '../../estados-proyecto/models/estado-proyecto.model';
import { PersonaDto } from '../../personas/models/persona.model';
import {
  opcionesEstado,
  opcionesFiltroEstado,
  opcionesFiltroResponsable,
  opcionesResponsable,
} from './opciones-proyecto';

/** Crea un estado de prueba. */
function estado(parcial: Partial<EstadoProyectoDto> & Pick<EstadoProyectoDto, 'id' | 'nombre'>): EstadoProyectoDto {
  return {
    descripcion: null,
    orden: 10,
    esInicial: false,
    esFinal: false,
    activo: true,
    esSistema: false,
    codigo: null,
    cantidadProyectos: 0,
    ...parcial,
  };
}

/** Crea una persona de prueba. */
function persona(id: number, nombre: string, activo = true): PersonaDto {
  return {
    id,
    nombre,
    email: `${nombre.toLowerCase()}@empresa.com`,
    esAdmin: false,
    activo,
    fechaCreacion: '2026-10-09T13:00:00Z',
    fechaModificacion: null,
  };
}

const estados: EstadoProyectoDto[] = [
  estado({ id: 4, nombre: 'Pausado', orden: 40 }),
  estado({ id: 1, nombre: 'En planificación', orden: 10, esInicial: true }),
  estado({ id: 3, nombre: 'En curso', orden: 30 }),
  estado({ id: 6, nombre: 'Cancelado', orden: 60, activo: false, esFinal: true }),
  estado({ id: 7, nombre: 'Archivado', orden: 9999, esSistema: true, codigo: 'ARCHIVADO' }),
  estado({ id: 8, nombre: 'En revisión', orden: 35 }),
];

/** Tests de opcionesEstado / opcionesResponsable (P2, P4, P1). */
describe('opciones-proyecto', () => {
  // CA-20, CA-07: activos no de sistema, en orden; nunca "Archivado"
  it('opcionesEstado devuelve activos no de sistema ordenados por orden', () => {
    expect(opcionesEstado(estados).map((o) => o.etiqueta)).toEqual([
      'En planificación',
      'En curso',
      'En revisión',
      'Pausado',
    ]);
  });

  // CA-13: el estado actual inactivo se agrega marcado "(inactivo)"
  it('opcionesEstado agrega el estado actual inactivo marcado', () => {
    const opciones = opcionesEstado(estados, {
      id: 6,
      nombre: 'Cancelado',
      esFinal: true,
      activo: false,
      esSistema: false,
    });

    expect(opciones.at(-1)).toEqual({ valor: 6, etiqueta: 'Cancelado (inactivo)' });
  });

  // CA-13: un estado inactivo que no es el actual no se ofrece
  it('opcionesEstado no ofrece inactivos que no son el actual', () => {
    expect(opcionesEstado(estados).some((o) => o.valor === 6)).toBe(false);
  });

  // CA-20, RN-09: aunque el actual sea de sistema, "Archivado" no se ofrece
  it('opcionesEstado nunca ofrece "Archivado" aunque sea el actual', () => {
    const opciones = opcionesEstado(estados, {
      id: 7,
      nombre: 'Archivado',
      esFinal: false,
      activo: true,
      esSistema: true,
    });

    expect(opciones.some((o) => o.valor === 7)).toBe(false);
  });

  // CA-05: solo personas activas
  it('opcionesResponsable excluye personas inactivas', () => {
    const opciones = opcionesResponsable([persona(2, 'Ana'), persona(5, 'Beto', false)]);

    expect(opciones).toEqual([{ valor: 2, etiqueta: 'Ana' }]);
  });

  // RN-17, P4: el responsable actual inactivo se agrega marcado "(inactivo)"
  it('opcionesResponsable agrega el responsable actual inactivo marcado', () => {
    const opciones = opcionesResponsable([persona(2, 'Ana')], { id: 5, nombre: 'Beto', activo: false });

    expect(opciones).toEqual([
      { valor: 2, etiqueta: 'Ana' },
      { valor: 5, etiqueta: 'Beto (inactivo)' },
    ]);
  });

  // P1: filtro Estado con todos, inactivos marcados e incluye "Archivado"
  it('opcionesFiltroEstado incluye todos los estados y marca inactivos', () => {
    const etiquetas = opcionesFiltroEstado(estados).map((o) => o.etiqueta);

    expect(etiquetas).toContain('Archivado');
    expect(etiquetas).toContain('Cancelado (inactivo)');
    expect(etiquetas).toHaveLength(estados.length);
  });

  // P1: filtro Responsable con personas inactivas marcadas
  it('opcionesFiltroResponsable incluye inactivas marcadas', () => {
    expect(opcionesFiltroResponsable([persona(5, 'Beto', false)])).toEqual([
      { valor: 5, etiqueta: 'Beto (inactivo)' },
    ]);
  });
});
