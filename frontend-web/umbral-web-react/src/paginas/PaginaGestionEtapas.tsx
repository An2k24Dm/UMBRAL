import { useEffect, useState } from 'react'
import { useNavigate, useParams } from 'react-router-dom'
import { LayoutPanel } from '../componentes/LayoutPanel'
import { Alerta } from '../componentes/Alerta'
import { Boton } from '../componentes/Boton'
import { CampoFormulario } from '../componentes/CampoFormulario'
import {
  obtenerDetalleBusqueda,
  agregarEtapa,
  modificarEtapa,
  eliminarEtapa,
  agregarMision,
  modificarMision,
  eliminarMision,
  activarBusqueda,
  agregarPista,
  modificarPista,
  type BusquedaTesoroDetalleDto,
  type TipoMision
} from '../autenticacion/clienteApiJuegos'
import { usarAutenticacion } from '../autenticacion/ProveedorAutenticacion'

// ---------------------------------------------------------------------------
// Tipos del formulario de etapa
// ---------------------------------------------------------------------------
interface FormEtapa {
  titulo: string
  descripcion: string
  orden?: string
}

interface ErroresEtapa {
  titulo?: string
  descripcion?: string
  orden?: string
}

// ---------------------------------------------------------------------------
// Tipos del formulario de misión
// ---------------------------------------------------------------------------
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

const TIPOS_MISION: { valor: TipoMision; etiqueta: string }[] = [
  { valor: 0, etiqueta: 'Pista de texto' },
  { valor: 1, etiqueta: 'Acertijo' },
  { valor: 2, etiqueta: 'Código QR' }
]

const FORM_ETAPA_VACIO: FormEtapa = { titulo: '', descripcion: '' }
const FORM_MISION_VACIO: FormMision = { titulo: '', descripcion: '', tipo: '0', pistaClave: '' }

function validarEtapa(form: FormEtapa): ErroresEtapa {
  const err: ErroresEtapa = {}
  if (!form.titulo.trim()) err.titulo = 'El título es obligatorio.'
  else if (form.titulo.trim().length > 200) err.titulo = 'Máximo 200 caracteres.'
  if (!form.descripcion.trim()) err.descripcion = 'La descripción es obligatoria.'
  else if (form.descripcion.trim().length > 1000) err.descripcion = 'Máximo 1000 caracteres.'
  return err
}

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

