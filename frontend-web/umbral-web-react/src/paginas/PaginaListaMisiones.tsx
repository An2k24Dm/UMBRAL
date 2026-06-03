import { useEffect, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { LayoutPanel } from '../componentes/LayoutPanel'
import { Alerta } from '../componentes/Alerta'
import { Boton } from '../componentes/Boton'
import { CampoFormulario } from '../componentes/CampoFormulario'
import {
  obtenerMisionesEnBorrador,
  obtenerMisionesActivas,
  crearMision,
  activarMision,
  desactivarMision,
  eliminarMision,
  type MisionResumenDto
} from '../autenticacion/clienteApiJuegos'
import { usarAutenticacion } from '../autenticacion/ProveedorAutenticacion'

type Filtro = 'todas' | 'inactivas' | 'activas'

function formatearFecha(iso: string): string {
  return new Date(iso).toLocaleDateString('es-VE', {
    day: '2-digit', month: '2-digit', year: 'numeric'
  })
}

export function PaginaListaMisiones() {
  const { token, usuario } = usarAutenticacion()
  const navegar = useNavigate()
  const rutaBase = usuario?.rol === 'Administrador' ? '/administrador/misiones' : '/operador/misiones'

  const [cargando, setCargando] = useState(true)
  const [mensajeError, setMensajeError] = useState<string | null>(null)
  const [misiones, setMisiones] = useState<MisionResumenDto[]>([])
  const [filtro, setFiltro] = useState<Filtro>('todas')
  const [procesandoId, setProcesandoId] = useState<string | null>(null)

  // Formulario crear misión
  const [mostrarFormCrear, setMostrarFormCrear] = useState(false)
  const [formCrear, setFormCrear] = useState({ nombre: '', descripcion: '', dificultad: '1' })
  const [errorFormCrear, setErrorFormCrear] = useState<string | null>(null)
  const [enviandoCrear, setEnviandoCrear] = useState(false)

  async function cargar(ref?: { cancelado: boolean }) {
    if (!token) { setMensajeError('Debe iniciar sesión.'); setCargando(false); return }
    setCargando(true); setMensajeError(null)
    try {
      const [inactivas, activas] = await Promise.all([
        obtenerMisionesEnBorrador(token),
        obtenerMisionesActivas(token)
      ])
      if (ref?.cancelado) return
      const todas = [...inactivas, ...activas].sort(
        (a, b) => new Date(b.fechaCreacion).getTime() - new Date(a.fechaCreacion).getTime()
      )
      setMisiones(todas)
    } catch (e) {
      if (ref?.cancelado) return
      setMensajeError(e instanceof Error ? e.message : 'No fue posible cargar las misiones.')
    } finally {
      if (!ref?.cancelado) setCargando(false)
    }
  }

  useEffect(() => {
    const ref = { cancelado: false }
    cargar(ref)
    return () => { ref.cancelado = true }
  }, [token])

  async function manejarCrear(e: React.FormEvent) {
    e.preventDefault()
    if (!formCrear.nombre.trim()) { setErrorFormCrear('El nombre es obligatorio.'); return }
    if (!formCrear.descripcion.trim()) { setErrorFormCrear('La descripción es obligatoria.'); return }
    if (!token) return
    setEnviandoCrear(true); setErrorFormCrear(null)
    try {
      const id = await crearMision({ nombre: formCrear.nombre.trim(), descripcion: formCrear.descripcion.trim(), dificultad: Number(formCrear.dificultad) }, token)
      setMostrarFormCrear(false); setFormCrear({ nombre: '', descripcion: '', dificultad: '1' })
      navegar(`${rutaBase}/${id}`)
    } catch (err) { setErrorFormCrear(err instanceof Error ? err.message : 'Error al crear la misión.') }
    finally { setEnviandoCrear(false) }
  }

  async function manejarActivar(e: React.MouseEvent, misionId: string) {
    e.stopPropagation()
    if (!token) return
    setProcesandoId(misionId); setMensajeError(null)
    try { await activarMision(misionId, token); await cargar() }
    catch (err) { setMensajeError(err instanceof Error ? err.message : 'No fue posible activar la misión.') }
    finally { setProcesandoId(null) }
  }

  async function manejarDesactivar(e: React.MouseEvent, misionId: string) {
    e.stopPropagation()
    if (!token) return
    setProcesandoId(misionId); setMensajeError(null)
    try { await desactivarMision(misionId, token); await cargar() }
    catch (err) { setMensajeError(err instanceof Error ? err.message : 'No fue posible desactivar la misión.') }
    finally { setProcesandoId(null) }
  }

  async function manejarEliminar(e: React.MouseEvent, misionId: string) {
    e.stopPropagation()
    if (!token) return
    setProcesandoId(misionId); setMensajeError(null)
    try { await eliminarMision(misionId, token); await cargar() }
    catch (err) { setMensajeError(err instanceof Error ? err.message : 'No fue posible eliminar la misión.') }
    finally { setProcesandoId(null) }
  }

  const misionesVisibles = misiones.filter(m =>
    filtro === 'todas' ? true :
    filtro === 'inactivas' ? m.estado === 'Inactiva' :
    m.estado === 'Activa'
  )
  const totalInactivas = misiones.filter(m => m.estado === 'Inactiva').length
  const totalActivas   = misiones.filter(m => m.estado === 'Activa').length

  return (
    <LayoutPanel titulo="Misiones" descripcion="Gestione sus misiones: secuencias de etapas de juego.">
      <section className="seccion">
        <div className="seccion-cabecera">
          <div>
            <h2>Misiones</h2>
            <p>Haga clic en una misión para gestionar sus etapas.</p>
          </div>
          <div className="cabecera-pagina-acciones">
            {!mostrarFormCrear && (
              <Boton variante="primario" onClick={() => setMostrarFormCrear(true)}>
                + Crear misión
              </Boton>
            )}
          </div>
        </div>

        {mostrarFormCrear && (
          <div className="formulario-pregunta-panel">
            <h3 className="formulario-pregunta-titulo">Nueva misión</h3>
            {errorFormCrear && <Alerta tono="error">{errorFormCrear}</Alerta>}
            <form onSubmit={manejarCrear} noValidate>
              <CampoFormulario etiqueta="Nombre" htmlFor="mision-nombre">
                <input id="mision-nombre" type="text" maxLength={200}
                  value={formCrear.nombre}
                  onChange={(e) => setFormCrear(p => ({ ...p, nombre: e.target.value }))}
                  disabled={enviandoCrear} placeholder="Ej. Misión Parque Central" />
              </CampoFormulario>
              <CampoFormulario etiqueta="Descripción" htmlFor="mision-desc">
                <textarea id="mision-desc" rows={3} maxLength={1000}
                  value={formCrear.descripcion}
                  onChange={(e) => setFormCrear(p => ({ ...p, descripcion: e.target.value }))}
                  disabled={enviandoCrear} placeholder="Describe el objetivo de la misión" />
              </CampoFormulario>
              <CampoFormulario etiqueta="Dificultad" htmlFor="mision-dificultad">
                <select id="mision-dificultad" value={formCrear.dificultad}
                  onChange={(e) => setFormCrear(p => ({ ...p, dificultad: e.target.value }))}
                  disabled={enviandoCrear}>
                  <option value="0">Baja</option>
                  <option value="1">Media</option>
                  <option value="2">Difícil</option>
                </select>
              </CampoFormulario>
              <div className="acciones-formulario-trivia">
                <Boton variante="volver" type="button"
                  onClick={() => { setMostrarFormCrear(false); setFormCrear({ nombre: '', descripcion: '', dificultad: '1' }) }}
                  disabled={enviandoCrear}>Cancelar</Boton>
                <Boton variante="primario" type="submit" disabled={enviandoCrear}>
                  {enviandoCrear ? 'Creando…' : 'Crear misión'}
                </Boton>
              </div>
            </form>
          </div>
        )}

        {/* Filtros */}
        <div style={{ display: 'flex', gap: 8, marginBottom: 16 }}>
          {(['todas', 'inactivas', 'activas'] as Filtro[]).map(f => (
            <Boton key={f} variante={filtro === f ? 'secundario' : 'fantasma'} onClick={() => setFiltro(f)}>
              {f === 'todas' ? `Todas (${misiones.length})` :
               f === 'inactivas' ? `Inactivas (${totalInactivas})` : `Activas (${totalActivas})`}
            </Boton>
          ))}
        </div>

        {mensajeError && <Alerta tono="error">{mensajeError}</Alerta>}
        {cargando && <p className="tabla-estado-mensaje">Cargando misiones…</p>}

        {!cargando && misionesVisibles.length === 0 && (
          <p className="tabla-estado-mensaje">
            {misiones.length === 0
              ? 'No tiene misiones. Cree una para comenzar.'
              : `No hay misiones ${filtro === 'inactivas' ? 'inactivas' : 'activas'}.`}
          </p>
        )}

        {!cargando && misionesVisibles.length > 0 && (
          <div className="lista-trivias">
            {misionesVisibles.map((m) => (
              <div
                key={m.id}
                className="trivia-card"
                role="button"
                tabIndex={0}
                onClick={() => navegar(`${rutaBase}/${m.id}`)}
                onKeyDown={(e) => e.key === 'Enter' && navegar(`${rutaBase}/${m.id}`)}
                style={{
                  borderLeft: `4px solid ${m.estado === 'Activa' ? '#22c55e' : '#94a3b8'}`,
                  opacity: m.estado === 'Inactiva' ? 0.85 : 1
                }}
              >
                <div className="trivia-card-cabecera">
                  <span className="trivia-card-nombre">{m.nombre}</span>
                  <span className="trivia-card-meta">
                    {m.totalEtapas} {m.totalEtapas === 1 ? 'etapa' : 'etapas'}
                    &nbsp;·&nbsp;{formatearFecha(m.fechaCreacion)}
                  </span>
                </div>
                <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', marginTop: 4 }}>
                  <p className="trivia-card-desc" style={{ margin: 0 }}>{m.descripcion}</p>
                  <div style={{ display: 'flex', gap: 6 }}>
                    <span className={`estado-badge estado-badge-dificultad-${(m.dificultad ?? 'media').toLowerCase()}`}>{m.dificultad}</span>
                    <span className={`estado-badge estado-badge-${m.estado.toLowerCase()}`}>{m.estado}</span>
                  </div>
                </div>
                <div className="acciones-formulario-trivia" style={{ marginTop: 8 }}>
                  {m.estado === 'Inactiva' ? (
                    <>
                      <Boton variante="secundario" onClick={(e) => manejarActivar(e, m.id)} disabled={procesandoId === m.id}>
                        {procesandoId === m.id ? 'Activando…' : 'Activar'}
                      </Boton>
                      <Boton variante="peligro" onClick={(e) => manejarEliminar(e, m.id)} disabled={procesandoId === m.id}>
                        {procesandoId === m.id ? 'Eliminando…' : 'Eliminar'}
                      </Boton>
                    </>
                  ) : (
                    <Boton variante="peligro" onClick={(e) => manejarDesactivar(e, m.id)} disabled={procesandoId === m.id}>
                      {procesandoId === m.id ? 'Desactivando…' : 'Desactivar'}
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
