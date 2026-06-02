import { dispatchSesionInvalida } from './eventosSesion'

const URL_API = import.meta.env.VITE_API_URL ?? 'http://localhost:5000'

// Helper compartido: ante un 401, avisamos al ProveedorAutenticacion
// para que limpie la sesión y deja el mensaje legible para la UI.
function lanzar401(mensaje: string): never {
  dispatchSesionInvalida()
  throw new Error(mensaje)
}

const ENDPOINTS = {
  // HU15
  crearTrivia:              '/api/juegos/trivias',
  listarBorrador:           '/api/juegos/trivias/borrador',
  detalleTrivia: (id: string) => `/api/juegos/trivias/${encodeURIComponent(id)}`,
  // HU16
  agregarPregunta: (triviaId: string) =>
    `/api/juegos/trivias/${encodeURIComponent(triviaId)}/preguntas`,
  // HU17
  modificarPregunta: (triviaId: string, preguntaId: string) =>
    `/api/juegos/trivias/${encodeURIComponent(triviaId)}/preguntas/${encodeURIComponent(preguntaId)}`,
  eliminarPregunta: (triviaId: string, preguntaId: string) =>
    `/api/juegos/trivias/${encodeURIComponent(triviaId)}/preguntas/${encodeURIComponent(preguntaId)}`,
  // HU18
  activarTrivia: (triviaId: string) =>
    `/api/juegos/trivias/${encodeURIComponent(triviaId)}/activar`,
  // HU19
  modificarTrivia: (triviaId: string) =>
    `/api/juegos/trivias/${encodeURIComponent(triviaId)}`,
  // HU20
  desactivarTrivia: (triviaId: string) =>
    `/api/juegos/trivias/${encodeURIComponent(triviaId)}`,
  listarActivas: '/api/juegos/trivias/activas',
  // HU21
  crearBusqueda:           '/api/juegos/busquedas',
  listarBusquedasBorrador: '/api/juegos/busquedas/borrador',
  listarBusquedasActivas:  '/api/juegos/busquedas/activas',
  // HU22
  detalleBusqueda: (busquedaId: string) =>
    `/api/juegos/busquedas/${encodeURIComponent(busquedaId)}`,
  // HU23 — misión única
  asignarMision: (busquedaId: string) =>
    `/api/juegos/busquedas/${encodeURIComponent(busquedaId)}/mision`,
  // HU25
  modificarMision: (busquedaId: string) =>
    `/api/juegos/busquedas/${encodeURIComponent(busquedaId)}/mision`,
  eliminarMision: (busquedaId: string) =>
    `/api/juegos/busquedas/${encodeURIComponent(busquedaId)}/mision`,
  // HU26
  activarBusqueda: (busquedaId: string) =>
    `/api/juegos/busquedas/${encodeURIComponent(busquedaId)}/activar`,
  desactivarBusqueda: (busquedaId: string) =>
    `/api/juegos/busquedas/${encodeURIComponent(busquedaId)}`,
  eliminarBusqueda: (busquedaId: string) =>
    `/api/juegos/busquedas/${encodeURIComponent(busquedaId)}/eliminar`,
  // HU28 — pistas bajo la misión
  agregarPista: (busquedaId: string) =>
    `/api/juegos/busquedas/${encodeURIComponent(busquedaId)}/mision/pistas`,
  // HU30
  modificarPista: (busquedaId: string, pistaId: string) =>
    `/api/juegos/busquedas/${encodeURIComponent(busquedaId)}/mision/pistas/${encodeURIComponent(pistaId)}`,
  // HU32
  eliminarPista: (busquedaId: string, pistaId: string) =>
    `/api/juegos/busquedas/${encodeURIComponent(busquedaId)}/mision/pistas/${encodeURIComponent(pistaId)}`
}

function auth(token: string) {
  return { Authorization: `Bearer ${token}` }
}

async function leerError(respuesta: Response): Promise<string> {
  const cuerpo = (await respuesta.json().catch(() => null)) as { mensaje?: string } | null
  return cuerpo?.mensaje ?? `Error ${respuesta.status} al consultar el servidor.`
}

// Lee la respuesta de error como objeto completo para poder distinguir
// por código (no sólo por status). Se usa donde sabemos que el backend
// devuelve { codigo, mensaje } y queremos un trato específico para
// algún código (por ejemplo CONTENIDO_CON_SESIONES_VIGENTES).
async function leerErrorEstructurado(
  respuesta: Response
): Promise<{ codigo?: string; mensaje?: string } | null> {
  return (await respuesta.json().catch(() => null)) as
    | { codigo?: string; mensaje?: string }
    | null
}

