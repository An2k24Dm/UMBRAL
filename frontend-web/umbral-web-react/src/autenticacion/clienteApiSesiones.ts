import { manejar401 } from './eventosSesion'

const URL_API = import.meta.env.VITE_API_URL ?? 'http://localhost:5000'

// Helper local que admite las dos formas históricas del repo:
//   lanzar401('msg')           → manejar401 lee el token de localStorage.
//   lanzar401(token, 'msg')    → manejar401 usa el token explícito.
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

// ---------------------------------------------------------------------------
// Tipos del nuevo modelo
// ---------------------------------------------------------------------------
export type ModoSesionApi = 'Individual' | 'Grupal'

export type EstadoSesionApi =
  | 'Programada'
  | 'EnPreparacion'
  | 'Activa'
  | 'Pausada'
  | 'Finalizada'
  | 'Cancelada'

// Cuerpo del POST /api/sesiones según el ERS actual. La capacidad es
// configurable por el Operador: se envía solo la que aplica al modo y el
// resto viaja en null (el backend los trata como opcionales por modo).
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
  // Capacidad configurada (solo se llena la que aplica al modo).
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
  // Capacidad configurada (solo se llena la que aplica al modo).
  maximoParticipantes: number | null
  maximoEquipos: number | null
  maximoParticipantesPorEquipo: number | null
  misiones: SesionMisionDto[]
  equipos: EquipoSesionDto[]
  participantesIndividuales: ParticipanteSesionDto[]
}

// Cuerpo del PUT /api/sesiones/{id}. No incluye código de acceso, estado,
// operadorCreadorId ni fechas de creación/inicio/fin: el backend los ignora
// y no deben modificarse. La capacidad va solo en el campo que aplica al
// modo; el resto viaja en null.
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

// ---------------------------------------------------------------------------
// Crear sesión (solo Operador)
// ---------------------------------------------------------------------------
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

// ---------------------------------------------------------------------------
// Listado de sesiones (Administrador ve todo, Operador ve propias)
// ---------------------------------------------------------------------------
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

// ---------------------------------------------------------------------------
// Detalle de sesión
// ---------------------------------------------------------------------------
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

// ---------------------------------------------------------------------------
// Modificar sesión (solo Operador, solo sesiones propias en estado Programada)
// ---------------------------------------------------------------------------
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
  // 400 (validación), 409 (no está Programada) y demás traen mensaje del backend.
  if (!respuesta.ok) throw new Error(await leerError(respuesta))
  return (await respuesta.json()) as SesionDetalleDto
}

// ---------------------------------------------------------------------------
// Eliminar sesión (solo Operador, solo sesiones propias en estado Programada)
// ---------------------------------------------------------------------------
export async function eliminarSesion(id: string, token: string): Promise<void> {
  const respuesta = await fetch(`${URL_API}${ENDPOINTS.porId(id)}`, {
    method: 'DELETE',
    headers: auth(token)
  })
  if (respuesta.status === 204) return
  if (respuesta.status === 401) lanzar401(token, 'Debe iniciar sesión.')
  if (respuesta.status === 403) throw new Error('No tienes permisos para eliminar esta sesión.')
  if (respuesta.status === 404) throw new Error('La sesión no existe o ya fue eliminada.')
  // 409: la sesión no está Programada. El backend envía el mensaje exacto
  // ("Solo se pueden eliminar sesiones en estado Programada."); lo propagamos.
  if (respuesta.status === 409) throw new Error(await leerError(respuesta))
  throw new Error('No se pudo eliminar la sesión. Intenta nuevamente.')
}

// ---------------------------------------------------------------------------
// HU44 — Expulsar participante (sesión individual) o equipo (sesión grupal).
// Solo el Operador creador y solo con la sesión En Preparación o Pausada. El
// backend valida la regla y devuelve 409 con el mensaje exacto si no aplica.
// ---------------------------------------------------------------------------
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
  // 409: la sesión no está En Preparación ni Pausada; propagamos el mensaje
  // exacto del backend.
  if (respuesta.status === 409) throw new Error(await leerError(respuesta))
  throw new Error('No se pudo expulsar al participante. Intenta nuevamente.')
}

// HU45 — Expulsar a un participante de un equipo. Lo puede hacer el líder
// del equipo (Participante) o el Operador dueño de la sesión. El backend
// valida estado (EnPreparacion/Pausada) y reasigna liderazgo si aplica.
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

// ---------------------------------------------------------------------------
// Transiciones de estado de sesión (solo Operador dueño)
// ---------------------------------------------------------------------------
export async function iniciarSesion(id: string, token: string): Promise<void> {
  const respuesta = await fetch(`${URL_API}${ENDPOINTS.porId(id)}/iniciar`, {
    method: 'POST',
    headers: auth(token)
  })
  if (respuesta.status === 204) return
  if (respuesta.status === 401) lanzar401(token, 'Debe iniciar sesión.')
  if (respuesta.status === 403) throw new Error('No tiene permisos para iniciar esta sesión.')
  if (respuesta.status === 404) throw new Error('Sesión no encontrada.')
  if (respuesta.status === 409) throw new Error(await leerError(respuesta))
  throw new Error('No se pudo iniciar la sesión. Intenta nuevamente.')
}

export async function pausarSesion(id: string, token: string): Promise<void> {
  const respuesta = await fetch(`${URL_API}${ENDPOINTS.porId(id)}/pausar`, {
    method: 'POST',
    headers: auth(token)
  })
  if (respuesta.status === 204) return
  if (respuesta.status === 401) lanzar401(token, 'Debe iniciar sesión.')
  if (respuesta.status === 403) throw new Error('No tiene permisos para pausar esta sesión.')
  if (respuesta.status === 404) throw new Error('Sesión no encontrada.')
  if (respuesta.status === 409) throw new Error(await leerError(respuesta))
  throw new Error('No se pudo pausar la sesión. Intenta nuevamente.')
}

export async function reanudarSesion(id: string, token: string): Promise<void> {
  const respuesta = await fetch(`${URL_API}${ENDPOINTS.porId(id)}/reanudar`, {
    method: 'POST',
    headers: auth(token)
  })
  if (respuesta.status === 204) return
  if (respuesta.status === 401) lanzar401(token, 'Debe iniciar sesión.')
  if (respuesta.status === 403) throw new Error('No tiene permisos para reanudar esta sesión.')
  if (respuesta.status === 404) throw new Error('Sesión no encontrada.')
  if (respuesta.status === 409) throw new Error(await leerError(respuesta))
  throw new Error('No se pudo reanudar la sesión. Intenta nuevamente.')
}

export async function cancelarSesion(id: string, token: string): Promise<void> {
  const respuesta = await fetch(`${URL_API}${ENDPOINTS.porId(id)}/cancelar`, {
    method: 'POST',
    headers: auth(token)
  })
  if (respuesta.status === 204) return
  if (respuesta.status === 401) lanzar401(token, 'Debe iniciar sesión.')
  if (respuesta.status === 403) throw new Error('No tiene permisos para cancelar esta sesión.')
  if (respuesta.status === 404) throw new Error('Sesión no encontrada.')
  if (respuesta.status === 409) throw new Error(await leerError(respuesta))
  throw new Error('No se pudo cancelar la sesión. Intenta nuevamente.')
}
