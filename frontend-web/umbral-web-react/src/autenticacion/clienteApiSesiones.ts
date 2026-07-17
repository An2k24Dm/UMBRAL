import { manejar401 } from './eventosSesion'

const URL_API = import.meta.env.VITE_API_URL ?? 'http://localhost:5000'

function lanzar401(arg1?: string | null, arg2?: string): never {
  const tieneAmbos = typeof arg2 === 'string'
  const token = tieneAmbos ? (arg1 ?? null) : null
  const mensaje = tieneAmbos ? arg2 : (arg1 ?? 'Debe iniciar sesión.')
  manejar401(token, mensaje)
}

const ENDPOINTS = {
  raiz: '/api/sesiones',
  porId: (id: string) => `/api/sesiones/${encodeURIComponent(id)}`
}

function auth(token: string) {
  return { Authorization: `Bearer ${token}` }
}

async function leerError(respuesta: Response): Promise<string> {
  const cuerpo = (await respuesta.json().catch(() => null)) as { mensaje?: string } | null
  return cuerpo?.mensaje ?? `Error ${respuesta.status} al consultar el servidor.`
}

async function leerErrorOperacionSesion(respuesta: Response): Promise<string> {
  const cuerpo = (await respuesta.json().catch(() => null)) as {
    codigo?: string
    mensaje?: string
  } | null

  if (respuesta.status === 404 && cuerpo?.codigo === 'SESION_NO_ENCONTRADA') {
    return cuerpo.mensaje ?? 'La sesión no existe o ya fue eliminada.'
  }

  return cuerpo?.mensaje ?? `Error ${respuesta.status} al operar la sesión.`
}

export type ModoSesionApi = 'Individual' | 'Grupal'

export type EstadoSesionApi =
  | 'Programada'
  | 'EnPreparacion'
  | 'Activa'
  | 'Pausada'
  | 'Finalizada'
  | 'Cancelada'

export interface CrearSesionSolicitud {
  nombre: string
  descripcion: string
  modo: ModoSesionApi
  fechaProgramada: string
  misionesIds: string[]
  maximoParticipantes: number | null
  maximoEquipos: number | null
  maximoParticipantesPorEquipo: number | null
}

export interface CrearSesionRespuestaDto {
  id: string
  nombre: string
  descripcion: string
  modo: string
  estado: string
  fechaProgramada: string
  codigoAcceso: string
  operadorCreadorId: string
  fechaCreacion: string
  misionesIds: string[]
}

export interface SesionListadoDto {
  id: string
  nombre: string
  descripcion: string
  modo: string
  estado: string
  fechaProgramada: string
  codigoAcceso: string
  operadorCreadorId: string
  fechaCreacion: string
  cantidadMisiones: number
  cantidadParticipantes: number
  cantidadEquipos: number
  maximoParticipantes: number | null
  maximoEquipos: number | null
  maximoParticipantesPorEquipo: number | null
}

export interface SesionMisionDto {
  id: string
  misionId: string
  orden: number
}

export interface ParticipanteEquipoDto {
  id: string
  participanteId: string
  fechaUnion: string
}

export interface EquipoSesionDto {
  id: string
  nombre: string
  tipo: string
  puntajeActual: number
  capacidadMaxima: number
  fechaCreacion: string
  participantes: ParticipanteEquipoDto[]
}

export interface EquipoSesionListadoDto {
  id: string
  sesionId: string
  nombre: string
  tipo: string
  puntaje: number
  cantidadParticipantes: number
  capacidadMaxima: number
  estaLleno: boolean
  fechaCreacion: string
  esMiEquipo: boolean
  soyLider: boolean
}

export interface IntegranteEquipoDto {
  participanteSesionId: string
  participanteIdentidadId: string
  alias: string
  nombre: string
  apellido: string
  puntaje: number
  fechaUnion: string
  esLider: boolean
}

