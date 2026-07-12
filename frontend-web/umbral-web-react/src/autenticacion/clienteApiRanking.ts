import { manejar401 } from './eventosSesion'

const URL_API = import.meta.env.VITE_API_URL ?? 'http://localhost:5000'

function lanzar401(token: string | null, mensaje: string): never {
  manejar401(token, mensaje)
}

export interface EntradaRankingParticipanteDto {
  sesionId: string
  participanteIdentidadId: string
  nombreParticipante: string
  puntajeTotal: number
  respuestasCorrectas: number
  respuestasTotales: number
  etapasCompletadas: number
  posicion: number
}

export interface EntradaRankingEquipoDto {
  sesionId: string
  equipoId: string
  nombreEquipo: string
  puntajeTotal: number
  etapasCompletadas: number
  posicion: number
}

function auth(token: string) {
  return { Authorization: `Bearer ${token}` }
}

async function leerError(respuesta: Response): Promise<string> {
  const cuerpo = (await respuesta.json().catch(() => null)) as { mensaje?: string } | null
  return cuerpo?.mensaje ?? `Error ${respuesta.status} al consultar el servidor.`
}

export async function obtenerRankingParticipantes(
  sesionId: string,
  token: string
): Promise<EntradaRankingParticipanteDto[]> {
  const resp = await fetch(
    `${URL_API}/api/ranking/sesiones/${encodeURIComponent(sesionId)}/participantes`,
    { headers: auth(token) }
  )
  if (resp.status === 401) lanzar401(token, 'Sesión expirada.')
  if (!resp.ok) throw new Error(await leerError(resp))
  return resp.json() as Promise<EntradaRankingParticipanteDto[]>
}

export async function obtenerRankingEquipos(
  sesionId: string,
  token: string
): Promise<EntradaRankingEquipoDto[]> {
  const resp = await fetch(
    `${URL_API}/api/ranking/sesiones/${encodeURIComponent(sesionId)}/equipos`,
    { headers: auth(token) }
  )
  if (resp.status === 401) lanzar401(token, 'Sesión expirada.')
  if (!resp.ok) throw new Error(await leerError(resp))
  return resp.json() as Promise<EntradaRankingEquipoDto[]>
}
