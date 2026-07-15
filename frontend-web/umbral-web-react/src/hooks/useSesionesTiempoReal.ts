import { useEffect, useRef } from 'react'
import * as signalR from '@microsoft/signalr'
import {
  crearConexionSesionesTiempoReal,
  esErrorNoAutenticadoTiempoReal,
  obtenerEquipoIdEvento,
  obtenerEstadoEvento,
  obtenerSesionIdEvento,
  type EventoSesionTiempoReal
} from '../servicios/sesionesTiempoReal'

export interface UbicacionActualizadaTR {
  sesionId?: string
  SesionId?: string
  participanteIdentidadId?: string
  ParticipanteIdentidadId?: string
  nombre?: string
  Nombre?: string
  equipoId?: string | null
  EquipoId?: string | null
  latitud?: number
  Latitud?: number
  longitud?: number
  Longitud?: number
  fechaEventoUtc?: string
  FechaEventoUtc?: string
}

interface OpcionesUseSesionesTiempoReal {
  token: string | null
  sesionId?: string | null
  equipoId?: string | null
  onParticipantesSesionActualizados?: () => void | Promise<void>
  onEquiposSesionActualizados?: () => void | Promise<void>
  onEquipoActualizado?: () => void | Promise<void>
  onSesionActualizada?: (estado?: string) => void | Promise<void>
  onParticipanteExpulsado?: () => void | Promise<void>
  onEquipoExpulsado?: () => void | Promise<void>
  onRespuestaRegistrada?: () => void | Promise<void>
  onEtapaCompletada?: () => void | Promise<void>
  onEtapaPorComenzar?: () => void | Promise<void>
  onEtapaIniciada?: () => void | Promise<void>
  onProgresoSecuencialActualizado?: () => void | Promise<void>
  onReconectado?: () => void | Promise<void>
  onUbicacionActualizada?: (dto: UbicacionActualizadaTR) => void | Promise<void>
}

type CallbacksTiempoReal = Omit<OpcionesUseSesionesTiempoReal, 'token' | 'sesionId' | 'equipoId'>

function logDetalle(mensaje: string, ...datos: unknown[]) {
  if (import.meta.env.DEV) {
    console.debug(`[SignalR Web Detalle] ${mensaje}`, ...datos)
  }
}