export interface EquipoSesionDetalleDto {
  id: string
  sesionId: string
  nombre: string
  tipo: string
  puntaje: number
  cantidadParticipantes: number
  capacidadMaxima: number
  fechaCreacion: string
  estaLleno: boolean
  liderParticipanteId: string
  esMiEquipo: boolean
  soyLider: boolean
  participantes: IntegranteEquipoDto[]
}

export interface ParticipanteSesionDto {
  participanteSesionId: string
  participanteIdentidadId: string
  alias: string
  nombre: string
  apellido: string
  puntaje: number
  fechaUnion: string
}

export interface SesionDetalleDto {
  id: string
  nombre: string
  descripcion: string
  modo: string
  estado: string
  fechaProgramada: string
  codigoAcceso: string
  operadorCreadorId: string
  fechaCreacion: string
  fechaInicioUtc: string | null
  fechaFinalizacionUtc: string | null
  maximoParticipantes: number | null
  maximoEquipos: number | null
  maximoParticipantesPorEquipo: number | null
  misiones: SesionMisionDto[]
  equipos: EquipoSesionDto[]
  participantesIndividuales: ParticipanteSesionDto[]
}

export interface ModificarSesionSolicitud {
  nombre: string
  descripcion: string
  modo: ModoSesionApi
  fechaProgramada: string
  misionesIds: string[]
  maximoParticipantes: number | null
  maximoEquipos: number | null
  maximoParticipantesPorEquipo: number | null
}

export interface OperacionSesionRespuestaDto {
  sesionId: string
  estado: EstadoSesionApi
  fechaInicioUtc: string | null
  fechaFinalizacionUtc: string | null
  mensaje: string
}

export async function crearSesion(
  datos: CrearSesionSolicitud,
  token: string
): Promise<CrearSesionRespuestaDto> {
  const respuesta = await fetch(`${URL_API}${ENDPOINTS.raiz}`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json', ...auth(token) },
    body: JSON.stringify(datos)
  })
  if (respuesta.status === 401) lanzar401(token, 'Debe iniciar sesión.')
  if (respuesta.status === 403) throw new Error('No tiene permisos para crear sesiones.')
  if (!respuesta.ok) throw new Error(await leerError(respuesta))
  return (await respuesta.json()) as CrearSesionRespuestaDto
}

export interface FiltrosListadoSesiones {
  estado?: EstadoSesionApi | ''
}

export async function listarSesiones(
  token: string,
  filtros: FiltrosListadoSesiones = {}
): Promise<SesionListadoDto[]> {
  const params = new URLSearchParams()
  if (filtros.estado) params.set('estado', filtros.estado)
  const cadena = params.toString()
  const url = cadena ? `${ENDPOINTS.raiz}?${cadena}` : ENDPOINTS.raiz

  const respuesta = await fetch(`${URL_API}${url}`, { headers: auth(token) })
  if (respuesta.status === 401) lanzar401(token, 'Debe iniciar sesión.')
  if (respuesta.status === 403) throw new Error('No tiene permisos.')
  if (!respuesta.ok) throw new Error(await leerError(respuesta))
  return (await respuesta.json()) as SesionListadoDto[]
}

export async function obtenerSesion(
  id: string, token: string
): Promise<SesionDetalleDto> {
  const respuesta = await fetch(`${URL_API}${ENDPOINTS.porId(id)}`, {
    headers: auth(token)
  })
  if (respuesta.status === 401) lanzar401(token, 'Debe iniciar sesión.')
  if (respuesta.status === 403) throw new Error('No tiene permiso para ver esta sesión.')
  if (respuesta.status === 404) throw new Error('Sesión no encontrada.')
  if (!respuesta.ok) throw new Error(await leerError(respuesta))
  return (await respuesta.json()) as SesionDetalleDto
}

export async function listarEquiposSesion(
  sesionId: string, token: string
): Promise<EquipoSesionListadoDto[]> {
  const respuesta = await fetch(
    `${URL_API}${ENDPOINTS.porId(sesionId)}/equipos`,
    { headers: auth(token) }
  )
  if (respuesta.status === 401) lanzar401(token, 'Debe iniciar sesión.')
  if (respuesta.status === 403) throw new Error('No tiene permiso para ver los equipos de esta sesión.')
  if (respuesta.status === 404) throw new Error('Sesión no encontrada.')
  if (!respuesta.ok) throw new Error(await leerError(respuesta))
  return (await respuesta.json()) as EquipoSesionListadoDto[]
}

