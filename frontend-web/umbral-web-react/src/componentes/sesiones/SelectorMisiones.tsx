import { useMemo } from 'react'
import { CampoFormulario } from '../CampoFormulario'
import { Boton } from '../Boton'
import { MAX_MISIONES, MIN_MISIONES } from '../../hooks/useCrearSesion'
import type { MisionResumenDto } from '../../tipos/misiones'

interface Props {
  misionesActivas: MisionResumenDto[]
  misionesSeleccionadasIds: string[]
  alAgregar: (misionId: string) => void
  alQuitar: (misionId: string) => void
  cargando: boolean
  errorCarga: string | null
  errorFormulario?: string
  deshabilitado?: boolean
}

// Componente reutilizable de selección de misiones (1..5). Encapsula
// el select de "agregar", la lista de seleccionadas y el contador.
export function SelectorMisiones({
  misionesActivas,
  misionesSeleccionadasIds,
  alAgregar,
  alQuitar,
  cargando,
  errorCarga,
  errorFormulario,
  deshabilitado,
}: Props) {
  const misionesPorId = useMemo(
    () => new Map(misionesActivas.map(m => [m.id, m])),
    [misionesActivas],
  )

  const seleccionadas = useMemo(
    () =>
      misionesSeleccionadasIds
        .map(id => misionesPorId.get(id))
        .filter((m): m is MisionResumenDto => m !== undefined),
    [misionesSeleccionadasIds, misionesPorId],
  )

  const disponibles = useMemo(
    () => misionesActivas.filter(m => !misionesSeleccionadasIds.includes(m.id)),
    [misionesActivas, misionesSeleccionadasIds],
  )

  const alcanzoMaximo = misionesSeleccionadasIds.length >= MAX_MISIONES
  const ayuda = cargando
    ? 'Cargando misiones activas…'
    : errorCarga ?? `Seleccione entre ${MIN_MISIONES} y ${MAX_MISIONES} misiones.`

  return (
    <>
      <CampoFormulario
        etiqueta={`Misiones activas (${misionesSeleccionadasIds.length}/${MAX_MISIONES})`}
        htmlFor="selector-mision"
        error={errorFormulario}
        ayuda={ayuda}
      >
        <select
          id="selector-mision"
          value=""
          onChange={(e) => {
            alAgregar(e.target.value)
            e.currentTarget.value = ''
          }}
          disabled={
            deshabilitado || cargando || disponibles.length === 0 || alcanzoMaximo
          }
        >
          <option value="">
            {cargando
              ? 'Cargando…'
              : alcanzoMaximo
                ? `Ya alcanzó el máximo de ${MAX_MISIONES} misiones`
                : disponibles.length === 0
                  ? 'No hay más misiones para agregar'
                  : 'Agregar misión…'}
          </option>
          {disponibles.map(m => (
            <option key={m.id} value={m.id}>
              {m.nombre} · {m.dificultad} · {m.totalEtapas} etapa(s)
            </option>
          ))}
        </select>
      </CampoFormulario>

      {seleccionadas.length > 0 && (
        <ul className="lista-misiones-seleccionadas">
          {seleccionadas.map((m, idx) => (
            <li key={m.id} className="mision-seleccionada">
              <div>
                <strong>
                  {idx + 1}. {m.nombre}
                </strong>
                <p style={{ margin: '4px 0 0', fontSize: '0.85rem', opacity: 0.8 }}>
                  {m.descripcion}
                </p>
                <small>
                  Dificultad: {m.dificultad} · Etapas: {m.totalEtapas} · Estado: {m.estado}
                </small>
              </div>
              <Boton
                variante="volver"
                type="button"
                onClick={() => alQuitar(m.id)}
                disabled={deshabilitado}
              >
                Quitar
              </Boton>
            </li>
          ))}
        </ul>
      )}
    </>
  )
}
