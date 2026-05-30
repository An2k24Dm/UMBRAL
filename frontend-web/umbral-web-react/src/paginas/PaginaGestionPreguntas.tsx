import { useEffect, useState } from 'react'
import { useNavigate, useParams } from 'react-router-dom'
import { LayoutPanel } from '../componentes/LayoutPanel'
import { Alerta } from '../componentes/Alerta'
import { Boton } from '../componentes/Boton'
import { CampoFormulario } from '../componentes/CampoFormulario'
import {
  obtenerDetalleTrivia,
  agregarPregunta,
  modificarPregunta,
  eliminarPregunta,
  activarTrivia,
  modificarTrivia,
  archivarTrivia,
  type TriviaDetalleDto,
  type PreguntaDetalleDto,
  type OpcionInput
} from '../autenticacion/clienteApiJuegos'
import { usarAutenticacion } from '../autenticacion/ProveedorAutenticacion'

// ---------------------------------------------------------------------------
// Tipos internos del formulario
// ---------------------------------------------------------------------------
interface OpcionForm {
  _key: string
  texto: string
  esCorrecta: boolean
}

interface FormPregunta {
  enunciado: string
  puntajeAsignado: string
  opciones: OpcionForm[]
}

interface ErroresPregunta {
  enunciado?: string
  puntajeAsignado?: string
  opciones?: string
}

let _contadorKey = 0
function nuevaKey() { return String(++_contadorKey) }

function opcionesVacias(): OpcionForm[] {
  return [
    { _key: nuevaKey(), texto: '', esCorrecta: false },
    { _key: nuevaKey(), texto: '', esCorrecta: false }
  ]
}

const FORM_VACIO: FormPregunta = {
  enunciado: '',
  puntajeAsignado: '10',
  opciones: opcionesVacias()
}

// ---------------------------------------------------------------------------
// Validación del formulario de pregunta
// ---------------------------------------------------------------------------
function validarPregunta(form: FormPregunta): ErroresPregunta {
  const err: ErroresPregunta = {}
  if (!form.enunciado.trim()) err.enunciado = 'El enunciado es obligatorio.'
  else if (form.enunciado.trim().length > 500) err.enunciado = 'Máximo 500 caracteres.'
  const puntaje = Number(form.puntajeAsignado)
  if (!form.puntajeAsignado || isNaN(puntaje) || puntaje <= 0)
    err.puntajeAsignado = 'El puntaje debe ser mayor a 0.'
  if (form.opciones.length < 2)
    err.opciones = 'La pregunta debe tener al menos dos opciones.'
  else if (form.opciones.some((o) => !o.texto.trim()))
    err.opciones = 'Todas las opciones deben tener texto.'
  else if (!form.opciones.some((o) => o.esCorrecta))
    err.opciones = 'Al menos una opción debe estar marcada como correcta.'
  return err
}

