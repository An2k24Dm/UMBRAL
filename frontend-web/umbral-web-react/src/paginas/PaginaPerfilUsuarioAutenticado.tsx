import { useEffect, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { LayoutPanel } from '../componentes/LayoutPanel'
import { VistaPerfilUsuario } from '../componentes/VistaPerfilUsuario'
import { Alerta } from '../componentes/Alerta'
import { Boton } from '../componentes/Boton'
import { obtenerPerfilActual } from '../autenticacion/clienteApi'
import { usarAutenticacion } from '../autenticacion/ProveedorAutenticacion'
import type { UsuarioDetalle } from '../autenticacion/tipos'

// HU06 — perfil del usuario autenticado.
// Tanto Administrador como Operador acceden a su propio perfil; el backend
// devuelve el detalle del usuario asociado al token.

export function PaginaPerfilUsuarioAutenticado() {
  const { token, cerrar } = usarAutenticacion()
  const navegar = useNavigate()
  const [estado, setEstado] = useState<'cargando' | 'error' | 'listo'>('cargando')
  const [mensajeError, setMensajeError] = useState<string | null>(null)
  const [perfil, setPerfil] = useState<UsuarioDetalle | null>(null)

  useEffect(() => {
    let cancelado = false
    async function cargar() {
      if (!token) {
        setEstado('error')
        setMensajeError('Debe iniciar sesión.')
        return
      }
      setEstado('cargando')
      setMensajeError(null)
      try {
        const detalle = await obtenerPerfilActual(token)
        if (!cancelado) {
          setPerfil(detalle)
          setEstado('listo')
        }
      } catch (e) {
        if (cancelado) return
        const mensaje = e instanceof Error ? e.message : 'No fue posible consultar el perfil.'
        setMensajeError(mensaje)
        setEstado('error')
        if (mensaje.includes('iniciar sesión')) {
          cerrar()
          navegar('/iniciar-sesion', { replace: true })
        }
      }
    }
    cargar()
    return () => { cancelado = true }
  }, [token, cerrar, navegar])

  return (
    <LayoutPanel titulo="Mi perfil" descripcion="Datos personales asociados a su cuenta.">
      <div className="cabecera-pagina">
        <div>
          <h2 style={{ margin: 0 }}>Información personal</h2>
          <p style={{ margin: '4px 0 0', color: 'var(--color-texto-tenue)' }}>
            La información proviene directamente del backend.
          </p>
        </div>
      </div>

      {estado === 'cargando' && <Alerta tono="informacion">Cargando perfil…</Alerta>}
      {estado === 'error' && (
        <>
          <Alerta tono="error">{mensajeError ?? 'No fue posible consultar el perfil.'}</Alerta>
          <Boton variante="secundario" onClick={() => window.location.reload()}>Reintentar</Boton>
        </>
      )}
      {estado === 'listo' && perfil && <VistaPerfilUsuario usuario={perfil} />}
    </LayoutPanel>
  )
}
