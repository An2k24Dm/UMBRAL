import { useEffect, useRef } from 'react'
import * as signalR from '@microsoft/signalr'
import { crearConexionRankingTiempoReal } from '../servicios/rankingTiempoReal'
import { esErrorNoAutenticadoTiempoReal } from '../servicios/sesionesTiempoReal'

interface OpcionesUseRankingTiempoReal {
  token: string | null
  sesionId?: string | null
  onPuntajeCalculado?: () => void
  onRankingParticipantesActualizado?: () => void
  onRankingEquiposActualizado?: () => void
  // HU52 — Penalización aplicada: el frontend refresca el ranking.
  onPenalizacionAplicada?: () => void
  onReconectado?: () => void
}

type Callbacks = Omit<OpcionesUseRankingTiempoReal, 'token' | 'sesionId'>

export function useRankingTiempoReal({
  token,
  sesionId,
  onPuntajeCalculado,
  onRankingParticipantesActualizado,
  onRankingEquiposActualizado,
  onPenalizacionAplicada,
  onReconectado
}: OpcionesUseRankingTiempoReal) {
  const callbacksRef = useRef<Callbacks>({})
  callbacksRef.current = {
    onPuntajeCalculado,
    onRankingParticipantesActualizado,
    onRankingEquiposActualizado,
    onPenalizacionAplicada,
    onReconectado
  }

  const tokenLimpio = token?.trim() ?? ''

  useEffect(() => {
    if (!tokenLimpio || !sesionId) return

    let desmontado = false
    const conexion = crearConexionRankingTiempoReal(tokenLimpio)

    const manejarError = (error: unknown) => {
      if (!esErrorNoAutenticadoTiempoReal(error)) return
    }

    conexion.on('RankingParticipantesActualizado', () => {
      callbacksRef.current.onRankingParticipantesActualizado?.()
    })

    conexion.on('PuntajeCalculado', () => {
      callbacksRef.current.onPuntajeCalculado?.()
    })

    conexion.on('RankingEquiposActualizado', () => {
      callbacksRef.current.onRankingEquiposActualizado?.()
    })

    conexion.on('PenalizacionAplicada', () => {
      callbacksRef.current.onPenalizacionAplicada?.()
    })

    conexion.onreconnected(() => {
      if (desmontado) return
      conexion.invoke('UnirseASesion', sesionId)
        .then(() => {
          if (!desmontado) {
            callbacksRef.current.onReconectado?.()
          }
        })
        .catch(manejarError)
    })

    conexion.onreconnecting(manejarError)
    conexion.onclose(manejarError)

    conexion
      .start()
      .then(() => {
        if (desmontado) return conexion.stop().catch(() => undefined)
        return conexion.invoke('UnirseASesion', sesionId).catch(manejarError)
      })
      .catch(manejarError)

    return () => {
      desmontado = true
      conexion.off('PuntajeCalculado')
      conexion.off('RankingParticipantesActualizado')
      conexion.off('RankingEquiposActualizado')
      conexion.off('PenalizacionAplicada')

      if (conexion.state === signalR.HubConnectionState.Connected) {
        conexion
          .invoke('SalirDeSesion', sesionId)
          .catch(() => undefined)
          .finally(() => conexion.stop().catch(() => undefined))
      }
    }
  }, [tokenLimpio, sesionId])
}
