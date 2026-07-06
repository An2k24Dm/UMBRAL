import { useEffect, useRef } from 'react'
import * as signalR from '@microsoft/signalr'
import {
  crearConexionSesionesTiempoReal,
  esErrorNoAutenticadoTiempoReal,
  type EventoSesionTiempoReal
} from '../servicios/sesionesTiempoReal'

interface OpcionesUseListadoSesionesTiempoReal {
  token: string | null
  onListadoActualizado: () => void
}

function logListado(mensaje: string, ...datos: unknown[]) {
  if (import.meta.env.DEV) {
    console.debug(`[SignalR Web Listado] ${mensaje}`, ...datos)
  }
}

export function useListadoSesionesTiempoReal({
  token,
  onListadoActualizado
}: OpcionesUseListadoSesionesTiempoReal) {
  const onListadoActualizadoRef = useRef(onListadoActualizado)
  onListadoActualizadoRef.current = onListadoActualizado

  const tokenLimpio = token?.trim() ?? ''

  useEffect(() => {
    if (!tokenLimpio) return

    let desmontado = false
    const conexion = crearConexionSesionesTiempoReal(tokenLimpio)

    const manejarErrorConexion = (error: unknown) => {
      if (esErrorNoAutenticadoTiempoReal(error)) {
        logListado('cerrado')
      }
    }

    const refrescarListado = () => {
      logListado('refrescando listado')
      onListadoActualizadoRef.current()
    }

    const manejarListado = (evento: EventoSesionTiempoReal) => {
      logListado('SesionActualizada recibida', evento)
      refrescarListado()
    }

    const unirseAlListado = async () => {
      await conexion.invoke('UnirseAListadoSesiones')
      logListado('unido al listado')
    }

    conexion.on('SesionActualizada', manejarListado)

    conexion.onreconnected(() => {
      if (desmontado) return
      unirseAlListado()
        .then(refrescarListado)
        .catch(manejarErrorConexion)
    })

    conexion.onreconnecting(manejarErrorConexion)
    conexion.onclose(manejarErrorConexion)

    conexion
      .start()
      .then(() => {
        logListado('conectado')
        if (desmontado) {
          return conexion.stop().catch(() => undefined)
        }
        return unirseAlListado().catch(manejarErrorConexion)
      })
      .catch(manejarErrorConexion)

    return () => {
      desmontado = true
      conexion.off('SesionActualizada', manejarListado)

      if (conexion.state === signalR.HubConnectionState.Connected) {
        conexion
          .invoke('SalirDeListadoSesiones')
          .catch(() => undefined)
          .finally(() => {
            conexion.stop().catch(() => undefined)
          })
      }
    }
  }, [tokenLimpio])
}
