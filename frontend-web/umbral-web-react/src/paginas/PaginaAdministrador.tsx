import { Link, useNavigate } from 'react-router-dom'
import { usarAutenticacion } from '../autenticacion/ProveedorAutenticacion'

export function PaginaAdministrador() {
  const { usuario, cerrar } = usarAutenticacion()
  const navegar = useNavigate()

  const cerrarSesion = () => {
    cerrar()
    navegar('/iniciar-sesion', { replace: true })
  }

  return (
    <div className="panel">
      <header>
        <h1>Panel de Administración</h1>
        <button className="salir" onClick={cerrarSesion}>Cerrar sesión</button>
      </header>
      <p>
        Bienvenido, <strong>{usuario?.nombre} {usuario?.apellido}</strong> ({usuario?.rol}).
      </p>
      <p>Usuario: {usuario?.nombreUsuario}</p>

      <section className="acciones">
        <h2>Gestión de usuarios</h2>
        <Link to="/administrador/usuarios/registrar" className="boton-enlace">
          Registrar usuario
        </Link>
      </section>
    </div>
  )
}
