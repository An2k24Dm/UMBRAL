import { useEffect, useState } from 'react'
import { useNavigate, useParams } from 'react-router-dom'
import { LayoutPanel } from '../componentes/LayoutPanel'
import { VistaPerfilUsuario } from '../componentes/VistaPerfilUsuario'
import { Alerta } from '../componentes/Alerta'
import { Boton } from '../componentes/Boton'
import { FormularioEditarOperador } from '../componentes/FormularioEditarOperador'
import { ModalConfirmacion } from '../componentes/ModalConfirmacion'
import { usarAutenticacion } from '../autenticacion/ProveedorAutenticacion'
import { eliminarOperador } from '../autenticacion/clienteApi'
import type { UsuarioDetalle } from '../autenticacion/tipos'

// Vista de detalle/perfil completo de un usuario seleccionado desde una lista.
// - Administrador: detalle de Participantes (HU07), Operadores y Administradores (HU08).
// - Operador: detalle restringido a Participantes (validación final en backend).
//
// HU09 — Si la prop `permiteEditarOperador` está activa y el usuario consultado
// es un Operador, se muestra el botón "Editar" que cambia esta pantalla a modo
// formulario. El cambio de rol del Operador NO está permitido y por eso el
// formulario nunca expone el campo Rol como editable.

interface Props {
  // Restringe a nivel de UI qué roles puede consultar el usuario actual.
  rolesPermitidosVista?: Array<'Participante' | 'Operador' | 'Administrador'>
  // Fuente de datos del detalle. Cada ruta inyecta el endpoint específico.
  obtenerUsuario: (id: string, token: string) => Promise<UsuarioDetalle>
  // HU09 — habilita el modo edición sólo en la ruta del Administrador.
  permiteEditarOperador?: boolean
}

export function PaginaDetalleUsuario({
  rolesPermitidosVista,
  obtenerUsuario,
  permiteEditarOperador = false
}: Props) {
  const { id } = useParams<{ id: string }>()
  const { token, usuario: usuarioAutenticado } = usarAutenticacion()
  const navegar = useNavigate()
  const [estado, setEstado] = useState<'cargando' | 'error' | 'denegado' | 'listo'>('cargando')
  const [mensajeError, setMensajeError] = useState<string | null>(null)
  const [usuario, setUsuario] = useState<UsuarioDetalle | null>(null)
  const [modoEdicion, setModoEdicion] = useState(false)
  const [mensajeExito, setMensajeExito] = useState<string | null>(null)

  // HU13 — estado del modal de confirmación de eliminación de Operador.
  // El botón sólo se ofrece a Administradores autenticados sobre un detalle
  // de Operador (NO Administrador, NO Participante). El backend repite la
  // validación: el frontend sólo decide visibilidad.
  const [modalEliminarAbierto, setModalEliminarAbierto] = useState(false)
  const [eliminando, setEliminando] = useState(false)
  const [errorEliminar, setErrorEliminar] = useState<string | null>(null)

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

  const mostrarBotonEditar =
    estado === 'listo' &&
    !modoEdicion &&
    permiteEditarOperador &&
    usuario?.rol === 'Operador'

  // HU13 — el botón "Eliminar operador" se muestra cuando:
  //  * la vista está en modo lectura (no edición),
  //  * el usuario autenticado es Administrador,
  //  * el detalle consultado corresponde a un Operador (nunca Administrador
  //    ni Participante — esos roles no se eliminan por esta vía).
  // La ruta de Administrador es la única que pasa `permiteEditarOperador`,
  // así que la reutilizamos como bandera de "estoy en la vista que tiene
  // sentido para eliminar operadores" (panel de Administrador).
  const mostrarBotonEliminar =
    estado === 'listo' &&
    !modoEdicion &&
    permiteEditarOperador &&
    usuario?.rol === 'Operador' &&
    usuarioAutenticado?.rol === 'Administrador'

  const abrirModalEliminar = () => {
    setErrorEliminar(null)
    setModalEliminarAbierto(true)
  }
  const cerrarModalEliminar = () => {
    if (eliminando) return
    setModalEliminarAbierto(false)
    setErrorEliminar(null)
  }

  const confirmarEliminar = async () => {
    if (!usuario || !token || eliminando) return
    setEliminando(true)
    setErrorEliminar(null)
    try {
      await eliminarOperador(usuario.id, token)
      // Éxito: redirigir al listado y forzar refresco (state propagado por
      // navegación, el listado lo lee si lo necesita).
      setModalEliminarAbierto(false)
      navegar('/administrador/usuarios/internos', {
        replace: true,
        state: { mensajeExito: 'Operador eliminado permanentemente.' }
      })
    } catch (e) {
      setErrorEliminar(
        e instanceof Error ? e.message : 'No fue posible eliminar el operador.'
      )
    } finally {
      setEliminando(false)
    }
  }

  return (
    <LayoutPanel titulo="Detalle de usuario" descripcion="Perfil completo del usuario seleccionado.">
      <div className="cabecera-pagina">
        <div>
          <h2 style={{ margin: 0 }}>
            {modoEdicion ? 'Editar operador' : 'Información del usuario'}
          </h2>
          <p style={{ margin: '4px 0 0', color: 'var(--color-texto-tenue)' }}>
            {modoEdicion
              ? 'Modifique los datos editables del Operador. Estado, rol y fecha de registro no se pueden cambiar.'
              : 'Los datos se obtienen del backend en tiempo real.'}
          </p>
        </div>
        <div className="cabecera-pagina-acciones">
          {mostrarBotonEditar && (
            <Boton variante="primario" onClick={() => { setModoEdicion(true); setMensajeExito(null) }}>
              Editar
            </Boton>
          )}
          {/* HU13 — botón destructivo de eliminación. Sólo visible para
              Administrador sobre un Operador. */}
          {mostrarBotonEliminar && (
            <Boton
              variante="peligro"
              onClick={abrirModalEliminar}
              data-testid="boton-eliminar-operador"
            >
              Eliminar operador
            </Boton>
          )}
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

      {estado === 'listo' && usuario && !modoEdicion && (
        <>
          {mensajeExito && <Alerta tono="exito">{mensajeExito}</Alerta>}
          <VistaPerfilUsuario usuario={usuario} />
        </>
      )}

      {estado === 'listo' && usuario && modoEdicion && (
        <FormularioEditarOperador
          usuario={usuario}
          alCancelar={() => setModoEdicion(false)}
          alGuardado={(respuesta, mensaje) => {
            // El backend devuelve el perfil actualizado: refrescamos en
            // memoria sin necesidad de volver a llamar al endpoint de detalle.
            setUsuario(respuesta.operador)
            setMensajeExito(mensaje)
            setModoEdicion(false)
          }}
        />
      )}

      {/* HU13 — modal de confirmación de eliminación de Operador. El
          contenido recuerda al usuario que la acción es permanente. */}
      <ModalConfirmacion
        abierto={modalEliminarAbierto}
        titulo="Eliminar operador"
        textoConfirmar="Eliminar operador"
        procesando={eliminando}
        mensajeError={errorEliminar}
        onConfirmar={confirmarEliminar}
        onCancelar={cerrarModalEliminar}
      >
        <p>
          ¿Estás seguro de que deseas eliminar esta cuenta de Operador? Esta
          acción es <strong>permanente e irreversible</strong>. Se perderán
          todos los datos asociados y el usuario no podrá acceder nuevamente
          a la plataforma.
        </p>
      </ModalConfirmacion>
    </LayoutPanel>
  )
}
