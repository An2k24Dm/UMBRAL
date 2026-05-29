import { useEffect, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { LayoutPanel } from '../componentes/LayoutPanel'
import { Alerta } from '../componentes/Alerta'
import { Boton } from '../componentes/Boton'
import { obtenerBusquedasActivas, type BusquedaTesoroResumenDto } from '../autenticacion/clienteApiJuegos'
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

  useEffect(() => {
    let cancelado = false
    async function cargar() {
      if (!token) { setEstado('error'); setMensajeError('Debe iniciar sesión.'); return }
      setEstado('cargando')
      setMensajeError(null)
      try {
        const lista = await obtenerBusquedasActivas(token)
        if (cancelado) return
        setBusquedas(lista)
        setEstado(lista.length === 0 ? 'vacio' : 'listo')
      } catch (e) {
        if (cancelado) return
        setMensajeError(e instanceof Error ? e.message : 'No fue posible cargar las búsquedas activas.')
        setEstado('error')
      }
    }
    cargar()
    return () => { cancelado = true }
  }, [token])

  return (
    <LayoutPanel
      titulo="Búsquedas activas"
      descripcion="Búsquedas del tesoro en estado Activa disponibles para sesiones de juego."
    >
      <section className="seccion">
        <div className="seccion-cabecera">
          <div>
            <h2>Búsquedas activas</h2>
            <p>Estas búsquedas están disponibles para crear sesiones de juego.</p>
          </div>
          <div className="cabecera-pagina-acciones">
            <Boton variante="volver" onClick={() => navegar(rutaBase)}>
              Volver a mis búsquedas
            </Boton>
          </div>
        </div>

        {estado === 'error' && mensajeError && (
          <Alerta tono="error">{mensajeError}</Alerta>
        )}

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
              </div>
            ))}
          </div>
        )}
      </section>
    </LayoutPanel>
  )
}
