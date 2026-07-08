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
  obtenerCodigoQrBusqueda,
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
  const [modalQrId, setModalQrId] = useState<string | null>(null)
  const [codigoQrModal, setCodigoQrModal] = useState<string | null>(null)
  const [cargandoQr, setCargandoQr] = useState(false)
  const [errorQr, setErrorQr] = useState<string | null>(null)

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

  async function abrirQr(e: React.MouseEvent, busquedaId: string) {
    e.stopPropagation()
    if (!token) return
    setModalQrId(busquedaId)
    setCodigoQrModal(null)
    setErrorQr(null)
    setCargandoQr(true)
    try {
      const data = await obtenerCodigoQrBusqueda(busquedaId, token)
      setCodigoQrModal(data.codigoQr)
    } catch (err) {
      setErrorQr(err instanceof Error ? err.message : 'No fue posible obtener el QR.')
    } finally {
      setCargandoQr(false)
    }
  }

  function cerrarQr() {
    setModalQrId(null)
    setCodigoQrModal(null)
    setErrorQr(null)
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
                    {b.totalPistas} {b.totalPistas === 1 ? 'pista' : 'pistas'}
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
                    <>
                      <Boton variante="secundario" onClick={(e) => abrirQr(e, b.id)}>
                        Ver QR
                      </Boton>
                      <Boton variante="peligro" onClick={(e) => manejarDesactivar(e, b.id)} disabled={procesandoId === b.id}>
                        {procesandoId === b.id ? 'Desactivando…' : 'Desactivar'}
                      </Boton>
                    </>
                  )}
                </div>
              </div>
            ))}
          </div>
        )}
      </section>

      {/* Modal QR */}
      {modalQrId && (
        <div
          style={{
            position: 'fixed', inset: 0, background: 'rgba(0,0,0,0.6)',
            display: 'flex', alignItems: 'center', justifyContent: 'center', zIndex: 1000
          }}
          onClick={cerrarQr}
        >
          <div
            style={{
              background: '#fff', borderRadius: 12, padding: 32, maxWidth: 360, width: '90%',
              display: 'flex', flexDirection: 'column', alignItems: 'center', gap: 16
            }}
            onClick={(e) => e.stopPropagation()}
          >
            <h3 style={{ margin: 0, fontSize: 18, fontWeight: 700 }}>Código QR de la búsqueda</h3>
            {cargandoQr && <p style={{ color: '#6b7280' }}>Cargando QR…</p>}
            {errorQr && <Alerta tono="error">{errorQr}</Alerta>}
            {codigoQrModal && (
              <>
                <img
                  src={`https://api.qrserver.com/v1/create-qr-code/?size=240x240&data=${encodeURIComponent(codigoQrModal)}`}
                  alt="Código QR"
                  style={{ border: '1px solid #e5e7eb', borderRadius: 8 }}
                  width={240}
                  height={240}
                />
                <p style={{ margin: 0, color: '#6b7280', fontSize: 12, wordBreak: 'break-all', textAlign: 'center' }}>
                  {codigoQrModal}
                </p>
                <Boton variante="secundario" onClick={() => window.print()}>Imprimir</Boton>
              </>
            )}
            <Boton variante="fantasma" onClick={cerrarQr}>Cerrar</Boton>
          </div>
        </div>
      )}
    </LayoutPanel>
  )
}
