import { useState } from 'react'
import { useRouter } from 'expo-router'
import { useAutenticacion } from '../autenticacion/ContextoAutenticacion'
import { ErrorInicioSesion } from '../tipos/errores'

function traducirErrorInicioSesion(e: unknown): string {
  if (e instanceof ErrorInicioSesion) {
    switch (e.codigo) {
      case 'DATOS_INVALIDOS':
        return 'Usuario o contraseña incorrectos.'
      case 'ACCESO_NO_PERMITIDO':
      case 'ROL_NO_VALIDO':
        return 'Este usuario no puede iniciar sesión desde la app móvil.'
      case 'CUENTA_DESACTIVADA':
        return 'Tu cuenta está desactivada. Contacta a un administrador.'
      default:
        return e.message || 'No fue posible iniciar sesión.'
    }
  }
  if (e instanceof Error) return e.message
  return 'No fue posible iniciar sesión.'
}

interface EstadoUseInicioSesion {
  nombreUsuario: string
  contrasena: string
  errorNombre: string | null
  errorContrasena: string | null
  mensajeError: string | null
  enviando: boolean
  alCambiarNombre: (valor: string) => void
  alCambiarContrasena: (valor: string) => void
  enviar: () => Promise<void>
}

export function useInicioSesion(): EstadoUseInicioSesion {
  const [nombreUsuario, setNombreUsuario] = useState('')
  const [contrasena, setContrasena] = useState('')
  const [errorNombre, setErrorNombre] = useState<string | null>(null)
  const [errorContrasena, setErrorContrasena] = useState<string | null>(null)
  const [mensajeError, setMensajeError] = useState<string | null>(null)
  const [enviando, setEnviando] = useState(false)

  const { iniciarSesion } = useAutenticacion()
  const enrutador = useRouter()

  function alCambiarNombre(valor: string) {
    setNombreUsuario(valor)
    if (errorNombre) setErrorNombre(null)
    if (mensajeError) setMensajeError(null)
  }

  function alCambiarContrasena(valor: string) {
    setContrasena(valor)
    if (errorContrasena) setErrorContrasena(null)
    if (mensajeError) setMensajeError(null)
  }

  async function enviar() {
    const usuario = nombreUsuario.trim()
    const errorN = !usuario ? 'El nombre de usuario es obligatorio.' : null
    const errorC = !contrasena ? 'La contraseña es obligatoria.' : null
    setErrorNombre(errorN)
    setErrorContrasena(errorC)
    setMensajeError(null)
    if (errorN || errorC) return

    setEnviando(true)
    try {
      await iniciarSesion(usuario, contrasena)
      enrutador.replace('/participante/menu')
    } catch (e) {
      setMensajeError(traducirErrorInicioSesion(e))
    } finally {
      setEnviando(false)
    }
  }

  return {
    nombreUsuario,
    contrasena,
    errorNombre,
    errorContrasena,
    mensajeError,
    enviando,
    alCambiarNombre,
    alCambiarContrasena,
    enviar,
  }
}
