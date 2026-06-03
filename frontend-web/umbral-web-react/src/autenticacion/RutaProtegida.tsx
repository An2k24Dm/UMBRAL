import { Navigate } from 'react-router-dom'
import type { ReactNode } from 'react'
import { usarAutenticacion } from './ProveedorAutenticacion'
import type { Rol } from './tipos'

interface Props {
  rolesPermitidos?: Rol[]
  children: ReactNode
}

// Dashboard al que mandamos a un usuario autenticado cuando intenta
// abrir una ruta que su rol no puede ver. Evita lanzarlo al login con
// la sesión todavía activa.
const DASHBOARD_POR_ROL: Record<Rol, string> = {
  Administrador: '/administrador',
  Operador: '/operador',
  Participante: '/iniciar-sesion',
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

  // Si está autenticado pero su rol no puede ver esta ruta, lo
  // redirigimos al dashboard de SU rol en lugar de mandarlo al login.
  // Así, si el Operador escribe /administrador/trivias en la barra de
  // direcciones, vuelve a /operador sin perder la sesión.
  if (rolesPermitidos && !rolesPermitidos.includes(usuario.rol)) {
    return <Navigate to={DASHBOARD_POR_ROL[usuario.rol]} replace />
  }

  return <>{children}</>
}
