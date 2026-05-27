import type {
  FiltrosParticipantes,
  FiltrosUsuariosInternos,
  RespuestaError,
  ResultadoInicioSesion,
  ResultadoPaginado,
  UsuarioDetalle,
  UsuarioListadoInterno,
  UsuarioListadoParticipante
} from './tipos'

const URL_API = import.meta.env.VITE_API_URL ?? 'http://localhost:5000'

// ---------------------------------------------------------------------------
// Endpoints — punto único para mantener las rutas alineadas con el backend.
// Cuando el equipo de backend confirme las rutas reales, basta con ajustarlas
// aquí. No mockear: si el endpoint todavía no existe, la pantalla mostrará el
// estado de error correspondiente.
// ---------------------------------------------------------------------------
const ENDPOINTS = {
  // HU01 — ya implementado.
  iniciarSesion: '/api/autenticacion/login-web',
  // HU05, HU06 — perfil del usuario autenticado.
  perfilActual: '/api/autenticacion/perfil-actual',
  // HU02 — registro de Operador/Administrador.
  registrarUsuario: '/api/usuarios',
  // HU07 — listado de Participantes.
  listarParticipantes: '/api/usuarios/participantes',
  // HU07 — detalle/perfil completo de un Participante seleccionado.
  detalleParticipante: (id: string) =>
    `/api/usuarios/participantes/${encodeURIComponent(id)}`,
  // HU08 — listado de Operadores y Administradores.
  listarUsuariosInternos: '/api/usuarios/internos',
  // HU08 — detalle de un usuario interno (Operador / Administrador).
  detalleUsuarioInterno: (id: string) =>
    `/api/usuarios/internos/${encodeURIComponent(id)}`,
  // HU09 — modificación parcial de un Operador (sólo Administrador).
  modificarOperador: (id: string) =>
    `/api/usuarios/operadores/${encodeURIComponent(id)}`,
  // HU13 — eliminación permanente de un Operador (sólo Administrador).
  eliminarOperador: (id: string) =>
    `/api/usuarios/operadores/${encodeURIComponent(id)}`
}

// ---------------------------------------------------------------------------
// Utilidades internas
// ---------------------------------------------------------------------------
function autorizacion(token: string) {
  return { Authorization: `Bearer ${token}` }
}

async function leerError(respuesta: Response): Promise<string> {
  const cuerpo = (await respuesta.json().catch(() => null)) as
    | { mensaje?: string }
    | null
  return cuerpo?.mensaje ?? `Error ${respuesta.status} al consultar el servidor.`
}

async function pedirJson<T>(url: string, token: string): Promise<T> {
  const respuesta = await fetch(url, { headers: autorizacion(token) })
  if (respuesta.status === 401) throw new Error('Debe iniciar sesión.')
  if (respuesta.status === 403) throw new Error('No tiene permisos para consultar este recurso.')
  if (respuesta.status === 404) throw new Error(await leerError(respuesta))
  if (respuesta.status === 404) throw new Error('Usuario no encontrado.')
  if (!respuesta.ok) throw new Error(await leerError(respuesta))
  return (await respuesta.json()) as T
}

// ---------------------------------------------------------------------------
// HU01 — autenticación
// ---------------------------------------------------------------------------
export async function iniciarSesion(
  nombreUsuario: string,
  contrasena: string
): Promise<ResultadoInicioSesion> {
  const respuesta = await fetch(`${URL_API}${ENDPOINTS.iniciarSesion}`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ nombreUsuario, contrasena })
  })

  if (!respuesta.ok) {
    const error = (await respuesta.json().catch(() => null)) as RespuestaError | null
    throw new Error(error?.mensaje ?? 'No fue posible iniciar sesión.')
  }

  return (await respuesta.json()) as ResultadoInicioSesion
}

// ---------------------------------------------------------------------------
// HU06 — perfil del usuario autenticado
// ---------------------------------------------------------------------------
export async function obtenerPerfilActual(token: string): Promise<UsuarioDetalle> {
  return pedirJson<UsuarioDetalle>(`${URL_API}${ENDPOINTS.perfilActual}`, token)
}

// ---------------------------------------------------------------------------
// HU02 — registrar Operador/Administrador
// ---------------------------------------------------------------------------
export type TipoUsuarioRegistro = 'Administrador' | 'Operador'

// Los códigos OP-### / AD-### los genera el backend (HU02). El frontend nunca
// los envía: solo los muestra como campo no editable y los recibe en la respuesta.
export interface DatosNuevoUsuario {
  tipoUsuario: TipoUsuarioRegistro
  nombreUsuario: string
  correo: string
  contrasena: string
  nombre: string
  apellido: string
  sexo: string
  fechaNacimiento: string
  direccion: string
  telefono: string
}

