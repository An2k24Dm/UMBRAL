import type { ReactNode } from 'react'

type Tono = 'exito' | 'error' | 'informacion' | 'aviso'

interface Props {
  tono: Tono
  children: ReactNode
}

function IconoExito() {
  return (
    <svg className="alerta-icono" viewBox="0 0 16 16" fill="none" aria-hidden="true">
      <circle cx="8" cy="8" r="7" stroke="currentColor" strokeWidth="1.5" />
      <path d="M5 8l2 2 4-4" stroke="currentColor" strokeWidth="1.5" strokeLinecap="round" strokeLinejoin="round" />
    </svg>
  )
}

function IconoError() {
  return (
    <svg className="alerta-icono" viewBox="0 0 16 16" fill="none" aria-hidden="true">
      <circle cx="8" cy="8" r="7" stroke="currentColor" strokeWidth="1.5" />
      <path d="M8 5v3.5M8 11h.01" stroke="currentColor" strokeWidth="1.5" strokeLinecap="round" />
    </svg>
  )
}

function IconoInfo() {
  return (
    <svg className="alerta-icono" viewBox="0 0 16 16" fill="none" aria-hidden="true">
      <circle cx="8" cy="8" r="7" stroke="currentColor" strokeWidth="1.5" />
      <path d="M8 7.5v3.5M8 5h.01" stroke="currentColor" strokeWidth="1.5" strokeLinecap="round" />
    </svg>
  )
}

function IconoAviso() {
  return (
    <svg className="alerta-icono" viewBox="0 0 16 16" fill="none" aria-hidden="true">
      <path d="M8 2L14.5 13.5H1.5L8 2z" stroke="currentColor" strokeWidth="1.5" strokeLinejoin="round" />
      <path d="M8 6.5v3M8 11.5h.01" stroke="currentColor" strokeWidth="1.5" strokeLinecap="round" />
    </svg>
  )
}

const iconos: Record<Tono, ReactNode> = {
  exito: <IconoExito />,
  error: <IconoError />,
  informacion: <IconoInfo />,
  aviso: <IconoAviso />,
}

export function Alerta({ tono, children }: Props) {
  return (
    <div className={`alerta alerta-${tono}`} role="alert">
      {iconos[tono]}
      <span>{children}</span>
    </div>
  )
}
