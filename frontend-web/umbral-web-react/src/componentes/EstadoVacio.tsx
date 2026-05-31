import type { ReactNode } from 'react'

interface Props {
  titulo?: string
  descripcion?: string
  accion?: ReactNode
  icono?: ReactNode
}

function IconoPorDefecto() {
  return (
    <svg width="22" height="22" viewBox="0 0 22 22" fill="none" aria-hidden="true">
      <circle cx="11" cy="11" r="9" stroke="currentColor" strokeWidth="1.5" />
      <path d="M11 8v4M11 14.5h.01" stroke="currentColor" strokeWidth="1.5" strokeLinecap="round" />
    </svg>
  )
}

export function EstadoVacio({ titulo = 'Sin resultados', descripcion, accion, icono }: Props) {
  return (
    <div className="estado-vacio">
      <div className="estado-vacio-icono">
        {icono ?? <IconoPorDefecto />}
      </div>
      <p className="estado-vacio-titulo">{titulo}</p>
      {descripcion && <p className="estado-vacio-desc">{descripcion}</p>}
      {accion}
    </div>
  )
}
