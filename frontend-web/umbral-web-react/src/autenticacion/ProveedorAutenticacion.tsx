import { createContext, useContext, useState, useEffect, type ReactNode } from 'react'
import type { UsuarioAutenticado } from './tipos'

interface EstadoAutenticacion {
  token: string | null
  usuario: UsuarioAutenticado | null
  iniciar: (token: string, usuario: UsuarioAutenticado) => void
  cerrar: () => void
}

const ContextoAutenticacion = createContext<EstadoAutenticacion | null>(null)

const CLAVE_TOKEN = 'umbral.token'
const CLAVE_USUARIO = 'umbral.usuario'

export function ProveedorAutenticacion({ children }: { children: ReactNode }) {
  const [token, setToken] = useState<string | null>(null)
  const [usuario, setUsuario] = useState<UsuarioAutenticado | null>(null)

  useEffect(() => {
    const tokenGuardado = localStorage.getItem(CLAVE_TOKEN)
    const usuarioGuardado = localStorage.getItem(CLAVE_USUARIO)
    if (tokenGuardado && usuarioGuardado) {
      setToken(tokenGuardado)
      setUsuario(JSON.parse(usuarioGuardado))
    }
  }, [])

  const iniciar = (nuevoToken: string, nuevoUsuario: UsuarioAutenticado) => {
    localStorage.setItem(CLAVE_TOKEN, nuevoToken)
    localStorage.setItem(CLAVE_USUARIO, JSON.stringify(nuevoUsuario))
    setToken(nuevoToken)
    setUsuario(nuevoUsuario)
  }

  const cerrar = () => {
    localStorage.removeItem(CLAVE_TOKEN)
    localStorage.removeItem(CLAVE_USUARIO)
    setToken(null)
    setUsuario(null)
  }

  return (
    <ContextoAutenticacion.Provider value={{ token, usuario, iniciar, cerrar }}>
      {children}
    </ContextoAutenticacion.Provider>
  )
}

export function usarAutenticacion() {
  const contexto = useContext(ContextoAutenticacion)
  if (!contexto) throw new Error('ProveedorAutenticacion ausente.')
  return contexto
}
