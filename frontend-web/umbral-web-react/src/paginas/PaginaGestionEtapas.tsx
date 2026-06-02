import { useEffect, useState } from 'react'
import { useNavigate, useParams } from 'react-router-dom'
import { LayoutPanel } from '../componentes/LayoutPanel'
import { Alerta } from '../componentes/Alerta'
import { Boton } from '../componentes/Boton'
import { CampoFormulario } from '../componentes/CampoFormulario'
import {
  obtenerDetalleBusqueda,
  asignarMision,
  modificarMision,
  eliminarMision,
  activarBusqueda,
  agregarPista,
  modificarPista,
  eliminarPista,
  type BusquedaTesoroDetalleDto,
  type TipoMision
} from '../autenticacion/clienteApiJuegos'
import { usarAutenticacion } from '../autenticacion/ProveedorAutenticacion'

const TIPOS_MISION: { valor: TipoMision; nombre: string; etiqueta: string; descripcion: string }[] = [
  { valor: 0, nombre: 'CodigoQR',     etiqueta: 'Código QR',     descripcion: 'El participante debe escanear un código QR' },
  { valor: 1, nombre: 'PalabraClave', etiqueta: 'Palabra clave', descripcion: 'El participante debe encontrar una palabra o frase específica' },
  { valor: 2, nombre: 'Codigo',       etiqueta: 'Código',        descripcion: 'El participante debe ingresar un código alfanumérico' }
]

interface FormMision {
  titulo: string
  descripcion: string
  tipo: string
  pistaClave: string
}

interface ErroresMision {
  titulo?: string
  descripcion?: string
  pistaClave?: string
}

const FORM_MISION_VACIO: FormMision = { titulo: '', descripcion: '', tipo: '1', pistaClave: '' }

function validarMision(form: FormMision): ErroresMision {
  const err: ErroresMision = {}
  if (!form.titulo.trim()) err.titulo = 'El título es obligatorio.'
  else if (form.titulo.trim().length > 200) err.titulo = 'Máximo 200 caracteres.'
  if (!form.descripcion.trim()) err.descripcion = 'La descripción es obligatoria.'
  else if (form.descripcion.trim().length > 1000) err.descripcion = 'Máximo 1000 caracteres.'
  if (!form.pistaClave.trim()) err.pistaClave = 'La pista clave es obligatoria.'
  else if (form.pistaClave.trim().length > 500) err.pistaClave = 'Máximo 500 caracteres.'
  return err
}

