import type { ButtonHTMLAttributes, ReactNode } from 'react'

type Variante = 'primario' | 'secundario' | 'peligro' | 'volver' | 'fantasma'

interface Props extends ButtonHTMLAttributes<HTMLButtonElement> {
  variante?: Variante
  children: ReactNode
}

export function Boton({ variante = 'primario', className, children, ...resto }: Props) {
  const clase = `boton-ui boton-${variante}${className ? ` ${className}` : ''}`
  return (
    <button className={clase} {...resto}>
      {children}
    </button>
  )
}
