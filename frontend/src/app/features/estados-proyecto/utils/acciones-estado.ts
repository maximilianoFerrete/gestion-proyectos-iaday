import { EstadoProyectoDto, LIMITES_ESTADO } from '../models/estado-proyecto.model';

/** Editar: oculto en el estado de sistema (RN-09). */
export function puedeEditar(estado: EstadoProyectoDto): boolean {
  return !estado.esSistema;
}

/** Marcar como inicial: activo, no final, no sistema y que no sea ya el inicial (RN-11). */
export function puedeMarcarInicial(estado: EstadoProyectoDto): boolean {
  return estado.activo && !estado.esFinal && !estado.esSistema && !estado.esInicial;
}

/** Desactivar: estados activos que no sean el inicial ni el de sistema (RN-09, RN-12). */
export function puedeDesactivar(estado: EstadoProyectoDto): boolean {
  return estado.activo && !estado.esInicial && !estado.esSistema;
}

/** Activar: estados inactivos no de sistema. */
export function puedeActivar(estado: EstadoProyectoDto): boolean {
  return !estado.activo && !estado.esSistema;
}

/** Orden sugerido en el alta: mayor orden de estados no de sistema + 10, tope 9998 (P7). */
export function ordenSugerido(estados: readonly EstadoProyectoDto[]): number {
  const ordenes = estados.filter((e) => !e.esSistema).map((e) => e.orden);
  const maximo = ordenes.length > 0 ? Math.max(...ordenes) : 0;
  return Math.min(maximo + LIMITES_ESTADO.incrementoOrdenSugerido, LIMITES_ESTADO.ordenMax);
}

/** Mensaje de confirmación al desactivar (avisa si hay proyectos en el estado). */
export function mensajeDesactivar(estado: EstadoProyectoDto): string {
  if (estado.cantidadProyectos > 0) {
    return (
      `Hay ${estado.cantidadProyectos} proyectos en este estado; lo conservarán, ` +
      'pero no podrá asignarse a otros.'
    );
  }
  return `¿Desactivar el estado ${estado.nombre}?`;
}
