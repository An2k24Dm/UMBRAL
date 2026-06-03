import { useEffect, useState } from 'react'
import { useNavigate, useParams } from 'react-router-dom'
import { LayoutPanel } from '../componentes/LayoutPanel'
import { Alerta } from '../componentes/Alerta'
import { Boton } from '../componentes/Boton'
import {
  obtenerSesion,
  type EquipoSesionDto,
  type SesionDetalleDto
} from '../autenticacion/clienteApiSesiones'
import { usarAutenticacion } from '../autenticacion/ProveedorAutenticacion'
import { formatearFechaSesion } from '../utilidades/formatoSesiones'

// Detalle de un equipo dentro de una sesión grupal.
//
// El backend devuelve el equipo embebido en SesionDetalleDto, por eso
// esta vista carga el detalle de la sesión y filtra el equipo pedido.
// Cuando el backend exponga un endpoint dedicado para equipo, se cambia
// la fuente de datos sin tocar la UI.

export function PaginaDetalleEquipo() {
  const { id, equipoId } = useParams<{ id: string; equipoId: string }>()
  const { token, usuario } = usarAutenticacion()
  const navegar = useNavigate()
  const rutaSesion = usuario?.rol === 'Administrador'
    ? `/administrador/sesiones/${id}`
    : `/operador/sesiones/${id}`

  const [estado, setEstado] = useState<'cargando' | 'error' | 'listo'>('cargando')
  const [mensajeError, setMensajeError] = useState<string | null>(null)
  const [sesion, setSesion] = useState<SesionDetalleDto | null>(null)
  const [equipo, setEquipo] = useState<EquipoSesionDto | null>(null)

  useEffect(() => {
    const ref = { cancelado: false }
    async function cargar() {
      if (!token || !id || !equipoId) {
        setEstado('error')
        setMensajeError('Identificadores inválidos.')
        return
      }
      try {
        const detalle = await obtenerSesion(id, token)
        if (ref.cancelado) return
        setSesion(detalle)
        const encontrado = detalle.equipos.find(e => e.id === equipoId) ?? null
        if (!encontrado) {
          setEstado('error')
          setMensajeError('El equipo solicitado no pertenece a esta sesión.')
          return
        }
        setEquipo(encontrado)
        setEstado('listo')
      } catch (e) {
        if (ref.cancelado) return
        setMensajeError(e instanceof Error ? e.message : 'No se pudo cargar el equipo.')
        setEstado('error')
      }
    }
    cargar()
    return () => { ref.cancelado = true }
  }, [token, id, equipoId])

  if (estado === 'cargando') {
    return (
      <LayoutPanel titulo="Detalle de equipo" descripcion="Cargando…">
        <section className="seccion">
          <p className="detalle-mensaje-vacio">Cargando equipo…</p>
        </section>
      </LayoutPanel>
    )
  }

  if (estado === 'error' || !equipo || !sesion) {
    return (
      <LayoutPanel titulo="Detalle de equipo" descripcion="">
        <div style={{ marginBottom: 'var(--espacio-4)' }}>
          <Boton variante="volver" onClick={() => navegar(rutaSesion)}>
            ← Volver al detalle de sesión
          </Boton>
        </div>
        <section className="seccion">
          <Alerta tono="error">
            {mensajeError ?? 'No se pudo cargar el equipo.'}
          </Alerta>
        </section>
      </LayoutPanel>
    )
  }

  return (
    <LayoutPanel
      titulo="Detalle de equipo"
      descripcion="Información del equipo y sus integrantes."
    >
      <div style={{ marginBottom: 'var(--espacio-4)' }}>
        <Boton variante="volver" onClick={() => navegar(rutaSesion)}>
          ← Volver al detalle de sesión
        </Boton>
      </div>

      <section className="seccion">
        <div className="detalle-sesion-cabecera">
          <div>
            <h2>{equipo.nombre}</h2>
            <p>Sesión: {sesion.nombre}</p>
          </div>
          <span className="badge badge-md badge-sesion-activa">
            {equipo.participantes.length} / 2 integrantes
          </span>
        </div>

        <div className="detalle-grilla">
          <div className="detalle-campo">
            <span className="detalle-campo-etiqueta">Nombre del equipo</span>
            <span className="detalle-campo-valor">{equipo.nombre}</span>
          </div>
          <div className="detalle-campo">
            <span className="detalle-campo-etiqueta">Sesión</span>
            <span className="detalle-campo-valor">{sesion.nombre}</span>
          </div>
          <div className="detalle-campo">
            <span className="detalle-campo-etiqueta">Puntaje actual</span>
            <span className="detalle-campo-valor">{equipo.puntajeActual}</span>
          </div>
          <div className="detalle-campo">
            <span className="detalle-campo-etiqueta">Fecha de creación</span>
            <span className="detalle-campo-valor">{formatearFechaSesion(equipo.fechaCreacion)}</span>
          </div>
        </div>
      </section>

      <section className="seccion">
        <div className="detalle-subtitulo">
          <div>
            <h3>Integrantes</h3>
            <p>{equipo.participantes.length} de 2 integrantes.</p>
          </div>
        </div>

        {equipo.participantes.length === 0 ? (
          <p className="detalle-mensaje-vacio">Este equipo aún no tiene integrantes.</p>
        ) : (
          <table className="tabla-usuarios">
            <thead>
              <tr>
                <th>#</th>
                <th>Participante</th>
                <th>Fecha de unión</th>
              </tr>
            </thead>
            <tbody>
              {equipo.participantes.map((p, idx) => (
                <tr key={p.id}>
                  <td>{idx + 1}</td>
                  {/* TODO: enriquecer con alias/nombre cuando identidad-servicio
                      sirva esa consulta. Hoy mostramos el id como fallback. */}
                  <td>{p.participanteId}</td>
                  <td>{formatearFechaSesion(p.fechaUnion)}</td>
                </tr>
              ))}
            </tbody>
          </table>
        )}
      </section>
    </LayoutPanel>
  )
}