// ---------------------------------------------------------------------------
// Componente principal
// ---------------------------------------------------------------------------
export function PaginaGestionPreguntas() {
  const { triviaId } = useParams<{ triviaId: string }>()
  const { token, usuario } = usarAutenticacion()
  const navegar = useNavigate()
  const rutaBase = usuario?.rol === 'Administrador' ? '/administrador/trivias' : '/operador/trivias'

  const [trivia, setTrivia] = useState<TriviaDetalleDto | null>(null)
  const [estadoCarga, setEstadoCarga] = useState<'cargando' | 'error' | 'listo'>('cargando')
  const [errorCarga, setErrorCarga] = useState<string | null>(null)

  const [modoForm, setModoForm] = useState<'oculto' | 'agregar' | 'editar'>('oculto')
  const [editandoId, setEditandoId] = useState<string | null>(null)
  const [form, setForm] = useState<FormPregunta>(FORM_VACIO)
  const [erroresForm, setErroresForm] = useState<ErroresPregunta>({})
  const [errorForm, setErrorForm] = useState<string | null>(null)
  const [enviando, setEnviando] = useState(false)

  const [confirmandoEliminacion, setConfirmandoEliminacion] = useState<string | null>(null)
  const [eliminando, setEliminando] = useState(false)

  const [activando, setActivando] = useState(false)
  const [errorActivacion, setErrorActivacion] = useState<string | null>(null)

  const [mostrarFormTrivia, setMostrarFormTrivia] = useState(false)
  const [formTrivia, setFormTrivia] = useState({ nombre: '', descripcion: '', tiempoLimitePorPregunta: '' })
  const [enviandoTrivia, setEnviandoTrivia] = useState(false)
  const [errorFormTrivia, setErrorFormTrivia] = useState<string | null>(null)

  const [confirmandoArchivado, setConfirmandoArchivado] = useState(false)
  const [archivando, setArchivando] = useState(false)
  const [errorArchivado, setErrorArchivado] = useState<string | null>(null)

  // ---------------------------------------------------------------------------
  // Carga inicial
  // ---------------------------------------------------------------------------
  useEffect(() => {
    if (!triviaId) return
    let cancelado = false
    async function cargar() {
      if (!token) { setEstadoCarga('error'); setErrorCarga('Debe iniciar sesión.'); return }
      setEstadoCarga('cargando')
      setErrorCarga(null)
      try {
        const datos = await obtenerDetalleTrivia(triviaId!, token)
        if (cancelado) return
        setTrivia(datos)
        setEstadoCarga('listo')
      } catch (e) {
        if (cancelado) return
        setErrorCarga(e instanceof Error ? e.message : 'No fue posible cargar la trivia.')
        setEstadoCarga('error')
      }
    }
    cargar()
    return () => { cancelado = true }
  }, [triviaId, token])

  // ---------------------------------------------------------------------------
  // Helpers del formulario
  // ---------------------------------------------------------------------------
  function abrirFormAgregar() {
    setForm({ ...FORM_VACIO, opciones: opcionesVacias() })
    setErroresForm({})
    setErrorForm(null)
    setEditandoId(null)
    setModoForm('agregar')
  }

  function abrirFormEditar(pregunta: PreguntaDetalleDto) {
    setForm({
      enunciado: pregunta.enunciado,
      puntajeAsignado: String(pregunta.puntajeAsignado),
      opciones: pregunta.opciones.map((o) => ({
        _key: nuevaKey(),
        texto: o.texto,
        esCorrecta: o.esCorrecta
      }))
    })
    setErroresForm({})
    setErrorForm(null)
    setEditandoId(pregunta.id)
    setModoForm('editar')
  }

  function cerrarForm() {
    setModoForm('oculto')
    setEditandoId(null)
    setErroresForm({})
    setErrorForm(null)
  }

  function cambiarEnunciado(valor: string) {
    setForm((p) => ({ ...p, enunciado: valor }))
    if (erroresForm.enunciado) setErroresForm((p) => ({ ...p, enunciado: undefined }))
  }

  function cambiarPuntaje(valor: string) {
    setForm((p) => ({ ...p, puntajeAsignado: valor }))
    if (erroresForm.puntajeAsignado) setErroresForm((p) => ({ ...p, puntajeAsignado: undefined }))
  }

  function cambiarOpcionTexto(key: string, valor: string) {
    setForm((p) => ({
      ...p,
      opciones: p.opciones.map((o) => o._key === key ? { ...o, texto: valor } : o)
    }))
    if (erroresForm.opciones) setErroresForm((p) => ({ ...p, opciones: undefined }))
  }

  function cambiarOpcionCorrecta(key: string) {
    setForm((p) => ({
      ...p,
      opciones: p.opciones.map((o) => o._key === key ? { ...o, esCorrecta: !o.esCorrecta } : o)
    }))
    if (erroresForm.opciones) setErroresForm((p) => ({ ...p, opciones: undefined }))
  }

  function agregarOpcion() {
    setForm((p) => ({
      ...p,
      opciones: [...p.opciones, { _key: nuevaKey(), texto: '', esCorrecta: false }]
    }))
  }

  function quitarOpcion(key: string) {
    if (form.opciones.length <= 2) return
    setForm((p) => ({ ...p, opciones: p.opciones.filter((o) => o._key !== key) }))
  }

  // ---------------------------------------------------------------------------
  // Envío del formulario
  // ---------------------------------------------------------------------------
  async function manejarEnvio(e: React.FormEvent) {
    e.preventDefault()
    const erroresValidacion = validarPregunta(form)
    if (Object.keys(erroresValidacion).length > 0) {
      setErroresForm(erroresValidacion)
      return
    }
    if (!token || !triviaId) return

    const opcionesInput: OpcionInput[] = form.opciones.map((o) => ({
      texto: o.texto.trim(),
      esCorrecta: o.esCorrecta
    }))

    setEnviando(true)
    setErrorForm(null)
    try {
      if (modoForm === 'agregar') {
        await agregarPregunta(
          triviaId,
          {
            enunciado: form.enunciado.trim(),
            puntajeAsignado: Number(form.puntajeAsignado),
            opciones: opcionesInput
          },
          token
        )
      } else if (modoForm === 'editar' && editandoId) {
        await modificarPregunta(
          triviaId,
          editandoId,
          {
            nuevoEnunciado: form.enunciado.trim(),
            nuevasOpciones: opcionesInput
          },
          token
        )
      }
      // Recargar trivia para reflejar cambios
      const datos = await obtenerDetalleTrivia(triviaId, token)
      setTrivia(datos)
      cerrarForm()
    } catch (err) {
      setErrorForm(err instanceof Error ? err.message : 'Ocurrió un error al guardar la pregunta.')
    } finally {
      setEnviando(false)
    }
  }

  // ---------------------------------------------------------------------------
  // Eliminar pregunta
  // ---------------------------------------------------------------------------
  async function confirmarEliminacion(preguntaId: string) {
    if (!token || !triviaId) return
    setEliminando(true)
    try {
      await eliminarPregunta(triviaId, preguntaId, token)
      const datos = await obtenerDetalleTrivia(triviaId, token)
      setTrivia(datos)
      setConfirmandoEliminacion(null)
    } catch (err) {
      // Mostrar error inline en la sección
      setErrorCarga(err instanceof Error ? err.message : 'No fue posible eliminar la pregunta.')
      setConfirmandoEliminacion(null)
    } finally {
      setEliminando(false)
    }
  }

  // ---------------------------------------------------------------------------
  // Activar trivia (HU18)
  // ---------------------------------------------------------------------------
  async function manejarActivar() {
    if (!token || !triviaId) return
    setActivando(true)
    setErrorActivacion(null)
    try {
      await activarTrivia(triviaId, token)
      const datos = await obtenerDetalleTrivia(triviaId, token)
      setTrivia(datos)
    } catch (err) {
      setErrorActivacion(err instanceof Error ? err.message : 'No fue posible activar la trivia.')
    } finally {
      setActivando(false)
    }
  }

  // ---------------------------------------------------------------------------
  // Modificar datos de trivia (HU19)
  // ---------------------------------------------------------------------------
  function abrirFormTrivia() {
    if (!trivia) return
    setFormTrivia({
      nombre: trivia.nombre,
      descripcion: trivia.descripcion,
      tiempoLimitePorPregunta: String(trivia.tiempoLimitePorPregunta)
    })
    setErrorFormTrivia(null)
    setMostrarFormTrivia(true)
  }

  async function manejarGuardarTrivia(e: React.FormEvent) {
    e.preventDefault()
    if (!token || !triviaId) return
    const tiempo = Number(formTrivia.tiempoLimitePorPregunta)
    if (!formTrivia.nombre.trim()) { setErrorFormTrivia('El nombre es obligatorio.'); return }
    if (!formTrivia.descripcion.trim()) { setErrorFormTrivia('La descripción es obligatoria.'); return }
    if (isNaN(tiempo) || tiempo <= 0) { setErrorFormTrivia('El tiempo debe ser mayor a 0.'); return }
    setEnviandoTrivia(true)
    setErrorFormTrivia(null)
    try {
      await modificarTrivia(triviaId, {
        nuevoNombre: formTrivia.nombre.trim(),
        nuevaDescripcion: formTrivia.descripcion.trim(),
        nuevoTiempoLimitePorPregunta: tiempo
      }, token)
      const datos = await obtenerDetalleTrivia(triviaId, token)
      setTrivia(datos)
      setMostrarFormTrivia(false)
    } catch (err) {
      setErrorFormTrivia(err instanceof Error ? err.message : 'No fue posible guardar los cambios.')
    } finally {
      setEnviandoTrivia(false)
    }
  }

  // ---------------------------------------------------------------------------
  // Archivar trivia (HU20)
  // ---------------------------------------------------------------------------
  async function manejarArchivar() {
    if (!token || !triviaId) return
    setArchivando(true)
    setErrorArchivado(null)
    try {
      await archivarTrivia(triviaId, token)
      navegar(rutaBase)
    } catch (err) {
      setErrorArchivado(err instanceof Error ? err.message : 'No fue posible archivar la trivia.')
      setConfirmandoArchivado(false)
    } finally {
      setArchivando(false)
    }
  }

  // ---------------------------------------------------------------------------
  // Render
  // ---------------------------------------------------------------------------
  if (estadoCarga === 'cargando') {
    return (
      <LayoutPanel titulo="Preguntas" descripcion="Cargando trivia…">
        <p className="tabla-estado-mensaje">Cargando…</p>
      </LayoutPanel>
    )
  }

  if (estadoCarga === 'error' || !trivia) {
    return (
      <LayoutPanel titulo="Preguntas" descripcion="Error al cargar la trivia.">
        <Alerta tono="error">{errorCarga ?? 'Error desconocido.'}</Alerta>
        <Boton variante="volver" onClick={() => navegar(rutaBase)}>
          Volver a mis trivias
        </Boton>
      </LayoutPanel>
    )
  }

  return (
    <LayoutPanel
      titulo={trivia.nombre}
      descripcion={`Gestión de preguntas · ${trivia.preguntas.length} ${trivia.preguntas.length === 1 ? 'pregunta' : 'preguntas'}`}
    >
      <section className="seccion">
        {/* Cabecera con info de la trivia */}
        <div className="seccion-cabecera">
          <div>
            <h2>Preguntas de la trivia</h2>
            <p>{trivia.descripcion} · {trivia.tiempoLimitePorPregunta}s por pregunta</p>
            <span className={`estado-badge estado-badge-${trivia.estado.toLowerCase()}`}>
              {trivia.estado}
            </span>
          </div>
          <div className="cabecera-pagina-acciones">
            <Boton variante="volver" onClick={() => navegar(rutaBase)}>
              Volver
            </Boton>
            {modoForm === 'oculto' && !mostrarFormTrivia && !confirmandoArchivado && (
              <Boton variante="fantasma" onClick={abrirFormTrivia}>
                Modificar datos
              </Boton>
            )}
            {trivia.estado === 'Inactiva' && modoForm === 'oculto' && !mostrarFormTrivia && !confirmandoArchivado && (
              <Boton variante="secundario" onClick={manejarActivar} disabled={activando}>
                {activando ? 'Activando…' : 'Activar trivia'}
              </Boton>
            )}
            {trivia.estado !== 'Activa' && modoForm === 'oculto' && !mostrarFormTrivia && (
              confirmandoArchivado ? (
                <>
                  <span className="texto-confirmacion">¿Archivar esta trivia?</span>
                  <Boton variante="peligro" onClick={manejarArchivar} disabled={archivando}>
                    {archivando ? 'Archivando…' : 'Sí, archivar'}
                  </Boton>
                  <Boton variante="volver" onClick={() => setConfirmandoArchivado(false)} disabled={archivando}>
                    Cancelar
                  </Boton>
                </>
              ) : (
                <Boton variante="peligro" onClick={() => setConfirmandoArchivado(true)}>
                  Archivar trivia
                </Boton>
              )
            )}
            {modoForm === 'oculto' && !mostrarFormTrivia && !confirmandoArchivado && (
              <Boton variante="primario" onClick={abrirFormAgregar}>
                + Agregar pregunta
              </Boton>
            )}
          </div>
        </div>

        {errorActivacion && <Alerta tono="error">{errorActivacion}</Alerta>}
        {errorArchivado && <Alerta tono="error">{errorArchivado}</Alerta>}
        {errorCarga && <Alerta tono="error">{errorCarga}</Alerta>}

        {/* Formulario modificar datos trivia (HU19) */}
        {mostrarFormTrivia && (
          <div className="formulario-pregunta-panel">
            <h3 className="formulario-pregunta-titulo">Modificar datos de la trivia</h3>
            {errorFormTrivia && <Alerta tono="error">{errorFormTrivia}</Alerta>}
            <form onSubmit={manejarGuardarTrivia} noValidate>
              <CampoFormulario etiqueta="Nombre" htmlFor="trivia-nombre">
                <input
                  id="trivia-nombre"
                  type="text"
                  maxLength={200}
                  value={formTrivia.nombre}
                  onChange={(e) => setFormTrivia((p) => ({ ...p, nombre: e.target.value }))}
                  disabled={enviandoTrivia}
                />
              </CampoFormulario>
              <CampoFormulario etiqueta="Descripción" htmlFor="trivia-descripcion">
                <textarea
                  id="trivia-descripcion"
                  rows={2}
                  maxLength={1000}
                  value={formTrivia.descripcion}
                  onChange={(e) => setFormTrivia((p) => ({ ...p, descripcion: e.target.value }))}
                  disabled={enviandoTrivia}
                />
              </CampoFormulario>
              <CampoFormulario etiqueta="Tiempo límite por pregunta (segundos)" htmlFor="trivia-tiempo">
                <input
                  id="trivia-tiempo"
                  type="number"
                  min={1}
                  value={formTrivia.tiempoLimitePorPregunta}
                  onChange={(e) => setFormTrivia((p) => ({ ...p, tiempoLimitePorPregunta: e.target.value }))}
                  disabled={enviandoTrivia}
                />
              </CampoFormulario>
              <div className="acciones-formulario-trivia">
                <Boton variante="volver" type="button" onClick={() => setMostrarFormTrivia(false)} disabled={enviandoTrivia}>
                  Cancelar
                </Boton>
                <Boton variante="primario" type="submit" disabled={enviandoTrivia}>
                  {enviandoTrivia ? 'Guardando…' : 'Guardar cambios'}
                </Boton>
              </div>
            </form>
          </div>
        )}

        {/* Formulario agregar / editar */}
        {modoForm !== 'oculto' && (
          <div className="formulario-pregunta-panel">
            <h3 className="formulario-pregunta-titulo">
              {modoForm === 'agregar' ? 'Nueva pregunta' : 'Modificar pregunta'}
            </h3>

            {errorForm && <Alerta tono="error">{errorForm}</Alerta>}

            <form onSubmit={manejarEnvio} noValidate>
              <CampoFormulario
                etiqueta="Enunciado"
                htmlFor="enunciado"
                error={erroresForm.enunciado}
              >
                <textarea
                  id="enunciado"
                  rows={3}
                  maxLength={500}
                  value={form.enunciado}
                  onChange={(e) => cambiarEnunciado(e.target.value)}
                  disabled={enviando}
                  placeholder="Escribe aquí la pregunta"
                />
              </CampoFormulario>

              <CampoFormulario
                etiqueta="Puntaje asignado"
                htmlFor="puntaje"
                error={erroresForm.puntajeAsignado}
              >
                <input
                  id="puntaje"
                  type="number"
                  min={1}
                  value={form.puntajeAsignado}
                  onChange={(e) => cambiarPuntaje(e.target.value)}
                  disabled={enviando}
                />
              </CampoFormulario>

              <div className="campo">
                <label className="campo-etiqueta">Opciones de respuesta</label>
                {erroresForm.opciones && (
                  <span className="error-campo">{erroresForm.opciones}</span>
                )}
                <div className="opciones-editor">
                  {form.opciones.map((opcion) => (
                    <div key={opcion._key} className="opcion-editor-fila">
                      <input
                        type="checkbox"
                        className="opcion-check"
                        title="Marcar como correcta"
                        checked={opcion.esCorrecta}
                        onChange={() => cambiarOpcionCorrecta(opcion._key)}
                        disabled={enviando}
                      />
                      <input
                        type="text"
                        className="opcion-texto"
                        placeholder="Texto de la opción"
                        maxLength={300}
                        value={opcion.texto}
                        onChange={(e) => cambiarOpcionTexto(opcion._key, e.target.value)}
                        disabled={enviando}
                      />
                      <button
                        type="button"
                        className="opcion-quitar"
                        title="Eliminar opción"
                        disabled={form.opciones.length <= 2 || enviando}
                        onClick={() => quitarOpcion(opcion._key)}
                      >
                        ✕
                      </button>
                    </div>
                  ))}
                </div>
                <div style={{ marginTop: 8 }}>
                  <Boton
                    variante="fantasma"
                    type="button"
                    onClick={agregarOpcion}
                    disabled={enviando}
                  >
                    + Agregar opción
                  </Boton>
                </div>
                <span className="ayuda-campo">Marque el checkbox de las opciones correctas.</span>
              </div>

              <div className="acciones-formulario-trivia">
                <Boton variante="volver" type="button" onClick={cerrarForm} disabled={enviando}>
                  Cancelar
                </Boton>
                <Boton variante="primario" type="submit" disabled={enviando}>
                  {enviando ? 'Guardando…' : modoForm === 'agregar' ? 'Agregar pregunta' : 'Guardar cambios'}
                </Boton>
              </div>
            </form>
          </div>
        )}

        {/* Lista de preguntas */}
        {trivia.preguntas.length === 0 && modoForm === 'oculto' ? (
          <p className="tabla-estado-mensaje">
            Esta trivia aún no tiene preguntas. Haga clic en "Agregar pregunta" para comenzar.
          </p>
        ) : (
          <div className="lista-preguntas">
            {trivia.preguntas.map((pregunta, idx) => (
              <div key={pregunta.id} className="pregunta-card">
                <div className="pregunta-card-cabecera">
                  <div className="pregunta-card-info">
                    <span className="pregunta-numero">Pregunta {idx + 1}</span>
                    <span className="pregunta-puntaje">{pregunta.puntajeAsignado} pts</span>
                  </div>
                  <div className="pregunta-card-acciones">
                    {confirmandoEliminacion === pregunta.id ? (
                      <>
                        <span className="texto-confirmacion">¿Confirmar eliminación?</span>
                        <Boton
                          variante="peligro"
                          onClick={() => confirmarEliminacion(pregunta.id)}
                          disabled={eliminando}
                        >
                          {eliminando ? 'Eliminando…' : 'Sí, eliminar'}
                        </Boton>
                        <Boton
                          variante="volver"
                          onClick={() => setConfirmandoEliminacion(null)}
                          disabled={eliminando}
                        >
                          Cancelar
                        </Boton>
                      </>
                    ) : (
                      <>
                        <Boton
                          variante="secundario"
                          onClick={() => { cerrarForm(); abrirFormEditar(pregunta) }}
                        >
                          Modificar
                        </Boton>
                        <Boton
                          variante="peligro"
                          onClick={() => setConfirmandoEliminacion(pregunta.id)}
                        >
                          Eliminar
                        </Boton>
                      </>
                    )}
                  </div>
                </div>
                <p className="pregunta-enunciado">{pregunta.enunciado}</p>
                <ul className="pregunta-opciones">
                  {pregunta.opciones.map((opcion) => (
                    <li
                      key={opcion.id}
                      className={`pregunta-opcion${opcion.esCorrecta ? ' pregunta-opcion-correcta' : ''}`}
                    >
                      {opcion.esCorrecta && <span className="opcion-check-icono">✓</span>}
                      {opcion.texto}
                    </li>
                  ))}
                </ul>
              </div>
            ))}
          </div>
        )}
      </section>
    </LayoutPanel>
  )
}
