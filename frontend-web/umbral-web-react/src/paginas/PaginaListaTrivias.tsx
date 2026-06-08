import { useEffect, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { LayoutPanel } from '../componentes/LayoutPanel'
import { Alerta } from '../componentes/Alerta'
import { Boton } from '../componentes/Boton'
import {
  obtenerTriviasEnBorrador,
  obtenerTriviasActivas,
  desactivarTrivia,
  activarTrivia,
  eliminarTrivia,
  type TriviaResumenDto
} from '../autenticacion/clienteApiJuegos'
import { usarAutenticacion } from '../autenticacion/ProveedorAutenticacion'

type Filtro = 'todas' | 'inactivas' | 'activas'

function formatearFecha(iso: string): string {
  return new Date(iso).toLocaleDateString('es-VE', {
    day: '2-digit', month: '2-digit', year: 'numeric'
  })
}

export function PaginaListaTrivias() {
  const { token, usuario } = usarAutenticacion()
  const navegar = useNavigate()
  const rutaBase = usuario?.rol === 'Administrador' ? '/administrador/trivias' : '/operador/trivias'

  const [cargando, setCargando] = useState(true)
  const [mensajeError, setMensajeError] = useState<string | null>(null)
  const [trivias, setTrivias] = useState<TriviaResumenDto[]>([])
  const [filtro, setFiltro] = useState<Filtro>('todas')
  const [procesandoId, setProcesandoId] = useState<string | null>(null)

  async function cargar(ref?: { cancelado: boolean }) {
    if (!token) { setMensajeError('Debe iniciar sesión.'); setCargando(false); return }
    setCargando(true)
    setMensajeError(null)
    try {
      const [inactivas, activasRaw] = await Promise.all([
        obtenerTriviasEnBorrador(token),
        obtenerTriviasActivas(token)
      ])
      if (ref?.cancelado) return
      const activas: TriviaResumenDto[] = activasRaw.map(t => ({ ...t, estado: 'Activa' }))
      const todas = [...inactivas, ...activas].sort(
        (a, b) => new Date(b.fechaCreacion).getTime() - new Date(a.fechaCreacion).getTime()
      )
      setTrivias(todas)
    } catch (e) {
      if (ref?.cancelado) return
      setMensajeError(e instanceof Error ? e.message : 'No fue posible cargar las trivias.')
    } finally {
      if (!ref?.cancelado) setCargando(false)
    }
  }

  useEffect(() => {
    const ref = { cancelado: false }
    cargar(ref)
    return () => { ref.cancelado = true }
  }, [token])

  async function manejarActivar(e: React.MouseEvent, triviaId: string) {
    e.stopPropagation()
    if (!token) return
    setProcesandoId(triviaId)
    setMensajeError(null)
    try { await activarTrivia(triviaId, token); await cargar() }
    catch (err) { setMensajeError(err instanceof Error ? err.message : 'No fue posible activar la trivia.') }
    finally { setProcesandoId(null) }
  }

  async function manejarDesactivar(e: React.MouseEvent, triviaId: string) {
    e.stopPropagation()
    if (!token) return
    setProcesandoId(triviaId)
    setMensajeError(null)
    try { await desactivarTrivia(triviaId, token); await cargar() }
    catch (err) { setMensajeError(err instanceof Error ? err.message : 'No fue posible desactivar la trivia.') }
    finally { setProcesandoId(null) }
  }

  async function manejarEliminar(e: React.MouseEvent, triviaId: string) {
    e.stopPropagation()
    if (!token) return
    setProcesandoId(triviaId)
    setMensajeError(null)
    try { await eliminarTrivia(triviaId, token); await cargar() }
    catch (err) { setMensajeError(err instanceof Error ? err.message : 'No fue posible eliminar la trivia.') }
    finally { setProcesandoId(null) }
  }

  const triviasVisibles = trivias.filter(t =>
    filtro === 'todas' ? true :
    filtro === 'inactivas' ? t.estado === 'Inactiva' :
    t.estado === 'Activa'
  )
  const totalInactivas = trivias.filter(t => t.estado === 'Inactiva').length
  const totalActivas = trivias.filter(t => t.estado === 'Activa').length

  return (
    <LayoutPanel titulo="Trivias" descripcion="Gestione sus trivias inactivas y activas.">
      <section className="seccion">
        <div className="seccion-cabecera">
          <div>
            <h2>Trivias</h2>
            <p>Haga clic en una trivia para gestionar sus preguntas.</p>
          </div>
          <div className="cabecera-pagina-acciones">
            <Boton variante="primario" onClick={() => navegar(`${rutaBase}/crear`)}>
              + Crear trivia
            </Boton>
          </div>
        </div>

        {/* Filtros */}
        <div style={{ display: 'flex', gap: 8, marginBottom: 16 }}>
          {(['todas', 'inactivas', 'activas'] as Filtro[]).map(f => (
            <Boton
              key={f}
              variante={filtro === f ? 'secundario' : 'fantasma'}
              onClick={() => setFiltro(f)}
            >
              {f === 'todas' ? `Todas (${trivias.length})` :
               f === 'inactivas' ? `Inactivas (${totalInactivas})` :
               `Activas (${totalActivas})`}
            </Boton>
          ))}
        </div>

        {mensajeError && <Alerta tono="error">{mensajeError}</Alerta>}

        {cargando && <p className="tabla-estado-mensaje">Cargando trivias…</p>}

        {!cargando && triviasVisibles.length === 0 && (
          <p className="tabla-estado-mensaje">
            {trivias.length === 0
              ? 'No tiene trivias. Cree una para comenzar.'
              : `No hay trivias ${filtro === 'inactivas' ? 'inactivas' : 'activas'}.`}
          </p>
        )}

        {!cargando && triviasVisibles.length > 0 && (
          <div className="lista-trivias">
            {triviasVisibles.map((t) => (
              <div
                key={t.id}
                className="trivia-card"
                role="button"
                tabIndex={0}
                onClick={() => navegar(`${rutaBase}/${t.id}/preguntas`)}
                onKeyDown={(e) => e.key === 'Enter' && navegar(`${rutaBase}/${t.id}/preguntas`)}
                style={{
                  borderLeft: `4px solid ${t.estado === 'Activa' ? '#22c55e' : '#94a3b8'}`,
                  opacity: t.estado === 'Inactiva' ? 0.85 : 1
                }}
              >
                <div className="trivia-card-cabecera">
                  <span className="trivia-card-nombre">{t.nombre}</span>
                  <span className="trivia-card-meta">
                    {t.totalPreguntas} {t.totalPreguntas === 1 ? 'pregunta' : 'preguntas'}
                    &nbsp;·&nbsp;{t.tiempoLimitePorPregunta}s
                    &nbsp;·&nbsp;{formatearFecha(t.fechaCreacion)}
                  </span>
                </div>
                <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', marginTop: 4 }}>
                  <p className="trivia-card-desc" style={{ margin: 0 }}>{t.descripcion}</p>
                  <span className={`estado-badge estado-badge-${t.estado.toLowerCase()}`}>{t.estado}</span>
                </div>
                <div className="acciones-formulario-trivia" style={{ marginTop: 8 }}>
                  {t.estado === 'Inactiva' ? (
                    <>
                      <Boton variante="secundario" onClick={(e) => manejarActivar(e, t.id)} disabled={procesandoId === t.id}>
                        {procesandoId === t.id ? 'Activando…' : 'Activar'}
                      </Boton>
                      <Boton variante="peligro" onClick={(e) => manejarEliminar(e, t.id)} disabled={procesandoId === t.id}>
                        {procesandoId === t.id ? 'Eliminando…' : 'Eliminar'}
                      </Boton>
                    </>
                  ) : (
                    <Boton variante="peligro" onClick={(e) => manejarDesactivar(e, t.id)} disabled={procesandoId === t.id}>
                      {procesandoId === t.id ? 'Desactivando…' : 'Desactivar'}
                    </Boton>
                  )}
                </div>
              </div>
            ))}
          </div>
        )}
      </section>
    </LayoutPanel>
  )
}