export async function obtenerDetalleEquipoSesion(
  sesionId: string, equipoId: string, token: string
): Promise<EquipoSesionDetalleDto> {
  const respuesta = await fetch(
    `${URL_API}${ENDPOINTS.porId(sesionId)}/equipos/${encodeURIComponent(equipoId)}`,
    { headers: auth(token) }
  )
  if (respuesta.status === 401) lanzar401(token, 'Debe iniciar sesión.')
  if (respuesta.status === 403) throw new Error('No tiene permiso para ver este equipo.')
  if (respuesta.status === 404) throw new Error('Equipo no encontrado.')
  if (!respuesta.ok) throw new Error(await leerError(respuesta))
  return (await respuesta.json()) as EquipoSesionDetalleDto
}

export async function actualizarSesion(
  id: string,
  datos: ModificarSesionSolicitud,
  token: string
): Promise<SesionDetalleDto> {
  const respuesta = await fetch(`${URL_API}${ENDPOINTS.porId(id)}`, {
    method: 'PUT',
    headers: { 'Content-Type': 'application/json', ...auth(token) },
    body: JSON.stringify(datos)
  })
  if (respuesta.status === 401) lanzar401(token, 'Debe iniciar sesión.')
  if (respuesta.status === 403) throw new Error('No tiene permiso para modificar esta sesión.')
  if (respuesta.status === 404) throw new Error('Sesión no encontrada.')
  if (!respuesta.ok) throw new Error(await leerError(respuesta))
  return (await respuesta.json()) as SesionDetalleDto
}

export async function eliminarSesion(id: string, token: string): Promise<void> {
  const respuesta = await fetch(`${URL_API}${ENDPOINTS.porId(id)}`, {
    method: 'DELETE',
    headers: auth(token)
  })
  if (respuesta.status === 204) return
  if (respuesta.status === 401) lanzar401(token, 'Debe iniciar sesión.')
  if (respuesta.status === 403) throw new Error('No tienes permisos para eliminar esta sesión.')
  if (respuesta.status === 404) throw new Error('La sesión no existe o ya fue eliminada.')
  if (respuesta.status === 409) throw new Error(await leerError(respuesta))
  throw new Error('No se pudo eliminar la sesión. Intenta nuevamente.')
}

type AccionSesion = 'iniciar' | 'pausar' | 'reanudar' | 'cancelar'

async function operarSesion(
  id: string, accion: AccionSesion, token: string
): Promise<OperacionSesionRespuestaDto> {
  const respuesta = await fetch(`${URL_API}${ENDPOINTS.porId(id)}/${accion}`, {
    method: 'PATCH',
    headers: auth(token)
  })
  if (respuesta.status === 401) lanzar401(token, 'Debe iniciar sesión.')
  if (respuesta.status === 403) throw new Error('No tienes permisos para operar esta sesión.')
  if (respuesta.status === 404) throw new Error(await leerErrorOperacionSesion(respuesta))
  if (!respuesta.ok) throw new Error(await leerError(respuesta))
  return (await respuesta.json()) as OperacionSesionRespuestaDto
}

export function iniciarSesionOperacion(
  id: string, token: string
): Promise<OperacionSesionRespuestaDto> {
  return operarSesion(id, 'iniciar', token)
}

export function pausarSesionOperacion(
  id: string, token: string
): Promise<OperacionSesionRespuestaDto> {
  return operarSesion(id, 'pausar', token)
}

export function reanudarSesionOperacion(
  id: string, token: string
): Promise<OperacionSesionRespuestaDto> {
  return operarSesion(id, 'reanudar', token)
}

export function cancelarSesionOperacion(
  id: string, token: string
): Promise<OperacionSesionRespuestaDto> {
  return operarSesion(id, 'cancelar', token)
}