export function useSesionesTiempoReal({
  token,
  sesionId,
  equipoId,
  onParticipantesSesionActualizados,
  onEquiposSesionActualizados,
  onEquipoActualizado,
  onSesionActualizada,
  onParticipanteExpulsado,
  onEquipoExpulsado,
  onRespuestaRegistrada,
  onEtapaCompletada,
  onEtapaPorComenzar,
  onEtapaIniciada,
  onProgresoSecuencialActualizado,
  onReconectado,
  onUbicacionActualizada
}: OpcionesUseSesionesTiempoReal) {
  const callbacksRef = useRef<CallbacksTiempoReal>({})
  callbacksRef.current = {
    onParticipantesSesionActualizados,
    onEquiposSesionActualizados,
    onEquipoActualizado,
    onSesionActualizada,
    onParticipanteExpulsado,
    onEquipoExpulsado,
    onRespuestaRegistrada,
    onEtapaCompletada,
    onEtapaPorComenzar,
    onEtapaIniciada,
    onProgresoSecuencialActualizado,
    onReconectado,
    onUbicacionActualizada
  }

  const tokenLimpio = token?.trim() ?? ''

  useEffect(() => {
    if (!tokenLimpio || (!sesionId && !equipoId)) return

    let desmontado = false
    const conexion = crearConexionSesionesTiempoReal(tokenLimpio)
    const sesionActual = (sesionId ?? '').toLowerCase()
    const equipoActual = (equipoId ?? '').toLowerCase()

    logDetalle('conexión creada')

    const manejarErrorConexion = (error: unknown) => {
      if (esErrorNoAutenticadoTiempoReal(error)) {
        logDetalle('cerrado')
      }
    }

    const coincideSesion = (evento: EventoSesionTiempoReal) =>
      !sesionActual || obtenerSesionIdEvento(evento).toLowerCase() === sesionActual

    const coincideEquipo = (evento: EventoSesionTiempoReal) =>
      !equipoActual || obtenerEquipoIdEvento(evento).toLowerCase() === equipoActual

    const refrescar = (accion: (() => void | Promise<void>) | undefined) => {
      logDetalle('refrescando detalle')
      void accion?.()
    }

    const manejarParticipantes = (evento: EventoSesionTiempoReal) => {
      logDetalle('evento recibido', 'ParticipantesSesionActualizados', evento)
      if (coincideSesion(evento)) {
        refrescar(callbacksRef.current.onParticipantesSesionActualizados)
      }
    }

    const manejarEquipos = (evento: EventoSesionTiempoReal) => {
      logDetalle('evento recibido', 'EquiposSesionActualizados', evento)
      if (coincideSesion(evento)) {
        refrescar(callbacksRef.current.onEquiposSesionActualizados)
      }
    }

    const manejarEquipo = (evento: EventoSesionTiempoReal) => {
      logDetalle('evento recibido', 'EquipoActualizado', evento)
      if (coincideSesion(evento) && coincideEquipo(evento)) {
        refrescar(callbacksRef.current.onEquipoActualizado)
      }
    }

    const manejarSesion = (evento: EventoSesionTiempoReal) => {
      logDetalle('evento recibido', 'SesionActualizada', evento)
      if (coincideSesion(evento)) {
        logDetalle('refrescando detalle')
        void callbacksRef.current.onSesionActualizada?.(obtenerEstadoEvento(evento))
      }
    }

    const manejarParticipanteExpulsado = (evento: EventoSesionTiempoReal) => {
      logDetalle('evento recibido', 'ParticipanteExpulsadoSesion', evento)
      if (coincideSesion(evento)) {
        refrescar(callbacksRef.current.onParticipanteExpulsado)
      }
    }

    const manejarEquipoExpulsado = (evento: EventoSesionTiempoReal) => {
      logDetalle('evento recibido', 'EquipoExpulsadoSesion', evento)
      if (coincideSesion(evento) && coincideEquipo(evento)) {
        refrescar(callbacksRef.current.onEquipoExpulsado)
      }
    }

    const manejarRespuesta = (evento: EventoSesionTiempoReal) => {
      logDetalle('evento recibido', 'RespuestaRegistrada', evento)
      if (coincideSesion(evento)) {
        refrescar(callbacksRef.current.onRespuestaRegistrada)
      }
    }

    const manejarEtapa = (evento: EventoSesionTiempoReal) => {
      logDetalle('evento recibido', 'EtapaCompletada', evento)
      if (coincideSesion(evento)) {
        refrescar(callbacksRef.current.onEtapaCompletada)
      }
    }

    const manejarEtapaIniciada = (evento: EventoSesionTiempoReal) => {
      logDetalle('evento recibido', 'EtapaIniciada', evento)
      if (coincideSesion(evento)) {
        refrescar(callbacksRef.current.onEtapaIniciada)
      }
    }

    const manejarEtapaPorComenzar = (evento: EventoSesionTiempoReal) => {
      logDetalle('evento recibido', 'EtapaPorComenzar', evento)
      if (coincideSesion(evento)) {
        refrescar(callbacksRef.current.onEtapaPorComenzar)
      }
    }

    const manejarProgresoSecuencial = (evento: EventoSesionTiempoReal) => {
      logDetalle('evento recibido', 'ProgresoSecuencialActualizado', evento)
      if (coincideSesion(evento)) {
        refrescar(callbacksRef.current.onProgresoSecuencialActualizado)
      }
    }

    const manejarUbicacion = (dto: UbicacionActualizadaTR) => {
      logDetalle('evento recibido', 'UbicacionActualizada', dto)
      const sid = (dto.sesionId ?? dto.SesionId ?? '').toLowerCase()
      if (!sesionActual || sid === sesionActual) {
        void callbacksRef.current.onUbicacionActualizada?.(dto)
      }
    }

    const unirseAGrupos = async () => {
      if (sesionId) {
        await conexion.invoke('UnirseASesion', sesionId)
        logDetalle('unido a sesión', sesionId)
      }
      if (equipoId) {
        await conexion.invoke('UnirseAEquipo', equipoId)
        logDetalle('unido a equipo', equipoId)
      }
    }

    conexion.on('ParticipantesSesionActualizados', manejarParticipantes)
    conexion.on('EquiposSesionActualizados', manejarEquipos)
    conexion.on('EquipoActualizado', manejarEquipo)
    conexion.on('SesionActualizada', manejarSesion)
    conexion.on('ParticipanteExpulsadoSesion', manejarParticipanteExpulsado)
    conexion.on('EquipoExpulsadoSesion', manejarEquipoExpulsado)
    conexion.on('RespuestaRegistrada', manejarRespuesta)
    conexion.on('EtapaCompletada', manejarEtapa)
    conexion.on('EtapaPorComenzar', manejarEtapaPorComenzar)
    conexion.on('EtapaIniciada', manejarEtapaIniciada)
    conexion.on('ProgresoSecuencialActualizado', manejarProgresoSecuencial)
    conexion.on('UbicacionActualizada', manejarUbicacion)

    conexion.onreconnected(() => {
      if (desmontado) return
      logDetalle('reconectado')
      unirseAGrupos()
        .then(() => {
          if (!desmontado) {
            logDetalle('refrescando detalle')
            void callbacksRef.current.onReconectado?.()
          }
        })
        .catch(manejarErrorConexion)
    })

    conexion.onreconnecting(manejarErrorConexion)
    conexion.onclose((error) => {
      logDetalle('cerrado')
      manejarErrorConexion(error)
    })

    conexion
      .start()
      .then(() => {
        logDetalle('conectado')
        if (desmontado) {
          return conexion.stop().catch(() => undefined)
        }
        return unirseAGrupos().catch(manejarErrorConexion)
      })
      .catch(manejarErrorConexion)

    return () => {
      desmontado = true
      conexion.off('ParticipantesSesionActualizados', manejarParticipantes)
      conexion.off('EquiposSesionActualizados', manejarEquipos)
      conexion.off('EquipoActualizado', manejarEquipo)
      conexion.off('SesionActualizada', manejarSesion)
      conexion.off('ParticipanteExpulsadoSesion', manejarParticipanteExpulsado)
      conexion.off('EquipoExpulsadoSesion', manejarEquipoExpulsado)
      conexion.off('RespuestaRegistrada', manejarRespuesta)
      conexion.off('EtapaCompletada', manejarEtapa)
      conexion.off('EtapaPorComenzar', manejarEtapaPorComenzar)
      conexion.off('EtapaIniciada', manejarEtapaIniciada)
      conexion.off('ProgresoSecuencialActualizado', manejarProgresoSecuencial)
      conexion.off('UbicacionActualizada', manejarUbicacion)

      if (conexion.state === signalR.HubConnectionState.Connected) {
        Promise.all([
          sesionId
            ? conexion.invoke('SalirDeSesion', sesionId).catch(() => undefined)
            : Promise.resolve(),
          equipoId
            ? conexion.invoke('SalirDeEquipo', equipoId).catch(() => undefined)
            : Promise.resolve()
        ]).finally(() => {
          conexion.stop().catch(() => undefined)
        })
      }
    }
  }, [tokenLimpio, sesionId, equipoId])
}
