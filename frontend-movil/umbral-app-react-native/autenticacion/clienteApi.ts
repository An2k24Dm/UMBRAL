import Constants from 'expo-constants'

const URL_API: string =
  (Constants.expoConfig?.extra as { urlApi?: string } | undefined)?.urlApi ??
  "http://192.168.1.11:5000";

export interface UsuarioAutenticado {
  id: string
  nombreUsuario: string
  rol: 'Administrador' | 'Operador' | 'Participante'
  nombre: string
  apellido: string
}

export interface ResultadoInicioSesion {
  tokenAcceso: string
  tokenRefresco: string
  expiraEn: number
  tipoToken: string
  usuario: UsuarioAutenticado
  rutaRedireccion: string
}

export async function iniciarSesionApi(
  nombreUsuario: string,
  contrasena: string
): Promise<ResultadoInicioSesion> {
  const respuesta = await fetch(`${URL_API}/api/autenticacion/login-movil`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ nombreUsuario, contrasena })
  })

  if (!respuesta.ok) {
    const cuerpo = await respuesta.json().catch(() => null)
    throw new Error(cuerpo?.mensaje ?? 'No fue posible iniciar sesión.')
  }

  return (await respuesta.json()) as ResultadoInicioSesion
}
