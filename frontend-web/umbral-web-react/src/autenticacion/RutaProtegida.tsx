import { Navigate } from 'react-router-dom'
import type { ReactNode } from 'react'
import { usarAutenticacion } from './ProveedorAutenticacion'
import type { Rol } from './tipos'

interface Props {
  rolesPermitidos?: Rol[]
  children: ReactNode
}

export function RutaProtegida({ rolesPermitidos, children }: Props) {
  const { token, usuario } = usarAutenticacion()

  if (!token || !usuario) {
    return <Navigate to="/iniciar-sesion" replace />
  }

  if (rolesPermitidos && !rolesPermitidos.includes(usuario.rol)) {
    return <Navigate to="/iniciar-sesion" replace />
  }

  return <>{children}</>
}
