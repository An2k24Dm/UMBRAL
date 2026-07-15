import * as SecureStore from 'expo-secure-store'
import type { ResultadoInicioSesion, UsuarioAutenticado } from './clienteApi'

// HU04 — la sesión móvil se persiste en SecureStore para que sobreviva al
// cierre de la app. Guardamos el resultado completo del login (tokens +
// metadatos + usuario) como un único JSON, además de mantener el usuario
// como entrada separada por compatibilidad con consumidores anteriores.
const CLAVE_SESION = 'umbral_sesion'

// Sesión persistida en el dispositivo. Es exactamente la información que el
// backend devuelve al iniciar sesión, menos la ruta de redirección (que es
// efímera y se decide en cada login).
export interface SesionPersistida {
  tokenAcceso: string
  tokenRefresco: string
  expiraEn: number
  tipoToken: string
  usuario: UsuarioAutenticado
}

interface CargaJwt {
  exp?: unknown
}

const MARGEN_EXPIRACION_SEGUNDOS = 30

function leerCargaJwt(token: string): CargaJwt | null {
  const partes = token.split('.')
  if (partes.length !== 3) return null

  try {
    const base64 = partes[1]
      .replace(/-/g, '+')
      .replace(/_/g, '/')
      .padEnd(Math.ceil(partes[1].length / 4) * 4, '=')
    return JSON.parse(globalThis.atob(base64)) as CargaJwt
  } catch {
    return null
  }
}

export function esTokenAccesoVigente(token: unknown): token is string {
  if (typeof token !== 'string' || token.trim().length === 0) return false

  const carga = leerCargaJwt(token)
  if (!carga || typeof carga.exp !== 'number') return false

  const ahoraSegundos = Math.floor(Date.now() / 1000)
  return carga.exp > ahoraSegundos + MARGEN_EXPIRACION_SEGUNDOS
}

function esSesionPersistidaValida(valor: unknown): valor is SesionPersistida {
  if (!valor || typeof valor !== 'object') return false

  const sesion = valor as Partial<SesionPersistida>
  const usuario = sesion.usuario as Partial<UsuarioAutenticado> | undefined

  return (
    esTokenAccesoVigente(sesion.tokenAcceso) &&
    typeof sesion.tokenRefresco === 'string' &&
    sesion.tokenRefresco.length > 0 &&
    typeof sesion.expiraEn === 'number' &&
    typeof sesion.tipoToken === 'string' &&
    !!usuario &&
    typeof usuario.id === 'string' &&
    typeof usuario.nombreUsuario === 'string' &&
    usuario.rol === 'Participante'
  )
}

export async function guardarSesion(resultado: ResultadoInicioSesion): Promise<void> {
  const sesion: SesionPersistida = {
    tokenAcceso: resultado.tokenAcceso,
    tokenRefresco: resultado.tokenRefresco,
    expiraEn: resultado.expiraEn,
    tipoToken: resultado.tipoToken,
    usuario: resultado.usuario
  }
  await SecureStore.setItemAsync(CLAVE_SESION, JSON.stringify(sesion))
}

export async function obtenerSesion(): Promise<SesionPersistida | null> {
  const valor = await SecureStore.getItemAsync(CLAVE_SESION)
  if (!valor) return null
  try {
    const sesion = JSON.parse(valor) as unknown
    if (esSesionPersistidaValida(sesion)) return sesion

    await SecureStore.deleteItemAsync(CLAVE_SESION)
    return null
  } catch {
    // Si el contenido está corrupto se descarta para no dejar al usuario
    // con una sesión inválida o vencida que nunca podría reparar desde la UI.
    await SecureStore.deleteItemAsync(CLAVE_SESION)
    return null
  }
}

export async function eliminarSesion(): Promise<void> {
  await SecureStore.deleteItemAsync(CLAVE_SESION)
}

// Atajos sobre la sesión persistida. No duplican el almacenamiento: leen y
// escriben siempre la misma entrada CLAVE_SESION. Se exponen para pantallas
// que solo necesitan el token o el usuario y no quieren acoplarse al tipo
// SesionPersistida completo.
export async function obtenerToken(): Promise<string | null> {
  const sesion = await obtenerSesion()
  return sesion?.tokenAcceso ?? null
}

export async function obtenerUsuario(): Promise<UsuarioAutenticado | null> {
  const sesion = await obtenerSesion()
  return sesion?.usuario ?? null
}

// Alias semántico de eliminarSesion: algunos lugares hablan de "limpiar"
// (cerrar sesión) y otros de "eliminar" (purgar SecureStore).
export async function limpiarSesion(): Promise<void> {
  await eliminarSesion()
}
