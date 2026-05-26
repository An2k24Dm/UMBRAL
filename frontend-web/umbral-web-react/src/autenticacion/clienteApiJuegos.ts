const URL_API = import.meta.env.VITE_API_URL ?? 'http://localhost:5000'

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
  archivarTrivia: (triviaId: string) =>
    `/api/juegos/trivias/${encodeURIComponent(triviaId)}`,
  listarActivas: '/api/juegos/trivias/activas'
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
  if (respuesta.status === 401) throw new Error('Debe iniciar sesión.')
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
  if (respuesta.status === 401) throw new Error('Debe iniciar sesión.')
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
  if (respuesta.status === 401) throw new Error('Debe iniciar sesión.')
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
  if (respuesta.status === 401) throw new Error('Debe iniciar sesión.')
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
  if (respuesta.status === 401) throw new Error('Debe iniciar sesión.')
  if (respuesta.status === 403) throw new Error('No tiene permisos.')
  if (respuesta.status === 404) throw new Error('Pregunta no encontrada.')
  if (!respuesta.ok) throw new Error(await leerError(respuesta))
}

// ---------------------------------------------------------------------------
// HU20 — Archivar trivia
// ---------------------------------------------------------------------------
export async function archivarTrivia(
  triviaId: string,
  token: string
): Promise<void> {
  const respuesta = await fetch(`${URL_API}${ENDPOINTS.archivarTrivia(triviaId)}`, {
    method: 'DELETE',
    headers: auth(token)
  })
  if (respuesta.status === 401) throw new Error('Debe iniciar sesión.')
  if (respuesta.status === 403) throw new Error('No tiene permisos.')
  if (respuesta.status === 404) throw new Error('Trivia no encontrada.')
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
  if (respuesta.status === 401) throw new Error('Debe iniciar sesión.')
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
  if (respuesta.status === 401) throw new Error('Debe iniciar sesión.')
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
  if (respuesta.status === 401) throw new Error('Debe iniciar sesión.')
  if (respuesta.status === 403) throw new Error('No tiene permisos.')
  if (respuesta.status === 404) throw new Error('Trivia no encontrada.')
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
  if (respuesta.status === 401) throw new Error('Debe iniciar sesión.')
  if (respuesta.status === 403) throw new Error('No tiene permisos.')
  if (respuesta.status === 404) throw new Error('Pregunta no encontrada.')
  if (!respuesta.ok) throw new Error(await leerError(respuesta))
}
