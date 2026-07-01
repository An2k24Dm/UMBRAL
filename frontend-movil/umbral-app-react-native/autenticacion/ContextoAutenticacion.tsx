import {
  createContext,
  useCallback,
  useContext,
  useEffect,
  useMemo,
  useState,
  type ReactNode
} from 'react'
import { iniciarSesionApi } from '../servicios/autenticacionApi'
import { obtenerPerfilActualApi } from '../servicios/participantesApi'
import { ErrorConsultaPerfil, ErrorInicioSesion } from '../tipos/errores'
import type {
  ResultadoInicioSesion,
  UsuarioAutenticado
} from '../tipos/autenticacion'
import {
  eliminarSesion,
  esTokenAccesoVigente,
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

    async function cargarSesionPersistida() {
      try {
        const sesionPersistida = await obtenerSesion()
        if (!sesionPersistida) {
          if (activo) setSesion(null)
          return
        }

        // La lectura de SecureStore ya comprobó estructura y expiración.
        // El backend confirma que el JWT sigue autorizado antes de habilitar
        // rutas protegidas o iniciar conexiones de tiempo real.
        const perfil = await obtenerPerfilActualApi(
          sesionPersistida.tokenAcceso
        )

        if (perfil.rol !== 'Participante') {
          await eliminarSesion()
          if (activo) setSesion(null)
          return
        }

        if (activo) setSesion(sesionPersistida)
      } catch (error) {
        // Un 401/403 invalida definitivamente la sesión. Ante un fallo
        // temporal de red también se bloquea la navegación protegida, pero
        // se conserva SecureStore para poder revalidarlo en otro arranque.
        if (
          error instanceof ErrorConsultaPerfil &&
          (error.estadoHttp === 401 || error.estadoHttp === 403)
        ) {
          await eliminarSesion()
        }
        if (activo) setSesion(null)
      } finally {
        if (activo) setCargandoSesion(false)
      }
    }

    void cargarSesionPersistida()

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
      if (!esTokenAccesoVigente(resultado.tokenAcceso)) {
        throw new ErrorInicioSesion(
          'El servidor devolvió una sesión inválida. Intenta iniciar sesión nuevamente.',
          'ERROR_INTERNO',
          500
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
    try {
      await eliminarSesion()
    } finally {
      // El estado en memoria nunca debe conservar una sesión que el backend
      // ya rechazó, incluso si SecureStore falla al limpiar el dispositivo.
      setSesion(null)
    }
  }, [])

  const valor = useMemo<ValorContextoAutenticacion>(
    () => ({
      sesion,
      usuario: sesion?.usuario ?? null,
      cargandoSesion,
      estaAutenticado: !cargandoSesion && sesion !== null,
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
