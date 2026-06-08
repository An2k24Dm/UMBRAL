// Canal único para señalar que la sesión vigente ya no sirve (token
// caducado, revocado por el backend, cuenta desactivada). Cualquier
// adaptador HTTP que reciba un 401 dispara este evento; el
// ProveedorAutenticacion lo escucha y limpia el estado + localStorage.

export const EVENTO_SESION_INVALIDA = 'umbral:sesion-invalida'

// Margen para tolerar pequeñas diferencias entre el reloj del cliente
// y el del servidor de Keycloak. Si el token vence en menos de este
// margen lo consideramos vencido.
const MARGEN_EXPIRACION_SEGUNDOS = 5
const CLAVE_TOKEN_LOCAL = 'umbral.token'

export function dispatchSesionInvalida(): void {
  if (typeof window === 'undefined') return
  window.dispatchEvent(new CustomEvent(EVENTO_SESION_INVALIDA))
}

function decodificarBase64Url(segmento: string): string {
  const reemplazado = segmento.replace(/-/g, '+').replace(/_/g, '/')
  const padding = '='.repeat((4 - (reemplazado.length % 4)) % 4)
  return atob(reemplazado + padding)
}

// Devuelve true cuando el JWT recibido ya pasó su `exp`, está mal
// formado o no se puede leer. Cualquiera de esos casos justifica cerrar
// sesión. Un JWT vigente y bien formado deja la decisión en manos del
// backend (mostrar el error sin destruir la sesión).
export function jwtSeAgoto(token: string | null | undefined): boolean {
  if (!token) return true
  const partes = token.split('.')
  if (partes.length !== 3) return true
  try {
    const cargaUtilJson = decodificarBase64Url(partes[1])
    const cargaUtil = JSON.parse(cargaUtilJson) as { exp?: number }
    if (typeof cargaUtil.exp !== 'number') return true
    const ahoraSegundos = Date.now() / 1000
    return ahoraSegundos >= cargaUtil.exp - MARGEN_EXPIRACION_SEGUNDOS
  } catch {
    return true
  }
}

function leerTokenDeAlmacenamiento(): string | null {
  if (typeof window === 'undefined') return null
  try {
    return window.localStorage.getItem(CLAVE_TOKEN_LOCAL)
  } catch {
    return null
  }
}

// Punto único de manejo del 401 desde los clientes HTTP.
//
// Recibe opcionalmente el token que se intentó usar (los clientes lo
// tienen como parámetro). Si no se pasa, lo leemos de localStorage como
// respaldo. Sobre ese token decidimos:
//   * Si está vencido o ausente: disparamos el evento de sesión inválida
//     (el provider cierra sesión) y lanzamos un error claro.
//   * Si sigue vigente: NO tocamos la sesión y lanzamos un error de
//     "intente nuevamente". El usuario ve la alerta en pantalla pero
//     conserva su estado y puede reintentar sin re-loguearse.
export function manejar401(
  tokenExplicito?: string | null,
  mensajeRespaldo = 'Debe iniciar sesión.',
): never {
  const token = tokenExplicito ?? leerTokenDeAlmacenamiento()
  if (jwtSeAgoto(token)) {
    dispatchSesionInvalida()
    throw new Error('Su sesión expiró. Inicie sesión nuevamente.')
  }
  throw new Error(
    `${mensajeRespaldo} El servidor rechazó la solicitud, pero la sesión local sigue activa. Intente nuevamente.`,
  )
}
