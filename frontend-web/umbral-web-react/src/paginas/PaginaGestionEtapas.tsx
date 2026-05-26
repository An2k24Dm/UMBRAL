import { useEffect, useState } from 'react'
import { useNavigate, useParams } from 'react-router-dom'
import { LayoutPanel } from '../componentes/LayoutPanel'
import { Alerta } from '../componentes/Alerta'
import { Boton } from '../componentes/Boton'
import { CampoFormulario } from '../componentes/CampoFormulario'
import {
  obtenerDetalleBusqueda,
  agregarEtapa,
  type BusquedaTesoroDetalleDto
} from '../autenticacion/clienteApiJuegos'
import { usarAutenticacion } from '../autenticacion/ProveedorAutenticacion'

interface FormEtapa {
  titulo: string
  descripcion: string
}

interface ErroresEtapa {
  titulo?: string
  descripcion?: string
}

const FORM_VACIO: FormEtapa = { titulo: '', descripcion: '' }

function validarEtapa(form: FormEtapa): ErroresEtapa {
  const err: ErroresEtapa = {}
  if (!form.titulo.trim()) err.titulo = 'El título es obligatorio.'
  else if (form.titulo.trim().length > 200) err.titulo = 'Máximo 200 caracteres.'
  if (!form.descripcion.trim()) err.descripcion = 'La descripción es obligatoria.'
  else if (form.descripcion.trim().length > 1000) err.descripcion = 'Máximo 1000 caracteres.'
  return err
}

export function PaginaGestionEtapas() {
  const { busquedaId } = useParams<{ busquedaId: string }>()
  const { token } = usarAutenticacion()
  const navegar = useNavigate()

  const [busqueda, setBusqueda] = useState<BusquedaTesoroDetalleDto | null>(null)
  const [estadoCarga, setEstadoCarga] = useState<'cargando' | 'error' | 'listo'>('cargando')
  const [errorCarga, setErrorCarga] = useState<string | null>(null)

  const [mostrarForm, setMostrarForm] = useState(false)
  const [form, setForm] = useState<FormEtapa>(FORM_VACIO)
  const [erroresForm, setErroresForm] = useState<ErroresEtapa>({})
  const [errorForm, setErrorForm] = useState<string | null>(null)
  const [enviando, setEnviando] = useState(false)

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

  function abrirForm() {
    setForm(FORM_VACIO)
    setErroresForm({})
    setErrorForm(null)
    setMostrarForm(true)
  }

  function cerrarForm() {
    setMostrarForm(false)
    setErroresForm({})
    setErrorForm(null)
  }

  async function manejarEnvio(e: React.FormEvent) {
    e.preventDefault()
    const erroresValidacion = validarEtapa(form)
    if (Object.keys(erroresValidacion).length > 0) {
      setErroresForm(erroresValidacion)
      return
    }
    if (!token || !busquedaId) return

    setEnviando(true)
    setErrorForm(null)
    try {
      await agregarEtapa(busquedaId, {
        titulo: form.titulo.trim(),
        descripcion: form.descripcion.trim()
      }, token)
      const datos = await obtenerDetalleBusqueda(busquedaId, token)
      setBusqueda(datos)
      cerrarForm()
    } catch (err) {
      setErrorForm(err instanceof Error ? err.message : 'Ocurrió un error al agregar la etapa.')
    } finally {
      setEnviando(false)
    }
  }

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
            {!mostrarForm && (
              <Boton variante="primario" onClick={abrirForm}>
                + Agregar etapa
              </Boton>
            )}
          </div>
        </div>

        {errorCarga && <Alerta tono="error">{errorCarga}</Alerta>}

        {/* Formulario agregar etapa */}
        {mostrarForm && (
          <div className="formulario-pregunta-panel">
            <h3 className="formulario-pregunta-titulo">Nueva etapa</h3>
            {errorForm && <Alerta tono="error">{errorForm}</Alerta>}
            <form onSubmit={manejarEnvio} noValidate>
              <CampoFormulario etiqueta="Título" htmlFor="etapa-titulo" error={erroresForm.titulo}>
                <input
                  id="etapa-titulo"
                  type="text"
                  maxLength={200}
                  value={form.titulo}
                  onChange={(e) => {
                    setForm((p) => ({ ...p, titulo: e.target.value }))
                    if (erroresForm.titulo) setErroresForm((p) => ({ ...p, titulo: undefined }))
                  }}
                  disabled={enviando}
                  placeholder="Ej. Punto de partida"
                />
              </CampoFormulario>
              <CampoFormulario etiqueta="Descripción" htmlFor="etapa-descripcion" error={erroresForm.descripcion}>
                <textarea
                  id="etapa-descripcion"
                  rows={3}
                  maxLength={1000}
                  value={form.descripcion}
                  onChange={(e) => {
                    setForm((p) => ({ ...p, descripcion: e.target.value }))
                    if (erroresForm.descripcion) setErroresForm((p) => ({ ...p, descripcion: undefined }))
                  }}
                  disabled={enviando}
                  placeholder="Describe qué debe hacer el participante en esta etapa"
                />
              </CampoFormulario>
              <div className="acciones-formulario-trivia">
                <Boton variante="volver" type="button" onClick={cerrarForm} disabled={enviando}>
                  Cancelar
                </Boton>
                <Boton variante="primario" type="submit" disabled={enviando}>
                  {enviando ? 'Guardando…' : 'Agregar etapa'}
                </Boton>
              </div>
            </form>
          </div>
        )}

        {/* Lista de etapas */}
        {busqueda.etapas.length === 0 && !mostrarForm ? (
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
                </div>
                <p className="pregunta-enunciado"><strong>{etapa.titulo}</strong></p>
                <p className="pregunta-enunciado">{etapa.descripcion}</p>
                {etapa.misiones.length > 0 && (
                  <ul className="pregunta-opciones">
                    {etapa.misiones.map((mision) => (
                      <li key={mision.id} className="pregunta-opcion">
                        <strong>{mision.titulo}</strong>
                        {' '}·{' '}{mision.tipo}
                        {mision.pistaClave && <span> — Pista: {mision.pistaClave}</span>}
                      </li>
                    ))}
                  </ul>
                )}
              </div>
            ))}
          </div>
        )}
      </section>
    </LayoutPanel>
  )
}