const MENSAJE_CONTENIDO_CON_SESIONES_VIGENTES =
  'No se puede desactivar este contenido porque tiene sesiones programadas o en ejecución.'

// ---------------------------------------------------------------------------
// Tipos
// ---------------------------------------------------------------------------
export interface TriviaResumenDto {
  id: string
  nombre: string
  descripcion: string
  tiempoLimitePorPregunta: number
  estado: string
  totalPreguntas: number
  fechaCreacion: string
}

export interface OpcionDetalleDto {
  id: string
  texto: string
  esCorrecta: boolean
}

export interface PreguntaDetalleDto {
  id: string
  enunciado: string
  puntajeAsignado: number
  opciones: OpcionDetalleDto[]
}

export interface TriviaDetalleDto {
  id: string
  nombre: string
  descripcion: string
  tiempoLimitePorPregunta: number
  estado: string
  fechaCreacion: string
  preguntas: PreguntaDetalleDto[]
}

export interface DatosCrearTrivia {
  nombre: string
  descripcion: string
  tiempoLimitePorPregunta: number
}

export interface OpcionInput {
  texto: string
  esCorrecta: boolean
}

export interface DatosAgregarPregunta {
  enunciado: string
  puntajeAsignado: number
  opciones: OpcionInput[]
}

export interface DatosModificarPregunta {
  nuevoEnunciado: string
  nuevasOpciones: OpcionInput[]
}

export interface DatosModificarTrivia {
  nuevoNombre: string
  nuevaDescripcion: string
  nuevoTiempoLimitePorPregunta: number
}

export interface TriviaActivaResumenDto {
  id: string
  nombre: string
  descripcion: string
  tiempoLimitePorPregunta: number
  totalPreguntas: number
  fechaCreacion: string
}

export interface BusquedaTesoroResumenDto {
  id: string
  nombre: string
  descripcion: string
  estado: string
  tieneMision: boolean
  fechaCreacion: string
}

export interface DatosCrearBusquedaTesoro {
  nombre: string
  descripcion: string
}

export interface PistaDetalleDto {
  id: string
  contenido: string
}

export interface MisionDetalleDto {
  id: string
  titulo: string
  descripcion: string
  tipo: string
  pistaClave: string
  pistas: PistaDetalleDto[]
}

export interface BusquedaTesoroDetalleDto {
  id: string
  nombre: string
  descripcion: string
  estado: string
  fechaCreacion: string
  mision: MisionDetalleDto | null
}

export interface DatosAgregarPista {
  contenido: string
}

export interface DatosModificarPista {
  nuevoContenido: string
}

export type TipoMision = 0 | 1 | 2 // 0 = CodigoQR, 1 = PalabraClave, 2 = Codigo

export interface DatosAsignarMision {
  titulo: string
  descripcion: string
  tipo: TipoMision
  pistaClave: string
}

// ---------------------------------------------------------------------------
// HU15 — Crear trivia
// ---------------------------------------------------------------------------
export async function crearTrivia(
  datos: DatosCrearTrivia,
  token: string
): Promise<string> {
  const respuesta = await fetch(`${URL_API}${ENDPOINTS.crearTrivia}`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json', ...auth(token) },
    body: JSON.stringify(datos)
  })
  if (respuesta.status === 401) lanzar401('Debe iniciar sesión.')
  if (respuesta.status === 403) throw new Error('No tiene permisos para crear trivias.')
  if (!respuesta.ok) throw new Error(await leerError(respuesta))
  const cuerpo = (await respuesta.json()) as { id: string }
  return cuerpo.id
}

// ---------------------------------------------------------------------------
// HU15 — Listar trivias en borrador
// ---------------------------------------------------------------------------
export async function obtenerTriviasEnBorrador(
  token: string
): Promise<TriviaResumenDto[]> {
  const respuesta = await fetch(`${URL_API}${ENDPOINTS.listarBorrador}`, {
    headers: auth(token)
  })
  if (respuesta.status === 401) lanzar401('Debe iniciar sesión.')
  if (respuesta.status === 403) throw new Error('No tiene permisos.')
  if (!respuesta.ok) throw new Error(await leerError(respuesta))
  return (await respuesta.json()) as TriviaResumenDto[]
}

