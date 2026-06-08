import { useEffect, useState } from 'react'
import { obtenerMisionesActivas } from '../servicios/misionesApi'
import type { MisionResumenDto } from '../tipos/misiones'

interface EstadoMisionesActivas {
  misiones: MisionResumenDto[]
  cargando: boolean
  error: string | null
}

// Carga las misiones activas desde juegos-servicio. Se cancela si el
// componente que lo usa se desmonta para evitar set-state en árboles
// desmontados.
export function useMisionesActivas(token: string | null): EstadoMisionesActivas {
  const [misiones, setMisiones] = useState<MisionResumenDto[]>([])
  const [cargando, setCargando] = useState(false)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    if (!token) {
      setMisiones([])
      setCargando(false)
      setError(null)
      return
    }
    const ref = { cancelado: false }
    async function cargar() {
      setCargando(true)
      setError(null)
      try {
        const lista = await obtenerMisionesActivas(token!)
        if (ref.cancelado) return
        setMisiones(lista)
        if (lista.length === 0) {
          setError('No hay misiones activas. Active al menos una desde el panel de misiones.')
        }
      } catch (e) {
        if (ref.cancelado) return
        setMisiones([])
        setError(e instanceof Error ? e.message : 'No fue posible cargar las misiones activas.')
      } finally {
        if (!ref.cancelado) setCargando(false)
      }
    }
    cargar()
    return () => { ref.cancelado = true }
  }, [token])

  return { misiones, cargando, error }
}
