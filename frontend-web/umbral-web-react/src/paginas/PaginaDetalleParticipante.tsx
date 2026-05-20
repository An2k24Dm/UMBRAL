import { useEffect, useState } from 'react'
import { useNavigate, useParams } from 'react-router-dom'
import { LayoutPanel } from '../componentes/LayoutPanel'
import { VistaPerfilUsuario } from '../componentes/VistaPerfilUsuario'
import { Alerta } from '../componentes/Alerta'
import { Boton } from '../componentes/Boton'
import { obtenerDetalleParticipante } from '../autenticacion/clienteApi'
import { usarAutenticacion } from '../autenticacion/ProveedorAutenticacion'
import type { UsuarioDetalle } from '../autenticacion/tipos'

// HU07 — perfil/detalle completo de un Participante seleccionado desde la
// lista. Consume el endpoint dedicado /api/usuarios/participantes/{id}, que
// nunca devuelve usuarios internos: si el id no es un Participante, el
// backend responde 404 y se muestra el mensaje correspondiente.

export function PaginaDetalleParticipante() {
  const { id } = useParams<{ id: string }>()
  const { token } = usarAutenticacion()
  const navegar = useNavigate()
  const [estado, setEstado] = useState<'cargando' | 'error' | 'listo'>('cargando')
  const [mensajeError, setMensajeError] = useState<string | null>(null)
  const [usuario, setUsuario] = useState<UsuarioDetalle | null>(null)

  useEffect(() => {
    let cancelado = false
    async function cargar() {
      if (!token) {
        setEstado('error')
        setMensajeError('Debe iniciar sesión.')
        return
      }
      if (!id) {
        setEstado('error')
        setMensajeError('Identificador de participante no especificado.')
        return
      }
      setEstado('cargando')
      setMensajeError(null)
      try {
        const detalle = await obtenerDetalleParticipante(id, token)
        if (cancelado) return
        setUsuario(detalle)
        setEstado('listo')
      } catch (e) {
        if (cancelado) return
        const mensaje = e instanceof Error ? e.message : 'No fue posible consultar el participante.'
        setMensajeError(mensaje)
        setEstado('error')
      }
    }
    cargar()
    return () => { cancelado = true }
  }, [token, id])

  return (
    <LayoutPanel titulo="Detalle de participante" descripcion="Perfil completo del participante seleccionado.">
      <div className="cabecera-pagina">
        <div>
          <h2 style={{ margin: 0 }}>Información del participante</h2>
          <p style={{ margin: '4px 0 0', color: 'var(--color-texto-tenue)' }}>
            Los datos se obtienen del backend en tiempo real.
          </p>
        </div>
        <div className="cabecera-pagina-acciones">
          <Boton variante="volver" onClick={() => navegar(-1)}>← Volver</Boton>
        </div>
      </div>

      {estado === 'cargando' && <Alerta tono="informacion">Cargando participante…</Alerta>}

      {estado === 'error' && (
        <Alerta tono="error">
          {mensajeError ?? 'No fue posible consultar el participante.'}
        </Alerta>
      )}

      {estado === 'listo' && usuario && <VistaPerfilUsuario usuario={usuario} />}
    </LayoutPanel>
  )
}
