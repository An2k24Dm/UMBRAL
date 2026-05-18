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
