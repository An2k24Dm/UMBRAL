import { Navigate } from 'react-router-dom'
import type { ReactNode } from 'react'
import { usarAutenticacion } from './ProveedorAutenticacion'
import type { Rol } from './tipos'

interface Props {
  rolesPermitidos?: Rol[]
  children: ReactNode
}

export function RutaProtegida({ rolesPermitidos, children }: Props) {
  const { token, usuario, cargandoSesion } = usarAutenticacion()

  // Mientras el ProveedorAutenticacion termina de restaurar el estado
  // desde localStorage, no decidimos nada: ni redirigimos al login ni
  // renderizamos la ruta. Esto evita el "flash" al login en cada
  // recarga, que era el síntoma del bug original.
  if (cargandoSesion) {
    return (
      <div className="cargando-sesion" role="status" aria-live="polite">
        Cargando…
      </div>
    )
  }

  if (!token || !usuario) {
    return <Navigate to="/iniciar-sesion" replace />
  }

  if (rolesPermitidos && !rolesPermitidos.includes(usuario.rol)) {
    return <Navigate to="/iniciar-sesion" replace />
  }

  return <>{children}</>
}
