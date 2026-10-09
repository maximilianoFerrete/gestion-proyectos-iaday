import { ValoresProyectoForm } from '../components/proyecto-form/proyecto-form.component';
import { CrearProyectoRequest, EditarProyectoRequest } from '../models/proyecto.model';

/** Arma el body de `POST /api/proyectos` (sin estado, RN-18). */
export function aCrearProyectoRequest(valores: ValoresProyectoForm): CrearProyectoRequest {
  return {
    nombre: valores.nombre,
    descripcion: valores.descripcion,
    responsableId: valores.responsableId,
    fechaInicio: valores.fechaInicio,
    fechaObjetivo: valores.fechaObjetivo,
  };
}

/** Arma el body de `PUT /api/proyectos/{id}`; null si falta el estado. */
export function aEditarProyectoRequest(valores: ValoresProyectoForm): EditarProyectoRequest | null {
  if (valores.estadoId === null) {
    return null;
  }
  return { ...aCrearProyectoRequest(valores), estadoId: valores.estadoId };
}
