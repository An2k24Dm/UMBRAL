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
import {
  obtenerDetalleMision,
  obtenerDetalleTrivia,
  obtenerDetalleBusqueda,
  type MisionDetalleDto,
  type TriviaDetalleDto,
  type BusquedaTesoroDetalleDto
} from '../autenticacion/clienteApiJuegos'
import { usarAutenticacion } from '../autenticacion/ProveedorAutenticacion'
import {
  formatearFechaSesion,
  nombreModoSesion,
  nombreTipoContenidoEtapa
} from '../utilidades/formatoSesiones'

interface EtapaEnriquecida {
  id: string
  orden: number
  tipoModoDeJuego: string
  modoDeJuegoId: string
  nombreModoDeJuego: string
  tiempoEstimado: number
  trivia?: TriviaDetalleDto
  busqueda?: BusquedaTesoroDetalleDto
  errorContenido?: string
}

interface MisionEnriquecida {
  id: string
  orden: number
  mision?: MisionDetalleDto
  etapas: EtapaEnriquecida[]
  cargando: boolean
  error?: string
}

function formatearDuracionSegundos(segundos: number | null | undefined): string {
  if (segundos === null || segundos === undefined || Number.isNaN(segundos)) {
    return '—'
  }

  if (segundos < 60) {
    return `${segundos} s`
  }

  const minutos = Math.floor(segundos / 60)
  const segundosRestantes = segundos % 60

  if (segundosRestantes === 0) {
    return `${minutos} min`
  }

  return `${minutos} min ${segundosRestantes} s`
}

