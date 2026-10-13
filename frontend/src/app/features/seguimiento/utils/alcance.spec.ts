import { TipoIncidenciaDto } from '../../configuracion-incidencias/models/tipo-incidencia.model';
import { AlcanceSeguimientoDto } from '../models/seguimiento.model';
import {
  LEYENDA_TODO_EL_ALCANCE,
  depurarTiposSeleccionados,
  textoLeyendaAlcance,
  textoSubtituloAlcance,
  tipoIdsEnAlcance,
  tiposDisponibles,
} from './alcance';

/** Tipo del catálogo con su nivel. */
function tipo(id: number, nombre: string, nivel: number): TipoIncidenciaDto {
  return {
    id,
    nombre,
    descripcion: null,
    nivel: { id: nivel, numero: nivel, nombre: `Nivel ${nivel}` },
    flujo: { id: 1, nombre: 'Flujo general' },
    activo: true,
    cantidadIncidencias: 0,
    fechaCreacion: '2026-10-09T13:00:00Z',
    fechaModificacion: null,
  };
}

const TIPOS = [tipo(1, 'Épica', 1), tipo(2, 'Historia de Usuario', 2), tipo(3, 'Subtarea', 3), tipo(5, 'Bug', 3)];

const TODO: AlcanceSeguimientoDto = {
  todosLosNiveles: true,
  niveles: [
    { id: 1, numero: 1, nombre: 'Nivel 1' },
    { id: 2, numero: 2, nombre: 'Nivel 2' },
    { id: 3, numero: 3, nombre: 'Nivel 3' },
  ],
  todosLosTipos: true,
  tipos: [],
};

const NIVEL3_SUBTAREA_BUG: AlcanceSeguimientoDto = {
  todosLosNiveles: false,
  niveles: [{ id: 3, numero: 3, nombre: 'Nivel 3' }],
  todosLosTipos: false,
  tipos: [
    { id: 5, nombre: 'Bug', activo: true, nivelNumero: 3 },
    { id: 3, nombre: 'Subtarea', activo: true, nivelNumero: 3 },
  ],
};

/** Tests de las utilidades del alcance (RF-005 RN-04 a RN-06, RN-23). */
describe('utils/alcance', () => {
  // CA-14: sin filtro de alcance la leyenda dice "todos los niveles y tipos"
  it('textoLeyendaAlcance con todo el alcance devuelve la leyenda general', () => {
    expect(textoLeyendaAlcance(TODO)).toBe(
      'Se contabilizan: todos los niveles y tipos de incidencia.',
    );
    expect(LEYENDA_TODO_EL_ALCANCE).toBe(textoLeyendaAlcance(TODO));
  });

  // CA-15: nivel 3 con Subtarea y Bug nombra el nivel y los tipos
  it('textoLeyendaAlcance con nivel 3 y dos tipos nombra el nivel y los tipos', () => {
    expect(textoLeyendaAlcance(NIVEL3_SUBTAREA_BUG)).toBe(
      'Se contabilizan: Nivel 3 (Bug, Subtarea).',
    );
  });

  // RN-23: varios niveles se separan con " · "
  it('textoLeyendaAlcance con dos niveles los separa con punto medio', () => {
    const alcance: AlcanceSeguimientoDto = {
      todosLosNiveles: false,
      niveles: [
        { id: 3, numero: 3, nombre: 'Nivel 3' },
        { id: 2, numero: 2, nombre: 'Nivel 2' },
      ],
      todosLosTipos: true,
      tipos: [
        { id: 2, nombre: 'Historia de Usuario', activo: true, nivelNumero: 2 },
        { id: 3, nombre: 'Subtarea', activo: true, nivelNumero: 3 },
      ],
    };

    expect(textoLeyendaAlcance(alcance)).toBe(
      'Se contabilizan: Nivel 2 (Historia de Usuario) · Nivel 3 (Subtarea).',
    );
  });

  // "IndicadoresComponent": subtítulo corto con el alcance
  it('textoSubtituloAlcance resume los niveles ("Niveles 2–3")', () => {
    const alcance: AlcanceSeguimientoDto = {
      ...TODO,
      todosLosNiveles: false,
      niveles: [
        { id: 2, numero: 2, nombre: 'Nivel 2' },
        { id: 3, numero: 3, nombre: 'Nivel 3' },
      ],
    };

    expect(textoSubtituloAlcance(alcance)).toBe('Niveles 2–3');
    expect(textoSubtituloAlcance(TODO)).toBe('Todos los niveles');
  });

  // RN-04: sin niveles elegidos se ofrecen todos los tipos; con niveles, solo los de esos niveles
  it('tiposDisponibles limita los tipos a los niveles seleccionados', () => {
    expect(tiposDisponibles(TIPOS, []).map((t) => t.id)).toEqual([1, 2, 3, 5]);
    expect(tiposDisponibles(TIPOS, [3]).map((t) => t.id)).toEqual([3, 5]);
  });

  // CA-16: al quitar un nivel se quitan de la selección sus tipos
  it('depurarTiposSeleccionados quita los tipos de niveles no elegidos', () => {
    expect(depurarTiposSeleccionados([2, 3, 5], TIPOS, [3])).toEqual([3, 5]);
  });

  // CA-18: con todo el alcance no se atenúa nada; si no, los tipos contabilizados
  it('tipoIdsEnAlcance devuelve null con todo el alcance y los ids si hay filtro', () => {
    expect(tipoIdsEnAlcance(TODO)).toBeNull();
    expect([...(tipoIdsEnAlcance(NIVEL3_SUBTAREA_BUG) ?? [])]).toEqual([5, 3]);
  });
});