// ---------------------------------------------------------------------------
// HU15 — Detalle de una trivia con sus preguntas
// ---------------------------------------------------------------------------
export async function obtenerDetalleTrivia(
  triviaId: string,
  token: string
): Promise<TriviaDetalleDto> {
  const respuesta = await fetch(`${URL_API}${ENDPOINTS.detalleTrivia(triviaId)}`, {
    headers: auth(token)
  })
  if (respuesta.status === 401) lanzar401('Debe iniciar sesión.')
  if (respuesta.status === 403) throw new Error('No tiene permisos.')
  if (respuesta.status === 404) throw new Error('Trivia no encontrada.')
  if (!respuesta.ok) throw new Error(await leerError(respuesta))
  return (await respuesta.json()) as TriviaDetalleDto
}

// ---------------------------------------------------------------------------
// HU16 — Agregar pregunta
// ---------------------------------------------------------------------------
export async function agregarPregunta(
  triviaId: string,
  datos: DatosAgregarPregunta,
  token: string
): Promise<string> {
  const respuesta = await fetch(`${URL_API}${ENDPOINTS.agregarPregunta(triviaId)}`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json', ...auth(token) },
    body: JSON.stringify(datos)
  })
  if (respuesta.status === 401) lanzar401('Debe iniciar sesión.')
  if (respuesta.status === 403) throw new Error('No tiene permisos.')
  if (respuesta.status === 404) throw new Error('Trivia no encontrada.')
  if (!respuesta.ok) throw new Error(await leerError(respuesta))
  const cuerpo = (await respuesta.json()) as { id: string }
  return cuerpo.id
}

// ---------------------------------------------------------------------------
// HU17 — Modificar pregunta
// ---------------------------------------------------------------------------
export async function modificarPregunta(
  triviaId: string,
  preguntaId: string,
  datos: DatosModificarPregunta,
  token: string
): Promise<void> {
  const respuesta = await fetch(
    `${URL_API}${ENDPOINTS.modificarPregunta(triviaId, preguntaId)}`,
    {
      method: 'PUT',
      headers: { 'Content-Type': 'application/json', ...auth(token) },
      body: JSON.stringify(datos)
    }
  )
  if (respuesta.status === 401) lanzar401('Debe iniciar sesión.')
  if (respuesta.status === 403) throw new Error('No tiene permisos.')
  if (respuesta.status === 404) throw new Error('Pregunta no encontrada.')
  if (!respuesta.ok) throw new Error(await leerError(respuesta))
}

// ---------------------------------------------------------------------------
// HU23 — Asignar la misión única a una búsqueda del tesoro
// ---------------------------------------------------------------------------
export async function asignarMision(
  busquedaId: string,
  datos: DatosAsignarMision,
  token: string
): Promise<string> {
  const respuesta = await fetch(`${URL_API}${ENDPOINTS.asignarMision(busquedaId)}`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json', ...auth(token) },
    body: JSON.stringify(datos)
  })
  if (respuesta.status === 401) lanzar401('Debe iniciar sesión.')
  if (respuesta.status === 403) throw new Error('No tiene permisos.')
  if (respuesta.status === 404) throw new Error('Búsqueda del tesoro no encontrada.')
  if (!respuesta.ok) throw new Error(await leerError(respuesta))
  const cuerpo = (await respuesta.json()) as { id: string }
  return cuerpo.id
}

// ---------------------------------------------------------------------------
// HU22 — Obtener detalle de búsqueda del tesoro
// ---------------------------------------------------------------------------
export async function obtenerDetalleBusqueda(
  busquedaId: string,
  token: string
): Promise<BusquedaTesoroDetalleDto> {
  const respuesta = await fetch(`${URL_API}${ENDPOINTS.detalleBusqueda(busquedaId)}`, {
    headers: auth(token)
  })
  if (respuesta.status === 401) lanzar401('Debe iniciar sesión.')
  if (respuesta.status === 403) throw new Error('No tiene permisos.')
  if (respuesta.status === 404) throw new Error('Búsqueda del tesoro no encontrada.')
  if (!respuesta.ok) throw new Error(await leerError(respuesta))
  return (await respuesta.json()) as BusquedaTesoroDetalleDto
}


