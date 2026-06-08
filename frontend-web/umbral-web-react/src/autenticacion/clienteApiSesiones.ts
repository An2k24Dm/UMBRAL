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

// Cuerpo del POST /api/sesiones según el ERS actual. NO incluye
// tiempoEjecucionMinutos: el backend ya no lo modela.
export interface CrearSesionSolicitud {
  nombre: string
  descripcion: string
  modo: ModoSesionApi
  fechaProgramada: string
  misionesIds: string[]
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
  puntajeActual: number
  fechaCreacion: string
  participantes: ParticipanteEquipoDto[]
}

export interface ParticipanteSesionDto {
  id: string
  participanteId: string
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
  misiones: SesionMisionDto[]
  equipos: EquipoSesionDto[]
  participantesIndividuales: ParticipanteSesionDto[]
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
