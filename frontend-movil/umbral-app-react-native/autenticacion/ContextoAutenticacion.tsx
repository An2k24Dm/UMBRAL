import {
  createContext,
  useCallback,
  useContext,
  useEffect,
  useMemo,
  useState,
  type ReactNode
} from 'react'
import {
  ErrorInicioSesion,
  iniciarSesionApi,
  type ResultadoInicioSesion,
  type UsuarioAutenticado
} from './clienteApi'
import {
  eliminarSesion,
  guardarSesion,
  obtenerSesion,
  type SesionPersistida
} from './almacenamientoSeguro'

// HU04 — capa mínima de estado de sesión móvil. Mantiene la sesión cargada
// desde SecureStore al abrir la app, expone el inicio/cierre de sesión y un
// indicador de carga inicial para evitar parpadeos antes de saber si el
// Participante ya estaba autenticado.
interface ValorContextoAutenticacion {
  sesion: SesionPersistida | null
  usuario: UsuarioAutenticado | null
  cargandoSesion: boolean
  estaAutenticado: boolean
  iniciarSesion: (
    nombreUsuario: string,
    contrasena: string
  ) => Promise<UsuarioAutenticado>
  cerrarSesion: () => Promise<void>
}

const ContextoAutenticacion = createContext<ValorContextoAutenticacion | null>(null)

export function ProveedorAutenticacion({ children }: { children: ReactNode }) {
  const [sesion, setSesion] = useState<SesionPersistida | null>(null)
  const [cargandoSesion, setCargandoSesion] = useState(true)

  useEffect(() => {
    let activo = true
    obtenerSesion()
      .then((s) => {
        if (!activo) return
        // Defensa visual: la app móvil es exclusiva del Participante. Si el
        // SecureStore tenía una sesión de otro rol (por ejemplo, restaurada
        // de un backup), se descarta en lugar de cargarla.
        if (s && s.usuario.rol !== 'Participante') {
          eliminarSesion().finally(() => {
            if (activo) setSesion(null)
          })
        } else {
          setSesion(s)
        }
      })
      .finally(() => {
        if (activo) setCargandoSesion(false)
      })
    return () => {
      activo = false
    }
  }, [])

  const iniciarSesion = useCallback(
    async (nombreUsuario: string, contrasena: string) => {
      const resultado: ResultadoInicioSesion = await iniciarSesionApi(
        nombreUsuario,
        contrasena
      )
      // Doble salvaguarda. El backend ya rechaza Administrador/Operador en
      // /login-movil, pero si algún día respondiera otro rol, no lo dejamos
      // entrar a la app del Participante.
      if (resultado.usuario.rol !== 'Participante') {
        throw new ErrorInicioSesion(
          'Este usuario no puede iniciar sesión desde la app móvil.',
          'ACCESO_NO_PERMITIDO',
          403
        )
      }
      await guardarSesion(resultado)
      setSesion({
        tokenAcceso: resultado.tokenAcceso,
        tokenRefresco: resultado.tokenRefresco,
        expiraEn: resultado.expiraEn,
        tipoToken: resultado.tipoToken,
        usuario: resultado.usuario
      })
      return resultado.usuario
    },
    []
  )

  const cerrarSesion = useCallback(async () => {
    await eliminarSesion()
    setSesion(null)
  }, [])

  const valor = useMemo<ValorContextoAutenticacion>(
    () => ({
      sesion,
      usuario: sesion?.usuario ?? null,
      cargandoSesion,
      estaAutenticado: sesion !== null,
      iniciarSesion,
      cerrarSesion
    }),
    [sesion, cargandoSesion, iniciarSesion, cerrarSesion]
  )

  return (
    <ContextoAutenticacion.Provider value={valor}>
      {children}
    </ContextoAutenticacion.Provider>
  )
}

export function useAutenticacion(): ValorContextoAutenticacion {
  const ctx = useContext(ContextoAutenticacion)
  if (!ctx) {
    throw new Error(
      'useAutenticacion debe usarse dentro de <ProveedorAutenticacion>.'
    )
  }
  return ctx
}
