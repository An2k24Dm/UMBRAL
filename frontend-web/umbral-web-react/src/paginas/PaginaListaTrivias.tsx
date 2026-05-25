import { useEffect, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { LayoutPanel } from '../componentes/LayoutPanel'
import { Alerta } from '../componentes/Alerta'
import { Boton } from '../componentes/Boton'
import { obtenerTriviasEnBorrador, type TriviaResumenDto } from '../autenticacion/clienteApiJuegos'
import { usarAutenticacion } from '../autenticacion/ProveedorAutenticacion'

function formatearFecha(iso: string): string {
  return new Date(iso).toLocaleDateString('es-VE', {
    day: '2-digit',
    month: '2-digit',
    year: 'numeric'
  })
}

export function PaginaListaTrivias() {
  const { token } = usarAutenticacion()
  const navegar = useNavigate()

  const [estado, setEstado] = useState<'cargando' | 'error' | 'vacio' | 'listo'>('cargando')
  const [mensajeError, setMensajeError] = useState<string | null>(null)
  const [trivias, setTrivias] = useState<TriviaResumenDto[]>([])

  useEffect(() => {
    let cancelado = false
    async function cargar() {
      if (!token) { setEstado('error'); setMensajeError('Debe iniciar sesión.'); return }
      setEstado('cargando')
      setMensajeError(null)
      try {
        const lista = await obtenerTriviasEnBorrador(token)
        if (cancelado) return
        setTrivias(lista)
        setEstado(lista.length === 0 ? 'vacio' : 'listo')
      } catch (e) {
        if (cancelado) return
        setMensajeError(e instanceof Error ? e.message : 'No fue posible cargar las trivias.')
        setEstado('error')
      }
    }
    cargar()
    return () => { cancelado = true }
  }, [token])

  return (
    <LayoutPanel
      titulo="Mis trivias"
      descripcion="Trivias en estado Borrador que usted ha creado."
    >
      <section className="seccion">
        <div className="seccion-cabecera">
          <div>
            <h2>Trivias en borrador</h2>
            <p>Haga clic en una trivia para gestionar sus preguntas.</p>
          </div>
          <div className="cabecera-pagina-acciones">
            <Boton variante="primario" onClick={() => navegar('/operador/trivias/crear')}>
              + Crear trivia
            </Boton>
          </div>
        </div>

        {estado === 'error' && mensajeError && (
          <Alerta tono="error">{mensajeError}</Alerta>
        )}

        {estado === 'cargando' && (
          <p className="tabla-estado-mensaje">Cargando trivias…</p>
        )}

        {estado === 'vacio' && (
          <p className="tabla-estado-mensaje">No tiene trivias en borrador. Cree una para comenzar.</p>
        )}

        {estado === 'listo' && (
          <div className="lista-trivias">
            {trivias.map((t) => (
              <div
                key={t.id}
                className="trivia-card"
                role="button"
                tabIndex={0}
                onClick={() => navegar(`/operador/trivias/${t.id}/preguntas`)}
                onKeyDown={(e) => e.key === 'Enter' && navegar(`/operador/trivias/${t.id}/preguntas`)}
              >
                <div className="trivia-card-cabecera">
                  <span className="trivia-card-nombre">{t.nombre}</span>
                  <span className="trivia-card-meta">
                    {t.totalPreguntas} {t.totalPreguntas === 1 ? 'pregunta' : 'preguntas'}
                    &nbsp;·&nbsp;{t.tiempoLimitePorPregunta}s por pregunta
                    &nbsp;·&nbsp;{formatearFecha(t.fechaCreacion)}
                  </span>
                </div>
                <p className="trivia-card-desc">{t.descripcion}</p>
              </div>
            ))}
          </div>
        )}
      </section>
    </LayoutPanel>
  )
}
