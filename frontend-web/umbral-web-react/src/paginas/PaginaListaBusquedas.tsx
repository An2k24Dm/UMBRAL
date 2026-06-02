import { useEffect, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { LayoutPanel } from '../componentes/LayoutPanel'
import { Alerta } from '../componentes/Alerta'
import { Boton } from '../componentes/Boton'
import {
  obtenerBusquedasEnBorrador,
  obtenerBusquedasActivas,
  desactivarBusqueda,
  activarBusqueda,
  eliminarBusqueda,
  type BusquedaTesoroResumenDto
} from '../autenticacion/clienteApiJuegos'
import { usarAutenticacion } from '../autenticacion/ProveedorAutenticacion'

type Filtro = 'todas' | 'inactivas' | 'activas'

function formatearFecha(iso: string): string {
  return new Date(iso).toLocaleDateString('es-VE', {
    day: '2-digit', month: '2-digit', year: 'numeric'
  })
}

export function PaginaListaBusquedas() {
  const { token, usuario } = usarAutenticacion()
  const navegar = useNavigate()
  const rutaBase = usuario?.rol === 'Administrador' ? '/administrador/busquedas' : '/operador/busquedas'

  const [cargando, setCargando] = useState(true)
  const [mensajeError, setMensajeError] = useState<string | null>(null)
  const [busquedas, setBusquedas] = useState<BusquedaTesoroResumenDto[]>([])
  const [filtro, setFiltro] = useState<Filtro>('todas')
  const [procesandoId, setProcesandoId] = useState<string | null>(null)

  async function cargar(ref?: { cancelado: boolean }) {
    if (!token) { setMensajeError('Debe iniciar sesión.'); setCargando(false); return }
    setCargando(true)
    setMensajeError(null)
    try {
      const [inactivas, activas] = await Promise.all([
        obtenerBusquedasEnBorrador(token),
        obtenerBusquedasActivas(token)
      ])
      if (ref?.cancelado) return
      const todas = [...inactivas, ...activas].sort(
        (a, b) => new Date(b.fechaCreacion).getTime() - new Date(a.fechaCreacion).getTime()
      )
      setBusquedas(todas)
    } catch (e) {
      if (ref?.cancelado) return
      setMensajeError(e instanceof Error ? e.message : 'No fue posible cargar las búsquedas.')
    } finally {
      if (!ref?.cancelado) setCargando(false)
    }
  }

  useEffect(() => {
    const ref = { cancelado: false }
    cargar(ref)
    return () => { ref.cancelado = true }
  }, [token])

  async function manejarActivar(e: React.MouseEvent, busquedaId: string) {
    e.stopPropagation()
    if (!token) return
    setProcesandoId(busquedaId)
    setMensajeError(null)
    try { await activarBusqueda(busquedaId, token); await cargar() }
    catch (err) { setMensajeError(err instanceof Error ? err.message : 'No fue posible activar la búsqueda.') }
    finally { setProcesandoId(null) }
  }

  async function manejarDesactivar(e: React.MouseEvent, busquedaId: string) {
    e.stopPropagation()
    if (!token) return
    setProcesandoId(busquedaId)
    setMensajeError(null)
    try { await desactivarBusqueda(busquedaId, token); await cargar() }
    catch (err) { setMensajeError(err instanceof Error ? err.message : 'No fue posible desactivar la búsqueda.') }
    finally { setProcesandoId(null) }
  }

  async function manejarEliminar(e: React.MouseEvent, busquedaId: string) {
    e.stopPropagation()
    if (!token) return
    setProcesandoId(busquedaId)
    setMensajeError(null)
    try { await eliminarBusqueda(busquedaId, token); await cargar() }
    catch (err) { setMensajeError(err instanceof Error ? err.message : 'No fue posible eliminar la búsqueda.') }
    finally { setProcesandoId(null) }
  }

  const busquedasVisibles = busquedas.filter(b =>
    filtro === 'todas' ? true :
    filtro === 'inactivas' ? b.estado === 'Inactiva' :
    b.estado === 'Activa'
  )
  const totalInactivas = busquedas.filter(b => b.estado === 'Inactiva').length
  const totalActivas = busquedas.filter(b => b.estado === 'Activa').length

  return (
    <LayoutPanel titulo="Búsquedas del tesoro" descripcion="Gestione sus búsquedas inactivas y activas.">
      <section className="seccion">
        <div className="seccion-cabecera">
          <div>
            <h2>Búsquedas del tesoro</h2>
            <p>Haga clic en una búsqueda para gestionar sus etapas.</p>
          </div>
          <div className="cabecera-pagina-acciones">
            <Boton variante="primario" onClick={() => navegar(`${rutaBase}/crear`)}>
              + Crear búsqueda
            </Boton>
          </div>
        </div>

        {/* Filtros */}
        <div style={{ display: 'flex', gap: 8, marginBottom: 16 }}>
          {(['todas', 'inactivas', 'activas'] as Filtro[]).map(f => (
            <Boton
              key={f}
              variante={filtro === f ? 'secundario' : 'fantasma'}
              onClick={() => setFiltro(f)}
            >
              {f === 'todas' ? `Todas (${busquedas.length})` :
               f === 'inactivas' ? `Inactivas (${totalInactivas})` :
               `Activas (${totalActivas})`}
            </Boton>
          ))}
        </div>

        {mensajeError && <Alerta tono="error">{mensajeError}</Alerta>}

        {cargando && <p className="tabla-estado-mensaje">Cargando búsquedas del tesoro…</p>}

        {!cargando && busquedasVisibles.length === 0 && (
          <p className="tabla-estado-mensaje">
            {busquedas.length === 0
              ? 'No tiene búsquedas del tesoro. Cree una para comenzar.'
              : `No hay búsquedas ${filtro === 'inactivas' ? 'inactivas' : 'activas'}.`}
          </p>
        )}

        {!cargando && busquedasVisibles.length > 0 && (
          <div className="lista-trivias">
            {busquedasVisibles.map((b) => (
              <div
                key={b.id}
                className="trivia-card"
                role="button"
                tabIndex={0}
                onClick={() => navegar(`${rutaBase}/${b.id}/mision`)}
                onKeyDown={(e) => e.key === 'Enter' && navegar(`${rutaBase}/${b.id}/mision`)}
                style={{
                  borderLeft: `4px solid ${b.estado === 'Activa' ? '#22c55e' : '#94a3b8'}`,
                  opacity: b.estado === 'Inactiva' ? 0.85 : 1
                }}
              >
                <div className="trivia-card-cabecera">
                  <span className="trivia-card-nombre">{b.nombre}</span>
                  <span className="trivia-card-meta">
                    {b.tieneMision ? 'Con misión' : 'Sin misión'}
                    &nbsp;·&nbsp;{formatearFecha(b.fechaCreacion)}
                  </span>
                </div>
                <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', marginTop: 4 }}>
                  <p className="trivia-card-desc" style={{ margin: 0 }}>{b.descripcion}</p>
                  <span className={`estado-badge estado-badge-${b.estado.toLowerCase()}`}>{b.estado}</span>
                </div>
                <div className="acciones-formulario-trivia" style={{ marginTop: 8 }}>
                  {b.estado === 'Inactiva' ? (
                    <>
                      <Boton variante="secundario" onClick={(e) => manejarActivar(e, b.id)} disabled={procesandoId === b.id}>
                        {procesandoId === b.id ? 'Activando…' : 'Activar'}
                      </Boton>
                      <Boton variante="peligro" onClick={(e) => manejarEliminar(e, b.id)} disabled={procesandoId === b.id}>
                        {procesandoId === b.id ? 'Eliminando…' : 'Eliminar'}
                      </Boton>
                    </>
                  ) : (
                    <Boton variante="peligro" onClick={(e) => manejarDesactivar(e, b.id)} disabled={procesandoId === b.id}>
                      {procesandoId === b.id ? 'Desactivando…' : 'Desactivar'}
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
