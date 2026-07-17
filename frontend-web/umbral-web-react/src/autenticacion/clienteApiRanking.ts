import { manejar401 } from './eventosSesion'

const URL_API = import.meta.env.VITE_API_URL ?? 'http://localhost:5000'

function lanzar401(token: string | null, mensaje: string): never {
  manejar401(token, mensaje)
}

// La posición se calcula en el backend al consultar (no se persiste). El alias
// se enriquece desde identidad-servicio; el ranking no almacena nombres.
export interface EntradaRankingParticipanteDto {
  posicion: number
  participanteSesionId: string
  participanteIdentidadId: string
  equipoId: string | null
  alias: string
  puntaje: number
  // HU52 — Magnitud positiva acumulada de penalizaciones (se muestra "-N pts").
  puntosPenalizados: number
}

// Aporte de un participante al puntaje de su equipo (detalle desplegable).
export interface AporteParticipanteEquipoDto {
  posicion: number
  participanteSesionId: string
  participanteIdentidadId: string
  alias: string
  puntaje: number
}

export interface EntradaRankingEquipoDto {
  posicion: number
  equipoId: string
  nombreEquipo: string
  puntaje: number
  // HU52 — Magnitud positiva acumulada de penalizaciones del equipo.
  puntosPenalizados: number
  participantes: AporteParticipanteEquipoDto[]
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
