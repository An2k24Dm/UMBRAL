import { useEffect, useState } from 'react'
import { useNavigate, useParams } from 'react-router-dom'
import { LayoutPanel } from '../componentes/LayoutPanel'
import { VistaPerfilUsuario } from '../componentes/VistaPerfilUsuario'
import { Alerta } from '../componentes/Alerta'
import { Boton } from '../componentes/Boton'
import { usarAutenticacion } from '../autenticacion/ProveedorAutenticacion'
import type { UsuarioDetalle } from '../autenticacion/tipos'

// Vista de detalle/perfil completo de un usuario seleccionado desde una lista.
// - Administrador: detalle de Participantes (HU07), Operadores y Administradores (HU08).
// - Operador: detalle restringido a Participantes (validación final en backend).
//
// La función de carga (obtenerUsuario) es obligatoria para que HU07 y HU08
// usen sus endpoints específicos sin duplicar la vista.

interface Props {
  // Restringe a nivel de UI qué roles puede consultar el usuario actual.
  rolesPermitidosVista?: Array<'Participante' | 'Operador' | 'Administrador'>
  // Fuente de datos del detalle. Cada ruta inyecta el endpoint específico.
  obtenerUsuario: (id: string, token: string) => Promise<UsuarioDetalle>
}

export function PaginaDetalleUsuario({ rolesPermitidosVista, obtenerUsuario }: Props) {
  const { id } = useParams<{ id: string }>()
  const { token } = usarAutenticacion()
  const navegar = useNavigate()
  const [estado, setEstado] = useState<'cargando' | 'error' | 'denegado' | 'listo'>('cargando')
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
        setMensajeError('Identificador de usuario no especificado.')
        return
      }
      setEstado('cargando')
      setMensajeError(null)
      try {
        const detalle = await obtenerUsuario(id, token)
        if (cancelado) return
        if (rolesPermitidosVista && !rolesPermitidosVista.includes(detalle.rol)) {
          setEstado('denegado')
          return
        }
        setUsuario(detalle)
        setEstado('listo')
      } catch (e) {
        if (cancelado) return
        const mensaje = e instanceof Error ? e.message : 'No fue posible consultar el usuario.'
        setMensajeError(mensaje)
        setEstado('error')
      }
    }
    cargar()
    return () => { cancelado = true }
  }, [token, id, rolesPermitidosVista, obtenerUsuario])

  return (
    <LayoutPanel titulo="Detalle de usuario" descripcion="Perfil completo del usuario seleccionado.">
      <div className="cabecera-pagina">
        <div>
          <h2 style={{ margin: 0 }}>Información del usuario</h2>
          <p style={{ margin: '4px 0 0', color: 'var(--color-texto-tenue)' }}>
            Los datos se obtienen del backend en tiempo real.
          </p>
        </div>
        <div className="cabecera-pagina-acciones">
          <Boton variante="volver" onClick={() => navegar(-1)}>← Volver</Boton>
        </div>
      </div>

      {estado === 'cargando' && <Alerta tono="informacion">Cargando usuario…</Alerta>}

      {estado === 'error' && (
        <Alerta tono="error">
          {mensajeError ?? 'No fue posible consultar el usuario.'}
        </Alerta>
      )}

      {estado === 'denegado' && (
        <section className="sin-permiso">
          <h2>Acceso denegado</h2>
          <p>No tiene permisos para consultar este usuario.</p>
        </section>
      )}

      {estado === 'listo' && usuario && <VistaPerfilUsuario usuario={usuario} />}
    </LayoutPanel>
  )
}
