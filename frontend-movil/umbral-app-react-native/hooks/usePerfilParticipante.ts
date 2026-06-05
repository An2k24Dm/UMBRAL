import { useCallback, useEffect, useState } from 'react'
import { useAutenticacion } from '../autenticacion/ContextoAutenticacion'
import { obtenerPerfilActualApi } from '../servicios/participantesApi'
import { ErrorConsultaPerfil } from '../tipos/errores'
import type { PerfilParticipante } from '../tipos/participantes'

interface EstadoUsePerfilParticipante {
  perfil: PerfilParticipante | null
  cargando: boolean
  error: string | null
  sesionExpirada: boolean
  refrescar: () => Promise<void>
}

export function usePerfilParticipante(): EstadoUsePerfilParticipante {
  const { sesion } = useAutenticacion()
  const token = sesion?.tokenAcceso ?? null

  const [perfil, setPerfil] = useState<PerfilParticipante | null>(null)
  const [cargando, setCargando] = useState<boolean>(false)
  const [error, setError] = useState<string | null>(null)
  const [sesionExpirada, setSesionExpirada] = useState<boolean>(false)

  const cargar = useCallback(async () => {
    if (!token) return
    setCargando(true)
    setError(null)
    setSesionExpirada(false)
    try {
      const datos = await obtenerPerfilActualApi(token)
      setPerfil(datos)
    } catch (e) {
      if (e instanceof ErrorConsultaPerfil && e.codigo === 'NO_AUTORIZADO') {
        setSesionExpirada(true)
        setError(e.message)
      } else if (e instanceof Error) {
        setError(e.message)
      } else {
        setError('No fue posible consultar tu perfil.')
      }
    } finally {
      setCargando(false)
    }
  }, [token])

  useEffect(() => {
    if (token) cargar()
  }, [token, cargar])

  return { perfil, cargando, error, sesionExpirada, refrescar: cargar }
}
