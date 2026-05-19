import type { RespuestaError, ResultadoInicioSesion } from './tipos'

const URL_API = import.meta.env.VITE_API_URL ?? 'http://localhost:5000'

export async function iniciarSesion(
  nombreUsuario: string,
  contrasena: string
): Promise<ResultadoInicioSesion> {
  const respuesta = await fetch(`${URL_API}/api/autenticacion/login-web`, {
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

export async function obtenerPerfilActual(token: string) {
  const respuesta = await fetch(`${URL_API}/api/autenticacion/perfil-actual`, {
    headers: { Authorization: `Bearer ${token}` }
  })
  if (!respuesta.ok) throw new Error('No autorizado.')
  return await respuesta.json()
}

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

  const respuesta = await fetch(`${URL_API}/api/usuarios`, {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
      Authorization: `Bearer ${token}`
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