export function PaginaGestionEtapas() {
  const { busquedaId } = useParams<{ busquedaId: string }>()
  const { token, usuario } = usarAutenticacion()
  const navegar = useNavigate()
  const rutaBase = usuario?.rol === 'Administrador' ? '/administrador/busquedas' : '/operador/busquedas'

  const [busqueda, setBusqueda] = useState<BusquedaTesoroDetalleDto | null>(null)
  const [estadoCarga, setEstadoCarga] = useState<'cargando' | 'error' | 'listo'>('cargando')
  const [errorCarga, setErrorCarga] = useState<string | null>(null)

  // Activar
  const [activando, setActivando] = useState(false)
  const [errorActivacion, setErrorActivacion] = useState<string | null>(null)

  // Formulario asignar misión
  const [mostrarFormMision, setMostrarFormMision] = useState(false)
  const [formMision, setFormMision] = useState<FormMision>(FORM_MISION_VACIO)
  const [erroresMision, setErroresMision] = useState<ErroresMision>({})
  const [errorFormMision, setErrorFormMision] = useState<string | null>(null)
  const [enviandoMision, setEnviandoMision] = useState(false)

  // Edición de misión
  const [modoEdicionMision, setModoEdicionMision] = useState(false)
  const [formEdicionMision, setFormEdicionMision] = useState<FormMision>(FORM_MISION_VACIO)
  const [erroresEdicionMision, setErroresEdicionMision] = useState<ErroresMision>({})
  const [errorFormEdicionMision, setErrorFormEdicionMision] = useState<string | null>(null)
  const [enviandoEdicionMision, setEnviandoEdicionMision] = useState(false)
  const [eliminandoMision, setEliminandoMision] = useState(false)

  // Pistas
  const [mostrarFormPista, setMostrarFormPista] = useState(false)
  const [formPistaContenido, setFormPistaContenido] = useState('')
  const [errorFormPista, setErrorFormPista] = useState<string | null>(null)
  const [enviandoPista, setEnviandoPista] = useState(false)
  const [eliminandoPistaId, setEliminandoPistaId] = useState<string | null>(null)
  const [pistaEnEdicion, setPistaEnEdicion] = useState<string | null>(null)
  const [formEdicionPistaContenido, setFormEdicionPistaContenido] = useState('')
  const [errorFormEdicionPista, setErrorFormEdicionPista] = useState<string | null>(null)
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

  // ---------------------------------------------------------------------------
  // Activar
  // ---------------------------------------------------------------------------
  async function manejarActivar() {
    if (!token || !busquedaId) return
    setActivando(true)
    setErrorActivacion(null)
    try { await activarBusqueda(busquedaId, token); await recargar() }
    catch (err) { setErrorActivacion(err instanceof Error ? err.message : 'Error al activar.') }
    finally { setActivando(false) }
  }

  // ---------------------------------------------------------------------------
  // Asignar misión
  // ---------------------------------------------------------------------------
  function abrirFormMision() {
    setFormMision(FORM_MISION_VACIO)
    setErroresMision({})
    setErrorFormMision(null)
    setMostrarFormMision(true)
  }

  async function manejarEnvioMision(e: React.FormEvent) {
    e.preventDefault()
    const errs = validarMision(formMision)
    if (Object.keys(errs).length > 0) { setErroresMision(errs); return }
    if (!token || !busquedaId) return
    setEnviandoMision(true)
    setErrorFormMision(null)
    try {
      await asignarMision(busquedaId, {
        titulo: formMision.titulo.trim(),
        descripcion: formMision.descripcion.trim(),
        tipo: Number(formMision.tipo) as TipoMision,
        pistaClave: formMision.pistaClave.trim()
      }, token)
      await recargar()
      setMostrarFormMision(false)
    } catch (err) {
      setErrorFormMision(err instanceof Error ? err.message : 'Error al asignar la misión.')
    } finally {
      setEnviandoMision(false)
    }
  }

  // ---------------------------------------------------------------------------
  // Editar misión
  // ---------------------------------------------------------------------------
  function abrirEdicionMision() {
    if (!busqueda?.mision) return
    const tipoActual = TIPOS_MISION.find(t => t.nombre === busqueda.mision!.tipo)?.valor ?? 1
    setFormEdicionMision({
      titulo: busqueda.mision.titulo,
      descripcion: busqueda.mision.descripcion,
      tipo: String(tipoActual),
      pistaClave: busqueda.mision.pistaClave
    })
    setErroresEdicionMision({})
    setErrorFormEdicionMision(null)
    setModoEdicionMision(true)
  }

  async function manejarEnvioEdicionMision(e: React.FormEvent) {
    e.preventDefault()
    const errs = validarMision(formEdicionMision)
    if (Object.keys(errs).length > 0) { setErroresEdicionMision(errs); return }
    if (!token || !busquedaId) return
    setEnviandoEdicionMision(true)
    setErrorFormEdicionMision(null)
    try {
      await modificarMision(busquedaId, {
        nuevoTitulo: formEdicionMision.titulo.trim(),
        nuevaDescripcion: formEdicionMision.descripcion.trim(),
        nuevoTipo: Number(formEdicionMision.tipo) as TipoMision,
        nuevaPistaClave: formEdicionMision.pistaClave.trim()
      }, token)
      await recargar()
      setModoEdicionMision(false)
    } catch (err) {
      setErrorFormEdicionMision(err instanceof Error ? err.message : 'Error al modificar la misión.')
    } finally {
      setEnviandoEdicionMision(false)
    }
  }

  async function manejarEliminarMision() {
    if (!token || !busquedaId) return
    setEliminandoMision(true)
    try { await eliminarMision(busquedaId, token); await recargar() }
    catch (err) { setErrorCarga(err instanceof Error ? err.message : 'Error al eliminar la misión.') }
    finally { setEliminandoMision(false) }
  }

  // ---------------------------------------------------------------------------
  // Pistas
  // ---------------------------------------------------------------------------
  async function manejarEnvioPista(e: React.FormEvent) {
    e.preventDefault()
    if (!formPistaContenido.trim()) { setErrorFormPista('El contenido es obligatorio.'); return }
    if (!token || !busquedaId) return
    setEnviandoPista(true)
    setErrorFormPista(null)
    try {
      await agregarPista(busquedaId, { contenido: formPistaContenido.trim() }, token)
      await recargar()
      setMostrarFormPista(false)
      setFormPistaContenido('')
    } catch (err) {
      setErrorFormPista(err instanceof Error ? err.message : 'Error al agregar la pista.')
    } finally {
      setEnviandoPista(false)
    }
  }

  async function manejarEliminarPista(pistaId: string) {
    if (!token || !busquedaId) return
    setEliminandoPistaId(pistaId)
    try { await eliminarPista(busquedaId, pistaId, token); await recargar() }
    catch (err) { setErrorCarga(err instanceof Error ? err.message : 'Error al eliminar la pista.') }
    finally { setEliminandoPistaId(null) }
  }

  async function manejarEnvioEdicionPista(e: React.FormEvent, pistaId: string) {
    e.preventDefault()
    if (!formEdicionPistaContenido.trim()) { setErrorFormEdicionPista('El contenido es obligatorio.'); return }
    if (!token || !busquedaId) return
    setEnviandoEdicionPista(true)
    setErrorFormEdicionPista(null)
    try {
      await modificarPista(busquedaId, pistaId, { nuevoContenido: formEdicionPistaContenido.trim() }, token)
      await recargar()
      setPistaEnEdicion(null)
    } catch (err) {
      setErrorFormEdicionPista(err instanceof Error ? err.message : 'Error al modificar la pista.')
    } finally {
      setEnviandoEdicionPista(false)
    }
  }

  // ---------------------------------------------------------------------------
  // Render
  // ---------------------------------------------------------------------------
  if (estadoCarga === 'cargando') {
    return (
      <LayoutPanel titulo="Misión" descripcion="Cargando búsqueda…">
        <p className="tabla-estado-mensaje">Cargando…</p>
      </LayoutPanel>
    )
  }

  if (estadoCarga === 'error' || !busqueda) {
    return (
      <LayoutPanel titulo="Misión" descripcion="Error al cargar.">
        <Alerta tono="error">{errorCarga ?? 'Error desconocido.'}</Alerta>
        <Boton variante="volver" onClick={() => navegar(rutaBase)}>Volver</Boton>
      </LayoutPanel>
    )
  }

  const esInactiva = busqueda.estado === 'Inactiva'

  return (
    <LayoutPanel
      titulo={busqueda.nombre}
      descripcion={`Misión de búsqueda del tesoro`}
    >
      <section className="seccion">
        <div className="seccion-cabecera">
          <div>
            <h2>{busqueda.nombre}</h2>
            <p>{busqueda.descripcion}</p>
            <span className={`estado-badge estado-badge-${busqueda.estado.toLowerCase()}`}>
              {busqueda.estado}
            </span>
          </div>
          <div className="cabecera-pagina-acciones">
            <Boton variante="volver" onClick={() => navegar(rutaBase)}>Volver</Boton>
            {esInactiva && !busqueda.mision && !mostrarFormMision && (
              <Boton variante="primario" onClick={abrirFormMision}>
                + Asignar misión
              </Boton>
            )}
            {esInactiva && busqueda.mision && (
              <Boton variante="primario" onClick={manejarActivar} disabled={activando}>
                {activando ? 'Activando…' : 'Activar búsqueda'}
              </Boton>
            )}
          </div>
        </div>

        {errorCarga && <Alerta tono="error">{errorCarga}</Alerta>}
        {errorActivacion && <Alerta tono="error">{errorActivacion}</Alerta>}

        {/* Formulario asignar misión */}
        {mostrarFormMision && (
          <div className="formulario-pregunta-panel">
            <h3 className="formulario-pregunta-titulo">Asignar misión</h3>
            {errorFormMision && <Alerta tono="error">{errorFormMision}</Alerta>}
            <form onSubmit={manejarEnvioMision} noValidate>
              <CampoFormulario etiqueta="Título" htmlFor="mision-titulo" error={erroresMision.titulo}>
                <input id="mision-titulo" type="text" maxLength={200}
                  value={formMision.titulo}
                  onChange={(e) => { setFormMision(p => ({ ...p, titulo: e.target.value })); if (erroresMision.titulo) setErroresMision(p => ({ ...p, titulo: undefined })) }}
                  disabled={enviandoMision} placeholder="Ej. Encuentra el cofre del parque" />
              </CampoFormulario>
              <CampoFormulario etiqueta="Descripción" htmlFor="mision-desc" error={erroresMision.descripcion}>
                <textarea id="mision-desc" rows={3} maxLength={1000}
                  value={formMision.descripcion}
                  onChange={(e) => { setFormMision(p => ({ ...p, descripcion: e.target.value })); if (erroresMision.descripcion) setErroresMision(p => ({ ...p, descripcion: undefined })) }}
                  disabled={enviandoMision} placeholder="Describe qué deben buscar los participantes" />
              </CampoFormulario>
              <CampoFormulario etiqueta="Tipo de misión" htmlFor="mision-tipo">
                <select id="mision-tipo"
                  value={formMision.tipo}
                  onChange={(e) => setFormMision(p => ({ ...p, tipo: e.target.value }))}
                  disabled={enviandoMision}>
                  {TIPOS_MISION.map(t => (
                    <option key={t.valor} value={String(t.valor)}>{t.etiqueta} — {t.descripcion}</option>
                  ))}
                </select>
              </CampoFormulario>
              <CampoFormulario etiqueta="Pista clave" htmlFor="mision-pista" error={erroresMision.pistaClave}
                ayuda="Palabra, frase o código que el participante debe encontrar para completar la misión.">
                <input id="mision-pista" type="text" maxLength={500}
                  value={formMision.pistaClave}
                  onChange={(e) => { setFormMision(p => ({ ...p, pistaClave: e.target.value })); if (erroresMision.pistaClave) setErroresMision(p => ({ ...p, pistaClave: undefined })) }}
                  disabled={enviandoMision} placeholder="Ej. UMBRAL2026" />
              </CampoFormulario>
              <div className="acciones-formulario-trivia">
                <Boton variante="volver" type="button" onClick={() => setMostrarFormMision(false)} disabled={enviandoMision}>Cancelar</Boton>
                <Boton variante="primario" type="submit" disabled={enviandoMision}>{enviandoMision ? 'Guardando…' : 'Asignar misión'}</Boton>
              </div>
            </form>
          </div>
        )}

        {/* Sin misión */}
        {!busqueda.mision && !mostrarFormMision && (
          <p className="tabla-estado-mensaje">
            Esta búsqueda no tiene misión asignada. {esInactiva && 'Haga clic en "Asignar misión" para comenzar.'}
          </p>
        )}

        {/* Misión asignada */}
        {busqueda.mision && (
          <div className="pregunta-card">
            <div className="pregunta-card-cabecera">
              <div className="pregunta-card-info">
                <span className="pregunta-numero">Misión</span>
                <span className="pregunta-puntaje">
                  {busqueda.mision.pistas.length} {busqueda.mision.pistas.length === 1 ? 'pista' : 'pistas'}
                </span>
              </div>
              {esInactiva && !modoEdicionMision && (
                <div className="pregunta-card-acciones">
                  <Boton variante="secundario" onClick={() => { setMostrarFormPista(true); setModoEdicionMision(false) }}>
                    + Agregar pista
                  </Boton>
                  <Boton variante="secundario" onClick={abrirEdicionMision}>Editar</Boton>
                  <Boton variante="peligro" onClick={manejarEliminarMision} disabled={eliminandoMision}>
                    {eliminandoMision ? 'Eliminando…' : 'Eliminar misión'}
                  </Boton>
                </div>
              )}
              {busqueda.estado === 'Activa' && (
                <div className="pregunta-card-acciones">
                  <Boton variante="secundario" onClick={() => { setMostrarFormPista(true) }}>
                    + Agregar pista (en tiempo real)
                  </Boton>
                </div>
              )}
            </div>

            {/* Detalle de la misión */}
            {!modoEdicionMision ? (
              <>
                <p className="pregunta-enunciado"><strong>{busqueda.mision.titulo}</strong></p>
                <p className="pregunta-enunciado">{busqueda.mision.descripcion}</p>
                <p style={{ fontSize: '0.85rem', color: '#64748b', marginTop: 4 }}>
                  Tipo: <strong>{TIPOS_MISION.find(t => t.nombre === busqueda.mision!.tipo)?.etiqueta ?? busqueda.mision.tipo}</strong>
                </p>
                <p style={{ fontSize: '0.85rem', color: '#64748b', marginTop: 4 }}>
                  🔑 Pista clave: <strong>{busqueda.mision.pistaClave}</strong>
                </p>
              </>
            ) : (
              <div className="formulario-pregunta-panel" style={{ marginTop: 12 }}>
                <h4 className="formulario-pregunta-titulo">Editar misión</h4>
                {errorFormEdicionMision && <Alerta tono="error">{errorFormEdicionMision}</Alerta>}
                <form onSubmit={manejarEnvioEdicionMision} noValidate>
                  <CampoFormulario etiqueta="Título" htmlFor="editar-mision-titulo" error={erroresEdicionMision.titulo}>
                    <input id="editar-mision-titulo" type="text" maxLength={200}
                      value={formEdicionMision.titulo}
                      onChange={(e) => { setFormEdicionMision(p => ({ ...p, titulo: e.target.value })); if (erroresEdicionMision.titulo) setErroresEdicionMision(p => ({ ...p, titulo: undefined })) }}
                      disabled={enviandoEdicionMision} />
                  </CampoFormulario>
                  <CampoFormulario etiqueta="Descripción" htmlFor="editar-mision-desc" error={erroresEdicionMision.descripcion}>
                    <textarea id="editar-mision-desc" rows={3} maxLength={1000}
                      value={formEdicionMision.descripcion}
                      onChange={(e) => { setFormEdicionMision(p => ({ ...p, descripcion: e.target.value })); if (erroresEdicionMision.descripcion) setErroresEdicionMision(p => ({ ...p, descripcion: undefined })) }}
                      disabled={enviandoEdicionMision} />
                  </CampoFormulario>
                  <CampoFormulario etiqueta="Tipo de misión" htmlFor="editar-mision-tipo">
                    <select id="editar-mision-tipo"
                      value={formEdicionMision.tipo}
                      onChange={(e) => setFormEdicionMision(p => ({ ...p, tipo: e.target.value }))}
                      disabled={enviandoEdicionMision}>
                      {TIPOS_MISION.map(t => (
                        <option key={t.valor} value={String(t.valor)}>{t.etiqueta} — {t.descripcion}</option>
                      ))}
                    </select>
                  </CampoFormulario>
                  <CampoFormulario etiqueta="Pista clave" htmlFor="editar-mision-pista" error={erroresEdicionMision.pistaClave}>
                    <input id="editar-mision-pista" type="text" maxLength={500}
                      value={formEdicionMision.pistaClave}
                      onChange={(e) => { setFormEdicionMision(p => ({ ...p, pistaClave: e.target.value })); if (erroresEdicionMision.pistaClave) setErroresEdicionMision(p => ({ ...p, pistaClave: undefined })) }}
                      disabled={enviandoEdicionMision} />
                  </CampoFormulario>
                  <div className="acciones-formulario-trivia">
                    <Boton variante="volver" type="button" onClick={() => setModoEdicionMision(false)} disabled={enviandoEdicionMision}>Cancelar</Boton>
                    <Boton variante="primario" type="submit" disabled={enviandoEdicionMision}>{enviandoEdicionMision ? 'Guardando…' : 'Guardar cambios'}</Boton>
                  </div>
                </form>
              </div>
            )}

            {/* Formulario agregar pista */}
            {mostrarFormPista && (
              <div className="formulario-pregunta-panel" style={{ marginTop: 16 }}>
                <h4 className="formulario-pregunta-titulo">Nueva pista de ayuda</h4>
                {errorFormPista && <Alerta tono="error">{errorFormPista}</Alerta>}
                <form onSubmit={manejarEnvioPista} noValidate>
                  <CampoFormulario etiqueta="Contenido" htmlFor="pista-contenido"
                    ayuda="Texto de ayuda que el Operador podrá liberar a los participantes durante la sesión.">
                    <textarea id="pista-contenido" rows={3} maxLength={1000}
                      value={formPistaContenido}
                      onChange={(e) => { setFormPistaContenido(e.target.value); setErrorFormPista(null) }}
                      disabled={enviandoPista}
                      placeholder="Ej. La respuesta se encuentra cerca de la entrada principal." />
                  </CampoFormulario>
                  <div className="acciones-formulario-trivia">
                    <Boton variante="volver" type="button" onClick={() => { setMostrarFormPista(false); setFormPistaContenido('') }} disabled={enviandoPista}>Cancelar</Boton>
                    <Boton variante="primario" type="submit" disabled={enviandoPista}>{enviandoPista ? 'Guardando…' : 'Agregar pista'}</Boton>
                  </div>
                </form>
              </div>
            )}

            {/* Lista de pistas */}
            {busqueda.mision.pistas.length > 0 && (
              <div style={{ marginTop: 12 }}>
                <strong style={{ fontSize: '0.85rem' }}>Pistas de ayuda:</strong>
                <ul className="pregunta-opciones" style={{ marginTop: 4 }}>
                  {busqueda.mision.pistas.map((pista) => (
                    <li key={pista.id} className="pregunta-opcion">
                      {pistaEnEdicion === pista.id ? (
                        <div className="formulario-pregunta-panel" style={{ marginTop: 4 }}>
                          <h5 className="formulario-pregunta-titulo">Editar pista</h5>
                          {errorFormEdicionPista && <Alerta tono="error">{errorFormEdicionPista}</Alerta>}
                          <form onSubmit={(e) => manejarEnvioEdicionPista(e, pista.id)} noValidate>
                            <CampoFormulario etiqueta="Contenido" htmlFor={`editar-pista-${pista.id}`}>
                              <textarea id={`editar-pista-${pista.id}`} rows={3} maxLength={1000}
                                value={formEdicionPistaContenido}
                                onChange={(e) => { setFormEdicionPistaContenido(e.target.value); setErrorFormEdicionPista(null) }}
                                disabled={enviandoEdicionPista} />
                            </CampoFormulario>
                            <div className="acciones-formulario-trivia">
                              <Boton variante="volver" type="button" onClick={() => setPistaEnEdicion(null)} disabled={enviandoEdicionPista}>Cancelar</Boton>
                              <Boton variante="primario" type="submit" disabled={enviandoEdicionPista}>{enviandoEdicionPista ? 'Guardando…' : 'Guardar'}</Boton>
                            </div>
                          </form>
                        </div>
                      ) : (
                        <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', gap: 8 }}>
                          <span>🔔 {pista.contenido}</span>
                          {esInactiva && (
                            <div style={{ display: 'flex', gap: 4, flexShrink: 0 }}>
                              <Boton variante="secundario" onClick={() => { setPistaEnEdicion(pista.id); setFormEdicionPistaContenido(pista.contenido); setErrorFormEdicionPista(null) }}>
                                Editar
                              </Boton>
                              <Boton variante="peligro" onClick={() => manejarEliminarPista(pista.id)} disabled={eliminandoPistaId === pista.id}>
                                {eliminandoPistaId === pista.id ? 'Eliminando…' : 'Eliminar'}
                              </Boton>
                            </div>
                          )}
                        </div>
                      )}
                    </li>
                  ))}
                </ul>
              </div>
            )}
          </div>
        )}
      </section>
    </LayoutPanel>
  )
}
