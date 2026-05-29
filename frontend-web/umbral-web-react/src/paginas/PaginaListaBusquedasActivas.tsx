import { useEffect, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { LayoutPanel } from '../componentes/LayoutPanel'
import { Alerta } from '../componentes/Alerta'
import { Boton } from '../componentes/Boton'
import {
  obtenerBusquedasActivas,
  archivarBusqueda,
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

export function PaginaListaBusquedasActivas() {
  const { token, usuario } = usarAutenticacion()
  const navegar = useNavigate()
  const rutaBase = usuario?.rol === 'Administrador' ? '/administrador/busquedas' : '/operador/busquedas'

  const [estado, setEstado] = useState<'cargando' | 'error' | 'vacio' | 'listo'>('cargando')
  const [mensajeError, setMensajeError] = useState<string | null>(null)
  const [busquedas, setBusquedas] = useState<BusquedaTesoroResumenDto[]>([])
  const [archivandoId, setArchivandoId] = useState<string | null>(null)

  async function cargar(cancelado?: { valor: boolean }) {
    if (!token) { setEstado('error'); setMensajeError('Debe iniciar sesión.'); return }
    setEstado('cargando')
    setMensajeError(null)
    try {
      const lista = await obtenerBusquedasActivas(token)
      if (cancelado?.valor) return
      setBusquedas(lista)
      setEstado(lista.length === 0 ? 'vacio' : 'listo')
    } catch (e) {
      if (cancelado?.valor) return
      setMensajeError(e instanceof Error ? e.message : 'No fue posible cargar las búsquedas activas.')
      setEstado('error')
    }
  }

  useEffect(() => {
    const ref = { valor: false }
    cargar(ref)
    return () => { ref.valor = true }
  }, [token])

  async function manejarArchivar(busquedaId: string) {
    if (!token) return
    setArchivandoId(busquedaId)
    setMensajeError(null)
    try {
      await archivarBusqueda(busquedaId, token)
      navegar(rutaBase)
    } catch (e) {
      setMensajeError(e instanceof Error ? e.message : 'No fue posible archivar la búsqueda.')
    } finally {
      setArchivandoId(null)
    }
  }

  return (
    <LayoutPanel
      titulo="Búsquedas activas"
      descripcion="Búsquedas del tesoro en estado Activa disponibles para sesiones de juego."
    >
      <section className="seccion">
        <div className="seccion-cabecera">
          <div>
            <h2>Búsquedas activas</h2>
            <p>Haga clic en una búsqueda para ver su contenido o archivarla.</p>
          </div>
          <div className="cabecera-pagina-acciones">
            <Boton variante="volver" onClick={() => navegar(rutaBase)}>
              Volver a mis búsquedas
            </Boton>
          </div>
        </div>

        {mensajeError && <Alerta tono="error">{mensajeError}</Alerta>}

        {estado === 'cargando' && (
          <p className="tabla-estado-mensaje">Cargando búsquedas activas…</p>
        )}

        {estado === 'vacio' && (
          <p className="tabla-estado-mensaje">No hay búsquedas del tesoro activas en este momento.</p>
        )}

        {estado === 'listo' && (
          <div className="lista-trivias">
            {busquedas.map((b) => (
              <div key={b.id} className="trivia-card">
                <div className="trivia-card-cabecera">
                  <span className="trivia-card-nombre">{b.nombre}</span>
                  <span className="trivia-card-meta">
                    {b.totalEtapas} {b.totalEtapas === 1 ? 'etapa' : 'etapas'}
                    &nbsp;·&nbsp;{formatearFecha(b.fechaCreacion)}
                  </span>
                </div>
                <p className="trivia-card-desc">{b.descripcion}</p>
                <div className="acciones-formulario-trivia" style={{ marginTop: 8 }}>
                  <Boton
                    variante="secundario"
                    onClick={() => navegar(`${rutaBase}/${b.id}/etapas`)}
                  >
                    Ver etapas
                  </Boton>
                  <Boton
                    variante="peligro"
                    onClick={() => manejarArchivar(b.id)}
                    disabled={archivandoId === b.id}
                  >
                    {archivandoId === b.id ? 'Archivando…' : 'Archivar'}
                  </Boton>
                </div>
              </div>
            ))}
          </div>
        )}
      </section>
    </LayoutPanel>
  )
}
