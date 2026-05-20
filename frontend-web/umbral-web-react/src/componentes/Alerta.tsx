import type { ReactNode } from 'react'

type Tono = 'exito' | 'error' | 'informacion' | 'aviso'

interface Props {
  tono: Tono
  children: ReactNode
}

export function Alerta({ tono, children }: Props) {
  return <div className={`alerta alerta-${tono}`} role="alert">{children}</div>
}
