import { EstadoIncidenciaDto } from '../models/estado-incidencia.model';
import { PrioridadIncidenciaDto } from '../models/prioridad-incidencia.model';
import { RelacionTipoIncidenciaDto } from '../models/relacion-tipo-incidencia.model';
import { TipoIncidenciaDto } from '../models/tipo-incidencia.model';
import {
  mensajeCrearRelacion,
  mensajeDesactivarTipo,
  ordenSugeridoEstado,
  puedeActivarPrioridad,
  puedeDesactivarEstado,
  puedeDesactivarPrioridad,
  puedeMarcarPredeterminada,
  tiposHijoElegibles,
} from './reglas-configuracion';

/** Tipo de prueba en el nivel indicado. */
function tipo(id: number, nivel: number, activo = true): TipoIncidenciaDto {
  return {
    id,
    nombre: `Tipo ${id}`,
    descripcion: null,
    nivel: { id: nivel, numero: nivel, nombre: `Nivel ${nivel}` },
    flujo: { id: 1, nombre: 'Flujo general' },
    activo,
    cantidadIncidencias: 3,
    fechaCreacion: '2026-10-09T13:00:00Z',
    fechaModificacion: null,
  };
}

/** Relación existente padre → hijo. */
function relacion(padre: number, hijo: number): RelacionTipoIncidenciaDto {
  return {
    id: padre * 10 + hijo,
    tipoPadre: { id: padre, nombre: `Tipo ${padre}`, activo: true, nivelNumero: 1 },
    tipoHijo: { id: hijo, nombre: `Tipo ${hijo}`, activo: true, nivelNumero: 2 },
    fechaCreacion: '2026-10-09T13:00:00Z',
  };
}

/** Estado de prueba con sus marcas en flujos. */
function estado(
  id: number,
  activo: boolean,
  inicialEnFlujo: boolean,
  orden = 10,
): EstadoIncidenciaDto {
  return {
    id,
    nombre: `Estado ${id}`,
    descripcion: null,
    orden,
    activo,
    cantidadIncidencias: 0,
    flujos: [{ id: 1, nombre: 'Flujo general', esInicial: inicialEnFlujo, esFinal: false }],
  };
}

/** Prioridad de prueba. */
function prioridad(activo: boolean, esPredeterminada: boolean): PrioridadIncidenciaDto {
  return {
    id: 2,
    nombre: 'Media',
    descripcion: null,
    orden: 20,
    esPredeterminada,
    activo,
    cantidadIncidencias: 0,
  };
}

const TIPOS = [tipo(1, 1), tipo(2, 2), tipo(3, 3), tipo(4, 3), tipo(5, 3, false), tipo(6, 2)];

/** Tests de las reglas puras de las pantallas de configuración (C1–C5). */
describe('reglas-configuracion', () => {
  // RN-08, CA-05: el select de hijo solo ofrece tipos activos de nivel mayor y sin relación
  it('tiposHijoElegibles devuelve activos de nivel mayor sin relación existente', () => {
    const elegibles = tiposHijoElegibles(TIPOS, [relacion(2, 3)], 2);

    expect(elegibles.map((t) => t.id)).toEqual([4]);
  });

  // RN-08: sin padre elegido no hay hijos elegibles
  it('tiposHijoElegibles sin padre devuelve vacío', () => {
    expect(tiposHijoElegibles(TIPOS, [], null)).toEqual([]);
  });

  // RN-08: un padre del último nivel no tiene hijos elegibles
  it('tiposHijoElegibles con padre de nivel 3 devuelve vacío', () => {
    expect(tiposHijoElegibles(TIPOS, [], 3)).toEqual([]);
  });

  // RN-11, CA-10: no se ofrece Desactivar en el inicial de algún flujo
  it('puedeDesactivarEstado es false para un estado inicial de algún flujo', () => {
    expect(puedeDesactivarEstado(estado(1, true, true))).toBe(false);
    expect(puedeDesactivarEstado(estado(2, true, false))).toBe(true);
    expect(puedeDesactivarEstado(estado(2, false, false))).toBe(false);
  });

  // RN-17: sin Desactivar ni "Marcar predeterminada" en la predeterminada
  it('la predeterminada no puede desactivarse ni volver a marcarse', () => {
    expect(puedeDesactivarPrioridad(prioridad(true, true))).toBe(false);
    expect(puedeMarcarPredeterminada(prioridad(true, true))).toBe(false);
  });

  // RN-17: sin "Marcar predeterminada" en inactivas; se pueden activar
  it('una inactiva no puede marcarse predeterminada pero sí activarse', () => {
    expect(puedeMarcarPredeterminada(prioridad(false, false))).toBe(false);
    expect(puedeActivarPrioridad(prioridad(false, false))).toBe(true);
  });

  // RN-17: una activa no predeterminada puede marcarse y desactivarse
  it('una activa no predeterminada puede marcarse predeterminada y desactivarse', () => {
    expect(puedeMarcarPredeterminada(prioridad(true, false))).toBe(true);
    expect(puedeDesactivarPrioridad(prioridad(true, false))).toBe(true);
  });

  // C3: orden sugerido = mayor + 10, tope 9999
  it('ordenSugeridoEstado suma 10 al mayor y respeta el tope 9999', () => {
    expect(ordenSugeridoEstado([estado(1, true, true, 10), estado(2, true, false, 30)])).toBe(40);
    expect(ordenSugeridoEstado([estado(1, true, true, 9995)])).toBe(9999);
    expect(ordenSugeridoEstado([])).toBe(10);
  });

  // C2, RN-09: confirmación de relación inmutable
  it('mensajeCrearRelacion advierte que no podrá modificarse ni eliminarse', () => {
    expect(mensajeCrearRelacion('Historia de Usuario', 'Spike')).toBe(
      'La relación Historia de Usuario → Spike no podrá modificarse ni eliminarse.',
    );
  });

  // C1, RN-07: confirmación de desactivar tipo con la cantidad
  it('mensajeDesactivarTipo informa la cantidad de incidencias', () => {
    expect(mensajeDesactivarTipo(tipo(2, 2))).toBe(
      'Las 3 incidencias de este tipo lo conservan, pero no podrán crearse nuevas incidencias de este tipo.',
    );
  });
});
