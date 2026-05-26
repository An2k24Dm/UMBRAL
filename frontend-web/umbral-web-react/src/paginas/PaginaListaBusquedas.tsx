import { useEffect, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { LayoutPanel } from '../componentes/LayoutPanel'
import { Alerta } from '../componentes/Alerta'
import { Boton } from '../componentes/Boton'
import { obtenerBusquedasEnBorrador, type BusquedaTesoroResumenDto } from '../autenticacion/clienteApiJuegos'
import { usarAutenticacion } from '../autenticacion/ProveedorAutenticacion'

function formatearFecha(iso: string): string {
  return new Date(iso).toLocaleDateString('es-VE', {
    day: '2-digit',
    month: '2-digit',
    year: 'numeric'
  })
}

export function PaginaListaBusquedas() {
  const { token } = usarAutenticacion()
  const navegar = useNavigate()

  const [estado, setEstado] = useState<'cargando' | 'error' | 'vacio' | 'listo'>('cargando')
  const [mensajeError, setMensajeError] = useState<string | null>(null)
  const [busquedas, setBusquedas] = useState<BusquedaTesoroResumenDto[]>([])

  useEffect(() => {
    let cancelado = false
    async function cargar() {
      if (!token) { setEstado('error'); setMensajeError('Debe iniciar sesión.'); return }
      setEstado('cargando')
      setMensajeError(null)
      try {
        const lista = await obtenerBusquedasEnBorrador(token)
        if (cancelado) return
        setBusquedas(lista)
        setEstado(lista.length === 0 ? 'vacio' : 'listo')
      } catch (e) {
        if (cancelado) return
        setMensajeError(e instanceof Error ? e.message : 'No fue posible cargar las búsquedas del tesoro.')
        setEstado('error')
      }
    }
    cargar()
    return () => { cancelado = true }
  }, [token])

  return (
    <LayoutPanel
      titulo="Búsquedas del tesoro"
      descripcion="Búsquedas en estado Borrador que usted ha creado."
    >
      <section className="seccion">
        <div className="seccion-cabecera">
          <div>
            <h2>Búsquedas en borrador</h2>
            <p>Haga clic en una búsqueda para gestionar sus etapas.</p>
          </div>
          <div className="cabecera-pagina-acciones">
            <Boton variante="primario" onClick={() => navegar('/operador/busquedas/crear')}>
              + Crear búsqueda
            </Boton>
          </div>
        </div>

        {estado === 'error' && mensajeError && (
          <Alerta tono="error">{mensajeError}</Alerta>
        )}

        {estado === 'cargando' && (
          <p className="tabla-estado-mensaje">Cargando búsquedas del tesoro…</p>
        )}

        {estado === 'vacio' && (
          <p className="tabla-estado-mensaje">No tiene búsquedas en borrador. Cree una para comenzar.</p>
        )}

        {estado === 'listo' && (
          <div className="lista-trivias">
            {busquedas.map((b) => (
              <div
                key={b.id}
                className="trivia-card"
                role="button"
                tabIndex={0}
                onClick={() => navegar(`/operador/busquedas/${b.id}/etapas`)}
                onKeyDown={(e) => e.key === 'Enter' && navegar(`/operador/busquedas/${b.id}/etapas`)}
              >
                <div className="trivia-card-cabecera">
                  <span className="trivia-card-nombre">{b.nombre}</span>
                  <span className="trivia-card-meta">
                    {b.totalEtapas} {b.totalEtapas === 1 ? 'etapa' : 'etapas'}
                    &nbsp;·&nbsp;{formatearFecha(b.fechaCreacion)}
                  </span>
                </div>
                <p className="trivia-card-desc">{b.descripcion}</p>
              </div>
            ))}
          </div>
        )}
      </section>
    </LayoutPanel>
  )
}