// ---------------------------------------------------------------------------
// HU21 — Crear búsqueda del tesoro
// ---------------------------------------------------------------------------
export async function crearBusquedaTesoro(
  datos: DatosCrearBusquedaTesoro,
  token: string
): Promise<string> {
  const respuesta = await fetch(`${URL_API}${ENDPOINTS.crearBusqueda}`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json', ...auth(token) },
    body: JSON.stringify(datos)
  })
  if (respuesta.status === 401) lanzar401('Debe iniciar sesión.')
  if (respuesta.status === 403) throw new Error('No tiene permisos para crear búsquedas del tesoro.')
  if (!respuesta.ok) throw new Error(await leerError(respuesta))
  const cuerpo = (await respuesta.json()) as { id: string }
  return cuerpo.id
}

// ---------------------------------------------------------------------------
// HU21 — Listar búsquedas en borrador
// ---------------------------------------------------------------------------
export async function obtenerBusquedasEnBorrador(
  token: string
): Promise<BusquedaTesoroResumenDto[]> {
  const respuesta = await fetch(`${URL_API}${ENDPOINTS.listarBusquedasBorrador}`, {
    headers: auth(token)
  })
  if (respuesta.status === 401) lanzar401('Debe iniciar sesión.')
  if (respuesta.status === 403) throw new Error('No tiene permisos.')
  if (!respuesta.ok) throw new Error(await leerError(respuesta))
  return (await respuesta.json()) as BusquedaTesoroResumenDto[]
}

// ---------------------------------------------------------------------------
// HU20 — Archivar trivia
// ---------------------------------------------------------------------------
//
// Si la trivia tiene sesiones vigentes asociadas, el backend responde
// 422 con código CONTENIDO_CON_SESIONES_VIGENTES. Mostramos un mensaje
// claro y específico en vez del genérico "ocurrió un error".
export async function desactivarTrivia(
  triviaId: string,
  token: string
): Promise<void> {
  const respuesta = await fetch(`${URL_API}${ENDPOINTS.desactivarTrivia(triviaId)}`, {
    method: 'DELETE',
    headers: auth(token)
  })
  if (respuesta.status === 401) lanzar401('Debe iniciar sesión.')
  if (respuesta.status === 403) throw new Error('No tiene permisos.')
  if (respuesta.status === 404) throw new Error('Trivia no encontrada.')
  if (respuesta.status === 422) {
    const cuerpo = await leerErrorEstructurado(respuesta)
    if (cuerpo?.codigo === 'CONTENIDO_CON_SESIONES_VIGENTES') {
      throw new Error(cuerpo.mensaje ?? MENSAJE_CONTENIDO_CON_SESIONES_VIGENTES)
    }
    throw new Error(cuerpo?.mensaje ?? 'No fue posible desactivar la trivia.')
  }
  if (!respuesta.ok) throw new Error(await leerError(respuesta))
}

// ---------------------------------------------------------------------------
// HU20 — Listar trivias activas
// ---------------------------------------------------------------------------
export async function obtenerTriviasActivas(
  token: string
): Promise<TriviaActivaResumenDto[]> {
  const respuesta = await fetch(`${URL_API}${ENDPOINTS.listarActivas}`, {
    headers: auth(token)
  })
  if (respuesta.status === 401) lanzar401('Debe iniciar sesión.')
  if (respuesta.status === 403) throw new Error('No tiene permisos.')
  if (!respuesta.ok) throw new Error(await leerError(respuesta))
  return (await respuesta.json()) as TriviaActivaResumenDto[]
}

// ---------------------------------------------------------------------------
// HU19 — Modificar datos de trivia
// ---------------------------------------------------------------------------
export async function modificarTrivia(
  triviaId: string,
  datos: DatosModificarTrivia,
  token: string
): Promise<void> {
  const respuesta = await fetch(`${URL_API}${ENDPOINTS.modificarTrivia(triviaId)}`, {
    method: 'PUT',
    headers: { 'Content-Type': 'application/json', ...auth(token) },
    body: JSON.stringify(datos)
  })
  if (respuesta.status === 401) lanzar401('Debe iniciar sesión.')
  if (respuesta.status === 403) throw new Error('No tiene permisos.')
  if (respuesta.status === 404) throw new Error('Trivia no encontrada.')
  if (!respuesta.ok) throw new Error(await leerError(respuesta))
}

// ---------------------------------------------------------------------------
// HU18 — Activar trivia
// ---------------------------------------------------------------------------
export async function activarTrivia(
  triviaId: string,
  token: string
): Promise<void> {
  const respuesta = await fetch(`${URL_API}${ENDPOINTS.activarTrivia(triviaId)}`, {
    method: 'PATCH',
    headers: auth(token)
  })
  if (respuesta.status === 401) lanzar401('Debe iniciar sesión.')
  if (respuesta.status === 403) throw new Error('No tiene permisos.')
  if (respuesta.status === 404) throw new Error('Trivia no encontrada.')
  if (!respuesta.ok) throw new Error(await leerError(respuesta))
}

