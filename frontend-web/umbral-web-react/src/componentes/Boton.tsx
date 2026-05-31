import type { ButtonHTMLAttributes, ReactNode } from 'react'

type Variante = 'primario' | 'secundario' | 'peligro' | 'volver' | 'fantasma'
type Tamaño = 'sm' | 'md' | 'lg'

interface Props extends ButtonHTMLAttributes<HTMLButtonElement> {
  variante?: Variante
  tamaño?: Tamaño
  cargando?: boolean
  ancho?: boolean
  children: ReactNode
}

export function Boton({
  variante = 'primario',
  tamaño = 'md',
  cargando = false,
  ancho = false,
  className,
  children,
  disabled,
  ...resto
}: Props) {
  const clases = [
    'boton-ui',
    `boton-${variante}`,
    tamaño !== 'md' ? `boton-${tamaño}` : '',
    ancho ? 'boton-ancho' : '',
    className ?? '',
  ]
    .filter(Boolean)
    .join(' ')

  return (
    <button className={clases} disabled={disabled || cargando} {...resto}>
      {cargando && <span className="boton-spinner" aria-hidden="true" />}
      {children}
    </button>
  )
}