export async function expulsarParticipanteSesion(
  sesionId: string, participanteSesionId: string, token: string
): Promise<void> {
  const respuesta = await fetch(
    `${URL_API}${ENDPOINTS.porId(sesionId)}/participantes/` +
    `${encodeURIComponent(participanteSesionId)}/expulsar`,
    { method: 'DELETE', headers: auth(token) }
  )
  if (respuesta.status === 204) return
  if (respuesta.status === 401) lanzar401(token, 'Debe iniciar sesión.')
  if (respuesta.status === 403) throw new Error('No tienes permisos para expulsar participantes de esta sesión.')
  if (respuesta.status === 404) throw new Error('El participante o la sesión ya no existen.')
  if (respuesta.status === 409) throw new Error(await leerError(respuesta))
  throw new Error('No se pudo expulsar al participante. Intenta nuevamente.')
}

export async function expulsarParticipanteEquipo(
  sesionId: string,
  equipoId: string,
  participanteSesionId: string,
  token: string
): Promise<void> {
  const respuesta = await fetch(
    `${URL_API}${ENDPOINTS.porId(sesionId)}/equipos/${encodeURIComponent(equipoId)}` +
    `/participantes/${encodeURIComponent(participanteSesionId)}/expulsar`,
    { method: 'DELETE', headers: auth(token) }
  )
  if (respuesta.status === 204) return
  if (respuesta.status === 401) lanzar401(token, 'Debe iniciar sesión.')
  if (respuesta.status === 403) throw new Error('No tienes permisos para expulsar este participante del equipo.')
  if (respuesta.status === 404) throw new Error('El participante, equipo o sesión ya no existe.')
  // 409: estado no permitido, líder único, etc. Propagamos el mensaje exacto.
  if (respuesta.status === 409) throw new Error(await leerError(respuesta))
  throw new Error('No se pudo expulsar al participante. Intenta nuevamente.')
}

export async function expulsarEquipoSesion(
  sesionId: string, equipoId: string, token: string
): Promise<void> {
  const respuesta = await fetch(
    `${URL_API}${ENDPOINTS.porId(sesionId)}/equipos/` +
    `${encodeURIComponent(equipoId)}/expulsar`,
    { method: 'DELETE', headers: auth(token) }
  )
  if (respuesta.status === 204) return
  if (respuesta.status === 401) lanzar401(token, 'Debe iniciar sesión.')
  if (respuesta.status === 403) throw new Error('No tienes permisos para expulsar equipos de esta sesión.')
  if (respuesta.status === 404) throw new Error('El equipo o la sesión ya no existen.')
  if (respuesta.status === 409) throw new Error(await leerError(respuesta))
  throw new Error('No se pudo expulsar al equipo. Intenta nuevamente.')
}

export interface PenalizacionEncoladaDto {
  eventoId: string
  estado: string
}

async function aplicarPenalizacion(
  url: string, puntos: number, motivo: string, token: string
): Promise<PenalizacionEncoladaDto> {
  const respuesta = await fetch(url, {
    method: 'POST',
    headers: { ...auth(token), 'Content-Type': 'application/json' },
    body: JSON.stringify({ puntos, motivo: motivo.trim() })
  })
  if (respuesta.status === 202) return respuesta.json() as Promise<PenalizacionEncoladaDto>
  if (respuesta.status === 401) lanzar401(token, 'Debe iniciar sesión.')
  if (respuesta.status === 403) throw new Error('No tienes permisos para penalizar en esta sesión.')
  if (respuesta.status === 404) throw new Error('La sesión o el objetivo ya no existen.')
  if (respuesta.status === 400 || respuesta.status === 409) throw new Error(await leerError(respuesta))
  throw new Error('No se pudo aplicar la penalización. Intenta nuevamente.')
}

export function penalizarParticipanteSesion(
  sesionId: string, participanteSesionId: string,
  puntos: number, motivo: string, token: string
): Promise<PenalizacionEncoladaDto> {
  return aplicarPenalizacion(
    `${URL_API}${ENDPOINTS.porId(sesionId)}/participantes/` +
    `${encodeURIComponent(participanteSesionId)}/penalizaciones`,
    puntos, motivo, token)
}

