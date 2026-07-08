import * as signalR from '@microsoft/signalr'

const URL_API = import.meta.env.VITE_API_URL ?? 'http://localhost:5000'

export interface EventoSesionTiempoReal {
  sesionId?: string
  SesionId?: string
  equipoId?: string | null
  EquipoId?: string | null
  estado?: string
  Estado?: string
  tipoEvento?: string
  TipoEvento?: string
  fechaEventoUtc?: string
  FechaEventoUtc?: string
}

export function obtenerSesionIdEvento(evento: EventoSesionTiempoReal): string {
  return evento.sesionId ?? evento.SesionId ?? ''
}

export function obtenerEquipoIdEvento(evento: EventoSesionTiempoReal): string {
  return evento.equipoId ?? evento.EquipoId ?? ''
}

export function obtenerEstadoEvento(
  evento: EventoSesionTiempoReal
): string | undefined {
  return evento.estado ?? evento.Estado
}

export function esErrorNoAutenticadoTiempoReal(error: unknown): boolean {
  if (!error) return false

  const candidato = error as {
    statusCode?: unknown
    status?: unknown
    message?: unknown
  }

  if (candidato.statusCode === 401 || candidato.status === 401) return true

  const mensaje =
    typeof candidato.message === 'string'
      ? candidato.message
      : typeof error === 'string'
        ? error
        : ''

  return /(?:\b401\b|unauthorized|no[_ ]autenticado)/i.test(mensaje)
}

export function crearConexionSesionesTiempoReal(token: string) {
  const tokenLimpio = token?.trim()
  if (!tokenLimpio) {
    throw new Error('No se puede crear conexion SignalR sin token de acceso.')
  }

  const urlHubSesiones = `${URL_API}/hubs/sesiones`
  if (import.meta.env.DEV) {
    console.debug('[SignalR Web] URL hub sesiones:', urlHubSesiones)
    console.debug('[SignalR Web] creando conexión con token disponible')
  }

  return new signalR.HubConnectionBuilder()
    .withUrl(urlHubSesiones, {
      accessTokenFactory: () => tokenLimpio
    })
    .withAutomaticReconnect()
    .build()
}
