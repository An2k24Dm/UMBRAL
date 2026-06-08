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
    return JSON.parse(valor) as SesionPersistida
  } catch {
    // Si el contenido está corrupto se descarta para no dejar al usuario
    // con una sesión inválida que nunca podría reparar desde la UI.
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
