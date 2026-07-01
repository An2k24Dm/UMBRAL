import { useEffect, useState } from 'react'
import { useNavigate, useParams } from 'react-router-dom'
import { LayoutPanel } from '../componentes/LayoutPanel'
import { Alerta } from '../componentes/Alerta'
import { Boton } from '../componentes/Boton'
import {
  obtenerDetalleEquipoSesion,
  type EquipoSesionDetalleDto
} from '../autenticacion/clienteApiSesiones'
import { usarAutenticacion } from '../autenticacion/ProveedorAutenticacion'
import { formatearFechaSesion } from '../utilidades/formatoSesiones'

// Detalle de un equipo dentro de una sesión grupal.
//
// Operador y Administrador consumen el MISMO DTO dedicado de HU43, ya
// enriquecido por el backend con los datos básicos no sensibles de identidad
// (incluidos los integrantes). El Administrador es de solo lectura.

export function PaginaDetalleEquipo() {
  const { id, equipoId } = useParams<{ id: string; equipoId: string }>()
  const { token, usuario } = usarAutenticacion()
  const navegar = useNavigate()
  const rutaSesion = usuario?.rol === 'Administrador'
    ? `/administrador/sesiones/${id}`
    : `/operador/sesiones/${id}`

  const [estado, setEstado] = useState<'cargando' | 'error' | 'listo'>('cargando')
  const [mensajeError, setMensajeError] = useState<string | null>(null)
  const [equipo, setEquipo] = useState<EquipoSesionDetalleDto | null>(null)

  useEffect(() => {
    const ref = { cancelado: false }
    async function cargar() {
      if (!token || !id || !equipoId) {
        setEstado('error')
        setMensajeError('Identificadores inválidos.')
        return
      }
      try {
        // Operador y Administrador usan el mismo endpoint real, que devuelve
        // los integrantes. Antes el Administrador construía el DTO a mano con
        // participantes: [], por eso nunca veía integrantes.
        const equipoDetalle = await obtenerDetalleEquipoSesion(id, equipoId, token)
        if (ref.cancelado) return
        setEquipo(equipoDetalle)
        setEstado('listo')
      } catch (e) {
        if (ref.cancelado) return
        setMensajeError(e instanceof Error ? e.message : 'No se pudo cargar el equipo.')
        setEstado('error')
      }
    }
    cargar()
    return () => { ref.cancelado = true }
  }, [token, id, equipoId, usuario?.rol])

  if (estado === 'cargando') {
    return (
      <LayoutPanel titulo="Detalle de equipo" descripcion="Cargando…">
        <section className="seccion">
          <p className="detalle-mensaje-vacio">Cargando equipo…</p>
        </section>
      </LayoutPanel>
    )
  }

  if (estado === 'error' || !equipo) {
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
        <div className="detalle-grilla">
          <div className="detalle-campo">
            <span className="detalle-campo-etiqueta">Nombre del equipo</span>
            <strong className="detalle-equipo-nombre">{equipo.nombre}</strong>
          </div>
          <div className="detalle-campo">
            <span className="detalle-campo-etiqueta">Tipo de equipo</span>
            <span>
              <span className={`badge ${equipo.tipo === 'Publico' ? 'badge-equipo-publico' : 'badge-equipo-privado'}`}>
                {equipo.tipo === 'Publico' ? 'Público' : 'Privado'}
              </span>
            </span>
          </div>
          <div className="detalle-campo">
            <span className="detalle-campo-etiqueta">Puntaje total</span>
            <span className="detalle-campo-valor">{equipo.puntaje}</span>
          </div>
          <div className="detalle-campo">
            <span className="detalle-campo-etiqueta">Integrantes</span>
            <span className="detalle-campo-valor">
              {equipo.cantidadParticipantes} / {equipo.capacidadMaxima}
            </span>
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
            <p>{equipo.cantidadParticipantes} de {equipo.capacidadMaxima} integrantes.</p>
          </div>
        </div>

        {equipo.participantes.length === 0 ? (
          <p className="detalle-mensaje-vacio">Este equipo aún no tiene integrantes.</p>
        ) : (
          <table className="tabla-usuarios">
            <thead>
              <tr>
                <th>#</th>
                <th>Alias</th>
                <th>Nombre</th>
                <th>Apellido</th>
                <th>Puntaje individual</th>
                <th>Fecha de unión</th>
                <th>Rol</th>
              </tr>
            </thead>
            <tbody>
              {equipo.participantes.map((p, idx) => (
                <tr key={p.participanteSesionId}>
                  <td>{idx + 1}</td>
                  <td><strong>{p.alias}</strong></td>
                  <td>{p.nombre || '—'}</td>
                  <td>{p.apellido || '—'}</td>
                  <td>{p.puntaje}</td>
                  <td>{formatearFechaSesion(p.fechaUnion)}</td>
                  <td>
                    <span className={`badge ${p.esLider ? 'badge-equipo-lider' : 'badge-neutro'}`}>
                      {p.esLider ? 'Líder' : 'Integrante'}
                    </span>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        )}
      </section>
    </LayoutPanel>
  )
}
