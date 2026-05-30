import { createContext, useCallback, useContext, useEffect, useState, type ReactNode } from 'react'
import type { UsuarioAutenticado } from './tipos'
import { EVENTO_SESION_INVALIDA } from './eventosSesion'

interface EstadoAutenticacion {
  token: string | null
  usuario: UsuarioAutenticado | null
  // Mientras es true, los componentes deben esperar antes de decidir
  // redirigir al login: el provider todavía no terminó de restaurar la
  // sesión desde localStorage.
  cargandoSesion: boolean
  iniciar: (token: string, usuario: UsuarioAutenticado) => void
  cerrar: () => void
}

const ContextoAutenticacion = createContext<EstadoAutenticacion | null>(null)

const CLAVE_TOKEN = 'umbral.token'
const CLAVE_USUARIO = 'umbral.usuario'

// Lectura síncrona de la sesión almacenada. Se usa como initializer
// perezoso de useState para que el primer render ya tenga el token y
// el usuario disponibles si existían en localStorage. Sin esto, el
// primer render arranca con null y RutaProtegida redirige al login
// antes de que un useEffect alcance a restaurar la sesión.
function leerSesionAlmacenada(): {
  token: string | null
  usuario: UsuarioAutenticado | null
} {
  if (typeof window === 'undefined') {
    return { token: null, usuario: null }
  }
  try {
    const token = window.localStorage.getItem(CLAVE_TOKEN)
    const usuarioCrudo = window.localStorage.getItem(CLAVE_USUARIO)
    if (!token || !usuarioCrudo) return { token: null, usuario: null }
    const usuario = JSON.parse(usuarioCrudo) as UsuarioAutenticado
    if (!usuario || typeof usuario !== 'object' || !usuario.rol) {
      // Datos corruptos: los limpiamos para que el próximo login parta
      // de un estado consistente y no quede ruido en el navegador.
      window.localStorage.removeItem(CLAVE_TOKEN)
      window.localStorage.removeItem(CLAVE_USUARIO)
      return { token: null, usuario: null }
    }
    return { token, usuario }
  } catch {
    // JSON inválido. Misma política: limpiar y empezar limpio.
    window.localStorage.removeItem(CLAVE_TOKEN)
    window.localStorage.removeItem(CLAVE_USUARIO)
    return { token: null, usuario: null }
  }
}

export function ProveedorAutenticacion({ children }: { children: ReactNode }) {
  // Lazy initial state: leer localStorage durante el primer render,
  // no después. Esto evita el "flash" hacia login en cada recarga.
  const sesionInicial = leerSesionAlmacenada()
  const [token, setToken] = useState<string | null>(sesionInicial.token)
  const [usuario, setUsuario] = useState<UsuarioAutenticado | null>(sesionInicial.usuario)
  const [cargandoSesion, setCargandoSesion] = useState<boolean>(true)

  // El primer effect simplemente marca que la restauración terminó.
  // Si en el futuro hace falta validar el token contra el backend,
  // este es el lugar para hacerlo antes de bajar cargandoSesion.
  useEffect(() => {
    setCargandoSesion(false)
  }, [])

  const cerrar = useCallback(() => {
    if (typeof window !== 'undefined') {
      window.localStorage.removeItem(CLAVE_TOKEN)
      window.localStorage.removeItem(CLAVE_USUARIO)
    }
    setToken(null)
    setUsuario(null)
  }, [])

  const iniciar = useCallback(
    (nuevoToken: string, nuevoUsuario: UsuarioAutenticado) => {
      if (typeof window !== 'undefined') {
        window.localStorage.setItem(CLAVE_TOKEN, nuevoToken)
        window.localStorage.setItem(CLAVE_USUARIO, JSON.stringify(nuevoUsuario))
      }
      setToken(nuevoToken)
      setUsuario(nuevoUsuario)
    },
    []
  )

  // Limpieza centralizada por 401. Los clientes HTTP disparan el
  // evento; aquí lo traducimos a "cerrar sesión". Una vez vacío el
  // estado, las RutaProtegida vigentes redirigirán al login en su
  // próximo render.
  useEffect(() => {
    if (typeof window === 'undefined') return
    const onSesionInvalida = () => cerrar()
    window.addEventListener(EVENTO_SESION_INVALIDA, onSesionInvalida)
    return () => window.removeEventListener(EVENTO_SESION_INVALIDA, onSesionInvalida)
  }, [cerrar])

  // Sincronizar entre pestañas: si en otra pestaña se hace logout o
  // login, esta pestaña refleja el cambio automáticamente.
  useEffect(() => {
    if (typeof window === 'undefined') return
    const onStorage = (e: StorageEvent) => {
      if (e.key !== CLAVE_TOKEN && e.key !== CLAVE_USUARIO) return
      const sesion = leerSesionAlmacenada()
      setToken(sesion.token)
      setUsuario(sesion.usuario)
    }
    window.addEventListener('storage', onStorage)
    return () => window.removeEventListener('storage', onStorage)
  }, [])

  return (
    <ContextoAutenticacion.Provider value={{ token, usuario, cargandoSesion, iniciar, cerrar }}>
      {children}
    </ContextoAutenticacion.Provider>
  )
}

export function usarAutenticacion() {
  const contexto = useContext(ContextoAutenticacion)
  if (!contexto) throw new Error('ProveedorAutenticacion ausente.')
  return contexto
}
