import { EstadoProyectoDto } from '../models/estado-proyecto.model';
import {
  mensajeDesactivar,
  ordenSugerido,
  puedeActivar,
  puedeDesactivar,
  puedeEditar,
  puedeMarcarInicial,
} from './acciones-estado';

/** Crea un estado de prueba (activo, no inicial, no final, no de sistema por defecto). */
function estado(parcial: Partial<EstadoProyectoDto> = {}): EstadoProyectoDto {
  return {
    id: 3,
    nombre: 'En curso',
    descripcion: null,
    orden: 30,
    esInicial: false,
    esFinal: false,
    activo: true,
    esSistema: false,
    codigo: null,
    cantidadProyectos: 0,
    ...parcial,
  };
}

const sistema = estado({ id: 7, nombre: 'Archivado', orden: 9999, esSistema: true, codigo: 'ARCHIVADO' });

/** Tests de las reglas de acciones por estado en P7 (RN-09 a RN-12, CA-11, CA-12). */
describe('acciones-estado', () => {
  // RN-11, CA-11: activo, no final, no sistema y no inicial
  it('puedeMarcarInicial es true para un estado activo no final', () => {
    expect(puedeMarcarInicial(estado())).toBe(true);
  });

  // RN-11, CA-11: final, inactivo, sistema o ya inicial → no se ofrece
  it.each([
    ['final', estado({ esFinal: true })],
    ['inactivo', estado({ activo: false })],
    ['sistema', sistema],
    ['ya inicial', estado({ esInicial: true })],
  ])('puedeMarcarInicial es false para un estado %s', (_caso, valor) => {
    expect(puedeMarcarInicial(valor)).toBe(false);
  });

  // RN-12, RN-09, CA-12: no se desactiva el inicial ni el de sistema
  it.each([
    ['inicial', estado({ esInicial: true })],
    ['sistema', sistema],
    ['ya inactivo', estado({ activo: false })],
  ])('puedeDesactivar es false para un estado %s', (_caso, valor) => {
    expect(puedeDesactivar(valor)).toBe(false);
  });

  // RN-13: un estado activo común se puede desactivar
  it('puedeDesactivar es true para un estado activo no inicial', () => {
    expect(puedeDesactivar(estado())).toBe(true);
  });

  // RN-09: Editar oculto en el de sistema; Activar solo en inactivos no de sistema
  it('puedeEditar y puedeActivar respetan el estado de sistema', () => {
    expect(puedeEditar(sistema)).toBe(false);
    expect(puedeEditar(estado())).toBe(true);
    expect(puedeActivar(estado({ activo: false }))).toBe(true);
    expect(puedeActivar({ ...sistema, activo: false })).toBe(false);
  });

  // P7: mayor orden no de sistema + 10
  it('ordenSugerido ignora el de sistema y suma 10 al mayor', () => {
    expect(ordenSugerido([estado({ orden: 10 }), estado({ orden: 60 }), sistema])).toBe(70);
  });

  // P7: tope 9998
  it('ordenSugerido no supera 9998', () => {
    expect(ordenSugerido([estado({ orden: 9995 })])).toBe(9998);
  });

  // P7: sin estados configurables sugiere 10
  it('ordenSugerido sin estados configurables devuelve 10', () => {
    expect(ordenSugerido([sistema])).toBe(10);
  });

  // P7: la confirmación avisa la cantidad de proyectos
  it('mensajeDesactivar con proyectos avisa la cantidad', () => {
    expect(mensajeDesactivar(estado({ cantidadProyectos: 3 }))).toBe(
      'Hay 3 proyectos en este estado; lo conservarán, pero no podrá asignarse a otros.',
    );
  });
});
