// Helpers de formato para fechas ISO 8601 que llegan desde el backend.
//
// Se centralizan aquí para que listado y detalle muestren la misma
// representación. La hora se muestra con reloj de 24 h para evitar
// ambigüedad PM/AM en sesiones programadas.

export function formatearFechaCorta(valorIso?: string | null): string {
  if (!valorIso) return "Sin fecha";
  const fecha = new Date(valorIso);
  if (Number.isNaN(fecha.getTime())) return "Sin fecha";
  const anio = fecha.getFullYear();
  const mes = String(fecha.getMonth() + 1).padStart(2, "0");
  const dia = String(fecha.getDate()).padStart(2, "0");
  return `${dia}/${mes}/${anio}`;
}

export function formatearFechaHora(valorIso?: string | null): string {
  if (!valorIso) return "Sin fecha";
  const fecha = new Date(valorIso);
  if (Number.isNaN(fecha.getTime())) return "Sin fecha";
  const anio = fecha.getFullYear();
  const mes = String(fecha.getMonth() + 1).padStart(2, "0");
  const dia = String(fecha.getDate()).padStart(2, "0");
  const horas = String(fecha.getHours()).padStart(2, "0");
  const minutos = String(fecha.getMinutes()).padStart(2, "0");
  return `${dia}/${mes}/${anio} ${horas}:${minutos}`;
}
