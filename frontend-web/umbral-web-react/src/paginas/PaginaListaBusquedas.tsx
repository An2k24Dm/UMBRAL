import { useEffect, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { LayoutPanel } from '../componentes/LayoutPanel'
import { Alerta } from '../componentes/Alerta'
import { Boton } from '../componentes/Boton'
import {
  obtenerBusquedasEnBorrador,
  archivarBusqueda,
  activarBusqueda,
  type BusquedaTesoroResumenDto
} from '../autenticacion/clienteApiJuegos'
import { usarAutenticacion } from '../autenticacion/ProveedorAutenticacion'

function formatearFecha(iso: string): string {
  return new Date(iso).toLocaleDateString('es-VE', {
    day: '2-digit',
    month: '2-digit',
    year: 'numeric'
  })
}

export function PaginaListaBusquedas() {
  const { token, usuario } = usarAutenticacion()
  const navegar = useNavigate()
  const rutaBase = usuario?.rol === 'Administrador' ? '/administrador/busquedas' : '/operador/busquedas'

  const [estado, setEstado] = useState<'cargando' | 'error' | 'vacio' | 'listo'>('cargando')
  const [mensajeError, setMensajeError] = useState<string | null>(null)
  const [busquedas, setBusquedas] = useState<BusquedaTesoroResumenDto[]>([])
  const [procesandoId, setProcesandoId] = useState<string | null>(null)

  async function cargar(ref?: { cancelado: boolean }) {
    if (!token) { setEstado('error'); setMensajeError('Debe iniciar sesión.'); return }
    setEstado('cargando')
    setMensajeError(null)
    try {
      const lista = await obtenerBusquedasEnBorrador(token)
      if (ref?.cancelado) return
      setBusquedas(lista)
      setEstado(lista.length === 0 ? 'vacio' : 'listo')
    } catch (e) {
      if (ref?.cancelado) return
      setMensajeError(e instanceof Error ? e.message : 'No fue posible cargar las búsquedas del tesoro.')
      setEstado('error')
    }
  }

  useEffect(() => {
    const ref = { cancelado: false }
    cargar(ref)
    return () => { ref.cancelado = true }
  }, [token])

  async function manejarArchivar(e: React.MouseEvent, busquedaId: string) {
    e.stopPropagation()
    if (!token) return
    setProcesandoId(busquedaId)
    setMensajeError(null)
    try {
      await archivarBusqueda(busquedaId, token)
      await cargar()
    } catch (err) {
      setMensajeError(err instanceof Error ? err.message : 'No fue posible archivar la búsqueda.')
    } finally {
      setProcesandoId(null)
    }
  }

  async function manejarReactivar(e: React.MouseEvent, busquedaId: string) {
    e.stopPropagation()
    if (!token) return
    setProcesandoId(busquedaId)
    setMensajeError(null)
    try {
      await activarBusqueda(busquedaId, token)
      await cargar()
    } catch (err) {
      setMensajeError(err instanceof Error ? err.message : 'No fue posible reactivar la búsqueda.')
    } finally {
      setProcesandoId(null)
    }
  }

  return (
    <LayoutPanel
      titulo="Búsquedas del tesoro"
      descripcion="Búsquedas en estado Borrador y Archivada."
    >
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

        {mensajeError && <Alerta tono="error">{mensajeError}</Alerta>}

        {estado === 'cargando' && (
          <p className="tabla-estado-mensaje">Cargando búsquedas del tesoro…</p>
        )}

        {estado === 'vacio' && (
          <p className="tabla-estado-mensaje">No tiene búsquedas. Cree una para comenzar.</p>
        )}

        {estado === 'listo' && (
          <div className="lista-trivias">
            {busquedas.map((b) => (
              <div
                key={b.id}
                className="trivia-card"
                role="button"
                tabIndex={0}
                onClick={() => navegar(`${rutaBase}/${b.id}/etapas`)}
                onKeyDown={(e) => e.key === 'Enter' && navegar(`${rutaBase}/${b.id}/etapas`)}
              >
                <div className="trivia-card-cabecera">
                  <span className="trivia-card-nombre">{b.nombre}</span>
                  <span className="trivia-card-meta">
                    {b.totalEtapas} {b.totalEtapas === 1 ? 'etapa' : 'etapas'}
                    &nbsp;·&nbsp;{formatearFecha(b.fechaCreacion)}
                  </span>
                </div>
                <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', marginTop: 4 }}>
                  <p className="trivia-card-desc" style={{ margin: 0 }}>{b.descripcion}</p>
                  <span className={`estado-badge estado-badge-${b.estado.toLowerCase()}`}>
                    {b.estado}
                  </span>
                </div>
                <div className="acciones-formulario-trivia" style={{ marginTop: 8 }}>
                  {b.estado === 'Archivada' ? (
                    <Boton
                      variante="secundario"
                      onClick={(e) => manejarReactivar(e, b.id)}
                      disabled={procesandoId === b.id}
                    >
                      {procesandoId === b.id ? 'Activando…' : 'Reactivar'}
                    </Boton>
                  ) : (
                    <Boton
                      variante="peligro"
                      onClick={(e) => manejarArchivar(e, b.id)}
                      disabled={procesandoId === b.id}
                    >
                      {procesandoId === b.id ? 'Archivando…' : 'Archivar'}
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
