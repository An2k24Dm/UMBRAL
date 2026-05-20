import { useNavigate } from 'react-router-dom'
import { usarAutenticacion } from '../autenticacion/ProveedorAutenticacion'
import { Boton } from './Boton'

interface Props {
  titulo: string
  descripcion?: string
}

function iniciales(nombre?: string, apellido?: string): string {
  const n = (nombre ?? '').trim().charAt(0)
  const a = (apellido ?? '').trim().charAt(0)
  const resultado = `${n}${a}`.toUpperCase()
  return resultado || 'U'
}

export function EncabezadoPanel({ titulo, descripcion }: Props) {
  const { usuario, cerrar } = usarAutenticacion()
  const navegar = useNavigate()

  const cerrarSesion = () => {
    cerrar()
    navegar('/iniciar-sesion', { replace: true })
  }

  return (
    <header className="encabezado-panel">
      <div className="encabezado-panel-titulo">
        <h1>{titulo}</h1>
        {descripcion && <p>{descripcion}</p>}
      </div>
      <div className="encabezado-panel-usuario">
        <div className="avatar" aria-hidden>
          {iniciales(usuario?.nombre, usuario?.apellido)}
        </div>
        <div className="encabezado-panel-datos">
          <span className="encabezado-panel-nombre">
            {usuario?.nombre} {usuario?.apellido}
          </span>
          <span className="encabezado-panel-rol">{usuario?.rol}</span>
        </div>
        <Boton variante="secundario" onClick={cerrarSesion}>Cerrar sesión</Boton>
      </div>
    </header>
  )
}
