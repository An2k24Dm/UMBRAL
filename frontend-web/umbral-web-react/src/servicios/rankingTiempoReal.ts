import * as signalR from '@microsoft/signalr'

const URL_API = import.meta.env.VITE_API_URL ?? 'http://localhost:5000'

export function crearConexionRankingTiempoReal(token: string) {
  const tokenLimpio = token?.trim()
  if (!tokenLimpio) {
    throw new Error('No se puede crear conexion SignalR sin token de acceso.')
  }

  return new signalR.HubConnectionBuilder()
    .withUrl(`${URL_API}/hubs/ranking`, {
      accessTokenFactory: () => tokenLimpio
    })
    .withAutomaticReconnect()
    .build()
}
