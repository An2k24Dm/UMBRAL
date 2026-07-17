import { useState } from 'react'
import { Boton } from './Boton'

const PUNTOS_MIN = 1
const PUNTOS_MAX = 100
const MOTIVO_MAX = 500

interface Props {
  abierto: boolean
  objetivo: string
  tipoObjetivo: string
  procesando: boolean
  mensajeError?: string | null
  onAplicar: (puntos: number, motivo: string) => void
  onCancelar: () => void
}

export function ModalAplicarPenalizacion({
  abierto,
  objetivo,
  tipoObjetivo,
  procesando,
  mensajeError,
  onAplicar,
  onCancelar
}: Props) {
  const [puntos, setPuntos] = useState('')
  const [motivo, setMotivo] = useState('')
  const [errorLocal, setErrorLocal] = useState<string | null>(null)

  if (!abierto) return null

  function validar(): { puntos: number; motivo: string } | null {
    const motivoLimpio = motivo.trim()
    if (!Number.isInteger(Number(puntos)) || puntos.trim() === '') {
      setErrorLocal('Los puntos deben ser un número entero.')
      return null
    }
    const valor = Number(puntos)
    if (valor < PUNTOS_MIN || valor > PUNTOS_MAX) {
      setErrorLocal(`Los puntos deben estar entre ${PUNTOS_MIN} y ${PUNTOS_MAX}.`)
      return null
    }
    if (motivoLimpio.length === 0) {
      setErrorLocal('El motivo es obligatorio.')
      return null
    }
    if (motivoLimpio.length > MOTIVO_MAX) {
      setErrorLocal(`El motivo no puede superar ${MOTIVO_MAX} caracteres.`)
      return null
    }
    setErrorLocal(null)
    return { puntos: valor, motivo: motivoLimpio }
  }

  function manejarAplicar() {
    if (procesando) return
    const datos = validar()
    if (!datos) return
    onAplicar(datos.puntos, datos.motivo)
  }

  function cancelar() {
    if (procesando) return
    setPuntos('')
    setMotivo('')
    setErrorLocal(null)
    onCancelar()
  }

  return (
    <div
      role="dialog"
      aria-modal="true"
      aria-labelledby="modal-penalizacion-titulo"
      className="modal-confirmacion-overlay"
      onClick={cancelar}
    >
      <div className="modal-confirmacion-caja" onClick={(e) => e.stopPropagation()}>
        <h3 id="modal-penalizacion-titulo" className="modal-confirmacion-titulo">
          Aplicar penalización
        </h3>
        <div className="modal-confirmacion-cuerpo">
          <p className="modal-penalizacion-objetivo">
            {tipoObjetivo}: <strong>{objetivo}</strong>
          </p>

          <div className="campo">
            <label htmlFor="modal-penalizacion-puntos">Puntos a descontar</label>
            <input
              id="modal-penalizacion-puntos"
              type="number"
              min={PUNTOS_MIN}
              max={PUNTOS_MAX}
              step={1}
              required
              value={puntos}
              disabled={procesando}
              onChange={(e) => setPuntos(e.target.value)}
            />
            <span className="modal-penalizacion-ayuda">
              Entre {PUNTOS_MIN} y {PUNTOS_MAX}.
            </span>
          </div>

          <div className="campo">
            <label htmlFor="modal-penalizacion-motivo">Motivo</label>
            <textarea
              id="modal-penalizacion-motivo"
              required
              maxLength={MOTIVO_MAX}
              rows={3}
              value={motivo}
              disabled={procesando}
              onChange={(e) => setMotivo(e.target.value)}
            />
            <span className="modal-penalizacion-contador">
              {motivo.trim().length} / {MOTIVO_MAX}
            </span>
          </div>
        </div>

        {(errorLocal || mensajeError) && (
          <div className="modal-confirmacion-error" role="alert">
            {errorLocal ?? mensajeError}
          </div>
        )}

        <div className="modal-confirmacion-acciones">
          <Boton variante="secundario" onClick={cancelar} disabled={procesando}>
            Cancelar
          </Boton>
          <Boton
            variante="peligro"
            onClick={manejarAplicar}
            disabled={procesando}
            data-testid="modal-penalizacion-aplicar"
          >
            {procesando ? 'Procesando…' : 'Aplicar penalización'}
          </Boton>
        </div>
      </div>
    </div>
  )
}
