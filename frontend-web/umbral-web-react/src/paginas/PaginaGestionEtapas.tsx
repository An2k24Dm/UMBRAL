import { useEffect, useState } from 'react'
import { useNavigate, useParams } from 'react-router-dom'
import { LayoutPanel } from '../componentes/LayoutPanel'
import { Alerta } from '../componentes/Alerta'
import { Boton } from '../componentes/Boton'
import { CampoFormulario } from '../componentes/CampoFormulario'
import { MapaLeaflet } from '../componentes/MapaLeaflet'
import {
  obtenerDetalleBusqueda,
  activarBusqueda,
  modificarBusquedaTesoro,
  agregarPista,
  modificarPista,
  eliminarPista,
  type BusquedaTesoroDetalleDto,
  type PistaDetalleDto
} from '../autenticacion/clienteApiJuegos'
import { usarAutenticacion } from '../autenticacion/ProveedorAutenticacion'

const PUNTAJE_OPCIONES = Array.from({ length: 20 }, (_, i) => (i + 1) * 5)

type TipoPista = 'Texto' | 'CoordenadaGps'


export function PaginaGestionEtapas() {
  const { busquedaId } = useParams<{ busquedaId: string }>()
  const { token, usuario } = usarAutenticacion()
  const navegar = useNavigate()
  const rutaBase = usuario?.rol === 'Administrador' ? '/administrador/busquedas' : '/operador/busquedas'

  const [busqueda, setBusqueda] = useState<BusquedaTesoroDetalleDto | null>(null)
  const [estadoCarga, setEstadoCarga] = useState<'cargando' | 'error' | 'listo'>('cargando')
  const [errorCarga, setErrorCarga] = useState<string | null>(null)

  const [activando, setActivando] = useState(false)
  const [errorActivacion, setErrorActivacion] = useState<string | null>(null)

  const [mostrarFormEditar, setMostrarFormEditar] = useState(false)
  const [formEditar, setFormEditar] = useState({ nombre: '', descripcion: '', tiempo: '5', puntaje: '5' })
  const [errorFormEditar, setErrorFormEditar] = useState<string | null>(null)
  const [enviandoEditar, setEnviandoEditar] = useState(false)

  // Estado del formulario "agregar pista"
  const [mostrarFormPista, setMostrarFormPista] = useState(false)
  const [formPistaContenido, setFormPistaContenido] = useState('')
  const [formPistaTipo, setFormPistaTipo] = useState<TipoPista>('Texto')
  const [formPistaLatitud, setFormPistaLatitud] = useState<number | undefined>(undefined)
  const [formPistaLongitud, setFormPistaLongitud] = useState<number | undefined>(undefined)
  const [errorFormPista, setErrorFormPista] = useState<string | null>(null)
  const [enviandoPista, setEnviandoPista] = useState(false)

  // Estado del formulario "editar pista"
  const [eliminandoPistaId, setEliminandoPistaId] = useState<string | null>(null)
  const [pistaEnEdicion, setPistaEnEdicion] = useState<string | null>(null)
  const [formEdicionPista, setFormEdicionPista] = useState('')
  const [formEdicionTipo, setFormEdicionTipo] = useState<TipoPista>('Texto')
  const [formEdicionLatitud, setFormEdicionLatitud] = useState<number | undefined>(undefined)
  const [formEdicionLongitud, setFormEdicionLongitud] = useState<number | undefined>(undefined)
  const [errorEdicionPista, setErrorEdicionPista] = useState<string | null>(null)
  const [enviandoEdicionPista, setEnviandoEdicionPista] = useState(false)

  useEffect(() => {
    if (!busquedaId) return
    let cancelado = false
    async function cargar() {
      if (!token) { setEstadoCarga('error'); setErrorCarga('Debe iniciar sesión.'); return }
      setEstadoCarga('cargando')
      try {
        const datos = await obtenerDetalleBusqueda(busquedaId!, token)
        if (cancelado) return
        setBusqueda(datos)
        setEstadoCarga('listo')
      } catch (e) {
        if (cancelado) return
        setErrorCarga(e instanceof Error ? e.message : 'No fue posible cargar la búsqueda.')
        setEstadoCarga('error')
      }
    }
    cargar()
    return () => { cancelado = true }
  }, [busquedaId, token])

  async function recargar() {
    if (!token || !busquedaId) return
    const datos = await obtenerDetalleBusqueda(busquedaId, token)
    setBusqueda(datos)
  }

  function abrirFormEditar() {
    if (!busqueda) return
    setFormEditar({
      nombre: busqueda.nombre,
      descripcion: busqueda.descripcion,
      tiempo: String(Math.max(busqueda.tiempo ?? 5, 5)),
      puntaje: String(Math.max(busqueda.puntaje ?? 5, 5))
    })
    setErrorFormEditar(null)
    setMostrarFormEditar(true)
  }

  async function manejarGuardarEdicion(e: React.FormEvent) {
    e.preventDefault()
    if (!formEditar.nombre.trim()) { setErrorFormEditar('El nombre es obligatorio.'); return }
    if (!formEditar.descripcion.trim()) { setErrorFormEditar('La descripción es obligatoria.'); return }
    const tiempoEditar = Number(formEditar.tiempo)
    if (isNaN(tiempoEditar) || tiempoEditar < 5) { setErrorFormEditar('El tiempo debe ser al menos 5 minutos.'); return }
    if (tiempoEditar > 60) { setErrorFormEditar('El tiempo no puede superar 60 minutos.'); return }
    if (!token || !busquedaId) return
    setEnviandoEditar(true); setErrorFormEditar(null)
    try {
      await modificarBusquedaTesoro(busquedaId, {
        nombre: formEditar.nombre.trim(),
        descripcion: formEditar.descripcion.trim(),
        tiempo: Number(formEditar.tiempo) || 0,
        puntaje: Number(formEditar.puntaje) || 0
      }, token)
      await recargar()
      setMostrarFormEditar(false)
    } catch (err) { setErrorFormEditar(err instanceof Error ? err.message : 'Error al guardar los cambios.') }
    finally { setEnviandoEditar(false) }
  }

  async function manejarActivar() {
    if (!token || !busquedaId) return
    setActivando(true); setErrorActivacion(null)
    try { await activarBusqueda(busquedaId, token); await recargar() }
    catch (err) { setErrorActivacion(err instanceof Error ? err.message : 'Error al activar.') }
    finally { setActivando(false) }
  }

  function resetFormPista() {
    setFormPistaContenido('')
    setFormPistaTipo('Texto')
    setFormPistaLatitud(undefined)
    setFormPistaLongitud(undefined)
    setErrorFormPista(null)
  }

  async function manejarEnvioPista(e: React.FormEvent) {
    e.preventDefault()
    if (formPistaTipo === 'Texto' && !formPistaContenido.trim()) {
      setErrorFormPista('El contenido es obligatorio.'); return
    }
    if (formPistaTipo === 'CoordenadaGps' && (formPistaLatitud == null || formPistaLongitud == null)) {
      setErrorFormPista('Haga clic en el mapa para fijar la ubicación GPS.'); return
    }
    if (!token || !busquedaId) return
    setEnviandoPista(true); setErrorFormPista(null)
    try {
      await agregarPista(busquedaId, {
        contenido: formPistaTipo === 'Texto' ? formPistaContenido.trim() : undefined,
        tipo: formPistaTipo,
        latitud: formPistaLatitud,
        longitud: formPistaLongitud
      }, token)
      await recargar()
      setMostrarFormPista(false)
      resetFormPista()
    } catch (err) { setErrorFormPista(err instanceof Error ? err.message : 'Error al agregar la pista.') }
    finally { setEnviandoPista(false) }
  }

  async function manejarEliminarPista(pistaId: string) {
    if (!token || !busquedaId) return
    setEliminandoPistaId(pistaId)
    try { await eliminarPista(busquedaId, pistaId, token); await recargar() }
    catch (err) { setErrorCarga(err instanceof Error ? err.message : 'Error al eliminar la pista.') }
    finally { setEliminandoPistaId(null) }
  }

  function abrirEdicionPista(pista: PistaDetalleDto) {
    setPistaEnEdicion(pista.id)
    setFormEdicionPista(pista.contenido)
    setFormEdicionTipo(pista.tipo ?? 'Texto')
    setFormEdicionLatitud(pista.latitud)
    setFormEdicionLongitud(pista.longitud)
    setErrorEdicionPista(null)
  }

  async function manejarEnvioEdicionPista(e: React.FormEvent, pistaId: string) {
    e.preventDefault()
    if (formEdicionTipo === 'Texto' && !formEdicionPista.trim()) {
      setErrorEdicionPista('El contenido es obligatorio.'); return
    }
    if (formEdicionTipo === 'CoordenadaGps' && (formEdicionLatitud == null || formEdicionLongitud == null)) {
      setErrorEdicionPista('Haga clic en el mapa para fijar la ubicación GPS.'); return
    }
    if (!token || !busquedaId) return
    setEnviandoEdicionPista(true); setErrorEdicionPista(null)
    try {
      await modificarPista(busquedaId, pistaId, {
        nuevoContenido: formEdicionTipo === 'Texto' ? formEdicionPista.trim() : undefined,
        tipo: formEdicionTipo,
        latitud: formEdicionLatitud,
        longitud: formEdicionLongitud
      }, token)
      await recargar(); setPistaEnEdicion(null)
    } catch (err) { setErrorEdicionPista(err instanceof Error ? err.message : 'Error al modificar la pista.') }
    finally { setEnviandoEdicionPista(false) }
  }

  if (estadoCarga === 'cargando') {
    return (
      <LayoutPanel titulo="Pistas" descripcion="Cargando búsqueda…">
        <p className="tabla-estado-mensaje">Cargando…</p>
      </LayoutPanel>
    )
  }

  if (estadoCarga === 'error' || !busqueda) {
    return (
      <LayoutPanel titulo="Pistas" descripcion="Error al cargar.">
        <Alerta tono="error">{errorCarga ?? 'Error desconocido.'}</Alerta>
        <Boton variante="volver" onClick={() => navegar(rutaBase)}>Volver</Boton>
      </LayoutPanel>
    )
  }

  const esInactiva = busqueda.estado === 'Inactiva'
  const yaHayPistaGps = busqueda.pistas.some(p => p.tipo === 'CoordenadaGps')

  return (
    <LayoutPanel titulo={busqueda.nombre} descripcion="Pistas de ayuda de la búsqueda del tesoro">
      <section className="seccion">
        <div className="seccion-cabecera">
          <div>
            <h2>{busqueda.nombre}</h2>
            <p>{busqueda.descripcion}</p>
            <div style={{ display: 'flex', gap: 12, marginTop: 4, alignItems: 'center' }}>
              <span className={`estado-badge estado-badge-${busqueda.estado.toLowerCase()}`}>{busqueda.estado}</span>
              {busqueda.tiempo > 0 && <span className="trivia-card-meta">{busqueda.tiempo} min</span>}
              {busqueda.puntaje > 0 && <span className="trivia-card-meta">{busqueda.puntaje} pts</span>}
            </div>
          </div>
          <div className="cabecera-pagina-acciones">
            <Boton variante="volver" onClick={() => navegar(rutaBase)}>Volver</Boton>
            {esInactiva && !mostrarFormEditar && !mostrarFormPista && (
              <Boton variante="fantasma" onClick={abrirFormEditar}>Editar</Boton>
            )}
            {esInactiva && !mostrarFormEditar && !mostrarFormPista && (
              <Boton variante="secundario" onClick={() => { resetFormPista(); setMostrarFormPista(true) }}>
                + Agregar pista
              </Boton>
            )}
            {esInactiva && !mostrarFormEditar && (
              <Boton variante="primario" onClick={manejarActivar} disabled={activando || !yaHayPistaGps}
                title={!yaHayPistaGps ? 'Debe agregar la coordenada GPS del tesoro antes de activar' : undefined}>
                {activando ? 'Activando…' : 'Activar búsqueda'}
              </Boton>
            )}
          </div>
        </div>

        {busqueda.estado === 'Activa' && (
          <div className="ayuda-modo-sesion" role="note">
            <span className="ayuda-modo-sesion-icono" aria-hidden="true">ⓘ</span>
            <span>Esta búsqueda del tesoro está activa y no puede modificarse.</span>
          </div>
        )}

        {errorCarga && <Alerta tono="error">{errorCarga}</Alerta>}
        {errorActivacion && <Alerta tono="error">{errorActivacion}</Alerta>}
        {esInactiva && !yaHayPistaGps && (
          <Alerta tono="aviso">Agregue la coordenada GPS del tesoro antes de activar la búsqueda.</Alerta>
        )}

        {mostrarFormEditar && (
          <div className="formulario-pregunta-panel">
            <h3 className="formulario-pregunta-titulo">Editar búsqueda del tesoro</h3>
            {errorFormEditar && <Alerta tono="error">{errorFormEditar}</Alerta>}
            <form onSubmit={manejarGuardarEdicion} noValidate>
              <CampoFormulario etiqueta="Nombre" htmlFor="edit-busqueda-nombre">
                <input id="edit-busqueda-nombre" type="text" maxLength={200}
                  value={formEditar.nombre}
                  onChange={(e) => setFormEditar(p => ({ ...p, nombre: e.target.value }))}
                  disabled={enviandoEditar} />
              </CampoFormulario>
              <CampoFormulario etiqueta="Descripción" htmlFor="edit-busqueda-desc">
                <textarea id="edit-busqueda-desc" rows={3} maxLength={1000}
                  value={formEditar.descripcion}
                  onChange={(e) => setFormEditar(p => ({ ...p, descripcion: e.target.value }))}
                  disabled={enviandoEditar} />
              </CampoFormulario>
              <CampoFormulario etiqueta="Tiempo estimado (minutos)" htmlFor="edit-busqueda-tiempo">
                <input id="edit-busqueda-tiempo" type="number" min={5} step={5} max={60}
                  value={formEditar.tiempo}
                  onChange={(e) => setFormEditar(p => ({ ...p, tiempo: e.target.value }))}
                  disabled={enviandoEditar} placeholder="Ej. 15" />
              </CampoFormulario>
              <CampoFormulario etiqueta="Puntaje" htmlFor="edit-busqueda-puntaje">
                <select id="edit-busqueda-puntaje"
                  value={formEditar.puntaje}
                  onChange={(e) => setFormEditar(p => ({ ...p, puntaje: e.target.value }))}
                  disabled={enviandoEditar}>
                  {PUNTAJE_OPCIONES.map((pts) => (
                    <option key={pts} value={String(pts)}>{pts} pts</option>
                  ))}
                </select>
              </CampoFormulario>
              <div className="acciones-formulario-trivia">
                <Boton variante="volver" type="button"
                  onClick={() => setMostrarFormEditar(false)} disabled={enviandoEditar}>Cancelar</Boton>
                <Boton variante="primario" type="submit" disabled={enviandoEditar}>
                  {enviandoEditar ? 'Guardando…' : 'Guardar cambios'}
                </Boton>
              </div>
            </form>
          </div>
        )}

        {/* Formulario: agregar pista */}
        {mostrarFormPista && (
          <div className="formulario-pregunta-panel">
            <h3 className="formulario-pregunta-titulo">Nueva pista de ayuda</h3>
            {errorFormPista && <Alerta tono="error">{errorFormPista}</Alerta>}
            <form onSubmit={manejarEnvioPista} noValidate>
              <CampoFormulario etiqueta="Tipo de pista" htmlFor="pista-tipo">
                <select id="pista-tipo" value={formPistaTipo}
                  onChange={(e) => {
                    setFormPistaTipo(e.target.value as TipoPista)
                    setFormPistaLatitud(undefined)
                    setFormPistaLongitud(undefined)
                    setErrorFormPista(null)
                  }}
                  disabled={enviandoPista}>
                  <option value="Texto">Texto libre</option>
                  <option value="CoordenadaGps" disabled={yaHayPistaGps}>
                    Coordenada GPS (mapa interactivo){yaHayPistaGps ? ' — ya definida' : ''}
                  </option>
                </select>
              </CampoFormulario>

              {formPistaTipo === 'Texto' ? (
                <CampoFormulario etiqueta="Contenido" htmlFor="pista-contenido"
                  ayuda="Texto de ayuda que el Operador puede liberar a los participantes durante la sesión.">
                  <textarea id="pista-contenido" rows={3} maxLength={1000}
                    value={formPistaContenido}
                    onChange={(e) => { setFormPistaContenido(e.target.value); setErrorFormPista(null) }}
                    disabled={enviandoPista}
                    placeholder="Ej. La respuesta se encuentra cerca de la entrada principal." />
                </CampoFormulario>
              ) : (
                <div style={{ marginBottom: 16 }}>
                  <p style={{ fontSize: '0.85rem', color: 'var(--color-texto-tenue)', marginBottom: 8 }}>
                    Haga clic en el mapa para fijar la ubicación exacta del tesoro.
                    {formPistaLatitud != null && formPistaLongitud != null && (
                      <span style={{ marginLeft: 8, color: 'var(--color-exito, #22c55e)', fontWeight: 600 }}>
                        ✓ ({formPistaLatitud.toFixed(6)}, {formPistaLongitud.toFixed(6)})
                      </span>
                    )}
                  </p>
                  <MapaLeaflet
                    latitud={formPistaLatitud}
                    longitud={formPistaLongitud}
                    onChange={(lat, lng) => {
                      setFormPistaLatitud(lat)
                      setFormPistaLongitud(lng)
                      setErrorFormPista(null)
                    }}
                    alto={380}
                  />
                </div>
              )}

              <div className="acciones-formulario-trivia">
                <Boton variante="volver" type="button"
                  onClick={() => { setMostrarFormPista(false); resetFormPista() }}
                  disabled={enviandoPista}>Cancelar</Boton>
                <Boton variante="primario" type="submit" disabled={enviandoPista}>
                  {enviandoPista ? 'Guardando…' : 'Agregar pista'}
                </Boton>
              </div>
            </form>
          </div>
        )}

        {busqueda.pistas.length === 0 && !mostrarFormPista && (
          <p className="tabla-estado-mensaje">
            Esta búsqueda no tiene pistas.
            {esInactiva && ' Haga clic en "Agregar pista" para comenzar.'}
          </p>
        )}

        {busqueda.pistas.length > 0 && (
          <div style={{ marginTop: 12 }}>
            <strong style={{ fontSize: '0.85rem' }}>
              Pistas de ayuda ({busqueda.pistas.length}):
            </strong>
            <ul className="pregunta-opciones" style={{ marginTop: 8 }}>
              {busqueda.pistas.map((pista) => (
                <li key={pista.id} className="pregunta-opcion">
                  {pistaEnEdicion === pista.id ? (
                    <div className="formulario-pregunta-panel" style={{ marginTop: 4 }}>
                      <h5 className="formulario-pregunta-titulo">Editar pista</h5>
                      {errorEdicionPista && <Alerta tono="error">{errorEdicionPista}</Alerta>}
                      <form onSubmit={(e) => manejarEnvioEdicionPista(e, pista.id)} noValidate>
                        <CampoFormulario etiqueta="Tipo de pista" htmlFor={`editar-pista-tipo-${pista.id}`}>
                          <select id={`editar-pista-tipo-${pista.id}`}
                            value={formEdicionTipo}
                            onChange={(e) => {
                              setFormEdicionTipo(e.target.value as TipoPista)
                              setFormEdicionLatitud(undefined)
                              setFormEdicionLongitud(undefined)
                              setErrorEdicionPista(null)
                            }}
                            disabled={enviandoEdicionPista}>
                            <option value="Texto">Texto libre</option>
                            <option value="CoordenadaGps">Coordenada GPS (mapa interactivo)</option>
                          </select>
                        </CampoFormulario>

                        {formEdicionTipo === 'Texto' ? (
                          <CampoFormulario etiqueta="Contenido" htmlFor={`editar-pista-${pista.id}`}>
                            <textarea id={`editar-pista-${pista.id}`} rows={3} maxLength={1000}
                              value={formEdicionPista}
                              onChange={(e) => { setFormEdicionPista(e.target.value); setErrorEdicionPista(null) }}
                              disabled={enviandoEdicionPista} />
                          </CampoFormulario>
                        ) : (
                          <div style={{ marginBottom: 16 }}>
                            <p style={{ fontSize: '0.85rem', color: 'var(--color-texto-tenue)', marginBottom: 8 }}>
                              Haga clic en el mapa para fijar la nueva ubicación.
                              {formEdicionLatitud != null && formEdicionLongitud != null && (
                                <span style={{ marginLeft: 8, color: 'var(--color-exito, #22c55e)', fontWeight: 600 }}>
                                  ✓ ({formEdicionLatitud.toFixed(6)}, {formEdicionLongitud.toFixed(6)})
                                </span>
                              )}
                            </p>
                            <MapaLeaflet
                              latitud={formEdicionLatitud}
                              longitud={formEdicionLongitud}
                              onChange={(lat, lng) => {
                                setFormEdicionLatitud(lat)
                                setFormEdicionLongitud(lng)
                                setErrorEdicionPista(null)
                              }}
                              alto={320}
                            />
                          </div>
                        )}

                        <div className="acciones-formulario-trivia">
                          <Boton variante="volver" type="button"
                            onClick={() => setPistaEnEdicion(null)}
                            disabled={enviandoEdicionPista}>Cancelar</Boton>
                          <Boton variante="primario" type="submit" disabled={enviandoEdicionPista}>
                            {enviandoEdicionPista ? 'Guardando…' : 'Guardar'}
                          </Boton>
                        </div>
                      </form>
                    </div>
                  ) : (
                    <div style={{ width: '100%' }}>
                      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start', gap: 8 }}>
                        <div style={{ flex: 1 }}>
                          {pista.tipo === 'CoordenadaGps' ? (
                            <span>
                              <span style={{
                                display: 'inline-block',
                                fontSize: '0.7rem',
                                fontWeight: 700,
                                letterSpacing: 0.5,
                                padding: '1px 6px',
                                borderRadius: 4,
                                background: '#dbeafe',
                                color: '#1d4ed8',
                                marginRight: 8,
                                verticalAlign: 'middle'
                              }}>GPS</span>
                              {pista.latitud != null && pista.longitud != null
                                ? `${pista.latitud.toFixed(6)}, ${pista.longitud.toFixed(6)}`
                                : 'Coordenadas no disponibles'}
                            </span>
                          ) : (
                            <span>🔔 {pista.contenido}</span>
                          )}
                        </div>
                        {esInactiva && (
                          <div style={{ display: 'flex', gap: 4, flexShrink: 0 }}>
                            <Boton variante="secundario"
                              onClick={() => abrirEdicionPista(pista)}>
                              Editar
                            </Boton>
                            <Boton variante="peligro"
                              onClick={() => manejarEliminarPista(pista.id)}
                              disabled={eliminandoPistaId === pista.id}>
                              {eliminandoPistaId === pista.id ? 'Eliminando…' : 'Eliminar'}
                            </Boton>
                          </div>
                        )}
                      </div>
                      {pista.tipo === 'CoordenadaGps' && pista.latitud != null && pista.longitud != null && (
                        <div style={{ marginTop: 12 }}>
                          <MapaLeaflet latitud={pista.latitud} longitud={pista.longitud} alto={220} />
                        </div>
                      )}
                    </div>
                  )}
                </li>
              ))}
            </ul>
          </div>
        )}
      </section>
    </LayoutPanel>
  )
}