// ---------------------------------------------------------------------------
// Componente principal
// ---------------------------------------------------------------------------
export function PaginaGestionEtapas() {
  const { busquedaId } = useParams<{ busquedaId: string }>()
  const { token, usuario } = usarAutenticacion()
  const navegar = useNavigate()
  const rutaBase = usuario?.rol === 'Administrador' ? '/administrador/busquedas' : '/operador/busquedas'

  const [busqueda, setBusqueda] = useState<BusquedaTesoroDetalleDto | null>(null)
  const [estadoCarga, setEstadoCarga] = useState<'cargando' | 'error' | 'listo'>('cargando')
  const [errorCarga, setErrorCarga] = useState<string | null>(null)

  // Estado del formulario de etapa (crear)
  const [mostrarFormEtapa, setMostrarFormEtapa] = useState(false)
  const [formEtapa, setFormEtapa] = useState<FormEtapa>(FORM_ETAPA_VACIO)
  const [erroresEtapa, setErroresEtapa] = useState<ErroresEtapa>({})
  const [errorFormEtapa, setErrorFormEtapa] = useState<string | null>(null)
  const [enviandoEtapa, setEnviandoEtapa] = useState(false)

  // Estado del formulario de etapa (editar)
  const [etapaEnEdicion, setEtapaEnEdicion] = useState<string | null>(null)
  const [formEdicionEtapa, setFormEdicionEtapa] = useState<FormEtapa>(FORM_ETAPA_VACIO)
  const [erroresEdicionEtapa, setErroresEdicionEtapa] = useState<ErroresEtapa>({})
  const [errorFormEdicionEtapa, setErrorFormEdicionEtapa] = useState<string | null>(null)
  const [enviandoEdicionEtapa, setEnviandoEdicionEtapa] = useState(false)
  const [eliminandoEtapaId, setEliminandoEtapaId] = useState<string | null>(null)
  const [activando, setActivando] = useState(false)
  const [errorActivacion, setErrorActivacion] = useState<string | null>(null)

  // Estado del formulario de misión (editar, indexado por misionId)
  const [misionEnEdicion, setMisionEnEdicion] = useState<string | null>(null)
  const [formEdicionMision, setFormEdicionMision] = useState<FormMision>(FORM_MISION_VACIO)
  const [erroresEdicionMision, setErroresEdicionMision] = useState<ErroresMision>({})
  const [errorFormEdicionMision, setErrorFormEdicionMision] = useState<string | null>(null)
  const [enviandoEdicionMision, setEnviandoEdicionMision] = useState(false)
  const [eliminandoMisionId, setEliminandoMisionId] = useState<string | null>(null)

  // Estado del formulario de pista (indexado por etapaId)
  const [etapaConFormPista, setEtapaConFormPista] = useState<string | null>(null)
  const [formPistaContenido, setFormPistaContenido] = useState('')
  const [errorFormPista, setErrorFormPista] = useState<string | null>(null)
  const [enviandoPista, setEnviandoPista] = useState(false)

  // Estado edición de pista
  const [pistaEnEdicion, setPistaEnEdicion] = useState<string | null>(null)
  const [formEdicionPistaContenido, setFormEdicionPistaContenido] = useState('')
  const [errorFormEdicionPista, setErrorFormEdicionPista] = useState<string | null>(null)
  const [enviandoEdicionPista, setEnviandoEdicionPista] = useState(false)

  // Estado del formulario de misión (indexado por etapaId)
  const [etapaConFormMision, setEtapaConFormMision] = useState<string | null>(null)
  const [formMision, setFormMision] = useState<FormMision>(FORM_MISION_VACIO)
  const [erroresMision, setErroresMision] = useState<ErroresMision>({})
  const [errorFormMision, setErrorFormMision] = useState<string | null>(null)
  const [enviandoMision, setEnviandoMision] = useState(false)

  // ---------------------------------------------------------------------------
  // Carga inicial
  // ---------------------------------------------------------------------------
  useEffect(() => {
    if (!busquedaId) return
    let cancelado = false
    async function cargar() {
      if (!token) { setEstadoCarga('error'); setErrorCarga('Debe iniciar sesión.'); return }
      setEstadoCarga('cargando')
      setErrorCarga(null)
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

  // ---------------------------------------------------------------------------
  // Etapas
  // ---------------------------------------------------------------------------
  function abrirFormEtapa() {
    setFormEtapa(FORM_ETAPA_VACIO)
    setErroresEtapa({})
    setErrorFormEtapa(null)
    setEtapaConFormMision(null)
    setMostrarFormEtapa(true)
  }

  function cerrarFormEtapa() {
    setMostrarFormEtapa(false)
    setErroresEtapa({})
    setErrorFormEtapa(null)
  }

  async function manejarEnvioEtapa(e: React.FormEvent) {
    e.preventDefault()
    const erroresValidacion = validarEtapa(formEtapa)
    if (Object.keys(erroresValidacion).length > 0) { setErroresEtapa(erroresValidacion); return }
    if (!token || !busquedaId) return

    setEnviandoEtapa(true)
    setErrorFormEtapa(null)
    try {
      await agregarEtapa(busquedaId, {
        titulo: formEtapa.titulo.trim(),
        descripcion: formEtapa.descripcion.trim()
      }, token)
      const datos = await obtenerDetalleBusqueda(busquedaId, token)
      setBusqueda(datos)
      cerrarFormEtapa()
    } catch (err) {
      setErrorFormEtapa(err instanceof Error ? err.message : 'Ocurrió un error al agregar la etapa.')
    } finally {
      setEnviandoEtapa(false)
    }
  }

  // ---------------------------------------------------------------------------
  // Editar etapa
  // ---------------------------------------------------------------------------
  async function manejarActivarBusqueda() {
    if (!token || !busquedaId) return
    setActivando(true)
    setErrorActivacion(null)
    try {
      await activarBusqueda(busquedaId, token)
      const datos = await obtenerDetalleBusqueda(busquedaId, token)
      setBusqueda(datos)
    } catch (err) {
      setErrorActivacion(err instanceof Error ? err.message : 'Ocurrió un error al activar la búsqueda.')
    } finally {
      setActivando(false)
    }
  }

  function abrirEdicionEtapa(etapa: { id: string; titulo: string; descripcion: string; orden: number }) {
    setEtapaEnEdicion(etapa.id)
    setFormEdicionEtapa({ titulo: etapa.titulo, descripcion: etapa.descripcion, orden: String(etapa.orden) })
    setErroresEdicionEtapa({})
    setErrorFormEdicionEtapa(null)
    setMostrarFormEtapa(false)
    setEtapaConFormMision(null)
  }

  function cerrarEdicionEtapa() {
    setEtapaEnEdicion(null)
    setErroresEdicionEtapa({})
    setErrorFormEdicionEtapa(null)
  }

  async function manejarEnvioEdicionEtapa(e: React.FormEvent, etapaId: string) {
    e.preventDefault()
    const erroresValidacion = validarEtapa(formEdicionEtapa)
    const ordenNum = Number(formEdicionEtapa.orden)
    if (!formEdicionEtapa.orden?.trim()) erroresValidacion.orden = 'El orden es obligatorio.'
    else if (!Number.isInteger(ordenNum) || ordenNum <= 0) erroresValidacion.orden = 'El orden debe ser un número entero positivo.'
    if (Object.keys(erroresValidacion).length > 0) { setErroresEdicionEtapa(erroresValidacion); return }
    if (!token || !busquedaId) return

    setEnviandoEdicionEtapa(true)
    setErrorFormEdicionEtapa(null)
    try {
      await modificarEtapa(busquedaId, etapaId, {
        nuevoTitulo: formEdicionEtapa.titulo.trim(),
        nuevaDescripcion: formEdicionEtapa.descripcion.trim(),
        nuevoOrden: ordenNum
      }, token)
      const datos = await obtenerDetalleBusqueda(busquedaId, token)
      setBusqueda(datos)
      cerrarEdicionEtapa()
    } catch (err) {
      setErrorFormEdicionEtapa(err instanceof Error ? err.message : 'Ocurrió un error al modificar la etapa.')
    } finally {
      setEnviandoEdicionEtapa(false)
    }
  }

  async function manejarEliminarEtapa(etapaId: string) {
    if (!token || !busquedaId) return
    setEliminandoEtapaId(etapaId)
    try {
      await eliminarEtapa(busquedaId, etapaId, token)
      const datos = await obtenerDetalleBusqueda(busquedaId, token)
      setBusqueda(datos)
    } catch (err) {
      setErrorCarga(err instanceof Error ? err.message : 'Ocurrió un error al eliminar la etapa.')
    } finally {
      setEliminandoEtapaId(null)
    }
  }

  // ---------------------------------------------------------------------------
  // Misiones
  // ---------------------------------------------------------------------------
  function abrirEdicionMision(mision: { id: string; titulo: string; descripcion: string; tipo: string; pistaClave: string }) {
    setMisionEnEdicion(mision.id)
    const tipoNumero = TIPOS_MISION.find(t => t.etiqueta === mision.tipo)?.valor ?? 0
    setFormEdicionMision({ titulo: mision.titulo, descripcion: mision.descripcion, tipo: String(tipoNumero), pistaClave: mision.pistaClave })
    setErroresEdicionMision({})
    setErrorFormEdicionMision(null)
    setEtapaConFormMision(null)
  }

  function cerrarEdicionMision() {
    setMisionEnEdicion(null)
    setErroresEdicionMision({})
    setErrorFormEdicionMision(null)
  }

  async function manejarEnvioEdicionMision(e: React.FormEvent, etapaId: string, misionId: string) {
    e.preventDefault()
    const erroresValidacion = validarMision(formEdicionMision)
    if (Object.keys(erroresValidacion).length > 0) { setErroresEdicionMision(erroresValidacion); return }
    if (!token || !busquedaId) return

    setEnviandoEdicionMision(true)
    setErrorFormEdicionMision(null)
    try {
      await modificarMision(busquedaId, etapaId, misionId, {
        nuevoTitulo: formEdicionMision.titulo.trim(),
        nuevaDescripcion: formEdicionMision.descripcion.trim(),
        nuevoTipo: Number(formEdicionMision.tipo) as TipoMision,
        nuevaPistaClave: formEdicionMision.pistaClave.trim()
      }, token)
      const datos = await obtenerDetalleBusqueda(busquedaId, token)
      setBusqueda(datos)
      cerrarEdicionMision()
    } catch (err) {
      setErrorFormEdicionMision(err instanceof Error ? err.message : 'Ocurrió un error al modificar la misión.')
    } finally {
      setEnviandoEdicionMision(false)
    }
  }

  async function manejarEliminarMision(etapaId: string, misionId: string) {
    if (!token || !busquedaId) return
    setEliminandoMisionId(misionId)
    try {
      await eliminarMision(busquedaId, etapaId, misionId, token)
      const datos = await obtenerDetalleBusqueda(busquedaId, token)
      setBusqueda(datos)
    } catch (err) {
      setErrorCarga(err instanceof Error ? err.message : 'Ocurrió un error al eliminar la misión.')
    } finally {
      setEliminandoMisionId(null)
    }
  }

  function abrirFormMision(etapaId: string) {
    setFormMision(FORM_MISION_VACIO)
    setErroresMision({})
    setErrorFormMision(null)
    setMostrarFormEtapa(false)
    setEtapaConFormMision(etapaId)
  }

  function cerrarFormMision() {
    setEtapaConFormMision(null)
    setErroresMision({})
    setErrorFormMision(null)
  }

  function abrirFormPista(etapaId: string) {
    setFormPistaContenido('')
    setErrorFormPista(null)
    setEtapaConFormMision(null)
    setMostrarFormEtapa(false)
    setEtapaConFormPista(etapaId)
  }

  function cerrarFormPista() {
    setEtapaConFormPista(null)
    setErrorFormPista(null)
  }

  async function manejarEnvioPista(e: React.FormEvent, etapaId: string) {
    e.preventDefault()
    if (!formPistaContenido.trim()) { setErrorFormPista('El contenido de la pista es obligatorio.'); return }
    if (!token || !busquedaId) return

    setEnviandoPista(true)
    setErrorFormPista(null)
    try {
      await agregarPista(busquedaId, etapaId, { contenido: formPistaContenido.trim() }, token)
      const datos = await obtenerDetalleBusqueda(busquedaId, token)
      setBusqueda(datos)
      cerrarFormPista()
    } catch (err) {
      setErrorFormPista(err instanceof Error ? err.message : 'Ocurrió un error al agregar la pista.')
    } finally {
      setEnviandoPista(false)
    }
  }

  function abrirEdicionPista(pistaId: string, contenidoActual: string) {
    setPistaEnEdicion(pistaId)
    setFormEdicionPistaContenido(contenidoActual)
    setErrorFormEdicionPista(null)
  }

  function cerrarEdicionPista() {
    setPistaEnEdicion(null)
    setErrorFormEdicionPista(null)
  }

  async function manejarEnvioEdicionPista(e: React.FormEvent, etapaId: string, pistaId: string) {
    e.preventDefault()
    if (!formEdicionPistaContenido.trim()) { setErrorFormEdicionPista('El contenido es obligatorio.'); return }
    if (!token || !busquedaId) return

    setEnviandoEdicionPista(true)
    setErrorFormEdicionPista(null)
    try {
      await modificarPista(busquedaId, etapaId, pistaId, { nuevoContenido: formEdicionPistaContenido.trim() }, token)
      const datos = await obtenerDetalleBusqueda(busquedaId, token)
      setBusqueda(datos)
      cerrarEdicionPista()
    } catch (err) {
      setErrorFormEdicionPista(err instanceof Error ? err.message : 'Ocurrió un error al modificar la pista.')
    } finally {
      setEnviandoEdicionPista(false)
    }
  }

  async function manejarEnvioMision(e: React.FormEvent, etapaId: string) {
    e.preventDefault()
    const erroresValidacion = validarMision(formMision)
    if (Object.keys(erroresValidacion).length > 0) { setErroresMision(erroresValidacion); return }
    if (!token || !busquedaId) return

    setEnviandoMision(true)
    setErrorFormMision(null)
    try {
      await agregarMision(busquedaId, etapaId, {
        titulo: formMision.titulo.trim(),
        descripcion: formMision.descripcion.trim(),
        tipo: Number(formMision.tipo) as TipoMision,
        pistaClave: formMision.pistaClave.trim()
      }, token)
      const datos = await obtenerDetalleBusqueda(busquedaId, token)
      setBusqueda(datos)
      cerrarFormMision()
    } catch (err) {
      setErrorFormMision(err instanceof Error ? err.message : 'Ocurrió un error al agregar la misión.')
    } finally {
      setEnviandoMision(false)
    }
  }

  // ---------------------------------------------------------------------------
  // Render
  // ---------------------------------------------------------------------------
  if (estadoCarga === 'cargando') {
    return (
      <LayoutPanel titulo="Etapas" descripcion="Cargando búsqueda del tesoro…">
        <p className="tabla-estado-mensaje">Cargando…</p>
      </LayoutPanel>
    )
  }

  if (estadoCarga === 'error' || !busqueda) {
    return (
      <LayoutPanel titulo="Etapas" descripcion="Error al cargar la búsqueda.">
        <Alerta tono="error">{errorCarga ?? 'Error desconocido.'}</Alerta>
        <Boton variante="volver" onClick={() => navegar(rutaBase)}>
          Volver a mis búsquedas
        </Boton>
      </LayoutPanel>
    )
  }

  return (
    <LayoutPanel
      titulo={busqueda.nombre}
      descripcion={`Gestión de etapas · ${busqueda.etapas.length} ${busqueda.etapas.length === 1 ? 'etapa' : 'etapas'}`}
    >
      <section className="seccion">
        <div className="seccion-cabecera">
          <div>
            <h2>Etapas de la búsqueda</h2>
            <p>{busqueda.descripcion}</p>
            <span className={`estado-badge estado-badge-${busqueda.estado.toLowerCase()}`}>
              {busqueda.estado}
            </span>
          </div>
          <div className="cabecera-pagina-acciones">
            <Boton variante="volver" onClick={() => navegar(rutaBase)}>
              Volver
            </Boton>
            {!mostrarFormEtapa && !etapaConFormMision && busqueda.estado === 'Inactiva' && (
              <>
                <Boton variante="primario" onClick={abrirFormEtapa}>
                  + Agregar etapa
                </Boton>
                <Boton variante="primario" onClick={manejarActivarBusqueda} disabled={activando}>
                  {activando ? 'Activando…' : 'Activar búsqueda'}
                </Boton>
              </>
            )}
          </div>
        </div>

        {errorCarga && <Alerta tono="error">{errorCarga}</Alerta>}
        {errorActivacion && <Alerta tono="error">{errorActivacion}</Alerta>}

        {/* Formulario agregar etapa */}
        {mostrarFormEtapa && (
          <div className="formulario-pregunta-panel">
            <h3 className="formulario-pregunta-titulo">Nueva etapa</h3>
            {errorFormEtapa && <Alerta tono="error">{errorFormEtapa}</Alerta>}
            <form onSubmit={manejarEnvioEtapa} noValidate>
              <CampoFormulario etiqueta="Título" htmlFor="etapa-titulo" error={erroresEtapa.titulo}>
                <input
                  id="etapa-titulo"
                  type="text"
                  maxLength={200}
                  value={formEtapa.titulo}
                  onChange={(e) => {
                    setFormEtapa((p) => ({ ...p, titulo: e.target.value }))
                    if (erroresEtapa.titulo) setErroresEtapa((p) => ({ ...p, titulo: undefined }))
                  }}
                  disabled={enviandoEtapa}
                  placeholder="Ej. Punto de partida"
                />
              </CampoFormulario>
              <CampoFormulario etiqueta="Descripción" htmlFor="etapa-descripcion" error={erroresEtapa.descripcion}>
                <textarea
                  id="etapa-descripcion"
                  rows={3}
                  maxLength={1000}
                  value={formEtapa.descripcion}
                  onChange={(e) => {
                    setFormEtapa((p) => ({ ...p, descripcion: e.target.value }))
                    if (erroresEtapa.descripcion) setErroresEtapa((p) => ({ ...p, descripcion: undefined }))
                  }}
                  disabled={enviandoEtapa}
                  placeholder="Describe qué debe hacer el participante en esta etapa"
                />
              </CampoFormulario>
              <div className="acciones-formulario-trivia">
                <Boton variante="volver" type="button" onClick={cerrarFormEtapa} disabled={enviandoEtapa}>
                  Cancelar
                </Boton>
                <Boton variante="primario" type="submit" disabled={enviandoEtapa}>
                  {enviandoEtapa ? 'Guardando…' : 'Agregar etapa'}
                </Boton>
              </div>
            </form>
          </div>
        )}

        {/* Lista de etapas */}
        {busqueda.etapas.length === 0 && !mostrarFormEtapa ? (
          <p className="tabla-estado-mensaje">
            Esta búsqueda aún no tiene etapas. Haga clic en "Agregar etapa" para comenzar.
          </p>
        ) : (
          <div className="lista-preguntas">
            {busqueda.etapas.map((etapa) => (
              <div key={etapa.id} className="pregunta-card">
                <div className="pregunta-card-cabecera">
                  <div className="pregunta-card-info">
                    <span className="pregunta-numero">Etapa {etapa.orden}</span>
                    <span className="pregunta-puntaje">
                      {etapa.misiones.length} {etapa.misiones.length === 1 ? 'misión' : 'misiones'}
                    </span>
                  </div>
                  <div className="pregunta-card-acciones">
                    {etapaConFormMision !== etapa.id && etapaConFormPista !== etapa.id && etapaEnEdicion !== etapa.id && !mostrarFormEtapa && (
                      <>
                        <Boton variante="secundario" onClick={() => abrirFormPista(etapa.id)}>
                          + Agregar pista
                        </Boton>
                        <Boton variante="secundario" onClick={() => abrirFormMision(etapa.id)}>
                          + Agregar misión
                        </Boton>
                        <Boton variante="secundario" onClick={() => abrirEdicionEtapa(etapa)}>
                          Editar
                        </Boton>
                        <Boton
                          variante="peligro"
                          onClick={() => manejarEliminarEtapa(etapa.id)}
                          disabled={eliminandoEtapaId === etapa.id}
                        >
                          {eliminandoEtapaId === etapa.id ? 'Eliminando…' : 'Eliminar'}
                        </Boton>
                      </>
                    )}
                  </div>
                </div>

                <p className="pregunta-enunciado"><strong>{etapa.titulo}</strong></p>
                <p className="pregunta-enunciado">{etapa.descripcion}</p>

                {/* Misiones de la etapa */}
                {etapa.misiones.length > 0 && (
                  <ul className="pregunta-opciones">
                    {etapa.misiones.map((mision) => (
                      <li key={mision.id} className="pregunta-opcion">
                        {misionEnEdicion === mision.id ? (
                          <div className="formulario-pregunta-panel" style={{ marginTop: 8 }}>
                            <h5 className="formulario-pregunta-titulo">Editar misión</h5>
                            {errorFormEdicionMision && <Alerta tono="error">{errorFormEdicionMision}</Alerta>}
                            <form onSubmit={(e) => manejarEnvioEdicionMision(e, etapa.id, mision.id)} noValidate>
                              <CampoFormulario etiqueta="Título" htmlFor={`editar-mision-titulo-${mision.id}`} error={erroresEdicionMision.titulo}>
                                <input id={`editar-mision-titulo-${mision.id}`} type="text" maxLength={200}
                                  value={formEdicionMision.titulo}
                                  onChange={(e) => { setFormEdicionMision((p) => ({ ...p, titulo: e.target.value })); if (erroresEdicionMision.titulo) setErroresEdicionMision((p) => ({ ...p, titulo: undefined })) }}
                                  disabled={enviandoEdicionMision} />
                              </CampoFormulario>
                              <CampoFormulario etiqueta="Descripción" htmlFor={`editar-mision-desc-${mision.id}`} error={erroresEdicionMision.descripcion}>
                                <textarea id={`editar-mision-desc-${mision.id}`} rows={2} maxLength={1000}
                                  value={formEdicionMision.descripcion}
                                  onChange={(e) => { setFormEdicionMision((p) => ({ ...p, descripcion: e.target.value })); if (erroresEdicionMision.descripcion) setErroresEdicionMision((p) => ({ ...p, descripcion: undefined })) }}
                                  disabled={enviandoEdicionMision} />
                              </CampoFormulario>
                              <CampoFormulario etiqueta="Tipo de misión" htmlFor={`editar-mision-tipo-${mision.id}`}>
                                <select id={`editar-mision-tipo-${mision.id}`} value={formEdicionMision.tipo}
                                  onChange={(e) => setFormEdicionMision((p) => ({ ...p, tipo: e.target.value }))}
                                  disabled={enviandoEdicionMision}>
                                  {TIPOS_MISION.map((t) => (<option key={t.valor} value={t.valor}>{t.etiqueta}</option>))}
                                </select>
                              </CampoFormulario>
                              <CampoFormulario etiqueta="Pista clave" htmlFor={`editar-mision-pista-${mision.id}`} error={erroresEdicionMision.pistaClave}>
                                <input id={`editar-mision-pista-${mision.id}`} type="text" maxLength={500}
                                  value={formEdicionMision.pistaClave}
                                  onChange={(e) => { setFormEdicionMision((p) => ({ ...p, pistaClave: e.target.value })); if (erroresEdicionMision.pistaClave) setErroresEdicionMision((p) => ({ ...p, pistaClave: undefined })) }}
                                  disabled={enviandoEdicionMision} />
                              </CampoFormulario>
                              <div className="acciones-formulario-trivia">
                                <Boton variante="volver" type="button" onClick={cerrarEdicionMision} disabled={enviandoEdicionMision}>Cancelar</Boton>
                                <Boton variante="primario" type="submit" disabled={enviandoEdicionMision}>{enviandoEdicionMision ? 'Guardando…' : 'Guardar cambios'}</Boton>
                              </div>
                            </form>
                          </div>
                        ) : (
                          <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start', gap: 8 }}>
                            <div>
                              <strong>{mision.titulo}</strong>
                              {' '}·{' '}<span>{mision.tipo}</span>
                              {mision.descripcion && <span> — {mision.descripcion}</span>}
                              {mision.pistaClave && <span className="opcion-check-icono"> 🔑 {mision.pistaClave}</span>}
                            </div>
                            <div style={{ display: 'flex', gap: 4, flexShrink: 0 }}>
                              <Boton variante="secundario" onClick={() => abrirEdicionMision(mision)}>Editar</Boton>
                              <Boton variante="peligro"
                                onClick={() => manejarEliminarMision(etapa.id, mision.id)}
                                disabled={eliminandoMisionId === mision.id}>
                                {eliminandoMisionId === mision.id ? 'Eliminando…' : 'Eliminar'}
                              </Boton>
                            </div>
                          </div>
                        )}
                      </li>
                    ))}
                  </ul>
                )}

                {/* Pistas de la etapa */}
                {etapa.pistas && etapa.pistas.length > 0 && (
                  <div style={{ marginTop: 8 }}>
                    <strong style={{ fontSize: '0.85rem' }}>Pistas de ayuda:</strong>
                    <ul className="pregunta-opciones" style={{ marginTop: 4 }}>
                      {etapa.pistas.map((pista) => (
                        <li key={pista.id} className="pregunta-opcion">
                          {pistaEnEdicion === pista.id ? (
                            <div className="formulario-pregunta-panel" style={{ marginTop: 4 }}>
                              <h5 className="formulario-pregunta-titulo">Editar pista</h5>
                              {errorFormEdicionPista && <Alerta tono="error">{errorFormEdicionPista}</Alerta>}
                              <form onSubmit={(e) => manejarEnvioEdicionPista(e, etapa.id, pista.id)} noValidate>
                                <CampoFormulario etiqueta="Contenido" htmlFor={`editar-pista-${pista.id}`}>
                                  <textarea
                                    id={`editar-pista-${pista.id}`}
                                    rows={3}
                                    maxLength={1000}
                                    value={formEdicionPistaContenido}
                                    onChange={(e) => { setFormEdicionPistaContenido(e.target.value); setErrorFormEdicionPista(null) }}
                                    disabled={enviandoEdicionPista}
                                  />
                                </CampoFormulario>
                                <div className="acciones-formulario-trivia">
                                  <Boton variante="volver" type="button" onClick={cerrarEdicionPista} disabled={enviandoEdicionPista}>Cancelar</Boton>
                                  <Boton variante="primario" type="submit" disabled={enviandoEdicionPista}>
                                    {enviandoEdicionPista ? 'Guardando…' : 'Guardar cambios'}
                                  </Boton>
                                </div>
                              </form>
                            </div>
                          ) : (
                            <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', gap: 8 }}>
                              <span>🔔 {pista.contenido}</span>
                              {busqueda.estado === 'Inactiva' && (
                                <Boton variante="secundario" onClick={() => abrirEdicionPista(pista.id, pista.contenido)}>
                                  Editar
                                </Boton>
                              )}
                            </div>
                          )}
                        </li>
                      ))}
                    </ul>
                  </div>
                )}

                {/* Formulario agregar pista (inline) */}
                {etapaConFormPista === etapa.id && (
                  <div className="formulario-pregunta-panel" style={{ marginTop: 16 }}>
                    <h4 className="formulario-pregunta-titulo">Nueva pista para esta etapa</h4>
                    {errorFormPista && <Alerta tono="error">{errorFormPista}</Alerta>}
                    <form onSubmit={(e) => manejarEnvioPista(e, etapa.id)} noValidate>
                      <CampoFormulario etiqueta="Contenido de la pista" htmlFor="pista-contenido"
                        ayuda="Texto de ayuda que el Operador podrá liberar a los participantes durante la sesión.">
                        <textarea
                          id="pista-contenido"
                          rows={3}
                          maxLength={1000}
                          value={formPistaContenido}
                          onChange={(e) => { setFormPistaContenido(e.target.value); setErrorFormPista(null) }}
                          disabled={enviandoPista}
                          placeholder="Ej. La respuesta se encuentra cerca de la entrada principal."
                        />
                      </CampoFormulario>
                      <div className="acciones-formulario-trivia">
                        <Boton variante="volver" type="button" onClick={cerrarFormPista} disabled={enviandoPista}>
                          Cancelar
                        </Boton>
                        <Boton variante="primario" type="submit" disabled={enviandoPista}>
                          {enviandoPista ? 'Guardando…' : 'Agregar pista'}
                        </Boton>
                      </div>
                    </form>
                  </div>
                )}

                {/* Formulario editar etapa (inline) */}
                {etapaEnEdicion === etapa.id && (
                  <div className="formulario-pregunta-panel" style={{ marginTop: 16 }}>
                    <h4 className="formulario-pregunta-titulo">Editar etapa</h4>
                    {errorFormEdicionEtapa && <Alerta tono="error">{errorFormEdicionEtapa}</Alerta>}
                    <form onSubmit={(e) => manejarEnvioEdicionEtapa(e, etapa.id)} noValidate>
                      <CampoFormulario etiqueta="Orden" htmlFor={`editar-etapa-orden-${etapa.id}`} error={erroresEdicionEtapa.orden}>
                        <input
                          id={`editar-etapa-orden-${etapa.id}`}
                          type="number"
                          min={1}
                          value={formEdicionEtapa.orden ?? ''}
                          onChange={(e) => {
                            setFormEdicionEtapa((p) => ({ ...p, orden: e.target.value }))
                            if (erroresEdicionEtapa.orden) setErroresEdicionEtapa((p) => ({ ...p, orden: undefined }))
                          }}
                          disabled={enviandoEdicionEtapa}
                        />
                      </CampoFormulario>
                      <CampoFormulario etiqueta="Título" htmlFor={`editar-etapa-titulo-${etapa.id}`} error={erroresEdicionEtapa.titulo}>
                        <input
                          id={`editar-etapa-titulo-${etapa.id}`}
                          type="text"
                          maxLength={200}
                          value={formEdicionEtapa.titulo}
                          onChange={(e) => {
                            setFormEdicionEtapa((p) => ({ ...p, titulo: e.target.value }))
                            if (erroresEdicionEtapa.titulo) setErroresEdicionEtapa((p) => ({ ...p, titulo: undefined }))
                          }}
                          disabled={enviandoEdicionEtapa}
                        />
                      </CampoFormulario>
                      <CampoFormulario etiqueta="Descripción" htmlFor={`editar-etapa-desc-${etapa.id}`} error={erroresEdicionEtapa.descripcion}>
                        <textarea
                          id={`editar-etapa-desc-${etapa.id}`}
                          rows={3}
                          maxLength={1000}
                          value={formEdicionEtapa.descripcion}
                          onChange={(e) => {
                            setFormEdicionEtapa((p) => ({ ...p, descripcion: e.target.value }))
                            if (erroresEdicionEtapa.descripcion) setErroresEdicionEtapa((p) => ({ ...p, descripcion: undefined }))
                          }}
                          disabled={enviandoEdicionEtapa}
                        />
                      </CampoFormulario>
                      <div className="acciones-formulario-trivia">
                        <Boton variante="volver" type="button" onClick={cerrarEdicionEtapa} disabled={enviandoEdicionEtapa}>
                          Cancelar
                        </Boton>
                        <Boton variante="primario" type="submit" disabled={enviandoEdicionEtapa}>
                          {enviandoEdicionEtapa ? 'Guardando…' : 'Guardar cambios'}
                        </Boton>
                      </div>
                    </form>
                  </div>
                )}

                {/* Formulario agregar misión (inline por etapa) */}
                {etapaConFormMision === etapa.id && (
                  <div className="formulario-pregunta-panel" style={{ marginTop: 16 }}>
                    <h4 className="formulario-pregunta-titulo">Nueva misión para esta etapa</h4>
                    {errorFormMision && <Alerta tono="error">{errorFormMision}</Alerta>}
                    <form onSubmit={(e) => manejarEnvioMision(e, etapa.id)} noValidate>
                      <CampoFormulario etiqueta="Título" htmlFor="mision-titulo" error={erroresMision.titulo}>
                        <input
                          id="mision-titulo"
                          type="text"
                          maxLength={200}
                          value={formMision.titulo}
                          onChange={(e) => {
                            setFormMision((p) => ({ ...p, titulo: e.target.value }))
                            if (erroresMision.titulo) setErroresMision((p) => ({ ...p, titulo: undefined }))
                          }}
                          disabled={enviandoMision}
                          placeholder="Ej. Encuentra el marcador rojo"
                        />
                      </CampoFormulario>
                      <CampoFormulario etiqueta="Descripción" htmlFor="mision-descripcion" error={erroresMision.descripcion}>
                        <textarea
                          id="mision-descripcion"
                          rows={2}
                          maxLength={1000}
                          value={formMision.descripcion}
                          onChange={(e) => {
                            setFormMision((p) => ({ ...p, descripcion: e.target.value }))
                            if (erroresMision.descripcion) setErroresMision((p) => ({ ...p, descripcion: undefined }))
                          }}
                          disabled={enviandoMision}
                          placeholder="Describe la misión"
                        />
                      </CampoFormulario>
                      <CampoFormulario etiqueta="Tipo de misión" htmlFor="mision-tipo">
                        <select
                          id="mision-tipo"
                          value={formMision.tipo}
                          onChange={(e) => setFormMision((p) => ({ ...p, tipo: e.target.value }))}
                          disabled={enviandoMision}
                        >
                          {TIPOS_MISION.map((t) => (
                            <option key={t.valor} value={t.valor}>{t.etiqueta}</option>
                          ))}
                        </select>
                      </CampoFormulario>
                      <CampoFormulario etiqueta="Pista clave" htmlFor="mision-pista" error={erroresMision.pistaClave}
                        ayuda="Palabra, frase o código que el participante debe encontrar para completar la misión.">
                        <input
                          id="mision-pista"
                          type="text"
                          maxLength={500}
                          value={formMision.pistaClave}
                          onChange={(e) => {
                            setFormMision((p) => ({ ...p, pistaClave: e.target.value }))
                            if (erroresMision.pistaClave) setErroresMision((p) => ({ ...p, pistaClave: undefined }))
                          }}
                          disabled={enviandoMision}
                          placeholder="Ej. UMBRAL2024"
                        />
                      </CampoFormulario>
                      <div className="acciones-formulario-trivia">
                        <Boton variante="volver" type="button" onClick={cerrarFormMision} disabled={enviandoMision}>
                          Cancelar
                        </Boton>
                        <Boton variante="primario" type="submit" disabled={enviandoMision}>
                          {enviandoMision ? 'Guardando…' : 'Agregar misión'}
                        </Boton>
                      </div>
                    </form>
                  </div>
                )}
              </div>
            ))}
          </div>
        )}
      </section>
    </LayoutPanel>
  )
}
