import { FlujoIncidenciaDto } from '../../configuracion-incidencias/models/flujo-incidencia.model';
import { TipoIncidenciaDto } from '../../configuracion-incidencias/models/tipo-incidencia.model';
import {
  opcionesEstadoCambio,
  resolverEstadoAlCambiarTipo,
  tiposDestinoCambio,
} from './cambio-tipo';

/** Flujo de prueba con los estados indicados (el primero es el inicial). */
function flujo(
  id: number,
  estados: { id: number; nombre: string; activo?: boolean }[],
): FlujoIncidenciaDto {
  return {
    id,
    nombre: `Flujo ${id}`,
    descripcion: null,
    estados: estados.map((e, i) => ({
      estadoId: e.id,
      nombre: e.nombre,
      activo: e.activo ?? true,
      orden: i + 1,
      esInicial: i === 0,
      esFinal: i === estados.length - 1,
    })),
    tipos: [],
    fechaCreacion: '2026-10-09T13:00:00Z',
    fechaModificacion: null,
  };
}

/** Tipo de prueba. */
function tipo(id: number, activo = true): TipoIncidenciaDto {
  return {
    id,
    nombre: `Tipo ${id}`,
    descripcion: null,
    nivel: { id: 3, numero: 3, nombre: 'Nivel 3' },
    flujo: { id: 1, nombre: 'Flujo general' },
    activo,
    cantidadIncidencias: 0,
    fechaCreacion: '2026-10-09T13:00:00Z',
    fechaModificacion: null,
  };
}

const GENERAL = flujo(1, [
  { id: 1, nombre: 'Por hacer' },
  { id: 2, nombre: 'En progreso' },
  { id: 3, nombre: 'Hecho' },
]);
const BUG = flujo(2, [
  { id: 4, nombre: 'Nuevo' },
  { id: 3, nombre: 'Hecho' },
]);

/** Tests de las reglas puras del diálogo "Cambiar tipo" (I3, RN-07, RN-23, CA-39). */
describe('cambio-tipo', () => {
  // RN-23, CA-37: el estado actual está en el nuevo flujo → se preselecciona el actual
  it('resolverEstadoAlCambiarTipo con el actual en el nuevo flujo devuelve el actual', () => {
    expect(resolverEstadoAlCambiarTipo(2, GENERAL)).toBe(2);
  });

  // RN-23, CA-39: el actual no está en el nuevo flujo → inicial del nuevo flujo
  it('resolverEstadoAlCambiarTipo con el actual fuera del nuevo flujo devuelve el inicial', () => {
    expect(resolverEstadoAlCambiarTipo(2, BUG)).toBe(4);
  });

  // Sin flujo cargado no hay preselección
  it('resolverEstadoAlCambiarTipo sin flujo devuelve null', () => {
    expect(resolverEstadoAlCambiarTipo(2, null)).toBeNull();
  });

  // RN-07: destino activo y distinto del actual
  it('tiposDestinoCambio excluye el tipo actual y los inactivos', () => {
    const destinos = tiposDestinoCambio([tipo(3), tipo(5), tipo(6, false)], 3);

    expect(destinos.map((t) => t.id)).toEqual([5]);
  });

  // RN-23: opciones = estados activos del nuevo flujo
  it('opcionesEstadoCambio ofrece solo estados del nuevo flujo', () => {
    const opciones = opcionesEstadoCambio(BUG, {
      id: 2,
      nombre: 'En progreso',
      activo: true,
      esFinal: false,
    });

    expect(opciones.map((o) => o.valor)).toEqual([4, 3]);
  });

  // RN-11, RN-23: el actual inactivo se ofrece solo si pertenece al nuevo flujo
  it('opcionesEstadoCambio incluye el actual inactivo si está en el nuevo flujo', () => {
    const conInactivo = flujo(1, [
      { id: 1, nombre: 'Por hacer' },
      { id: 2, nombre: 'En progreso', activo: false },
      { id: 3, nombre: 'Hecho' },
    ]);

    const opciones = opcionesEstadoCambio(conInactivo, {
      id: 2,
      nombre: 'En progreso',
      activo: false,
      esFinal: false,
    });

    expect(opciones).toContainEqual({ valor: 2, etiqueta: 'En progreso (inactivo)' });
  });
});
