import { useEffect, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { LayoutPanel } from '../componentes/LayoutPanel'
import { Alerta } from '../componentes/Alerta'
import { Boton } from '../componentes/Boton'
import {
  obtenerTriviasActivas,
  archivarTrivia,
  type TriviaActivaResumenDto
} from '../autenticacion/clienteApiJuegos'
import { usarAutenticacion } from '../autenticacion/ProveedorAutenticacion'

function formatearFecha(iso: string): string {
  return new Date(iso).toLocaleDateString('es-VE', {
    day: '2-digit',
    month: '2-digit',
    year: 'numeric'
  })
}

export function PaginaListaTriviasActivas() {
  const { token, usuario } = usarAutenticacion()
  const navegar = useNavigate()
  const rutaBase = usuario?.rol === 'Administrador' ? '/administrador/trivias' : '/operador/trivias'

  const [estado, setEstado] = useState<'cargando' | 'error' | 'vacio' | 'listo'>('cargando')
  const [mensajeError, setMensajeError] = useState<string | null>(null)
  const [trivias, setTrivias] = useState<TriviaActivaResumenDto[]>([])
  const [desactivandoId, setDesactivandoId] = useState<string | null>(null)

  async function cargar(cancelado?: { valor: boolean }) {
    if (!token) { setEstado('error'); setMensajeError('Debe iniciar sesión.'); return }
    setEstado('cargando')
    setMensajeError(null)
    try {
      const lista = await obtenerTriviasActivas(token)
      if (cancelado?.valor) return
      setTrivias(lista)
      setEstado(lista.length === 0 ? 'vacio' : 'listo')
    } catch (e) {
      if (cancelado?.valor) return
      setMensajeError(e instanceof Error ? e.message : 'No fue posible cargar las trivias activas.')
      setEstado('error')
    }
  }

  useEffect(() => {
    const ref = { valor: false }
    cargar(ref)
    return () => { ref.valor = true }
  }, [token])

  async function manejarDesactivar(triviaId: string) {
    if (!token) return
    setDesactivandoId(triviaId)
    setMensajeError(null)
    try {
      await archivarTrivia(triviaId, token)
      await cargar()
    } catch (e) {
      setMensajeError(e instanceof Error ? e.message : 'No fue posible desactivar la trivia.')
    } finally {
      setDesactivandoId(null)
    }
  }

  return (
    <LayoutPanel
      titulo="Trivias activas"
      descripcion="Trivias en estado Activa disponibles para los participantes."
    >
      <section className="seccion">
        <div className="seccion-cabecera">
          <div>
            <h2>Trivias activas</h2>
            <p>Haga clic en una trivia para ver su contenido o desactivarla.</p>
          </div>
          <div className="cabecera-pagina-acciones">
            <Boton variante="volver" onClick={() => navegar(rutaBase)}>
              Volver a mis trivias
            </Boton>
          </div>
        </div>

        {mensajeError && <Alerta tono="error">{mensajeError}</Alerta>}

        {estado === 'cargando' && (
          <p className="tabla-estado-mensaje">Cargando trivias activas…</p>
        )}

        {estado === 'vacio' && (
          <p className="tabla-estado-mensaje">No hay trivias activas en este momento.</p>
        )}

        {estado === 'listo' && (
          <div className="lista-trivias">
            {trivias.map((t) => (
              <div key={t.id} className="trivia-card">
                <div className="trivia-card-cabecera">
                  <span className="trivia-card-nombre">{t.nombre}</span>
                  <span className="trivia-card-meta">
                    {t.totalPreguntas} {t.totalPreguntas === 1 ? 'pregunta' : 'preguntas'}
                    &nbsp;·&nbsp;{t.tiempoLimitePorPregunta}s por pregunta
                    &nbsp;·&nbsp;{formatearFecha(t.fechaCreacion)}
                  </span>
                </div>
                <p className="trivia-card-desc">{t.descripcion}</p>
                <div className="acciones-formulario-trivia" style={{ marginTop: 8 }}>
                  <Boton
                    variante="secundario"
                    onClick={() => navegar(`${rutaBase}/${t.id}/preguntas`)}
                  >
                    Ver preguntas
                  </Boton>
                  <Boton
                    variante="peligro"
                    onClick={() => manejarDesactivar(t.id)}
                    disabled={desactivandoId === t.id}
                  >
                    {desactivandoId === t.id ? 'Desactivando…' : 'Desactivar'}
                  </Boton>
                </div>
              </div>
            ))}
          </div>
        )}
      </section>
    </LayoutPanel>
  )
}
