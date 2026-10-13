import { IndicadoresDto } from '../models/seguimiento.model';
import {
  avisoSinEstadosCategoria,
  definicionIndicador,
  porcentaje,
  tarjetasIndicadores,
} from './indicadores';

const INDICADORES: IndicadoresDto = {
  total: 42,
  abiertas: 30,
  finalizadas: 12,
  vencidas: 5,
  bloqueadas: 0,
  enEspera: 0,
  sinResponsable: 7,
  porEstado: [],
};

/** Tests de las tarjetas y definiciones de indicadores (RF-005 RN-09 a RN-14, RN-23). */
describe('utils/indicadores', () => {
  // "IndicadoresComponent": seis tarjetas en el orden de la spec
  it('tarjetasIndicadores devuelve las seis tarjetas en orden con sus valores', () => {
    const tarjetas = tarjetasIndicadores(INDICADORES, '2026-10-10');

    expect(tarjetas.map((t) => [t.clave, t.valor])).toEqual([
      ['total', 42],
      ['abiertas', 30],
      ['vencidas', 5],
      ['bloqueadas', 0],
      ['en-espera', 0],
      ['sin-responsable', 7],
    ]);
  });

  // "IndicadoresComponent": alerta solo en Vencidas/Bloqueadas con valor > 0
  it('marca alerta en Vencidas con valor > 0 y no en Bloqueadas en 0', () => {
    const tarjetas = tarjetasIndicadores(INDICADORES, '2026-10-10');

    expect(tarjetas.filter((t) => t.alerta).map((t) => t.clave)).toEqual(['vencidas']);
  });

  // RN-23: la definición de Vencidas incluye la fecha de referencia
  it('la definición de Vencidas nombra la fecha de referencia en dd/MM/yyyy', () => {
    expect(definicionIndicador('vencidas', '2026-10-10')).toBe(
      'Abiertas con vencimiento anterior al 10/10/2026.',
    );
  });

  // CA-12: aviso cuando ningún estado tiene la categoría
  it('avisoSinEstadosCategoria avisa que el indicador siempre será 0', () => {
    expect(avisoSinEstadosCategoria('EN_ESPERA')).toBe(
      'No hay estados configurados como En espera; el indicador siempre será 0.',
    );
  });

  // "ConteoPorEstadoComponent": porcentaje entero y 0 si el total es 0
  it('porcentaje redondea y devuelve 0 con total 0', () => {
    expect([porcentaje(1, 3), porcentaje(5, 0)]).toEqual([33, 0]);
  });
});
