import { manejar401 } from './eventosSesion'

const URL_API = import.meta.env.VITE_API_URL ?? 'http://localhost:5000'

function auth(token: string) {
  return { Authorization: `Bearer ${token}` }
}

function lanzar401(token: string | null, mensaje: string): never {
  manejar401(token, mensaje)
}

async function leerError(respuesta: Response): Promise<string> {
  const cuerpo = (await respuesta.json().catch(() => null)) as { mensaje?: string } | null
  return cuerpo?.mensaje ?? `Error ${respuesta.status} al consultar el servidor.`
}

function base(sesionId: string) {
  return `/api/partidas/sesiones/${encodeURIComponent(sesionId)}`
}

async function transicionPartida(
  sesionId: string,
  accion: 'iniciar' | 'pausar' | 'reanudar' | 'finalizar' | 'cancelar',
  token: string
): Promise<void> {
  const respuesta = await fetch(`${URL_API}${base(sesionId)}/${accion}`, {
    method: 'POST',
    headers: auth(token)
  })
  if (respuesta.status === 200 || respuesta.status === 204) return
  if (respuesta.status === 401) lanzar401(token, 'Debe iniciar sesión.')
  if (respuesta.status === 403) throw new Error('No tiene permisos para esta acción en la partida.')
  if (respuesta.status === 404) throw new Error('Partida no encontrada.')
  if (respuesta.status === 409 || respuesta.status === 422) throw new Error(await leerError(respuesta))
  throw new Error('No se pudo realizar la acción en la partida. Intenta nuevamente.')
}

export async function iniciarPartida(sesionId: string, token: string): Promise<void> {
  return transicionPartida(sesionId, 'iniciar', token)
}

export async function pausarPartida(sesionId: string, token: string): Promise<void> {
  return transicionPartida(sesionId, 'pausar', token)
}

export async function reanudarPartida(sesionId: string, token: string): Promise<void> {
  return transicionPartida(sesionId, 'reanudar', token)
}

export async function finalizarPartida(sesionId: string, token: string): Promise<void> {
  return transicionPartida(sesionId, 'finalizar', token)
}

export async function cancelarPartida(sesionId: string, token: string): Promise<void> {
  return transicionPartida(sesionId, 'cancelar', token)
}

export interface EstadoPartidaDto {
  existe: boolean
  estado: string | null
  estaActiva: boolean
}

export async function obtenerEstadoPartida(
  sesionId: string,
  token: string
): Promise<EstadoPartidaDto> {
  const respuesta = await fetch(`${URL_API}${base(sesionId)}/estado`, {
    headers: auth(token)
  })
  if (respuesta.status === 401) lanzar401(token, 'Debe iniciar sesión.')
  if (!respuesta.ok) throw new Error(await leerError(respuesta))
  return (await respuesta.json()) as EstadoPartidaDto
}
