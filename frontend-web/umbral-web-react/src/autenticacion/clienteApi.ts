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
import { manejar401 } from './eventosSesion'

function lanzar401(arg1?: string | null, arg2?: string): never {
  const tieneAmbos = typeof arg2 === 'string'
  const token = tieneAmbos ? (arg1 ?? null) : null
  const mensaje = tieneAmbos ? arg2 : (arg1 ?? 'Debe iniciar sesión.')
  manejar401(token, mensaje)
}

const URL_API = import.meta.env.VITE_API_URL ?? 'http://localhost:5000'

const ENDPOINTS = {
  iniciarSesion: '/api/autenticacion/login-web',
  perfilActual: '/api/autenticacion/perfil-actual',
  cambiarContrasenaObligatoria: '/api/autenticacion/cambiar-contrasena-obligatoria',
  registrarUsuario: '/api/usuarios',
  listarParticipantes: '/api/usuarios/participantes',
  detalleParticipante: (id: string) =>
    `/api/usuarios/participantes/${encodeURIComponent(id)}`,
  listarUsuariosInternos: '/api/usuarios/internos',
  detalleUsuarioInterno: (id: string) =>
    `/api/usuarios/internos/${encodeURIComponent(id)}`,
  modificarOperador: (id: string) =>
    `/api/usuarios/operadores/${encodeURIComponent(id)}`,
  resetearContrasenaUsuarioInterno: (id: string) =>
    `/api/usuarios/internos/${encodeURIComponent(id)}/resetear-contrasena`,
  eliminarOperador: (id: string) =>
    `/api/usuarios/operadores/${encodeURIComponent(id)}`,
  desactivarOperador: (id: string) =>
    `/api/usuarios/operadores/${encodeURIComponent(id)}/desactivar`,
  desactivarParticipante: (id: string) =>
    `/api/usuarios/participantes/${encodeURIComponent(id)}/desactivar`,
  activarOperador: (id: string) =>
    `/api/usuarios/operadores/${encodeURIComponent(id)}/activar`,
  activarParticipante: (id: string) =>
    `/api/usuarios/participantes/${encodeURIComponent(id)}/activar`
}

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
  if (respuesta.status === 401) lanzar401(token, 'Debe iniciar sesión.')
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

export interface RespuestaCambiarContrasenaObligatoria {
  mensaje: string
  rutaRedireccion: string
}

export async function cambiarContrasenaObligatoria(
  nuevaContrasena: string,
  confirmacionContrasena: string,
  token: string
): Promise<RespuestaCambiarContrasenaObligatoria> {
  const respuesta = await fetch(
    `${URL_API}${ENDPOINTS.cambiarContrasenaObligatoria}`,
    {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        ...autorizacion(token)
      },
      body: JSON.stringify({ nuevaContrasena, confirmacionContrasena })
    }
  )

  if (respuesta.status === 401) lanzar401(token, 'Debe iniciar sesión.')
  if (respuesta.status === 403) {
    const cuerpo = (await respuesta.json().catch(() => null)) as
      | { mensaje?: string }
      | null
    throw new Error(cuerpo?.mensaje ?? 'No tiene permisos para esta acción.')
  }

  if (!respuesta.ok) {
    const cuerpoError = (await respuesta.json().catch(() => null)) as
      | { mensaje?: string; errores?: ErrorCampo[] }
      | null
    if (cuerpoError?.errores && cuerpoError.errores.length > 0) {
      throw new ErrorValidacionRegistro(
        cuerpoError.mensaje ?? 'Revise los campos marcados.',
        cuerpoError.errores
      )
    }
    throw new Error(cuerpoError?.mensaje ?? 'No fue posible cambiar la contraseña.')
  }

  return (await respuesta.json()) as RespuestaCambiarContrasenaObligatoria
}

export async function obtenerPerfilActual(token: string): Promise<UsuarioDetalle> {
  return pedirJson<UsuarioDetalle>(`${URL_API}${ENDPOINTS.perfilActual}`, token)
}

export type TipoUsuarioRegistro = 'Administrador' | 'Operador'

export interface DatosNuevoUsuario {
  tipoUsuario: TipoUsuarioRegistro
  nombreUsuario: string
  correo: string
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

  if (respuesta.status === 401) lanzar401(token, 'Debe iniciar sesión como administrador.')
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
  if (respuesta.status === 401) lanzar401(token, 'Debe iniciar sesión.')
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
  // La contraseña ya NO se cambia desde el endpoint de modificación. El
  // reseteo se hace con el endpoint dedicado `resetearContrasenaUsuario`,
  // que genera contraseña temporal y la envía por correo.
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

