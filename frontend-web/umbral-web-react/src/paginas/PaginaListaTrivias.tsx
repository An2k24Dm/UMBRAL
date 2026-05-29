import { useEffect, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { LayoutPanel } from '../componentes/LayoutPanel'
import { Alerta } from '../componentes/Alerta'
import { Boton } from '../componentes/Boton'
import {
  obtenerTriviasEnBorrador,
  archivarTrivia,
  activarTrivia,
  type TriviaResumenDto
} from '../autenticacion/clienteApiJuegos'
import { usarAutenticacion } from '../autenticacion/ProveedorAutenticacion'

function formatearFecha(iso: string): string {
  return new Date(iso).toLocaleDateString('es-VE', {
    day: '2-digit',
    month: '2-digit',
    year: 'numeric'
  })
}

export function PaginaListaTrivias() {
  const { token, usuario } = usarAutenticacion()
  const navegar = useNavigate()
  const rutaBase = usuario?.rol === 'Administrador' ? '/administrador/trivias' : '/operador/trivias'

  const [estado, setEstado] = useState<'cargando' | 'error' | 'vacio' | 'listo'>('cargando')
  const [mensajeError, setMensajeError] = useState<string | null>(null)
  const [trivias, setTrivias] = useState<TriviaResumenDto[]>([])
  const [procesandoId, setProcesandoId] = useState<string | null>(null)

  async function cargar(ref?: { cancelado: boolean }) {
    if (!token) { setEstado('error'); setMensajeError('Debe iniciar sesión.'); return }
    setEstado('cargando')
    setMensajeError(null)
    try {
      const lista = await obtenerTriviasEnBorrador(token)
      if (ref?.cancelado) return
      setTrivias(lista)
      setEstado(lista.length === 0 ? 'vacio' : 'listo')
    } catch (e) {
      if (ref?.cancelado) return
      setMensajeError(e instanceof Error ? e.message : 'No fue posible cargar las trivias.')
      setEstado('error')
    }
  }

  useEffect(() => {
    const ref = { cancelado: false }
    cargar(ref)
    return () => { ref.cancelado = true }
  }, [token])

  async function manejarDesactivar(e: React.MouseEvent, triviaId: string) {
    e.stopPropagation()
    if (!token) return
    setProcesandoId(triviaId)
    setMensajeError(null)
    try {
      await archivarTrivia(triviaId, token)
      await cargar()
    } catch (err) {
      setMensajeError(err instanceof Error ? err.message : 'No fue posible desactivar la trivia.')
    } finally {
      setProcesandoId(null)
    }
  }

  async function manejarReactivar(e: React.MouseEvent, triviaId: string) {
    e.stopPropagation()
    if (!token) return
    setProcesandoId(triviaId)
    setMensajeError(null)
    try {
      await activarTrivia(triviaId, token)
      await cargar()
    } catch (err) {
      setMensajeError(err instanceof Error ? err.message : 'No fue posible reactivar la trivia.')
    } finally {
      setProcesandoId(null)
    }
  }

  return (
    <LayoutPanel
      titulo="Mis trivias"
      descripcion="Trivias en estado Borrador y Archivada."
    >
      <section className="seccion">
        <div className="seccion-cabecera">
          <div>
            <h2>Trivias</h2>
            <p>Haga clic en una trivia para gestionar sus preguntas.</p>
          </div>
          <div className="cabecera-pagina-acciones">
            <Boton variante="secundario" onClick={() => navegar(`${rutaBase}/activas`)}>
              Ver activas
            </Boton>
            <Boton variante="primario" onClick={() => navegar(`${rutaBase}/crear`)}>
              + Crear trivia
            </Boton>
          </div>
        </div>

        {mensajeError && <Alerta tono="error">{mensajeError}</Alerta>}

        {estado === 'cargando' && (
          <p className="tabla-estado-mensaje">Cargando trivias…</p>
        )}

        {estado === 'vacio' && (
          <p className="tabla-estado-mensaje">No tiene trivias. Cree una para comenzar.</p>
        )}

        {estado === 'listo' && (
          <div className="lista-trivias">
            {trivias.map((t) => (
              <div
                key={t.id}
                className="trivia-card"
                role="button"
                tabIndex={0}
                onClick={() => navegar(`${rutaBase}/${t.id}/preguntas`)}
                onKeyDown={(e) => e.key === 'Enter' && navegar(`${rutaBase}/${t.id}/preguntas`)}
              >
                <div className="trivia-card-cabecera">
                  <span className="trivia-card-nombre">{t.nombre}</span>
                  <span className="trivia-card-meta">
                    {t.totalPreguntas} {t.totalPreguntas === 1 ? 'pregunta' : 'preguntas'}
                    &nbsp;·&nbsp;{t.tiempoLimitePorPregunta}s por pregunta
                    &nbsp;·&nbsp;{formatearFecha(t.fechaCreacion)}
                  </span>
                </div>
                <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', marginTop: 4 }}>
                  <p className="trivia-card-desc" style={{ margin: 0 }}>{t.descripcion}</p>
                  <span className={`estado-badge estado-badge-${t.estado.toLowerCase()}`}>
                    {t.estado}
                  </span>
                </div>
                <div className="acciones-formulario-trivia" style={{ marginTop: 8 }}>
                  {t.estado === 'Archivada' ? (
                    <Boton
                      variante="secundario"
                      onClick={(e) => manejarReactivar(e, t.id)}
                      disabled={procesandoId === t.id}
                    >
                      {procesandoId === t.id ? 'Activando…' : 'Reactivar'}
                    </Boton>
                  ) : (
                    <Boton
                      variante="peligro"
                      onClick={(e) => manejarDesactivar(e, t.id)}
                      disabled={procesandoId === t.id}
                    >
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
