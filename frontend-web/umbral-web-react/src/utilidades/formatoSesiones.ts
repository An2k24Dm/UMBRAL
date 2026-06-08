// Helpers compartidos para formatear datos de sesiones en la UI.
// Aislados acá para que un solo cambio repercuta en listado, detalle y
// vista de equipo.

export function formatearFechaSesion(iso: string | null | undefined): string {
  if (!iso) return '—'
  const fecha = new Date(iso)
  if (Number.isNaN(fecha.getTime())) return '—'
  return fecha.toLocaleString('es-VE', {
    day: '2-digit',
    month: '2-digit',
    year: 'numeric',
    hour: '2-digit',
    minute: '2-digit'
  })
}

// Texto legible para el estado. El badge ya usa el valor crudo del enum;
// esto se usa fuera del badge (tooltips, mensajes).
export function nombreEstadoSesion(estado: string | null | undefined): string {
  if (!estado) return '—'
  if (estado === 'EnPreparacion') return 'En preparación'
  return estado
}

// Texto legible para el modo de sesión.
export function nombreModoSesion(modo: string | null | undefined): string {
  if (!modo) return '—'
  if (modo === 'Grupal') return 'Grupal'
  if (modo === 'Individual') return 'Individual'
  return modo
}

// Texto del tipo de contenido de una etapa.
export function nombreTipoContenidoEtapa(tipo: string | null | undefined): string {
  if (!tipo) return '—'
  if (tipo === 'BusquedaTesoro') return 'Búsqueda del Tesoro'
  if (tipo === 'Trivia') return 'Trivia'
  return tipo
}
