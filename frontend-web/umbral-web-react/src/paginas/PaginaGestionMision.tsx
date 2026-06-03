
import { useEffect, useState } from 'react'
import { useNavigate, useParams } from 'react-router-dom'
import { LayoutPanel } from '../componentes/LayoutPanel'
import { Alerta } from '../componentes/Alerta'
import { Boton } from '../componentes/Boton'
import { CampoFormulario } from '../componentes/CampoFormulario'
import {
  obtenerDetalleMision,
  agregarEtapa,
  eliminarEtapa,
  activarMision,
  modificarMision,
  obtenerBusquedasActivas,
  obtenerTriviasActivas,
  type MisionDetalleDto,
  type BusquedaTesoroResumenDto,
  type TriviaActivaResumenDto
} from '../autenticacion/clienteApiJuegos'
import { usarAutenticacion } from '../autenticacion/ProveedorAutenticacion'

const TIPO_BUSQUEDA = 1
const TIPO_TRIVIA = 0

export function PaginaGestionMision() {
  const { misionId } = useParams<{ misionId: string }>()
  const { token, usuario } = usarAutenticacion()
  const navegar = useNavigate()
  const rutaBase = usuario?.rol === 'Administrador' ? '/administrador/misiones' : '/operador/misiones'

  const [mision, setMision] = useState<MisionDetalleDto | null>(null)
  const [estadoCarga, setEstadoCarga] = useState<'cargando' | 'error' | 'listo'>('cargando')
  const [errorCarga, setErrorCarga] = useState<string | null>(null)

  const [activando, setActivando] = useState(false)
  const [errorActivacion, setErrorActivacion] = useState<string | null>(null)

  // Formulario de edición
  const [mostrarFormEditar, setMostrarFormEditar] = useState(false)
  const [formEditar, setFormEditar] = useState({ nombre: '', descripcion: '', dificultad: '1' })
  const [errorFormEditar, setErrorFormEditar] = useState<string | null>(null)
  const [enviandoEditar, setEnviandoEditar] = useState(false)

  const [mostrarFormEtapa, setMostrarFormEtapa] = useState(false)
  const [tipoEtapa, setTipoEtapa] = useState<string>('1')
  const [modoDeJuegoId, setModoDeJuegoId] = useState('')
  const [busquedasActivas, setBusquedasActivas] = useState<BusquedaTesoroResumenDto[]>([])
  const [triviasActivas, setTriviasActivas] = useState<TriviaActivaResumenDto[]>([])
  const [errorFormEtapa, setErrorFormEtapa] = useState<string | null>(null)
  const [enviandoEtapa, setEnviandoEtapa] = useState(false)
  const [eliminandoEtapaId, setEliminandoEtapaId] = useState<string | null>(null)

  useEffect(() => {
    if (!misionId || !token) return
    let cancelado = false
    async function cargar() {
      setEstadoCarga('cargando')
      try {
        const [detalle, busquedas, trivias] = await Promise.all([
          obtenerDetalleMision(misionId!, token!),
          obtenerBusquedasActivas(token!),
          obtenerTriviasActivas(token!)
        ])
        if (cancelado) return
        setMision(detalle)
        setBusquedasActivas(busquedas)
        setTriviasActivas(trivias)
        setEstadoCarga('listo')
      } catch (e) {
        if (cancelado) return
        setErrorCarga(e instanceof Error ? e.message : 'No fue posible cargar la misión.')
        setEstadoCarga('error')
      }
    }
    cargar()
    return () => { cancelado = true }
  }, [misionId, token])

  async function recargar() {
    if (!token || !misionId) return
    const detalle = await obtenerDetalleMision(misionId, token)
    setMision(detalle)
  }

  function abrirFormEditar() {
    if (!mision) return
    const nivelANum: Record<string, string> = { Baja: '0', Media: '1', Dificil: '2' }
    setFormEditar({
      nombre: mision.nombre,
      descripcion: mision.descripcion,
      dificultad: nivelANum[mision.dificultad] ?? '1'
    })
    setErrorFormEditar(null)
    setMostrarFormEditar(true)
  }

  async function manejarEditar(e: React.FormEvent) {
    e.preventDefault()
    if (!formEditar.nombre.trim()) { setErrorFormEditar('El nombre es obligatorio.'); return }
    if (!formEditar.descripcion.trim()) { setErrorFormEditar('La descripción es obligatoria.'); return }
    if (!token || !misionId) return
    setEnviandoEditar(true); setErrorFormEditar(null)
    try {
      await modificarMision(misionId, {
        nombre: formEditar.nombre.trim(),
        descripcion: formEditar.descripcion.trim(),
        dificultad: Number(formEditar.dificultad)
      }, token)
      await recargar()
      setMostrarFormEditar(false)
    } catch (err) { setErrorFormEditar(err instanceof Error ? err.message : 'Error al guardar los cambios.') }
    finally { setEnviandoEditar(false) }
  }

  async function manejarActivar() {
    if (!token || !misionId) return
    setActivando(true); setErrorActivacion(null)
    try { await activarMision(misionId, token); await recargar() }
    catch (err) { setErrorActivacion(err instanceof Error ? err.message : 'Error al activar.') }
    finally { setActivando(false) }
  }

  async function manejarAgregarEtapa(e: React.FormEvent) {
    e.preventDefault()
    if (!modoDeJuegoId) { setErrorFormEtapa('Seleccione un contenido de juego.'); return }
    if (!token || !misionId) return
    setEnviandoEtapa(true); setErrorFormEtapa(null)
    try {
      await agregarEtapa(misionId, { tipoModoDeJuego: Number(tipoEtapa), modoDeJuegoId }, token)
      await recargar()
      setMostrarFormEtapa(false); setModoDeJuegoId('')
    } catch (err) { setErrorFormEtapa(err instanceof Error ? err.message : 'Error al agregar la etapa.') }
    finally { setEnviandoEtapa(false) }
  }

  async function manejarEliminarEtapa(etapaId: string) {
    if (!token || !misionId) return
    setEliminandoEtapaId(etapaId)
    try { await eliminarEtapa(misionId, etapaId, token); await recargar() }
    catch (err) { setErrorCarga(err instanceof Error ? err.message : 'Error al eliminar la etapa.') }
    finally { setEliminandoEtapaId(null) }
  }

  const contenidosPorTipo = Number(tipoEtapa) === TIPO_BUSQUEDA
    ? busquedasActivas.map(b => ({ id: b.id, nombre: b.nombre }))
    : triviasActivas.map(t => ({ id: t.id, nombre: t.nombre }))

  if (estadoCarga === 'cargando') {
    return <LayoutPanel titulo="Misión" descripcion="Cargando…"><p className="tabla-estado-mensaje">Cargando…</p></LayoutPanel>
  }

  if (estadoCarga === 'error' || !mision) {
    return (
      <LayoutPanel titulo="Misión" descripcion="Error al cargar.">
        <Alerta tono="error">{errorCarga ?? 'Error desconocido.'}</Alerta>
        <Boton variante="volver" onClick={() => navegar(rutaBase)}>Volver</Boton>
      </LayoutPanel>
    )
  }

  const esInactiva = mision.estado === 'Inactiva'

  return (
    <LayoutPanel titulo={mision.nombre} descripcion="Etapas de la misión">
      <section className="seccion">
        <div className="seccion-cabecera">
          <div>
            <h2>{mision.nombre}</h2>
            <p>{mision.descripcion}</p>
            <div style={{ display: 'flex', gap: 12, marginTop: 4, alignItems: 'center', flexWrap: 'wrap' }}>
              <span className={`estado-badge estado-badge-${mision.estado.toLowerCase()}`}>{mision.estado}</span>
              <span className={`estado-badge estado-badge-dificultad-${(mision.dificultad ?? 'media').toLowerCase()}`}>{mision.dificultad}</span>
              {mision.tiempoTotal > 0 && (
                <span className="trivia-card-meta">Tiempo total: {mision.tiempoTotal}s</span>
              )}
            </div>
          </div>
          <div className="cabecera-pagina-acciones">
            <Boton variante="volver" onClick={() => navegar(rutaBase)}>Volver</Boton>
            {esInactiva && !mostrarFormEditar && !mostrarFormEtapa && (
              <Boton variante="fantasma" onClick={abrirFormEditar}>
                Editar
              </Boton>
            )}
            {esInactiva && !mostrarFormEtapa && !mostrarFormEditar && (
              <Boton variante="secundario" onClick={() => setMostrarFormEtapa(true)}>
                + Agregar etapa
              </Boton>
            )}
            {esInactiva && mision.etapas.length > 0 && !mostrarFormEditar && (
              <Boton variante="primario" onClick={manejarActivar} disabled={activando}>
                {activando ? 'Activando…' : 'Activar misión'}
              </Boton>
            )}
          </div>
        </div>

        {errorCarga && <Alerta tono="error">{errorCarga}</Alerta>}
        {errorActivacion && <Alerta tono="error">{errorActivacion}</Alerta>}

        {mostrarFormEditar && (
          <div className="formulario-pregunta-panel">
            <h3 className="formulario-pregunta-titulo">Editar misión</h3>
            {errorFormEditar && <Alerta tono="error">{errorFormEditar}</Alerta>}
            <form onSubmit={manejarEditar} noValidate>
              <CampoFormulario etiqueta="Nombre" htmlFor="editar-mision-nombre">
                <input id="editar-mision-nombre" type="text" maxLength={200}
                  value={formEditar.nombre}
                  onChange={(e) => setFormEditar(p => ({ ...p, nombre: e.target.value }))}
                  disabled={enviandoEditar} />
              </CampoFormulario>
              <CampoFormulario etiqueta="Descripción" htmlFor="editar-mision-desc">
                <textarea id="editar-mision-desc" rows={3} maxLength={1000}
                  value={formEditar.descripcion}
                  onChange={(e) => setFormEditar(p => ({ ...p, descripcion: e.target.value }))}
                  disabled={enviandoEditar} />
              </CampoFormulario>
              <CampoFormulario etiqueta="Dificultad" htmlFor="editar-mision-dificultad">
                <select id="editar-mision-dificultad" value={formEditar.dificultad}
                  onChange={(e) => setFormEditar(p => ({ ...p, dificultad: e.target.value }))}
                  disabled={enviandoEditar}>
                  <option value="0">Baja</option>
                  <option value="1">Media</option>
                  <option value="2">Difícil</option>
                </select>
              </CampoFormulario>
              <div className="acciones-formulario-trivia">
                <Boton variante="volver" type="button"
                  onClick={() => setMostrarFormEditar(false)}
                  disabled={enviandoEditar}>Cancelar</Boton>
                <Boton variante="primario" type="submit" disabled={enviandoEditar}>
                  {enviandoEditar ? 'Guardando…' : 'Guardar cambios'}
                </Boton>
              </div>
            </form>
          </div>
        )}

        {mostrarFormEtapa && (
          <div className="formulario-pregunta-panel">
            <h3 className="formulario-pregunta-titulo">Nueva etapa</h3>
            {errorFormEtapa && <Alerta tono="error">{errorFormEtapa}</Alerta>}
            <form onSubmit={manejarAgregarEtapa} noValidate>
              <CampoFormulario etiqueta="Tipo de contenido" htmlFor="etapa-tipo">
                <select id="etapa-tipo" value={tipoEtapa}
                  onChange={(e) => { setTipoEtapa(e.target.value); setModoDeJuegoId('') }}
                  disabled={enviandoEtapa}>
                  <option value="1">Búsqueda del tesoro</option>
                  <option value="0">Trivia</option>
                </select>
              </CampoFormulario>
              <CampoFormulario etiqueta="Contenido de juego" htmlFor="etapa-contenido"
                ayuda="Solo se muestran contenidos activos.">
                <select id="etapa-contenido" value={modoDeJuegoId}
                  onChange={(e) => setModoDeJuegoId(e.target.value)}
                  disabled={enviandoEtapa}>
                  <option value="">— Seleccione —</option>
                  {contenidosPorTipo.map(c => (
                    <option key={c.id} value={c.id}>{c.nombre}</option>
                  ))}
                </select>
              </CampoFormulario>
              <div className="acciones-formulario-trivia">
                <Boton variante="volver" type="button"
                  onClick={() => { setMostrarFormEtapa(false); setModoDeJuegoId('') }}
                  disabled={enviandoEtapa}>Cancelar</Boton>
                <Boton variante="primario" type="submit" disabled={enviandoEtapa}>
                  {enviandoEtapa ? 'Agregando…' : 'Agregar etapa'}
                </Boton>
              </div>
            </form>
          </div>
        )}

        {mision.etapas.length === 0 && !mostrarFormEtapa && (
          <p className="tabla-estado-mensaje">
            Esta misión no tiene etapas.
            {esInactiva && ' Haga clic en "Agregar etapa" para comenzar.'}
          </p>
        )}

        {mision.etapas.length > 0 && (
          <div className="lista-trivias" style={{ marginTop: 12 }}>
            {mision.etapas.map((etapa) => (
              <div key={etapa.id} className="trivia-card">
                <div className="trivia-card-cabecera">
                  <span className="trivia-card-nombre">Etapa {etapa.orden}</span>
                  <span className="trivia-card-meta">
                    {etapa.tipoModoDeJuego}
                    {etapa.tiempoEstimado > 0 && ` · ${etapa.tiempoEstimado}s`}
                  </span>
                </div>
                <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', marginTop: 4 }}>
                  <p className="trivia-card-desc" style={{ margin: 0 }}>{etapa.nombreModoDeJuego}</p>
                  {esInactiva && (
                    <Boton variante="peligro"
                      onClick={() => manejarEliminarEtapa(etapa.id)}
                      disabled={eliminandoEtapaId === etapa.id}>
                      {eliminandoEtapaId === etapa.id ? 'Eliminando…' : 'Eliminar'}
                    </Boton>
                  )}
                </div>
              </div>
            ))}
          </div>
        )}
      </section>
    </LayoutPanel>
  )
}