// ---------------------------------------------------------------------------
// HU25 — Modificar la misión única de una búsqueda
// ---------------------------------------------------------------------------
export interface DatosModificarMision {
  nuevoTitulo: string
  nuevaDescripcion: string
  nuevoTipo: TipoMision
  nuevaPistaClave: string
}

export async function modificarMision(
  busquedaId: string,
  datos: DatosModificarMision,
  token: string
): Promise<void> {
  const respuesta = await fetch(
    `${URL_API}${ENDPOINTS.modificarMision(busquedaId)}`,
    {
      method: 'PUT',
      headers: { 'Content-Type': 'application/json', ...auth(token) },
      body: JSON.stringify(datos)
    }
  )
  if (respuesta.status === 401) lanzar401('Debe iniciar sesión.')
  if (respuesta.status === 403) throw new Error('No tiene permisos.')
  if (respuesta.status === 404) throw new Error('Misión no encontrada.')
  if (!respuesta.ok) throw new Error(await leerError(respuesta))
}

// ---------------------------------------------------------------------------
// HU25 — Eliminar la misión de una búsqueda
// ---------------------------------------------------------------------------
export async function eliminarMision(
  busquedaId: string,
  token: string
): Promise<void> {
  const respuesta = await fetch(
    `${URL_API}${ENDPOINTS.eliminarMision(busquedaId)}`,
    { method: 'DELETE', headers: auth(token) }
  )
  if (respuesta.status === 401) lanzar401('Debe iniciar sesión.')
  if (respuesta.status === 403) throw new Error('No tiene permisos.')
  if (respuesta.status === 404) throw new Error('Misión no encontrada.')
  if (!respuesta.ok) throw new Error(await leerError(respuesta))
}

// ---------------------------------------------------------------------------
// HU26 — Activar búsqueda del tesoro
// ---------------------------------------------------------------------------
export async function activarBusqueda(
  busquedaId: string,
  token: string
): Promise<void> {
  const respuesta = await fetch(
    `${URL_API}${ENDPOINTS.activarBusqueda(busquedaId)}`,
    { method: 'PATCH', headers: auth(token) }
  )
  if (respuesta.status === 401) lanzar401('Debe iniciar sesión.')
  if (respuesta.status === 403) throw new Error('No tiene permisos.')
  if (respuesta.status === 404) throw new Error('Búsqueda del tesoro no encontrada.')
  if (!respuesta.ok) throw new Error(await leerError(respuesta))
}

// ---------------------------------------------------------------------------
// HU26 — Listar búsquedas activas
// ---------------------------------------------------------------------------
export async function obtenerBusquedasActivas(
  token: string
): Promise<BusquedaTesoroResumenDto[]> {
  const respuesta = await fetch(`${URL_API}${ENDPOINTS.listarBusquedasActivas}`, {
    headers: auth(token)
  })
  if (respuesta.status === 401) lanzar401('Debe iniciar sesión.')
  if (respuesta.status === 403) throw new Error('No tiene permisos.')
  if (!respuesta.ok) throw new Error(await leerError(respuesta))
  return (await respuesta.json()) as BusquedaTesoroResumenDto[]
}

// ---------------------------------------------------------------------------
// HU26 — Archivar búsqueda del tesoro
// ---------------------------------------------------------------------------
//
// Mismo tratamiento que desactivarTrivia: si hay sesiones vigentes
// asociadas, el backend responde 422 con CONTENIDO_CON_SESIONES_VIGENTES
// y mostramos el mensaje específico.
export async function desactivarBusqueda(
  busquedaId: string,
  token: string
): Promise<void> {
  const respuesta = await fetch(
    `${URL_API}${ENDPOINTS.desactivarBusqueda(busquedaId)}`,
    { method: 'DELETE', headers: auth(token) }
  )
  if (respuesta.status === 401) lanzar401('Debe iniciar sesión.')
  if (respuesta.status === 403) throw new Error('No tiene permisos.')
  if (respuesta.status === 404) throw new Error('Búsqueda del tesoro no encontrada.')
  if (respuesta.status === 422) {
    const cuerpo = await leerErrorEstructurado(respuesta)
    if (cuerpo?.codigo === 'CONTENIDO_CON_SESIONES_VIGENTES') {
      throw new Error(cuerpo.mensaje ?? MENSAJE_CONTENIDO_CON_SESIONES_VIGENTES)
    }
    throw new Error(cuerpo?.mensaje ?? 'No fue posible archivar la búsqueda del tesoro.')
  }
  if (!respuesta.ok) throw new Error(await leerError(respuesta))
}

