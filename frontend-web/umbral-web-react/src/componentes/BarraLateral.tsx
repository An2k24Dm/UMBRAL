import { NavLink } from 'react-router-dom'
import type { Rol } from '../autenticacion/tipos'

interface EnlaceNavegacion {
  destino: string
  etiqueta: string
}

interface Props {
  rol: Rol
}

// Solo se listan las opciones de navegación que tienen una pantalla
// implementada. Los módulos pendientes (Trivias, Misiones, Sesiones,
// Ranking, Logs) se agregarán cuando dispongan de funcionalidad real.
function obtenerEnlaces(rol: Rol): EnlaceNavegacion[] {
  if (rol === 'Administrador') {
    return [
      { destino: '/administrador', etiqueta: 'Dashboard' },
      { destino: '/administrador/usuarios/registrar', etiqueta: 'Registrar usuario' },
      { destino: '/administrador/usuarios/participantes', etiqueta: 'Participantes' },
      { destino: '/administrador/usuarios/internos', etiqueta: 'Operadores y Administradores' },
      { destino: '/administrador/trivias', etiqueta: 'Trivias' },
      { destino: '/administrador/perfil', etiqueta: 'Mi perfil' }
    ]
  }
  if (rol === 'Operador') {
    return [
      { destino: '/operador', etiqueta: 'Dashboard' },
      { destino: '/operador/trivias', etiqueta: 'Trivias' },
      { destino: '/operador/busquedas', etiqueta: 'Búsquedas del tesoro' },
      { destino: '/operador/usuarios/participantes', etiqueta: 'Participantes' },
      { destino: '/operador/perfil', etiqueta: 'Mi perfil' }
    ]
  }
  return []
}

export function BarraLateral({ rol }: Props) {
  const enlaces = obtenerEnlaces(rol)
  return (
    <aside className="barra-lateral">
      <div className="barra-lateral-marca">
        <span className="marca-logo">UMBRAL</span>
        <span className="marca-subtitulo">Panel administrativo</span>
      </div>
      <nav className="barra-lateral-navegacion" aria-label="Navegación principal">
        {enlaces.map((enlace) => (
          <NavLink
            key={enlace.destino}
            to={enlace.destino}
            end={enlace.destino === '/administrador' || enlace.destino === '/operador'}
            className={({ isActive }) =>
              `enlace-nav${isActive ? ' enlace-nav-activo' : ''}`
            }
          >
            {enlace.etiqueta}
          </NavLink>
        ))}
      </nav>
      <div className="barra-lateral-pie">
        <span>UMBRAL · {rol}</span>
      </div>
    </aside>
  )
}
