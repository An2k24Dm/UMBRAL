import { useEffect } from 'react'
import {
  crearConexionSesionesTiempoReal,
  obtenerSesionIdEvento,
  type EventoSesionTiempoReal
} from '../servicios/sesionesTiempoReal'

interface OpcionesUseSesionesTiempoReal {
  token: string | null
  sesionId?: string
  onSesionActualizada: () => void
}

export function useSesionesTiempoReal({
  token,
  sesionId,
  onSesionActualizada
}: OpcionesUseSesionesTiempoReal) {
  useEffect(() => {
    if (!token || !sesionId) return

    let desmontado = false
    const conexion = crearConexionSesionesTiempoReal(token)
    const sesionActual = sesionId.toLowerCase()

    const manejarEvento = (evento: EventoSesionTiempoReal) => {
      if (obtenerSesionIdEvento(evento).toLowerCase() === sesionActual) {
        onSesionActualizada()
      }
    }

    conexion.on('ParticipantesSesionActualizados', manejarEvento)
    conexion.on('EquiposSesionActualizados', manejarEvento)
    conexion.on('EquipoActualizado', manejarEvento)

    conexion
      .start()
      .then(() => {
        if (!desmontado) {
          return conexion.invoke('UnirseASesion', sesionId)
        }
      })
      .catch(() => {
        // El detalle sigue funcionando por HTTP; si el canal en tiempo real no
        // conecta, el usuario mantiene la recarga manual como respaldo.
      })

    return () => {
      desmontado = true
      conexion.off('ParticipantesSesionActualizados', manejarEvento)
      conexion.off('EquiposSesionActualizados', manejarEvento)
      conexion.off('EquipoActualizado', manejarEvento)

      conexion
        .invoke('SalirDeSesion', sesionId)
        .catch(() => undefined)
        .finally(() => {
          conexion.stop().catch(() => undefined)
        })
    }
  }, [token, sesionId, onSesionActualizada])
}
