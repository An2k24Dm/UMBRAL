import { NavLink } from 'react-router-dom'
import type { Rol } from '../autenticacion/tipos'

interface EnlaceNavegacion {
  destino: string
  etiqueta: string
  proximamente?: boolean
}

interface Props {
  rol: Rol
}

function obtenerEnlaces(rol: Rol): EnlaceNavegacion[] {
  if (rol === 'Administrador') {
    return [
      { destino: '/administrador', etiqueta: 'Dashboard' },
      { destino: '/administrador/usuarios/registrar', etiqueta: 'Registrar usuario' },
      { destino: '/administrador/usuarios/participantes', etiqueta: 'Participantes' },
      { destino: '/administrador/usuarios/internos', etiqueta: 'Operadores y Administradores' },
      { destino: '/administrador/perfil', etiqueta: 'Mi perfil' },
      { destino: '#', etiqueta: 'Trivias', proximamente: true },
      { destino: '#', etiqueta: 'Misiones', proximamente: true },
      { destino: '#', etiqueta: 'Sesiones', proximamente: true },
      { destino: '#', etiqueta: 'Ranking', proximamente: true },
      { destino: '#', etiqueta: 'Logs', proximamente: true }
    ]
  }
  if (rol === 'Operador') {
    return [
      { destino: '/operador', etiqueta: 'Dashboard' },
      { destino: '/operador/usuarios/participantes', etiqueta: 'Participantes' },
      { destino: '/operador/perfil', etiqueta: 'Mi perfil' },
      { destino: '#', etiqueta: 'Sesiones', proximamente: true }
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
        {enlaces.map((enlace) =>
          enlace.proximamente ? (
            <span key={enlace.etiqueta} className="enlace-nav enlace-nav-deshabilitado" aria-disabled="true">
              {enlace.etiqueta}
              <span className="etiqueta-proximamente">Próximamente</span>
            </span>
          ) : (
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
          )
        )}
      </nav>
      <div className="barra-lateral-pie">
        <span>UMBRAL · {rol}</span>
      </div>
    </aside>
  )
}
