import { useState, type FormEvent } from 'react'
import { useNavigate } from 'react-router-dom'
import {
  cambiarContrasenaObligatoria,
  ErrorValidacionRegistro
} from '../autenticacion/clienteApi'
import { usarAutenticacion } from '../autenticacion/ProveedorAutenticacion'

const CARACTERES_ESPECIALES = '!@#$%^&*_-.?'

interface Errores {
  nuevaContrasena?: string
  confirmacionContrasena?: string
  general?: string
}

function validarLocal(nueva: string, confirmacion: string): Errores {
  const e: Errores = {}
  if (!nueva) e.nuevaContrasena = 'La contraseña es obligatoria.'
  else if (nueva.length < 5 || nueva.length > 10)
    e.nuevaContrasena = 'La contraseña debe tener entre 5 y 10 caracteres.'
  else if (!/\d/.test(nueva))
    e.nuevaContrasena = 'La contraseña debe contener al menos un número.'
  else if (![...nueva].some((c) => CARACTERES_ESPECIALES.includes(c)))
    e.nuevaContrasena = 'La contraseña debe contener al menos un carácter especial.'

  if (!confirmacion) e.confirmacionContrasena = 'Debe confirmar la nueva contraseña.'
  else if (confirmacion !== nueva)
    e.confirmacionContrasena = 'La confirmación no coincide con la nueva contraseña.'

  return e
}

export function PaginaCambioContrasenaObligatorio() {
  const { token, cerrar } = usarAutenticacion()
  const navegar = useNavigate()
  const [nuevaContrasena, setNueva] = useState('')
  const [confirmacionContrasena, setConfirmacion] = useState('')
  const [errores, setErrores] = useState<Errores>({})
  const [cargando, setCargando] = useState(false)
  const [exito, setExito] = useState<string | null>(null)

  const enviar = async (evento: FormEvent) => {
    evento.preventDefault()
    setErrores({})
    setExito(null)

    if (!token) {
      setErrores({ general: 'Debe iniciar sesión nuevamente.' })
      return
    }

    const erroresLocales = validarLocal(nuevaContrasena, confirmacionContrasena)
    if (Object.values(erroresLocales).some((v) => v)) {
      setErrores(erroresLocales)
      return
    }

    setCargando(true)
    try {
      const respuesta = await cambiarContrasenaObligatoria(
        nuevaContrasena, confirmacionContrasena, token)
      setNueva('')
      setConfirmacion('')
      setExito(respuesta.mensaje ?? 'Contraseña actualizada correctamente.')
      setTimeout(() => {
        cerrar()
        navegar('/iniciar-sesion', { replace: true })
      }, 1500)
    } catch (e) {
      if (e instanceof ErrorValidacionRegistro && e.errores.length > 0) {
        const nuevos: Errores = {}
        for (const err of e.errores) {
          if (err.campo === 'contrasena' || err.campo === 'nuevaContrasena') {
            nuevos.nuevaContrasena = err.mensaje
          } else if (err.campo === 'confirmacionContrasena') {
            nuevos.confirmacionContrasena = err.mensaje
          } else {
            nuevos.general = err.mensaje
          }
        }
        setErrores(nuevos)
      } else if (e instanceof Error) {
        setErrores({ general: e.message })
      } else {
        setErrores({ general: 'No fue posible cambiar la contraseña.' })
      }
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
          <span className="auth-subtitulo">Cambio de contraseña obligatorio</span>
        </div>

        <div className="auth-tarjeta">
          <h2>Cambia tu contraseña</h2>
          <p style={{ marginTop: 0, color: 'var(--color-texto-tenue)' }}>
            Estás usando una contraseña temporal. Antes de continuar al
            panel, define una nueva contraseña personal.
          </p>

          {errores.general && (
            <div className="error" role="alert">{errores.general}</div>
          )}
          {exito && (
            <div className="exito" role="status">{exito}</div>
          )}

          <form onSubmit={enviar}>
            <div className="campo">
              <label htmlFor="nuevaContrasena">Nueva contraseña</label>
              <input
                id="nuevaContrasena"
                type="password"
                value={nuevaContrasena}
                maxLength={10}
                autoComplete="new-password"
                onChange={(e) => setNueva(e.target.value)}
                required
              />
              {errores.nuevaContrasena && (
                <small className="error-campo">{errores.nuevaContrasena}</small>
              )}
            </div>

            <div className="campo">
              <label htmlFor="confirmacionContrasena">Confirmar contraseña</label>
              <input
                id="confirmacionContrasena"
                type="password"
                value={confirmacionContrasena}
                maxLength={10}
                autoComplete="new-password"
                onChange={(e) => setConfirmacion(e.target.value)}
                required
              />
              {errores.confirmacionContrasena && (
                <small className="error-campo">{errores.confirmacionContrasena}</small>
              )}
            </div>

            <button
              className="boton"
              type="submit"
              disabled={cargando}
              style={{ marginTop: '8px' }}
            >
              {cargando ? 'Guardando…' : 'Cambiar contraseña'}
            </button>
          </form>
        </div>
      </div>
    </div>
  )
}
