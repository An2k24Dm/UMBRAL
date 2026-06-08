import { CampoFormulario } from '../CampoFormulario'
import type { ModoSesion } from '../../tipos/sesiones'

interface Props {
  valor: ModoSesion
  alCambiar: (modo: ModoSesion) => void
  deshabilitado?: boolean
  error?: string
}

// Select limpio para el tipo de sesión. Las restricciones de capacidad
// (10 participantes, 5 equipos, etc.) NO se muestran aquí: viven en
// AyudaModoSesion para no recargar visualmente el select.
export function SelectorModoSesion({ valor, alCambiar, deshabilitado, error }: Props) {
  return (
    <CampoFormulario etiqueta="Tipo de sesión" htmlFor="modo" error={error}>
      <select
        id="modo"
        value={valor}
        onChange={(e) => alCambiar(e.target.value as ModoSesion)}
        disabled={deshabilitado}
      >
        <option value="Individual">Individual</option>
        <option value="Grupal">Grupal</option>
      </select>
    </CampoFormulario>
  )
}