export interface RespuestaCrearUsuario {
  id: string
  nombreUsuario: string
  correo: string
  rol: string
  estado: string
  codigo: string | null
  mensaje: string
}

export interface ErrorCampo {
  campo: string
  mensaje: string
}

// Excepción que transporta los errores por campo que devuelve el backend (HU02).
export class ErrorValidacionRegistro extends Error {
  errores: ErrorCampo[]
  constructor(mensaje: string, errores: ErrorCampo[]) {
    super(mensaje)
    this.errores = errores
  }
}

export async function registrarUsuario(
  datos: DatosNuevoUsuario,
  token: string
): Promise<RespuestaCrearUsuario> {
  const cuerpo = {
    tipoUsuario: datos.tipoUsuario,
    nombreUsuario: datos.nombreUsuario,
    correo: datos.correo,
    contrasena: datos.contrasena,
    nombre: datos.nombre,
    apellido: datos.apellido,
    sexo: datos.sexo,
    fechaNacimiento: datos.fechaNacimiento,
    datosContacto: { direccion: datos.direccion, telefono: datos.telefono }
  }

  const respuesta = await fetch(`${URL_API}${ENDPOINTS.registrarUsuario}`, {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
      ...autorizacion(token)
    },
    body: JSON.stringify(cuerpo)
  })

  if (respuesta.status === 401) throw new Error('Debe iniciar sesión como administrador.')
  if (respuesta.status === 403) throw new Error('No tiene permisos para registrar usuarios.')

  if (!respuesta.ok) {
    const cuerpoError = await respuesta.json().catch(() => null) as
      | { mensaje?: string; errores?: ErrorCampo[] }
      | null
    if (cuerpoError?.errores && cuerpoError.errores.length > 0) {
      throw new ErrorValidacionRegistro(
        cuerpoError.mensaje ?? 'No fue posible registrar el usuario. Revise los campos marcados.',
        cuerpoError.errores
      )
    }
    throw new Error(cuerpoError?.mensaje ?? 'No fue posible registrar el usuario.')
  }

  return (await respuesta.json()) as RespuestaCrearUsuario
}

// ---------------------------------------------------------------------------
// HU07 — listado de Participantes
// ---------------------------------------------------------------------------
function construirQuery(parametros: Record<string, string | number | undefined | null>): string {
  const partes: string[] = []
  for (const [clave, valor] of Object.entries(parametros)) {
    if (valor === undefined || valor === null || valor === '') continue
    partes.push(`${encodeURIComponent(clave)}=${encodeURIComponent(String(valor))}`)
  }
  return partes.length > 0 ? `?${partes.join('&')}` : ''
}

export async function obtenerParticipantes(
  filtros: FiltrosParticipantes,
  token: string
): Promise<ResultadoPaginado<UsuarioListadoParticipante>> {
  const query = construirQuery({
    pagina: filtros.pagina,
    tamanioPagina: filtros.tamanioPagina,
    ordenEstado: filtros.ordenEstado ?? undefined
  })
  return pedirJson<ResultadoPaginado<UsuarioListadoParticipante>>(
    `${URL_API}${ENDPOINTS.listarParticipantes}${query}`,
    token
  )
}

// HU07 — detalle/perfil de un Participante seleccionado. Maneja 401/403/404
// con mensajes específicos para la pantalla de detalle.
export async function obtenerDetalleParticipante(
  id: string,
  token: string
): Promise<UsuarioDetalle> {
  const respuesta = await fetch(
    `${URL_API}${ENDPOINTS.detalleParticipante(id)}`,
    { headers: autorizacion(token) }
  )
  if (respuesta.status === 401) throw new Error('Debe iniciar sesión.')
  if (respuesta.status === 403) throw new Error('No tiene permisos para consultar este participante.')
  if (respuesta.status === 404) throw new Error('Participante no encontrado.')
  if (!respuesta.ok) throw new Error(await leerError(respuesta))
  return (await respuesta.json()) as UsuarioDetalle
}

// ---------------------------------------------------------------------------
// HU08 — listado de Operadores y Administradores
// ---------------------------------------------------------------------------
export async function obtenerUsuariosInternos(
  filtros: FiltrosUsuariosInternos,
  token: string
): Promise<ResultadoPaginado<UsuarioListadoInterno>> {
  const query = construirQuery({
    pagina: filtros.pagina,
    tamanioPagina: filtros.tamanioPagina,
    rol: filtros.rol && filtros.rol !== 'Todos' ? filtros.rol : undefined,
    ordenEstado: filtros.ordenEstado ?? undefined
  })
  return pedirJson<ResultadoPaginado<UsuarioListadoInterno>>(
    `${URL_API}${ENDPOINTS.listarUsuariosInternos}${query}`,
    token
  )
}

