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
  listarActivas: '/api/juegos/trivias/activas',
  // HU21
  crearBusqueda:       '/api/juegos/busquedas',
  listarBusquedasBorrador: '/api/juegos/busquedas/borrador',
  // HU22
  detalleBusqueda: (busquedaId: string) =>
    `/api/juegos/busquedas/${encodeURIComponent(busquedaId)}`,
  agregarEtapa: (busquedaId: string) =>
    `/api/juegos/busquedas/${encodeURIComponent(busquedaId)}/etapas`,
  // HU23
  agregarMision: (busquedaId: string, etapaId: string) =>
    `/api/juegos/busquedas/${encodeURIComponent(busquedaId)}/etapas/${encodeURIComponent(etapaId)}/misiones`,
  // HU24
  modificarEtapa: (busquedaId: string, etapaId: string) =>
    `/api/juegos/busquedas/${encodeURIComponent(busquedaId)}/etapas/${encodeURIComponent(etapaId)}`,
  eliminarEtapa: (busquedaId: string, etapaId: string) =>
    `/api/juegos/busquedas/${encodeURIComponent(busquedaId)}/etapas/${encodeURIComponent(etapaId)}`,
  // HU25
  modificarMision: (busquedaId: string, etapaId: string, misionId: string) =>
    `/api/juegos/busquedas/${encodeURIComponent(busquedaId)}/etapas/${encodeURIComponent(etapaId)}/misiones/${encodeURIComponent(misionId)}`,
  eliminarMision: (busquedaId: string, etapaId: string, misionId: string) =>
    `/api/juegos/busquedas/${encodeURIComponent(busquedaId)}/etapas/${encodeURIComponent(etapaId)}/misiones/${encodeURIComponent(misionId)}`,
  // HU26
  activarBusqueda: (busquedaId: string) =>
    `/api/juegos/busquedas/${encodeURIComponent(busquedaId)}/activar`,
  archivarBusqueda: (busquedaId: string) =>
    `/api/juegos/busquedas/${encodeURIComponent(busquedaId)}`,
  listarBusquedasActivas: '/api/juegos/busquedas/activas'
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

export interface BusquedaTesoroResumenDto {
  id: string
  nombre: string
  descripcion: string
  estado: string
  totalEtapas: number
  fechaCreacion: string
}

export interface DatosCrearBusquedaTesoro {
  nombre: string
  descripcion: string
}

export interface MisionDetalleDto {
  id: string
  titulo: string
  descripcion: string
  tipo: string
  pistaClave: string
}

export interface EtapaDetalleDto {
  id: string
  titulo: string
  descripcion: string
  orden: number
  misiones: MisionDetalleDto[]
}

export interface BusquedaTesoroDetalleDto {
  id: string
  nombre: string
  descripcion: string
  estado: string
  fechaCreacion: string
  etapas: EtapaDetalleDto[]
}

export interface DatosAgregarEtapa {
  titulo: string
  descripcion: string
}

export type TipoMision = 0 | 1 | 2

export interface DatosAgregarMision {
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
// HU23 — Agregar misión a una etapa
// ---------------------------------------------------------------------------
export async function agregarMision(
  busquedaId: string,
  etapaId: string,
  datos: DatosAgregarMision,
  token: string
): Promise<string> {
  const respuesta = await fetch(`${URL_API}${ENDPOINTS.agregarMision(busquedaId, etapaId)}`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json', ...auth(token) },
    body: JSON.stringify(datos)
  })
  if (respuesta.status === 401) throw new Error('Debe iniciar sesión.')
  if (respuesta.status === 403) throw new Error('No tiene permisos.')
  if (respuesta.status === 404) throw new Error('Etapa no encontrada.')
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
  if (respuesta.status === 401) throw new Error('Debe iniciar sesión.')
  if (respuesta.status === 403) throw new Error('No tiene permisos.')
  if (respuesta.status === 404) throw new Error('Búsqueda del tesoro no encontrada.')
  if (!respuesta.ok) throw new Error(await leerError(respuesta))
  return (await respuesta.json()) as BusquedaTesoroDetalleDto
}

// ---------------------------------------------------------------------------
// HU22 — Agregar etapa a búsqueda del tesoro
// ---------------------------------------------------------------------------
export async function agregarEtapa(
  busquedaId: string,
  datos: DatosAgregarEtapa,
  token: string
): Promise<string> {
  const respuesta = await fetch(`${URL_API}${ENDPOINTS.agregarEtapa(busquedaId)}`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json', ...auth(token) },
    body: JSON.stringify(datos)
  })
  if (respuesta.status === 401) throw new Error('Debe iniciar sesión.')
  if (respuesta.status === 403) throw new Error('No tiene permisos.')
  if (respuesta.status === 404) throw new Error('Búsqueda del tesoro no encontrada.')
  if (!respuesta.ok) throw new Error(await leerError(respuesta))
  const cuerpo = (await respuesta.json()) as { id: string }
  return cuerpo.id
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
  if (respuesta.status === 401) throw new Error('Debe iniciar sesión.')
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
  if (respuesta.status === 401) throw new Error('Debe iniciar sesión.')
  if (respuesta.status === 403) throw new Error('No tiene permisos.')
  if (!respuesta.ok) throw new Error(await leerError(respuesta))
  return (await respuesta.json()) as BusquedaTesoroResumenDto[]
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
// HU24 — Modificar etapa
// ---------------------------------------------------------------------------
export interface DatosModificarEtapa {
  nuevoTitulo: string
  nuevaDescripcion: string
}

