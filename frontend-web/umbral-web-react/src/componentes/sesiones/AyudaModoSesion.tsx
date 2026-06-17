import type { ModoSesion } from '../../tipos/sesiones'

interface Props {
  modo: ModoSesion
  // Valores de capacidad actuales del formulario (texto de los inputs).
  // El texto se arma dinámicamente con lo que el Operador escribe; no hay
  // capacidades fijas.
  maximoParticipantes?: string
  maximoEquipos?: string
  maximoParticipantesPorEquipo?: string
}

function tieneValor(valor?: string): boolean {
  return typeof valor === 'string' && valor.trim() !== ''
}

function mensaje(props: Props): string {
  if (props.modo === 'Individual') {
    return tieneValor(props.maximoParticipantes)
      ? `Sesión individual: permitirá hasta ${props.maximoParticipantes} participante(s).`
      : 'Sesión individual: define el número máximo de participantes.'
  }

  return tieneValor(props.maximoEquipos) && tieneValor(props.maximoParticipantesPorEquipo)
    ? `Sesión grupal: permitirá hasta ${props.maximoEquipos} equipo(s) de ${props.maximoParticipantesPorEquipo} integrante(s) cada uno.`
    : 'Sesión grupal: define el número máximo de equipos y el tamaño de cada equipo.'
}

// Tarjeta auxiliar pequeña y consistente con el tema oscuro. Refleja la
// capacidad que el Operador está configurando, sin valores hardcodeados.
export function AyudaModoSesion(props: Props) {
  return (
    <div
      className="ayuda-modo-sesion"
      role="note"
      aria-live="polite"
    >
      <span className="ayuda-modo-sesion-icono" aria-hidden="true">ⓘ</span>
      <span>{mensaje(props)}</span>
    </div>
  )
}