// ---------------------------------------------------------------------------
// HU08 — detalle de un usuario interno (Operador o Administrador). El backend
// devuelve 404 si el id corresponde a un Participante: la pantalla muestra el
// mensaje de "Usuario no encontrado" que produce pedirJson.
// ---------------------------------------------------------------------------
export async function obtenerDetalleUsuarioInterno(
  id: string,
  token: string
): Promise<UsuarioDetalle> {
  return pedirJson<UsuarioDetalle>(
    `${URL_API}${ENDPOINTS.detalleUsuarioInterno(id)}`,
    token
  )
}

// ---------------------------------------------------------------------------
// HU09 — modificación parcial del Operador
// ---------------------------------------------------------------------------
//
// El payload sólo lleva los campos que efectivamente cambiaron en el formulario
// — los demás se omiten para que el backend no los sobrescriba. Estado,
// FechaRegistro y Rol no son editables y NUNCA se envían.
export interface ModificarOperadorPayload {
  nombreUsuario?: string
  correo?: string
  nombre?: string
  apellido?: string
  sexo?: string
  fechaNacimiento?: string
  datosContacto?: {
    direccion?: string
    telefono?: string
  }
  // HU09 — cambio administrativo de contraseña. Ambos campos viajan
  // únicamente si el Administrador escribió valores en el formulario.
  nuevaContrasena?: string
  confirmacionContrasena?: string
}

export interface ModificarOperadorRespuesta {
  huboCambios: boolean
  camposActualizados: string[]
  mensaje: string
  operador: UsuarioDetalle
}

// ---------------------------------------------------------------------------
// HU13 — eliminación permanente de un Operador
// ---------------------------------------------------------------------------
//
// Sólo el Administrador puede invocar este endpoint. El backend:
//  * 401 si no hay token,
//  * 403 si el token es de Operador o Participante,
//  * 404 si el id no corresponde a un Operador (no existe, o es
//    Administrador / Participante; no se permite eliminar esos roles
//    por esta vía).
// La respuesta no contiene datos sensibles: sólo { idOperador, eliminado,
// mensaje }.
export interface EliminarOperadorRespuesta {
  idOperador: string
  eliminado: boolean
  mensaje: string
}

export async function eliminarOperador(
  id: string,
  token: string
): Promise<EliminarOperadorRespuesta> {
  const respuesta = await fetch(`${URL_API}${ENDPOINTS.eliminarOperador(id)}`, {
    method: 'DELETE',
    headers: autorizacion(token)
  })

  if (respuesta.status === 401) throw new Error('Debe iniciar sesión como administrador.')
  if (respuesta.status === 403) throw new Error('No tiene permisos para eliminar operadores.')
  if (respuesta.status === 404) throw new Error('El operador solicitado no existe.')

  if (!respuesta.ok) {
    const cuerpo = (await respuesta.json().catch(() => null)) as
      | { mensaje?: string }
      | null
    throw new Error(cuerpo?.mensaje ?? 'No fue posible eliminar el operador.')
  }

  return (await respuesta.json()) as EliminarOperadorRespuesta
}

export async function modificarOperador(
  id: string,
  cambios: ModificarOperadorPayload,
  token: string
): Promise<ModificarOperadorRespuesta> {
  const respuesta = await fetch(`${URL_API}${ENDPOINTS.modificarOperador(id)}`, {
    method: 'PATCH',
    headers: {
      'Content-Type': 'application/json',
      ...autorizacion(token)
    },
    body: JSON.stringify(cambios)
  })

  if (respuesta.status === 401) throw new Error('Debe iniciar sesión como administrador.')
  if (respuesta.status === 403) throw new Error('No tiene permisos para modificar operadores.')
  if (respuesta.status === 404) throw new Error('El operador solicitado no existe.')

  if (!respuesta.ok) {
    const cuerpoError = (await respuesta.json().catch(() => null)) as
      | { mensaje?: string; errores?: ErrorCampo[] }
      | null
    if (cuerpoError?.errores && cuerpoError.errores.length > 0) {
      throw new ErrorValidacionRegistro(
        cuerpoError.mensaje ?? 'No fue posible modificar el operador. Revise los campos marcados.',
        cuerpoError.errores
      )
    }
    throw new Error(cuerpoError?.mensaje ?? 'No fue posible modificar el operador.')
  }

  return (await respuesta.json()) as ModificarOperadorRespuesta
}
