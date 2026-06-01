// HU34 — Helpers compartidos para formatear datos de sesiones en la
// UI. Aislados acá para mantener consistencia entre el listado y el
// detalle, y para que un solo cambio repercuta en ambas pantallas.

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

// Convierte el tipo de juego que viene del backend (Trivia /
// BusquedaTesoro) en su nombre legible para mostrar al usuario.
export function nombreTipoJuego(tipoJuego: string | null | undefined): string {
  if (!tipoJuego) return '—'
  if (tipoJuego === 'BusquedaTesoro') return 'Búsqueda del Tesoro'
  return tipoJuego
}

// Devuelve el nombre legible del estado. Útil fuera del badge cuando
// se necesita el texto inline (ej. tooltip, mensaje, título secundario).
export function nombreEstadoSesion(estado: string | null | undefined): string {
  if (!estado) return '—'
  if (estado === 'EnPreparacion') return 'En preparación'
  return estado
}
