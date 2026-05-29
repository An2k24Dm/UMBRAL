import { useEffect, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { LayoutPanel } from '../componentes/LayoutPanel'
import { Alerta } from '../componentes/Alerta'
import { Boton } from '../componentes/Boton'
import {
  listarSesiones,
  type SesionListadoDto
} from '../autenticacion/clienteApiSesiones'
import { usarAutenticacion } from '../autenticacion/ProveedorAutenticacion'

// HU33 — Listado de sesiones para el panel del Operador/Administrador.
// Muestra nombre, tipo de juego, contenido, modo, fecha y estado tal
// como lo exigen los criterios de aceptación.

function formatearFecha(iso: string): string {
  return new Date(iso).toLocaleString('es-VE', {
    day: '2-digit',
    month: '2-digit',
    year: 'numeric',
    hour: '2-digit',
    minute: '2-digit'
  })
}

export function PaginaSesiones() {
  const { token, usuario } = usarAutenticacion()
  const navegar = useNavigate()
  const rutaBase = usuario?.rol === 'Administrador'
    ? '/administrador/sesiones'
    : '/operador/sesiones'

  const [estado, setEstado] = useState<'cargando' | 'error' | 'vacio' | 'listo'>('cargando')
  const [mensajeError, setMensajeError] = useState<string | null>(null)
  const [sesiones, setSesiones] = useState<SesionListadoDto[]>([])

  useEffect(() => {
    const ref = { cancelado: false }
    async function cargar() {
      if (!token) { setEstado('error'); setMensajeError('Debe iniciar sesión.'); return }
      try {
        const lista = await listarSesiones(token)
        if (ref.cancelado) return
        setSesiones(lista)
        setEstado(lista.length === 0 ? 'vacio' : 'listo')
      } catch (e) {
        if (ref.cancelado) return
        setMensajeError(e instanceof Error ? e.message : 'No fue posible cargar las sesiones.')
        setEstado('error')
      }
    }
    cargar()
    return () => { ref.cancelado = true }
  }, [token])

  return (
    <LayoutPanel
      titulo="Sesiones"
      descripcion="Sesiones programadas y en ejecución."
    >
      <section className="seccion">
        <div className="seccion-cabecera">
          <div>
            <h2>Listado de sesiones</h2>
            <p>Puede crear una sesión en vivo a partir de una Trivia o Búsqueda del Tesoro activa.</p>
          </div>
          <div className="cabecera-pagina-acciones">
            <Boton variante="primario" onClick={() => navegar(`${rutaBase}/crear`)}>
              + Crear sesión
            </Boton>
          </div>
        </div>

        {mensajeError && <Alerta tono="error">{mensajeError}</Alerta>}

        {estado === 'cargando' && (
          <p className="tabla-estado-mensaje">Cargando sesiones…</p>
        )}

        {estado === 'vacio' && (
          <p className="tabla-estado-mensaje">No hay sesiones todavía. Cree una para comenzar.</p>
        )}

        {estado === 'listo' && (
          <div className="lista-trivias">
            {sesiones.map((s) => (
              <div key={s.id} className="trivia-card">
                <div className="trivia-card-cabecera">
                  <span className="trivia-card-nombre">{s.nombre}</span>
                  <span className="trivia-card-meta">
                    {s.tipoJuego === 'BusquedaTesoro' ? 'Búsqueda del Tesoro' : s.tipoJuego}
                    &nbsp;·&nbsp;Modo {s.modo}
                    &nbsp;·&nbsp;{formatearFecha(s.fechaProgramada)}
                  </span>
                </div>
                <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'flex-end', marginTop: 4 }}>
                  <span className={`estado-badge estado-badge-${s.estado.toLowerCase()}`}>
                    {s.estado}
                  </span>
                </div>
              </div>
            ))}
          </div>
        )}
      </section>
    </LayoutPanel>
  )
}
