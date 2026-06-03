import type { ModoSesion } from '../../tipos/sesiones'

interface Props {
  modo: ModoSesion
}

const MENSAJES: Record<ModoSesion, string> = {
  Individual: 'Sesión individual: permite hasta 10 participantes.',
  Grupal: 'Sesión grupal: permite hasta 5 equipos de 2 integrantes cada uno.',
}

// Tarjeta auxiliar pequeña y consistente con el tema oscuro. Muestra
// las restricciones del tipo de sesión sin invadir el selector.
export function AyudaModoSesion({ modo }: Props) {
  return (
    <div
      className="ayuda-modo-sesion"
      role="note"
      aria-live="polite"
    >
      <span className="ayuda-modo-sesion-icono" aria-hidden="true">ⓘ</span>
      <span>{MENSAJES[modo]}</span>
    </div>
  )
}
