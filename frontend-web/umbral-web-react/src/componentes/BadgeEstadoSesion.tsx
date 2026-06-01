// HU34 — Badge visual para el estado de una sesión.
//
// Recibe el enum tal como lo entrega el backend (Programada,
// EnPreparacion, Activa, Pausada, Finalizada, Cancelada) y lo renderiza
// con la misma estética del panel (badge pastilla, colores semánticos
// consistentes con .badge-activo / .badge-inactivo / .badge-neutro).
//
// "EnPreparacion" se muestra como "EN PREPARACIÓN".

interface Props {
  estado?: string | null
}

// Etiqueta legible en mayúsculas (el CSS de .badge ya aplica
// text-transform: uppercase y letter-spacing).
function etiqueta(valor: string): string {
  if (valor === 'EnPreparacion') return 'En preparación'
  return valor
}

function clase(valor: string): string {
  switch (valor) {
    case 'Programada':    return 'badge-sesion-programada'
    case 'EnPreparacion': return 'badge-sesion-preparacion'
    case 'Activa':        return 'badge-sesion-activa'
    case 'Pausada':       return 'badge-sesion-pausada'
    case 'Finalizada':    return 'badge-sesion-finalizada'
    case 'Cancelada':     return 'badge-sesion-cancelada'
    default:              return 'badge-neutro'
  }
}

export function BadgeEstadoSesion({ estado }: Props) {
  const valor = (estado ?? '').toString().trim()
  if (!valor) return <span className="badge badge-neutro">No disponible</span>
  return <span className={`badge ${clase(valor)}`}>{etiqueta(valor)}</span>
}
