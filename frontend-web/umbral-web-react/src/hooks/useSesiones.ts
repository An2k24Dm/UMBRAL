import { useCallback, useEffect, useState } from 'react'
import { listarSesiones } from '../servicios/sesionesApi'
import type {
  EstadoSesion,
  FiltrosListadoSesiones,
  SesionListadoDto
} from '../tipos/sesiones'

interface OpcionesUseSesiones {
  token: string | null
  estado: EstadoSesion | ''
}

interface EstadoUseSesiones {
  sesiones: SesionListadoDto[]
  cargando: boolean
  vacio: boolean
  error: string | null
  // Fuerza una recarga del listado (lo usa SignalR para refrescar en vivo).
  refrescar: () => void
}

// Trae el listado del backend aplicando el filtro de estado del lado
// servidor. Modo y nombre se filtran en memoria desde la página porque
// el endpoint actual no los acepta como query params.
export function useSesiones({ token, estado }: OpcionesUseSesiones): EstadoUseSesiones {
  const [sesiones, setSesiones] = useState<SesionListadoDto[]>([])
  const [cargando, setCargando] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [version, setVersion] = useState(0)

  const refrescar = useCallback(() => setVersion(v => v + 1), [])

  useEffect(() => {
    if (!token) {
      setSesiones([])
      setCargando(false)
      setError('Debe iniciar sesión.')
      return
    }
    const ref = { cancelado: false }
    async function cargar() {
      setCargando(true)
      setError(null)
      const filtros: FiltrosListadoSesiones = {}
      if (estado) filtros.estado = estado
      try {
        const lista = await listarSesiones(token!, filtros)
        if (ref.cancelado) return
        setSesiones(lista)
      } catch (e) {
        if (ref.cancelado) return
        setSesiones([])
        setError(e instanceof Error ? e.message : 'No se pudieron cargar las sesiones.')
      } finally {
        if (!ref.cancelado) setCargando(false)
      }
    }
    cargar()
    return () => { ref.cancelado = true }
  }, [token, estado, version])

  return {
    sesiones,
    cargando,
    error,
    vacio: !cargando && !error && sesiones.length === 0,
    refrescar,
  }
}