// ---------------------------------------------------------------------------
// Eliminar búsqueda del tesoro (solo si está Inactiva)
// ---------------------------------------------------------------------------
export async function eliminarBusqueda(
  busquedaId: string,
  token: string
): Promise<void> {
  const respuesta = await fetch(
    `${URL_API}${ENDPOINTS.eliminarBusqueda(busquedaId)}`,
    { method: 'DELETE', headers: auth(token) }
  )
  if (respuesta.status === 401) lanzar401('Debe iniciar sesión.')
  if (respuesta.status === 403) throw new Error('No tiene permisos.')
  if (respuesta.status === 404) throw new Error('Búsqueda del tesoro no encontrada.')
  if (respuesta.status === 422) throw new Error(await leerError(respuesta))
  if (!respuesta.ok) throw new Error(await leerError(respuesta))
}

// ---------------------------------------------------------------------------
// HU28 — Agregar pista a la misión
// ---------------------------------------------------------------------------
export async function agregarPista(
  busquedaId: string,
  datos: DatosAgregarPista,
  token: string
): Promise<string> {
  const respuesta = await fetch(`${URL_API}${ENDPOINTS.agregarPista(busquedaId)}`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json', ...auth(token) },
    body: JSON.stringify(datos)
  })
  if (respuesta.status === 401) lanzar401('Debe iniciar sesión.')
  if (respuesta.status === 403) throw new Error('No tiene permisos.')
  if (respuesta.status === 404) throw new Error('Misión no encontrada.')
  if (!respuesta.ok) throw new Error(await leerError(respuesta))
  const cuerpo = (await respuesta.json()) as { id: string }
  return cuerpo.id
}

// ---------------------------------------------------------------------------
// HU30 — Modificar pista
// ---------------------------------------------------------------------------
export async function modificarPista(
  busquedaId: string,
  pistaId: string,
  datos: DatosModificarPista,
  token: string
): Promise<void> {
  const respuesta = await fetch(
    `${URL_API}${ENDPOINTS.modificarPista(busquedaId, pistaId)}`,
    {
      method: 'PUT',
      headers: { 'Content-Type': 'application/json', ...auth(token) },
      body: JSON.stringify(datos)
    }
  )
  if (respuesta.status === 401) lanzar401('Debe iniciar sesión.')
  if (respuesta.status === 403) throw new Error('No tiene permisos.')
  if (respuesta.status === 404) throw new Error('Pista no encontrada.')
  if (!respuesta.ok) throw new Error(await leerError(respuesta))
}

// ---------------------------------------------------------------------------
// HU32 — Eliminar pista
// ---------------------------------------------------------------------------
export async function eliminarPista(
  busquedaId: string,
  pistaId: string,
  token: string
): Promise<void> {
  const respuesta = await fetch(
    `${URL_API}${ENDPOINTS.eliminarPista(busquedaId, pistaId)}`,
    { method: 'DELETE', headers: auth(token) }
  )
  if (respuesta.status === 401) lanzar401('Debe iniciar sesión.')
  if (respuesta.status === 403) throw new Error('No tiene permisos.')
  if (respuesta.status === 404) throw new Error('Pista no encontrada.')
  if (!respuesta.ok) throw new Error(await leerError(respuesta))
}

// ---------------------------------------------------------------------------
// HU17 — Eliminar pregunta
// ---------------------------------------------------------------------------
export async function eliminarPregunta(
  triviaId: string,
  preguntaId: string,
  token: string
): Promise<void> {
  const respuesta = await fetch(
    `${URL_API}${ENDPOINTS.eliminarPregunta(triviaId, preguntaId)}`,
    { method: 'DELETE', headers: auth(token) }
  )
  if (respuesta.status === 401) lanzar401('Debe iniciar sesión.')
  if (respuesta.status === 403) throw new Error('No tiene permisos.')
  if (respuesta.status === 404) throw new Error('Pregunta no encontrada.')
  if (!respuesta.ok) throw new Error(await leerError(respuesta))
}
