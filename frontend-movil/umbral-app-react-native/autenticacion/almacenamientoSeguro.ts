import * as SecureStore from 'expo-secure-store'

const CLAVE_TOKEN = 'umbral_token'
const CLAVE_USUARIO = 'umbral_usuario'

export async function guardarSesion(token: string, usuario: unknown) {
  await SecureStore.setItemAsync(CLAVE_TOKEN, token)
  await SecureStore.setItemAsync(CLAVE_USUARIO, JSON.stringify(usuario))
}

export async function obtenerToken(): Promise<string | null> {
  return await SecureStore.getItemAsync(CLAVE_TOKEN)
}

export async function obtenerUsuario<T>(): Promise<T | null> {
  const valor = await SecureStore.getItemAsync(CLAVE_USUARIO)
  return valor ? (JSON.parse(valor) as T) : null
}

export async function limpiarSesion() {
  await SecureStore.deleteItemAsync(CLAVE_TOKEN)
  await SecureStore.deleteItemAsync(CLAVE_USUARIO)
}