export function PaginaDetalleSesion() {
  const { id } = useParams<{ id: string }>()
  const { token, usuario } = usarAutenticacion()
  const navegar = useNavigate()
  const rutaListado = usuario?.rol === 'Administrador'
    ? '/administrador/sesiones'
    : '/operador/sesiones'
  const rutaBaseEquipos = `${rutaListado}/${id}/equipos`

  const [estado, setEstado] = useState<'cargando' | 'error' | 'listo'>('cargando')
  const [mensajeError, setMensajeError] = useState<string | null>(null)
  const [sesion, setSesion] = useState<SesionDetalleDto | null>(null)
  const [misiones, setMisiones] = useState<MisionEnriquecida[]>([])
  const [misionAbierta, setMisionAbierta] = useState<string | null>(null)

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

        const inicial: MisionEnriquecida[] = detalle.misiones
          .slice()
          .sort((a, b) => a.orden - b.orden)
          .map(m => ({ id: m.misionId, orden: m.orden, etapas: [], cargando: true }))
        setMisiones(inicial)
        if (inicial.length > 0) setMisionAbierta(inicial[0].id)

        await Promise.all(inicial.map(slot => cargarMisionConEtapas(slot.id, ref)))
      } catch (e) {
        if (ref.cancelado) return
        setMensajeError(e instanceof Error ? e.message : 'No se pudo cargar el detalle de la sesión.')
        setEstado('error')
      }
    }

    async function cargarMisionConEtapas(misionId: string, ref: { cancelado: boolean }) {
      try {
        const detalleMision = await obtenerDetalleMision(misionId, token!)
        if (ref.cancelado) return

        const etapas: EtapaEnriquecida[] = detalleMision.etapas
          .slice()
          .sort((a, b) => a.orden - b.orden)
          .map(e => ({
            id: e.id,
            orden: e.orden,
            tipoModoDeJuego: e.tipoModoDeJuego,
            modoDeJuegoId: e.modoDeJuegoId,
            nombreModoDeJuego: e.nombreModoDeJuego,
            tiempoEstimado: e.tiempoEstimado
          }))

        // Resolvemos en paralelo el contenido de cada etapa.
        const etapasEnriquecidas = await Promise.all(etapas.map(async (etapa) => {
          try {
            if (etapa.tipoModoDeJuego === 'Trivia') {
              const trivia = await obtenerDetalleTrivia(etapa.modoDeJuegoId, token!)
              return { ...etapa, trivia }
            }
            if (etapa.tipoModoDeJuego === 'BusquedaTesoro') {
              const busqueda = await obtenerDetalleBusqueda(etapa.modoDeJuegoId, token!)
              return { ...etapa, busqueda }
            }
            return etapa
          } catch (e) {
            return {
              ...etapa,
              errorContenido: e instanceof Error
                ? e.message
                : 'No fue posible cargar el contenido de la etapa.'
            }
          }
        }))

        if (ref.cancelado) return
        setMisiones(prev => prev.map(m => m.id === misionId
          ? { ...m, mision: detalleMision, etapas: etapasEnriquecidas, cargando: false }
          : m))
      } catch (e) {
        if (ref.cancelado) return
        setMisiones(prev => prev.map(m => m.id === misionId
          ? {
              ...m,
              cargando: false,
              error: e instanceof Error ? e.message : 'No se pudo cargar la misión.'
            }
          : m))
      }
    }

    cargar()
    return () => { ref.cancelado = true }
  }, [token, id])

  if (estado === 'cargando') {
    return (
      <LayoutPanel titulo="Detalle de sesión" descripcion="Cargando…">
        <section className="seccion">
          <p className="detalle-mensaje-vacio">Cargando detalle de la sesión…</p>
        </section>
      </LayoutPanel>
    )
  }

  if (estado === 'error' || !sesion) {
    return (
      <LayoutPanel titulo="Detalle de sesión" descripcion="">
        <div style={{ marginBottom: 'var(--espacio-4)' }}>
          <Boton variante="volver" onClick={() => navegar(rutaListado)}>
            ← Volver a sesiones
          </Boton>
        </div>
        <section className="seccion">
          <Alerta tono="error">
            {mensajeError ?? 'No se pudo cargar el detalle de la sesión.'}
          </Alerta>
        </section>
      </LayoutPanel>
    )
  }

  const esGrupal = sesion.modo === 'Grupal'

  return (
    <LayoutPanel
      titulo="Detalle de sesión"
      descripcion="Información completa de la sesión y de sus misiones asociadas."
    >
      <div style={{ marginBottom: 'var(--espacio-4)' }}>
        <Boton variante="volver" onClick={() => navegar(rutaListado)}>
          ← Volver a sesiones
        </Boton>
      </div>

      {/* Cabecera con nombre + badge de estado + código de acceso */}
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
            <span className="detalle-campo-etiqueta">Descripción</span>
            <span className="detalle-campo-valor">{sesion.descripcion || '—'}</span>
          </div>
          <div className="detalle-campo">
            <span className="detalle-campo-etiqueta">Modo</span>
            <span className="detalle-campo-valor">{nombreModoSesion(sesion.modo)}</span>
          </div>
          <div className="detalle-campo">
            <span className="detalle-campo-etiqueta">Código de acceso</span>
            <span className="detalle-campo-valor" style={{ fontFamily: 'monospace', fontSize: '1.05rem' }}>
              {sesion.codigoAcceso || '—'}
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
          {sesion.fechaInicioUtc && (
            <div className="detalle-campo">
              <span className="detalle-campo-etiqueta">Inicio</span>
              <span className="detalle-campo-valor">{formatearFechaSesion(sesion.fechaInicioUtc)}</span>
            </div>
          )}
          {sesion.fechaFinalizacionUtc && (
            <div className="detalle-campo">
              <span className="detalle-campo-etiqueta">Finalización</span>
              <span className="detalle-campo-valor">{formatearFechaSesion(sesion.fechaFinalizacionUtc)}</span>
            </div>
          )}
        </div>
      </section>

      {/* Misiones asociadas con acordeón por misión */}
      <section className="seccion">
        <div className="detalle-subtitulo">
          <div>
            <h3>Misiones asociadas</h3>
            <p>{misiones.length} misión(es) ordenadas según su orden de ejecución.</p>
          </div>
        </div>

        {misiones.length === 0 && (
          <p className="detalle-mensaje-vacio">Esta sesión no tiene misiones asociadas.</p>
        )}

        <div className="lista-misiones-detalle">
          {misiones.map((m) => {
            const abierta = misionAbierta === m.id
            return (
              <article key={m.id} className="mision-card">
                <button
                  type="button"
                  className="mision-card-cabecera"
                  onClick={() => setMisionAbierta(abierta ? null : m.id)}
                  aria-expanded={abierta}
                >
                  <div>
                    <strong>
                      {m.orden}. {m.mision?.nombre ?? (m.cargando ? 'Cargando…' : `Misión ${m.id}`)}
                    </strong>
                    {m.mision?.descripcion && (
                      <p style={{ margin: '4px 0 0', fontSize: '0.85rem', opacity: 0.8 }}>
                        {m.mision.descripcion}
                      </p>
                    )}
                    {m.mision && (
                      <small>
                        Dificultad: {m.mision.dificultad} ·
                        Tiempo total: {formatearDuracionSegundos(m.mision.tiempoTotal)} ·
                        Etapas: {m.mision.etapas.length} ·
                        Estado: {m.mision.estado}
                      </small>
                    )}
                  </div>
                  <span aria-hidden="true">{abierta ? '▾' : '▸'}</span>
                </button>

                {abierta && (
                  <div className="mision-card-cuerpo">
                    {m.cargando && <p className="detalle-mensaje-vacio">Cargando misión…</p>}
                    {m.error && <Alerta tono="error">{m.error}</Alerta>}
                    {!m.cargando && !m.error && m.etapas.length === 0 && (
                      <p className="detalle-mensaje-vacio">Esta misión no tiene etapas.</p>
                    )}
                    {m.etapas.map(etapa => (
                      <div key={etapa.id} className="etapa-card">
                        <div className="detalle-subtitulo">
                          <div>
                            <h4>Etapa {etapa.orden}: {etapa.nombreModoDeJuego}</h4>
                            <small>
                              Tipo de contenido: {nombreTipoContenidoEtapa(etapa.tipoModoDeJuego)} ·
                              Tiempo estimado: {formatearDuracionSegundos(etapa.tiempoEstimado)}
                            </small>
                          </div>
                        </div>

                        {etapa.errorContenido && (
                          <Alerta tono="error">{etapa.errorContenido}</Alerta>
                        )}

                        {etapa.trivia && <BloqueTrivia trivia={etapa.trivia} />}
                        {etapa.busqueda && <BloqueBusqueda busqueda={etapa.busqueda} />}
                        {!etapa.trivia && !etapa.busqueda && !etapa.errorContenido && (
                          <p className="detalle-mensaje-vacio">Cargando contenido de la etapa…</p>
                        )}
                      </div>
                    ))}
                  </div>
                )}
              </article>
            )
          })}
        </div>
      </section>

      {/* Participantes individuales (modo Individual) */}
      {!esGrupal && (
        <section className="seccion">
          <div className="detalle-subtitulo">
            <div>
              <h3>Participantes individuales</h3>
              <p>Participantes: {sesion.participantesIndividuales.length} / 10</p>
            </div>
          </div>
          {sesion.participantesIndividuales.length === 0 ? (
            <p className="detalle-mensaje-vacio">
              Aún no hay participantes unidos a esta sesión.
            </p>
          ) : (
            <table className="tabla-usuarios">
              <thead>
                <tr>
                  <th>#</th>
                  <th>Participante</th>
                  <th>Fecha de unión</th>
                </tr>
              </thead>
              <tbody>
                {sesion.participantesIndividuales.map((p, idx) => (
                  <tr key={p.id}>
                    <td>{idx + 1}</td>
                    {/* El backend hoy sólo expone el id. Cuando identidad-servicio
                        sirva alias/nombre por id se enriquece aquí. */}
                    <td>{p.participanteId}</td>
                    <td>{formatearFechaSesion(p.fechaUnion)}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          )}
        </section>
      )}

      {/* Equipos (modo Grupal) */}
      {esGrupal && (
        <section className="seccion">
          <div className="detalle-subtitulo">
            <div>
              <h3>Equipos</h3>
              <p>Equipos: {sesion.equipos.length} / 5</p>
            </div>
          </div>
          {sesion.equipos.length === 0 ? (
            <p className="detalle-mensaje-vacio">
              Aún no hay equipos unidos a esta sesión.
            </p>
          ) : (
            <table className="tabla-usuarios">
              <thead>
                <tr>
                  <th>#</th>
                  <th>Nombre del equipo</th>
                  <th>Integrantes</th>
                  <th>Puntaje actual</th>
                  <th>Fecha de creación</th>
                  <th>Acciones</th>
                </tr>
              </thead>
              <tbody>
                {sesion.equipos.map((eq, idx) => (
                  <tr key={eq.id}>
                    <td>{idx + 1}</td>
                    <td>{eq.nombre}</td>
                    <td>{eq.participantes.length} / 2</td>
                    <td>{eq.puntajeActual}</td>
                    <td>{formatearFechaSesion(eq.fechaCreacion)}</td>
                    <td>
                      <Boton
                        variante="secundario"
                        tamaño="sm"
                        onClick={() => navegar(`${rutaBaseEquipos}/${eq.id}`)}
                      >
                        Ver equipo
                      </Boton>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          )}
        </section>
      )}
    </LayoutPanel>
  )
}

function BloqueTrivia({ trivia }: { trivia: TriviaDetalleDto }) {
  return (
    <div>
      <p style={{ marginTop: 12 }}>
        <strong>Trivia:</strong> {trivia.nombre}
        {trivia.descripcion && <span> — {trivia.descripcion}</span>}
      </p>
      {trivia.preguntas.length === 0 ? (
        <p className="detalle-mensaje-vacio">Esta trivia no tiene preguntas cargadas.</p>
      ) : (
        <div className="lista-preguntas">
          {trivia.preguntas.map((p, idx) => (
            <article key={p.id} className="pregunta-card">
              <div className="pregunta-card-cabecera">
                <div className="pregunta-card-info">
                  <span className="pregunta-numero">Pregunta {idx + 1}</span>
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
    </div>
  )
}

function BloqueBusqueda({ busqueda }: { busqueda: BusquedaTesoroDetalleDto }) {
  return (
    <div>
      <p style={{ marginTop: 12 }}>
        <strong>Búsqueda del Tesoro:</strong> {busqueda.nombre}
        {busqueda.descripcion && <span> — {busqueda.descripcion}</span>}
      </p>
      {busqueda.pistas.length === 0 ? (
        <p className="detalle-mensaje-vacio">Esta búsqueda no tiene pistas registradas.</p>
      ) : (
        <ul className="lista-pistas">
          {busqueda.pistas.map((p, idx) => (
            <li key={p.id} className="pista-item">
              <span className="pista-orden">{idx + 1}</span>
              <span>{p.contenido}</span>
            </li>
          ))}
        </ul>
      )}
    </div>
  )
}
