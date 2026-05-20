import type { ReactNode } from 'react'

interface Props {
  etiqueta: string
  htmlFor?: string
  error?: string
  ayuda?: string
  opcional?: string
  children: ReactNode
}

export function CampoFormulario({ etiqueta, htmlFor, error, ayuda, opcional, children }: Props) {
  return (
    <div className="campo">
      <label htmlFor={htmlFor}>
        {etiqueta}
        {opcional && <span className="opcional"> ({opcional})</span>}
      </label>
      {children}
      {ayuda && !error && <span className="ayuda-campo">{ayuda}</span>}
      {error && <span className="error-campo">{error}</span>}
    </div>
  )
}