export async function modificarEtapa(
  busquedaId: string,
  etapaId: string,
  datos: DatosModificarEtapa,
  token: string
): Promise<void> {
  const respuesta = await fetch(
    `${URL_API}${ENDPOINTS.modificarEtapa(busquedaId, etapaId)}`,
    {
      method: 'PUT',
      headers: { 'Content-Type': 'application/json', ...auth(token) },
      body: JSON.stringify(datos)
    }
  )
  if (respuesta.status === 401) throw new Error('Debe iniciar sesión.')
  if (respuesta.status === 403) throw new Error('No tiene permisos.')
  if (respuesta.status === 404) throw new Error('Etapa no encontrada.')
  if (!respuesta.ok) throw new Error(await leerError(respuesta))
}

// ---------------------------------------------------------------------------
// HU24 — Eliminar etapa
// ---------------------------------------------------------------------------
export async function eliminarEtapa(
  busquedaId: string,
  etapaId: string,
  token: string
): Promise<void> {
  const respuesta = await fetch(
    `${URL_API}${ENDPOINTS.eliminarEtapa(busquedaId, etapaId)}`,
    { method: 'DELETE', headers: auth(token) }
  )
  if (respuesta.status === 401) throw new Error('Debe iniciar sesión.')
  if (respuesta.status === 403) throw new Error('No tiene permisos.')
  if (respuesta.status === 404) throw new Error('Etapa no encontrada.')
  if (!respuesta.ok) throw new Error(await leerError(respuesta))
}

// ---------------------------------------------------------------------------
// HU25 — Modificar misión
// ---------------------------------------------------------------------------
export interface DatosModificarMision {
  nuevoTitulo: string
  nuevaDescripcion: string
  nuevoTipo: TipoMision
  nuevaPistaClave: string
}

export async function modificarMision(
  busquedaId: string,
  etapaId: string,
  misionId: string,
  datos: DatosModificarMision,
  token: string
): Promise<void> {
  const respuesta = await fetch(
    `${URL_API}${ENDPOINTS.modificarMision(busquedaId, etapaId, misionId)}`,
    {
      method: 'PUT',
      headers: { 'Content-Type': 'application/json', ...auth(token) },
      body: JSON.stringify(datos)
    }
  )
  if (respuesta.status === 401) throw new Error('Debe iniciar sesión.')
  if (respuesta.status === 403) throw new Error('No tiene permisos.')
  if (respuesta.status === 404) throw new Error('Misión no encontrada.')
  if (!respuesta.ok) throw new Error(await leerError(respuesta))
}

// ---------------------------------------------------------------------------
// HU25 — Eliminar misión
// ---------------------------------------------------------------------------
export async function eliminarMision(
  busquedaId: string,
  etapaId: string,
  misionId: string,
  token: string
): Promise<void> {
  const respuesta = await fetch(
    `${URL_API}${ENDPOINTS.eliminarMision(busquedaId, etapaId, misionId)}`,
    { method: 'DELETE', headers: auth(token) }
  )
  if (respuesta.status === 401) throw new Error('Debe iniciar sesión.')
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
  if (respuesta.status === 401) throw new Error('Debe iniciar sesión.')
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
  if (respuesta.status === 401) throw new Error('Debe iniciar sesión.')
  if (respuesta.status === 403) throw new Error('No tiene permisos.')
  if (!respuesta.ok) throw new Error(await leerError(respuesta))
  return (await respuesta.json()) as BusquedaTesoroResumenDto[]
}

// ---------------------------------------------------------------------------
// HU26 — Archivar búsqueda del tesoro
// ---------------------------------------------------------------------------
export async function archivarBusqueda(
  busquedaId: string,
  token: string
): Promise<void> {
  const respuesta = await fetch(
    `${URL_API}${ENDPOINTS.archivarBusqueda(busquedaId)}`,
    { method: 'DELETE', headers: auth(token) }
  )
  if (respuesta.status === 401) throw new Error('Debe iniciar sesión.')
  if (respuesta.status === 403) throw new Error('No tiene permisos.')
  if (respuesta.status === 404) throw new Error('Búsqueda del tesoro no encontrada.')
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
