import { useEffect, useState } from 'react'
import { useNavigate, useParams } from 'react-router-dom'
import { LayoutPanel } from '../componentes/LayoutPanel'
import { Alerta } from '../componentes/Alerta'
import { Boton } from '../componentes/Boton'
import { BadgeEstadoSesion } from '../componentes/BadgeEstadoSesion'
import {
  obtenerSesion,
  type SesionDetalleDto
} from '../autenticacion/clienteApiSesiones'
import { usarAutenticacion } from '../autenticacion/ProveedorAutenticacion'
import {
  formatearFechaSesion,
  nombreTipoJuego
} from '../utilidades/formatoSesiones'

// HU34/5.2 — Detalle de una sesión rediseñado.
//
// Estructura:
//   ← Volver a sesiones
//   [Nombre de la sesión]                       [Badge Estado]
//   Tarjeta: Información general (grilla de campos).
//   Tarjeta: Contenido asociado (Trivia o Búsqueda del Tesoro).
//     - Trivia: preguntas con opciones; la correcta se resalta.
//     - Búsqueda: etapas con sus pistas ordenadas.
//
// El backend decide si el usuario puede ver el detalle. Aquí sólo
// presentamos lo que llega; los errores de permiso vienen como 403.

export function PaginaDetalleSesion() {
  const { id } = useParams<{ id: string }>()
  const { token, usuario } = usarAutenticacion()
  const navegar = useNavigate()
  const rutaListado = usuario?.rol === 'Administrador'
    ? '/administrador/sesiones'
    : '/operador/sesiones'

  const [estado, setEstado] = useState<'cargando' | 'error' | 'listo'>('cargando')
  const [mensajeError, setMensajeError] = useState<string | null>(null)
  const [sesion, setSesion] = useState<SesionDetalleDto | null>(null)

  useEffect(() => {
    const ref = { cancelado: false }
    async function cargar() {
      if (!token || !id) {
        setEstado('error')
        setMensajeError('Identificador de sesión inválido.')
        return
      }
      try {
        const detalle = await obtenerSesion(id, token)
        if (ref.cancelado) return
        setSesion(detalle)
        setEstado('listo')
      } catch (e) {
        if (ref.cancelado) return
        setMensajeError(e instanceof Error ? e.message : 'No se pudo cargar el detalle de la sesión.')
        setEstado('error')
      }
    }
    cargar()
    return () => { ref.cancelado = true }
  }, [token, id])

  return (
    <LayoutPanel
      titulo="Detalle de sesión"
      descripcion="Información completa de la sesión y de su contenido asociado."
    >
      <div style={{ marginBottom: 'var(--espacio-4)' }}>
        <Boton variante="volver" onClick={() => navegar(rutaListado)}>
          ← Volver a sesiones
        </Boton>
      </div>

      {estado === 'cargando' && (
        <section className="seccion">
          <p className="detalle-mensaje-vacio">Cargando detalle de la sesión…</p>
        </section>
      )}

      {estado === 'error' && (
        <section className="seccion">
          <Alerta tono="error">
            {mensajeError ?? 'No se pudo cargar el detalle de la sesión.'}
          </Alerta>
        </section>
      )}

      {estado === 'listo' && sesion && (
        <>
          {/* Cabecera con nombre + badge de estado. */}
          <section className="seccion">
            <div className="detalle-sesion-cabecera">
              <div>
                <h2>{sesion.nombre}</h2>
                <p>Identificador: {sesion.id}</p>
              </div>
              <BadgeEstadoSesion estado={sesion.estado} />
            </div>

            <div className="detalle-grilla">
              <div className="detalle-campo">
                <span className="detalle-campo-etiqueta">Tipo de juego</span>
                <span className="detalle-campo-valor">{nombreTipoJuego(sesion.tipoJuego)}</span>
              </div>
              <div className="detalle-campo">
                <span className="detalle-campo-etiqueta">Modo</span>
                <span className="detalle-campo-valor">{sesion.modo}</span>
              </div>
              <div className="detalle-campo">
                <span className="detalle-campo-etiqueta">Estado</span>
                <span className="detalle-campo-valor">
                  <BadgeEstadoSesion estado={sesion.estado} />
                </span>
              </div>
              <div className="detalle-campo">
                <span className="detalle-campo-etiqueta">Fecha programada</span>
                <span className="detalle-campo-valor">{formatearFechaSesion(sesion.fechaProgramada)}</span>
              </div>
              <div className="detalle-campo">
                <span className="detalle-campo-etiqueta">Fecha de creación</span>
                <span className="detalle-campo-valor">{formatearFechaSesion(sesion.fechaCreacion)}</span>
              </div>
              <div className="detalle-campo">
                <span className="detalle-campo-etiqueta">Id del contenido</span>
                <span className="detalle-campo-valor">{sesion.contenidoJuegoId}</span>
              </div>
            </div>
          </section>

          {/* Contenido asociado: Trivia. */}
          {sesion.trivia && (
            <section className="seccion">
              <div className="detalle-subtitulo">
                <div>
                  <h3>Trivia: {sesion.trivia.nombre}</h3>
                  {sesion.trivia.descripcion && (
                    <p>{sesion.trivia.descripcion}</p>
                  )}
                </div>
                <span className="badge badge-md badge-sesion-activa">
                  {sesion.trivia.estado}
                </span>
              </div>

              <div className="detalle-subtitulo">
                <div>
                  <h3>Preguntas</h3>
                  <p>{sesion.trivia.preguntas.length} pregunta(s)</p>
                </div>
              </div>

              {sesion.trivia.preguntas.length === 0 ? (
                <p className="detalle-mensaje-vacio">
                  Esta trivia no tiene preguntas cargadas.
                </p>
              ) : (
                <div className="lista-preguntas">
                  {sesion.trivia.preguntas.map((p, indice) => (
                    <article key={p.id} className="pregunta-card">
                      <div className="pregunta-card-cabecera">
                        <div className="pregunta-card-info">
                          <span className="pregunta-numero">Pregunta {indice + 1}</span>
                        </div>
                        <span className="pregunta-puntaje">{p.puntajeAsignado} pts</span>
                      </div>
                      <p className="pregunta-enunciado">{p.enunciado}</p>
                      <ul className="pregunta-opciones">
                        {p.opciones.map(o => (
                          <li
                            key={o.id}
                            className={`pregunta-opcion${o.esCorrecta ? ' pregunta-opcion-correcta' : ''}`}
                          >
                            {o.esCorrecta && (
                              <span className="opcion-check-icono" aria-hidden="true">✓</span>
                            )}
                            <span style={{ flex: 1 }}>{o.texto}</span>
                            {o.esCorrecta && (
                              <span className="badge badge-sesion-activa">Correcta</span>
                            )}
                          </li>
                        ))}
                      </ul>
                    </article>
                  ))}
                </div>
              )}
            </section>
          )}

          {/* Contenido asociado: Búsqueda del Tesoro. */}
          {sesion.busquedaTesoro && (
            <section className="seccion">
              <div className="detalle-subtitulo">
                <div>
                  <h3>Búsqueda del Tesoro: {sesion.busquedaTesoro.nombre}</h3>
                  {sesion.busquedaTesoro.descripcion && (
                    <p>{sesion.busquedaTesoro.descripcion}</p>
                  )}
                </div>
                <span className="badge badge-md badge-sesion-activa">
                  {sesion.busquedaTesoro.estado}
                </span>
              </div>

              <div className="detalle-subtitulo">
                <div>
                  <h3>Etapas</h3>
                  <p>{sesion.busquedaTesoro.etapas.length} etapa(s)</p>
                </div>
              </div>

              {sesion.busquedaTesoro.etapas.length === 0 ? (
                <p className="detalle-mensaje-vacio">
                  Esta búsqueda no tiene etapas cargadas.
                </p>
              ) : (
                <div className="lista-etapas">
                  {sesion.busquedaTesoro.etapas.map((e, indice) => (
                    <article key={e.id} className="etapa-card">
                      <div className="etapa-card-cabecera">
                        <span className="etapa-numero">Etapa {e.orden || indice + 1}</span>
                        <span className="etapa-nombre">{e.nombre}</span>
                      </div>
                      {e.descripcion && (
                        <p className="etapa-descripcion">{e.descripcion}</p>
                      )}
                      {e.pistas.length === 0 ? (
                        <p className="detalle-mensaje-vacio">Sin pistas registradas.</p>
                      ) : (
                        <ul className="lista-pistas">
                          {e.pistas.map((p, idxPista) => (
                            <li key={p.id} className="pista-item">
                              <span className="pista-orden">{p.orden || idxPista + 1}</span>
                              <span>{p.texto}</span>
                            </li>
                          ))}
                        </ul>
                      )}
                    </article>
                  ))}
                </div>
              )}
            </section>
          )}
        </>
      )}
    </LayoutPanel>
  )
}
