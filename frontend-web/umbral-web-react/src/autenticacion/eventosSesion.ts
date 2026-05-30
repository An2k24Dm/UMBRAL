// Canal único para señalar que la sesión vigente ya no sirve (token
// caducado, revocado por el backend, cuenta desactivada). Cualquier
// adaptador HTTP que reciba un 401 dispara este evento; el
// ProveedorAutenticacion lo escucha y limpia el estado + localStorage.
//
// Usar un CustomEvent del DOM evita acoplar los clientes HTTP al
// contexto de React: los adaptadores siguen siendo funciones puras
// reutilizables desde cualquier capa, y el provider hace el trabajo
// de purgar la sesión en un solo lugar.

export const EVENTO_SESION_INVALIDA = 'umbral:sesion-invalida'

export function dispatchSesionInvalida(): void {
  // En entornos sin window (tests SSR, por ejemplo) no hay nada que
  // hacer. La sesión se invalidará la próxima vez que el usuario
  // navegue en el navegador real.
  if (typeof window === 'undefined') return
  window.dispatchEvent(new CustomEvent(EVENTO_SESION_INVALIDA))
}
