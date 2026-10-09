/** Convierte un parámetro de ruta/query en id entero positivo o null si no es válido. */
export function parsearId(valor: string | null | undefined): number | null {
  if (valor === null || valor === undefined || !/^\d+$/.test(valor)) {
    return null;
  }
  const id = Number(valor);
  return Number.isSafeInteger(id) && id > 0 ? id : null;
}
