import type { ReactNode } from 'react'
import { usarAutenticacion } from '../autenticacion/ProveedorAutenticacion'
import { BarraLateral } from './BarraLateral'
import { EncabezadoPanel } from './EncabezadoPanel'

interface Props {
  titulo: string
  descripcion?: string
  children: ReactNode
}

export function LayoutPanel({ titulo, descripcion, children }: Props) {
  const { usuario } = usarAutenticacion()
  if (!usuario) return null

  return (
    <div className="layout-panel">
      <BarraLateral rol={usuario.rol} />
      <div className="layout-panel-contenido">
        <EncabezadoPanel titulo={titulo} descripcion={descripcion} />
        <main className="layout-panel-principal">{children}</main>
      </div>
    </div>
  )
}
