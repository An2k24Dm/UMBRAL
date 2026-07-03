import { useCallback, useEffect, useState } from 'react'
import { useNavigate, useParams } from 'react-router-dom'
import { LayoutPanel } from '../componentes/LayoutPanel'
import { Alerta } from '../componentes/Alerta'
import { Boton } from '../componentes/Boton'
import { ModalConfirmacion } from '../componentes/ModalConfirmacion'
import {
  obtenerDetalleEquipoSesion,
  obtenerSesion,
  expulsarParticipanteEquipo,
  type EquipoSesionDetalleDto,
  type IntegranteEquipoDto
} from '../autenticacion/clienteApiSesiones'
import { usarAutenticacion } from '../autenticacion/ProveedorAutenticacion'
import { useSesionesTiempoReal } from '../hooks/useSesionesTiempoReal'
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
  const [estadoSesion, setEstadoSesion] = useState<string | null>(null)
  const [versionTiempoReal, setVersionTiempoReal] = useState(0)

  // HU45 — Expulsión de un integrante por el Operador dueño.
  const [integranteAExpulsar, setIntegranteAExpulsar] =
    useState<IntegranteEquipoDto | null>(null)
  const [expulsando, setExpulsando] = useState(false)
  const [errorExpulsar, setErrorExpulsar] = useState<string | null>(null)
  const [mensajeExito, setMensajeExito] = useState<string | null>(null)

  // Event-driven: SignalR solo marca "datos sucios"; el detalle se vuelve a
  // pedir por HTTP. EquipoActualizado se emite al grupo de la sesión.
  const refrescarPorTiempoReal = useCallback(() => {
    setVersionTiempoReal(v => v + 1)
  }, [])
  useSesionesTiempoReal({
    token,
    sesionId: id,
    onSesionActualizada: refrescarPorTiempoReal
  })

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
        // los integrantes. La sesión se carga en paralelo para conocer su
        // estado (HU45: expulsar solo En Preparación o Pausada).
        const [equipoDetalle, sesionDetalle] = await Promise.all([
          obtenerDetalleEquipoSesion(id, equipoId, token),
          obtenerSesion(id, token)
        ])
        if (ref.cancelado) return
        setEquipo(equipoDetalle)
        setEstadoSesion(sesionDetalle.estado)
        setEstado('listo')
      } catch (e) {
        if (ref.cancelado) return
        setMensajeError(e instanceof Error ? e.message : 'No se pudo cargar el equipo.')
        setEstado('error')
      }
    }
    cargar()
    return () => { ref.cancelado = true }
  }, [token, id, equipoId, usuario?.rol, versionTiempoReal])

  function cerrarModalExpulsar() {
    if (expulsando) return
    setIntegranteAExpulsar(null)
    setErrorExpulsar(null)
  }

  async function confirmarExpulsar() {
    if (!token || !id || !equipoId || !integranteAExpulsar) return
    setExpulsando(true)
    setErrorExpulsar(null)
    setMensajeExito(null)
    try {
      await expulsarParticipanteEquipo(
        id, equipoId, integranteAExpulsar.participanteSesionId, token)
      setIntegranteAExpulsar(null)
      setMensajeExito('Participante expulsado del equipo correctamente.')
      // Refetch inmediato; SignalR también refresca a los demás clientes.
      refrescarPorTiempoReal()
    } catch (e) {
      // 403/404/409 se muestran dentro del modal sin romper la pantalla.
      setErrorExpulsar(
        e instanceof Error ? e.message : 'No se pudo expulsar al participante.')
    } finally {
      setExpulsando(false)
    }
  }

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

  // HU45 — Solo el Operador (dueño) puede expulsar, y solo con la sesión
  // En Preparación o Pausada. El Administrador nunca ve la acción.
  const puedeExpulsar = usuario?.rol === 'Operador' &&
    (estadoSesion === 'EnPreparacion' || estadoSesion === 'Pausada')

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

      {mensajeExito && <Alerta tono="exito">{mensajeExito}</Alerta>}

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
                {puedeExpulsar && <th>Acciones</th>}
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
                  {puedeExpulsar && (
                    <td>
                      <Boton
                        variante="peligro"
                        tamaño="sm"
                        onClick={() => {
                          setErrorExpulsar(null)
                          setIntegranteAExpulsar(p)
                        }}
                      >
                        Expulsar
                      </Boton>
                    </td>
                  )}
                </tr>
              ))}
            </tbody>
          </table>
        )}
      </section>

      {/* HU45 — Confirmar expulsión del integrante. Si es el líder, el
          backend reasigna el liderazgo y aquí solo se refresca. */}
      <ModalConfirmacion
        abierto={integranteAExpulsar !== null}
        titulo="Expulsar participante"
        textoConfirmar="Expulsar"
        textoCancelar="Cancelar"
        procesando={expulsando}
        mensajeError={errorExpulsar}
        onConfirmar={confirmarExpulsar}
        onCancelar={cerrarModalExpulsar}
      >
        <p>
          ¿Seguro que deseas expulsar a este participante del equipo? Quedará
          fuera de la sesión grupal.
        </p>
        {integranteAExpulsar && (
          <p style={{ fontSize: '0.85rem', opacity: 0.8 }}>
            {integranteAExpulsar.alias}
            {integranteAExpulsar.esLider
              ? ' (líder: el liderazgo pasará al siguiente integrante)'
              : ''}
          </p>
        )}
      </ModalConfirmacion>
    </LayoutPanel>
  )
}
