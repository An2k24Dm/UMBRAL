import { useNavigate } from 'react-router-dom'
import { usarAutenticacion } from '../autenticacion/ProveedorAutenticacion'

export function PanelGenerico({ titulo }: { titulo: string }) {
  const { usuario, cerrar } = usarAutenticacion()
  const navegar = useNavigate()

  const cerrarSesion = () => {
    cerrar()
    navegar('/iniciar-sesion', { replace: true })
  }

  return (
    <div className="panel">
      <header>
        <h1>{titulo}</h1>
        <button className="salir" onClick={cerrarSesion}>Cerrar sesión</button>
      </header>
      <p>
        Bienvenido, <strong>{usuario?.nombre} {usuario?.apellido}</strong> ({usuario?.rol}).
      </p>
      <p>Usuario: {usuario?.nombreUsuario}</p>
    </div>
  )
}
