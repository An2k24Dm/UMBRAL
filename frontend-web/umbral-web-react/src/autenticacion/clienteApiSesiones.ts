// Apunta al api-gateway, que enruta /api/sesiones hacia el microservicio
// sesiones-servicio. Conserva el patrón de los otros clientes
// (clienteApi.ts / clienteApiJuegos.ts) para no introducir disonancia.

import { dispatchSesionInvalida } from './eventosSesion'

const URL_API = import.meta.env.VITE_API_URL ?? 'http://localhost:5000'

// Helper compartido: ante un 401, avisamos al ProveedorAutenticacion
// para que limpie la sesión y deja el mensaje legible para la UI.
function lanzar401(mensaje: string): never {
  dispatchSesionInvalida()
  throw new Error(mensaje)
}

const ENDPOINTS = {
  raiz: '/api/sesiones',
  porId: (id: string) => `/api/sesiones/${encodeURIComponent(id)}`,
  triviasActivas: '/api/juegos/trivias/activas',
  busquedasActivas: '/api/juegos/busquedas/activas'
}

function auth(token: string) {
  return { Authorization: `Bearer ${token}` }
}

async function leerError(respuesta: Response): Promise<string> {
  const cuerpo = (await respuesta.json().catch(() => null)) as { mensaje?: string } | null
  return cuerpo?.mensaje ?? `Error ${respuesta.status} al consultar el servidor.`
}

// ---------------------------------------------------------------------------
// Tipos
// ---------------------------------------------------------------------------
export type TipoJuegoSesion = 'Trivia' | 'BusquedaTesoro'
export type ModoSesionApi = 'Individual' | 'Grupo'

export interface CrearSesionSolicitud {
  nombre: string
  tipoJuego: TipoJuegoSesion
  contenidoJuegoId: string
  modo: ModoSesionApi
  fechaProgramada: string
}

export interface SesionRespuestaDto {
  id: string
  nombre: string
  tipoJuego: string
  contenidoJuegoId: string
  modo: string
  estado: string
  fechaProgramada: string
  creadaPorUsuarioId: string
  fechaCreacion: string
}

export interface SesionListadoDto {
  id: string
  nombre: string
  tipoJuego: string
  contenidoJuegoId: string
  modo: string
  estado: string
  fechaProgramada: string
}

export type SesionDetalleDto = SesionRespuestaDto

export interface ContenidoJuegoActivoDto {
  id: string
  nombre: string
  tipoJuego: string
  estado: string
  estaActivo: boolean
}

// ---------------------------------------------------------------------------
// HU33 — Crear sesión
// ---------------------------------------------------------------------------
export async function crearSesion(
  datos: CrearSesionSolicitud,
  token: string
): Promise<SesionRespuestaDto> {
  const respuesta = await fetch(`${URL_API}${ENDPOINTS.raiz}`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json', ...auth(token) },
    body: JSON.stringify(datos)
  })
  if (respuesta.status === 401) lanzar401('Debe iniciar sesión.')
  if (respuesta.status === 403) throw new Error('No tiene permisos para crear sesiones.')
  if (!respuesta.ok) throw new Error(await leerError(respuesta))
  return (await respuesta.json()) as SesionRespuestaDto
}

// ---------------------------------------------------------------------------
// HU33 — Listar sesiones
// ---------------------------------------------------------------------------
export async function listarSesiones(token: string): Promise<SesionListadoDto[]> {
  const respuesta = await fetch(`${URL_API}${ENDPOINTS.raiz}`, {
    headers: auth(token)
  })
  if (respuesta.status === 401) lanzar401('Debe iniciar sesión.')
  if (respuesta.status === 403) throw new Error('No tiene permisos.')
  if (!respuesta.ok) throw new Error(await leerError(respuesta))
  return (await respuesta.json()) as SesionListadoDto[]
}

// ---------------------------------------------------------------------------
// HU33 — Obtener una sesión por id
// ---------------------------------------------------------------------------
export async function obtenerSesion(
  id: string, token: string
): Promise<SesionDetalleDto> {
  const respuesta = await fetch(`${URL_API}${ENDPOINTS.porId(id)}`, {
    headers: auth(token)
  })
  if (respuesta.status === 401) lanzar401('Debe iniciar sesión.')
  if (respuesta.status === 403) throw new Error('No tiene permisos.')
  if (respuesta.status === 404) throw new Error('Sesión no encontrada.')
  if (!respuesta.ok) throw new Error(await leerError(respuesta))
  return (await respuesta.json()) as SesionDetalleDto
}

// ---------------------------------------------------------------------------
// HU33 — Listado de contenido activo (Trivias / Búsquedas) para el
// formulario de creación. Reutiliza los endpoints de juegos-servicio.
// ---------------------------------------------------------------------------
export interface ContenidoActivoResumen {
  id: string
  nombre: string
}

export async function listarContenidoActivo(
  tipoJuego: TipoJuegoSesion, token: string
): Promise<ContenidoActivoResumen[]> {
  const url = tipoJuego === 'Trivia' ? ENDPOINTS.triviasActivas : ENDPOINTS.busquedasActivas
  const respuesta = await fetch(`${URL_API}${url}`, { headers: auth(token) })
  if (respuesta.status === 401) lanzar401('Debe iniciar sesión.')
  if (respuesta.status === 403) throw new Error('No tiene permisos.')
  if (!respuesta.ok) throw new Error(await leerError(respuesta))
  const cuerpo = (await respuesta.json()) as Array<{ id: string; nombre: string }>
  return cuerpo.map(c => ({ id: c.id, nombre: c.nombre }))
}