export function penalizarEquipoSesion(
  sesionId: string, equipoId: string,
  puntos: number, motivo: string, token: string
): Promise<PenalizacionEncoladaDto> {
  return aplicarPenalizacion(
    `${URL_API}${ENDPOINTS.porId(sesionId)}/equipos/` +
    `${encodeURIComponent(equipoId)}/penalizaciones`,
    puntos, motivo, token)
}

export async function liberarPista(
  sesionId: string,
  etapaId: string,
  pistaId: string | null,
  contenido: string,
  token: string,
  tipo?: 'Texto' | 'CoordenadaGps',
  latitud?: number,
  longitud?: number
): Promise<void> {
  const respuesta = await fetch(
    `${URL_API}${ENDPOINTS.porId(sesionId)}/etapas/${encodeURIComponent(etapaId)}/pistas-liberadas`,
    {
      method: 'POST',
      headers: { 'Content-Type': 'application/json', ...auth(token) },
      body: JSON.stringify({ pistaId, contenido, tipo: tipo ?? 'Texto', latitud, longitud })
    }
  )
  if (respuesta.status === 204) return
  if (respuesta.status === 401) lanzar401(token, 'Debe iniciar sesión.')
  if (respuesta.status === 403) throw new Error('No tienes permisos para liberar pistas.')
  if (respuesta.status === 404) throw new Error('Sesión o etapa no encontrada.')
  if (respuesta.status === 409) {
    const cuerpo = (await respuesta.json().catch(() => null)) as { mensaje?: string } | null
    throw new Error(cuerpo?.mensaje ?? 'Esta pista ya fue liberada.')
  }
  if (respuesta.status === 422) throw new Error(await leerError(respuesta))
  throw new Error('No se pudo liberar la pista. Intenta nuevamente.')
}

export interface ProgresoTriviaParticipanteDto {
  participanteIdentidadId: string
  totalRespondidas: number
  correctas: number
  incorrectas: number
  puntosGanados: number
}

export async function obtenerProgresoTrivia(
  sesionId: string,
  token: string
): Promise<ProgresoTriviaParticipanteDto[]> {
  const respuesta = await fetch(
    `${URL_API}${ENDPOINTS.porId(sesionId)}/progreso-trivia`,
    { headers: auth(token) }
  )
  if (respuesta.status === 401) lanzar401(token, 'Debe iniciar sesión.')
  if (respuesta.status === 403) throw new Error('No tienes permisos para ver el progreso.')
  if (!respuesta.ok) throw new Error(await leerError(respuesta))
  return respuesta.json() as Promise<ProgresoTriviaParticipanteDto[]>
}

export interface ProgresoSesionParticipanteDto {
  participanteIdentidadId: string
  equipoId: string | null
  triviaEtapasCompletadas: number
  triviaRespondidas: number
  triviaCorrectas: number
  triviaIncorrectas: number
  tesoroIntentosEnviados: number
  tesoroEtapasCompletadas: number
}

export interface ProgresoSesionDto {
  misionActualId: string | null
  etapaActualId: string | null
  ordenMisionActual: number | null
  ordenEtapaActual: number | null
  tipoEtapaActual: string | null
  faseEtapaActual: string | null
  filas: ProgresoSesionParticipanteDto[]
}

export async function obtenerProgresoSesion(
  sesionId: string,
  token: string
): Promise<ProgresoSesionDto> {
  const respuesta = await fetch(
    `${URL_API}${ENDPOINTS.porId(sesionId)}/progreso`,
    { headers: auth(token) }
  )
  if (respuesta.status === 401) lanzar401(token, 'Debe iniciar sesión.')
  if (respuesta.status === 403) throw new Error('No tienes permisos para ver el progreso.')
  if (!respuesta.ok) throw new Error(await leerError(respuesta))
  return respuesta.json() as Promise<ProgresoSesionDto>
}
