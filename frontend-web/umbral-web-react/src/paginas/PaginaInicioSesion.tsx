import { useState, type FormEvent } from 'react'
import { useNavigate } from 'react-router-dom'
import { iniciarSesion } from '../autenticacion/clienteApi'
import { usarAutenticacion } from '../autenticacion/ProveedorAutenticacion'

export function PaginaInicioSesion() {
  const [nombreUsuario, setNombreUsuario] = useState('')
  const [contrasena, setContrasena] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [cargando, setCargando] = useState(false)
  const { iniciar } = usarAutenticacion()
  const navegar = useNavigate()

  const enviar = async (evento: FormEvent) => {
    evento.preventDefault()
    setError(null)
    setCargando(true)
    try {
      const respuesta = await iniciarSesion(nombreUsuario, contrasena)
      iniciar(respuesta.tokenAcceso, respuesta.usuario)
      navegar(respuesta.rutaRedireccion, { replace: true })
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Error desconocido.')
    } finally {
      setCargando(false)
    }
  }

  return (
    <div className="pagina-auth">
      <div className="auth-contenedor">
        <div className="auth-marca">
          <span className="auth-logo">
            <span className="auth-logo-acento">U</span>MBRAL
          </span>
          <span className="auth-subtitulo">Plataforma de juegos y misiones</span>
        </div>

        <div className="auth-tarjeta">
          <h2>Iniciar sesión</h2>

          {error && <div className="error" role="alert">{error}</div>}

          <form onSubmit={enviar}>
            <div className="campo">
              <label htmlFor="nombreUsuario">Nombre de usuario</label>
              <input
                id="nombreUsuario"
                type="text"
                value={nombreUsuario}
                onChange={(e) => setNombreUsuario(e.target.value)}
                required
                autoComplete="username"
                placeholder="tu.usuario"
              />
            </div>

            <div className="campo">
              <label htmlFor="contrasena">Contraseña</label>
              <input
                id="contrasena"
                type="password"
                value={contrasena}
                onChange={(e) => setContrasena(e.target.value)}
                required
                autoComplete="current-password"
                placeholder="••••••••"
              />
            </div>

            <button
              className="boton"
              type="submit"
              disabled={cargando}
              style={{ marginTop: '8px' }}
            >
              {cargando ? 'Ingresando…' : 'Iniciar sesión'}
            </button>
          </form>
        </div>
      </div>
    </div>
  )
}
