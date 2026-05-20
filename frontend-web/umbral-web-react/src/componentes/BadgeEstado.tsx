import type { EstadoUsuario } from '../autenticacion/tipos'

interface Props {
  estado?: EstadoUsuario | null
}

export function BadgeEstado({ estado }: Props) {
  const valor = (estado ?? '').toString().trim()
  if (!valor) return <span className="badge badge-neutro">No disponible</span>
  const normalizado = valor.toLowerCase()
  const clase =
    normalizado === 'activo' ? 'badge-activo'
    : normalizado === 'inactivo' ? 'badge-inactivo'
    : 'badge-neutro'
  return <span className={`badge ${clase}`}>{valor}</span>
}
