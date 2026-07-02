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
  // Nunca abrir una conexión sin token válido (evita 401 no controlados).
  const tokenLimpio = token?.trim()
  if (!tokenLimpio) {
    throw new Error('No se puede crear conexión SignalR sin token de acceso.')
  }
  if (import.meta.env.DEV) {
    // Nunca imprimir el JWT completo; solo indicar disponibilidad.
    console.debug('[SignalR] creando conexión (token disponible).')
  }
  return new signalR.HubConnectionBuilder()
    .withUrl(`${URL_API}/hubs/sesiones`, {
      accessTokenFactory: () => tokenLimpio
    })
    .withAutomaticReconnect()
    .build()
}