  if (respuesta.status === 401) lanzar401(token, 'Debe iniciar sesión como administrador.')
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

// ---------------------------------------------------------------------------
// HU12 — desactivación temporal de un Operador o Participante
// ---------------------------------------------------------------------------
//
// Misma forma de respuesta para ambos: id del usuario afectado, estado
// resultante ("Inactivo") y mensaje legible. El backend nunca devuelve
// datos sensibles.
export interface CambiarEstadoUsuarioRespuesta {
  idUsuario: string
  estado: 'Activo' | 'Inactivo' | string
  mensaje: string
}

async function patchSinCuerpo(
  url: string,
  token: string,
  mensajeError: string
): Promise<CambiarEstadoUsuarioRespuesta> {
  const respuesta = await fetch(url, {
    method: 'PATCH',
    headers: { 'Content-Type': 'application/json', ...autorizacion(token) },
    body: '{}'
  })

  if (respuesta.status === 401) lanzar401(token, 'Debe iniciar sesión.')
  if (respuesta.status === 403) {
    const cuerpo = (await respuesta.json().catch(() => null)) as
      | { codigo?: string; mensaje?: string }
      | null
    if (cuerpo?.codigo === 'CUENTA_DESACTIVADA') {
      throw new Error(
        'Su cuenta se encuentra desactivada. Inicie sesión nuevamente.'
      )
    }
    throw new Error('No tiene permisos para realizar esta acción.')
  }
  if (respuesta.status === 404) throw new Error('El usuario solicitado no existe.')

  if (!respuesta.ok) {
    const cuerpo = (await respuesta.json().catch(() => null)) as
      | { codigo?: string; mensaje?: string }
      | null
    if (cuerpo?.codigo === 'USUARIO_YA_INACTIVO') {
      throw new Error('La cuenta ya se encuentra inactiva.')
    }
    if (cuerpo?.codigo === 'USUARIO_YA_ACTIVO') {
      throw new Error('La cuenta ya se encuentra activa.')
    }
    throw new Error(cuerpo?.mensaje ?? mensajeError)
  }

  return (await respuesta.json()) as CambiarEstadoUsuarioRespuesta
}

export function desactivarOperadorApi(
  id: string, token: string
): Promise<CambiarEstadoUsuarioRespuesta> {
  return patchSinCuerpo(
    `${URL_API}${ENDPOINTS.desactivarOperador(id)}`,
    token,
    'No fue posible desactivar el operador.'
  )
}

export function desactivarParticipanteApi(
  id: string, token: string
): Promise<CambiarEstadoUsuarioRespuesta> {
  return patchSinCuerpo(
    `${URL_API}${ENDPOINTS.desactivarParticipante(id)}`,
    token,
    'No fue posible desactivar el participante.'
  )
}

// Reactivar — comparten el mismo helper que desactivar, solo cambia la URL.
export function activarOperadorApi(
  id: string, token: string
): Promise<CambiarEstadoUsuarioRespuesta> {
  return patchSinCuerpo(
    `${URL_API}${ENDPOINTS.activarOperador(id)}`,
    token,
    'No fue posible activar el operador.'
  )
}

export function activarParticipanteApi(
  id: string, token: string
): Promise<CambiarEstadoUsuarioRespuesta> {
  return patchSinCuerpo(
    `${URL_API}${ENDPOINTS.activarParticipante(id)}`,
    token,
    'No fue posible activar el participante.'
  )
}

// Reseteo administrativo de contraseña para Operador o Administrador.
// El backend genera la nueva contraseña temporal, la asigna en Keycloak
// con temporary:true (forzando UPDATE_PASSWORD en el próximo login) y la
// envía por correo al usuario. La respuesta NUNCA contiene la contraseña.
export interface RespuestaResetearContrasena {
  idUsuario: string
  correoDestino: string
  mensaje: string
}

export async function resetearContrasenaUsuario(
  id: string,
  token: string
): Promise<RespuestaResetearContrasena> {
  const respuesta = await fetch(
    `${URL_API}${ENDPOINTS.resetearContrasenaUsuarioInterno(id)}`,
    {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        ...autorizacion(token)
      },
      body: '{}'
    }
  )

  if (respuesta.status === 401) lanzar401(token, 'Debe iniciar sesión como administrador.')
  if (respuesta.status === 403) throw new Error('No tiene permisos para resetear contraseñas.')
  if (respuesta.status === 404) throw new Error('El usuario solicitado no existe.')

  if (!respuesta.ok) {
    const cuerpo = (await respuesta.json().catch(() => null)) as
      | { mensaje?: string }
      | null
    throw new Error(cuerpo?.mensaje ?? 'No fue posible enviar la contraseña temporal.')
  }

  return (await respuesta.json()) as RespuestaResetearContrasena
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

  if (respuesta.status === 401) lanzar401(token, 'Debe iniciar sesión como administrador.')
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
