import type { ReactNode } from 'react'
import { Boton } from './Boton'

// Modal genérico de confirmación con botón destructivo. Lo usa HU13
// (eliminar Operador) y queda preparado para futuras acciones permanentes
// del panel del Administrador. Conscientemente simple: sin librería extra,
// sin foco automático complejo, sólo el overlay + caja + botones. El llamador
// controla la visibilidad y el deshabilitado del botón destructivo durante
// el envío.
interface Props {
  // Si false, el componente no se renderiza en absoluto.
  abierto: boolean
  titulo: string
  // Contenido principal — generalmente un párrafo explicativo. Se acepta
  // ReactNode para permitir énfasis tipográfico (negrita, listas, etc.).
  children: ReactNode
  textoConfirmar: string
  textoCancelar?: string
  // Bloquea ambos botones mientras la acción está en curso (evita doble click).
  procesando?: boolean
  // Mensaje de error a renderizar dentro del modal (sin cerrar).
  mensajeError?: string | null
  onConfirmar: () => void
  onCancelar: () => void
}

export function ModalConfirmacion({
  abierto,
  titulo,
  children,
  textoConfirmar,
  textoCancelar = 'Cancelar',
  procesando = false,
  mensajeError,
  onConfirmar,
  onCancelar
}: Props) {
  if (!abierto) return null

  return (
    <div
      role="dialog"
      aria-modal="true"
      aria-labelledby="modal-confirmacion-titulo"
      className="modal-confirmacion-overlay"
      // Cerrar al hacer clic fuera. Si está procesando, ignorar para no
      // perder el contexto de la acción.
      onClick={() => { if (!procesando) onCancelar() }}
    >
      <div
        className="modal-confirmacion-caja"
        onClick={(e) => e.stopPropagation()}
      >
        <h3 id="modal-confirmacion-titulo" className="modal-confirmacion-titulo">
          {titulo}
        </h3>
        <div className="modal-confirmacion-cuerpo">{children}</div>

        {mensajeError && (
          <div className="modal-confirmacion-error" role="alert">
            {mensajeError}
          </div>
        )}

        <div className="modal-confirmacion-acciones">
          <Boton
            variante="secundario"
            onClick={onCancelar}
            disabled={procesando}
          >
            {textoCancelar}
          </Boton>
          <Boton
            variante="peligro"
            onClick={onConfirmar}
            disabled={procesando}
            data-testid="modal-confirmacion-confirmar"
          >
            {procesando ? 'Procesando…' : textoConfirmar}
          </Boton>
        </div>
      </div>
    </div>
  )
}
