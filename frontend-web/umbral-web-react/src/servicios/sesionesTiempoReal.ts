import * as signalR from '@microsoft/signalr'

const URL_API = import.meta.env.VITE_API_URL ?? 'http://localhost:5000'

export interface EventoSesionTiempoReal {
  sesionId?: string
  SesionId?: string
  equipoId?: string | null
  EquipoId?: string | null
  tipoEvento?: string
  TipoEvento?: string
  fechaEventoUtc?: string
  FechaEventoUtc?: string
}

export function obtenerSesionIdEvento(evento: EventoSesionTiempoReal): string {
  return evento.sesionId ?? evento.SesionId ?? ''
}

export function crearConexionSesionesTiempoReal(token: string) {
  return new signalR.HubConnectionBuilder()
    .withUrl(`${URL_API}/hubs/sesiones`, {
      accessTokenFactory: () => token
    })
    .withAutomaticReconnect()
    .build()
}
