import { useEffect, useState } from 'react'
import { useNavigate, useParams } from 'react-router-dom'
import { LayoutPanel } from '../componentes/LayoutPanel'
import { Alerta } from '../componentes/Alerta'
import { Boton } from '../componentes/Boton'
import { CampoFormulario } from '../componentes/CampoFormulario'
import {
  obtenerDetalleBusqueda,
  agregarEtapa,
  agregarMision,
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
}

interface ErroresEtapa {
  titulo?: string
  descripcion?: string
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
  const { token } = usarAutenticacion()
  const navegar = useNavigate()

  const [busqueda, setBusqueda] = useState<BusquedaTesoroDetalleDto | null>(null)
  const [estadoCarga, setEstadoCarga] = useState<'cargando' | 'error' | 'listo'>('cargando')
  const [errorCarga, setErrorCarga] = useState<string | null>(null)

  // Estado del formulario de etapa
  const [mostrarFormEtapa, setMostrarFormEtapa] = useState(false)
  const [formEtapa, setFormEtapa] = useState<FormEtapa>(FORM_ETAPA_VACIO)
  const [erroresEtapa, setErroresEtapa] = useState<ErroresEtapa>({})
  const [errorFormEtapa, setErrorFormEtapa] = useState<string | null>(null)
  const [enviandoEtapa, setEnviandoEtapa] = useState(false)

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
  // Misiones
  // ---------------------------------------------------------------------------
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
        <Boton variante="volver" onClick={() => navegar('/operador/busquedas')}>
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
            <Boton variante="volver" onClick={() => navegar('/operador/busquedas')}>
              Volver
            </Boton>
            {!mostrarFormEtapa && !etapaConFormMision && (
              <Boton variante="primario" onClick={abrirFormEtapa}>
                + Agregar etapa
              </Boton>
            )}
          </div>
        </div>

        {errorCarga && <Alerta tono="error">{errorCarga}</Alerta>}

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
                    {etapaConFormMision !== etapa.id && !mostrarFormEtapa && (
                      <Boton variante="secundario" onClick={() => abrirFormMision(etapa.id)}>
                        + Agregar misión
                      </Boton>
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
                        <strong>{mision.titulo}</strong>
                        {' '}·{' '}<span>{mision.tipo}</span>
                        {mision.descripcion && <span> — {mision.descripcion}</span>}
                        {mision.pistaClave && (
                          <span className="opcion-check-icono"> 🔑 {mision.pistaClave}</span>
                        )}
                      </li>
                    ))}
                  </ul>
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
