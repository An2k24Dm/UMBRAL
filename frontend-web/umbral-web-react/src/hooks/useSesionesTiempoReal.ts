import { useEffect } from 'react'
import * as signalR from '@microsoft/signalr'
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

    // Tras una reconexión automática SignalR pierde la membresía de grupos:
    // hay que volver a unirse a la sesión y refrescar, porque el estado pudo
    // cambiar mientras el canal estuvo caído.
    conexion.onreconnected(() => {
      if (desmontado) return
      conexion
        .invoke('UnirseASesion', sesionId)
        .then(() => onSesionActualizada())
        .catch(() => undefined)
    })

    conexion
      .start()
      .then(() => {
        // Si la pantalla se desmontó mientras negociaba, recién aquí (ya
        // conectada) es seguro detenerla, sin cortar la negociación.
        if (desmontado) {
          return conexion.stop().catch(() => undefined)
        }
        return conexion.invoke('UnirseASesion', sesionId).catch(() => undefined)
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

      // Solo cerramos si ya está Conectada. Si está Connecting/Reconnecting,
      // NO llamamos stop aquí (cortaría la negociación y dispara
      // "The connection was stopped during negotiation"); el then de start ve
      // `desmontado` y la cierra al terminar. Si está Disconnected, nada.
      if (conexion.state === signalR.HubConnectionState.Connected) {
        conexion
          .invoke('SalirDeSesion', sesionId)
          .catch(() => undefined)
          .finally(() => {
            conexion.stop().catch(() => undefined)
          })
      }
    }
  }, [token, sesionId, onSesionActualizada])
}
