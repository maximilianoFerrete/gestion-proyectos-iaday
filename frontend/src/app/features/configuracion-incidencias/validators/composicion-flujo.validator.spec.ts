import { FormControl } from '@angular/forms';
import { EstadoFlujoRequest } from '../models/flujo-incidencia.model';
import {
  ERRORES_COMPOSICION_FLUJO,
  composicionFlujoValida,
  erroresComposicionFlujo,
} from './composicion-flujo.validator';

/** Estado de flujo para la composición. */
function estado(estadoId: number, esInicial = false, esFinal = false): EstadoFlujoRequest {
  return { estadoId, esInicial, esFinal };
}

/** Tests del validador de composición de flujo (C4, RN-13, RN-14, CA-07). */
describe('composicion-flujo.validator', () => {
  // RN-13, CA-07: sin estados
  it('sin estados devuelve flujoSinEstados', () => {
    expect(erroresComposicionFlujo([])).toEqual({ [ERRORES_COMPOSICION_FLUJO.sinEstados]: true });
  });

  // RN-14, CA-07: sin estado inicial
  it('sin inicial devuelve flujoInicialInvalido', () => {
    expect(erroresComposicionFlujo([estado(1), estado(3, false, true)])).toEqual({
      [ERRORES_COMPOSICION_FLUJO.inicial]: true,
    });
  });

  // RN-14, CA-07: dos iniciales
  it('con dos iniciales devuelve flujoInicialInvalido', () => {
    expect(erroresComposicionFlujo([estado(1, true), estado(2, true)])).toEqual({
      [ERRORES_COMPOSICION_FLUJO.inicial]: true,
    });
  });

  // RN-14, CA-07: inicial y final a la vez
  it('con un estado inicial y final devuelve flujoInicialYFinal', () => {
    expect(erroresComposicionFlujo([estado(1, true, true)])).toEqual({
      [ERRORES_COMPOSICION_FLUJO.inicialFinal]: true,
    });
  });

  // RN-13: estado repetido
  it('con un estado repetido devuelve flujoEstadosRepetidos', () => {
    expect(erroresComposicionFlujo([estado(1, true), estado(1)])).toEqual({
      [ERRORES_COMPOSICION_FLUJO.repetidos]: true,
    });
  });

  // RN-14: un inicial y cero finales es válido
  it('un inicial sin finales es válido', () => {
    expect(erroresComposicionFlujo([estado(1, true), estado(2)])).toBeNull();
  });

  // CA-07: el validador del control usa la lista del valor; un valor no lista equivale a vacío
  it('composicionFlujoValida sobre un control sin lista devuelve flujoSinEstados', () => {
    expect(composicionFlujoValida(new FormControl(null))).toEqual({
      [ERRORES_COMPOSICION_FLUJO.sinEstados]: true,
    });
    expect(composicionFlujoValida(new FormControl([estado(1, true)]))).toBeNull();
  });
});
