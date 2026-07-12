import { useEffect, useState } from 'react'
import { LayoutPanel } from '../componentes/LayoutPanel'
import { Alerta } from '../componentes/Alerta'
import { Boton } from '../componentes/Boton'
import {
  obtenerRankingGlobal,
  type EntradaRankingGlobalDto
} from '../autenticacion/clienteApiRanking'
import { usarAutenticacion } from '../autenticacion/ProveedorAutenticacion'

export function PaginaRankingGlobal() {
  const { token } = usarAutenticacion()
  const [entradas, setEntradas] = useState<EntradaRankingGlobalDto[] | null>(null)
  const [cargando, setCargando] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [version, setVersion] = useState(0)

  useEffect(() => {
    if (!token) return
    let cancelado = false
    setCargando(true)
    setError(null)
    obtenerRankingGlobal(token, 50)
      .then(data => { if (!cancelado) { setEntradas(data); setCargando(false) } })
      .catch(e => {
        if (!cancelado) {
          setError(e instanceof Error ? e.message : 'No se pudo cargar el ranking global.')
          setCargando(false)
        }
      })
    return () => { cancelado = true }
  }, [token, version])

  return (
    <LayoutPanel
      titulo="Ranking global"
      descripcion="Clasificación histórica de participantes por puntaje acumulado en todas las sesiones finalizadas."
    >
      <div style={{ display: 'flex', justifyContent: 'flex-end', marginBottom: 'var(--espacio-3)' }}>
        <Boton variante="secundario" onClick={() => setVersion(v => v + 1)} disabled={cargando}>
          {cargando ? 'Actualizando…' : 'Actualizar'}
        </Boton>
      </div>

      {cargando && <p className="detalle-mensaje-vacio">Cargando ranking…</p>}
      {!cargando && error && <Alerta tono="error">{error}</Alerta>}

      {!cargando && !error && entradas !== null && entradas.length === 0 && (
        <section className="seccion">
          <p className="detalle-mensaje-vacio">
            Aún no hay datos en el ranking global. Los puntajes se acumulan cuando finalizan
            las sesiones.
          </p>
        </section>
      )}

      {!cargando && !error && entradas !== null && entradas.length > 0 && (
        <section className="seccion">
          <div style={{ overflowX: 'auto' }}>
            <table className="tabla-usuarios">
              <thead>
                <tr>
                  <th style={{ width: 60 }}>#</th>
                  <th>Participante</th>
                  <th>Puntaje acumulado</th>
                  <th>Sesiones jugadas</th>
                  <th>Etapas completadas</th>
                </tr>
              </thead>
              <tbody>
                {entradas.map(e => {
                  const medalla =
                    e.posicion === 1 ? '🥇'
                    : e.posicion === 2 ? '🥈'
                    : e.posicion === 3 ? '🥉'
                    : null
                  return (
                    <tr key={e.participanteIdentidadId}>
                      <td style={{ textAlign: 'center', fontSize: '1.15rem' }}>
                        {medalla ?? <span style={{ opacity: 0.6 }}>#{e.posicion}</span>}
                      </td>
                      <td>
                        <strong>{e.nombreParticipante}</strong>
                      </td>
                      <td>
                        <strong style={{ color: 'var(--color-primario, #6366f1)', fontSize: '1.05rem' }}>
                          {e.puntajeAcumulado.toLocaleString()} pts
                        </strong>
                      </td>
                      <td>{e.sesionesJugadas}</td>
                      <td>{e.etapasCompletadasTotal}</td>
                    </tr>
                  )
                })}
              </tbody>
            </table>
          </div>
          <p style={{ marginTop: 'var(--espacio-3)', fontSize: '0.8rem', opacity: 0.6 }}>
            Mostrando los primeros {entradas.length} participantes. Los puntajes se actualizan
            al finalizar cada sesión.
          </p>
        </section>
      )}
    </LayoutPanel>
  )
}
