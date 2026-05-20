import type { UsuarioDetalle } from '../autenticacion/tipos'
import { BadgeEstado } from './BadgeEstado'

interface Props {
  usuario: UsuarioDetalle
}

function noDisponible(valor: string | null | undefined): string {
  const v = (valor ?? '').toString().trim()
  return v.length > 0 ? v : 'No disponible'
}

function formatearFecha(valor: string | null | undefined): string {
  if (!valor) return 'No disponible'
  const fecha = new Date(valor)
  if (Number.isNaN(fecha.getTime())) return valor
  return fecha.toLocaleDateString('es-VE', { day: '2-digit', month: '2-digit', year: 'numeric' })
}

function codigoUsuario(usuario: UsuarioDetalle): string {
  if (usuario.rol === 'Operador') return noDisponible(usuario.codigoOperador)
  if (usuario.rol === 'Administrador') return noDisponible(usuario.codigoAdministrador)
  return 'No aplica'
}

export function VistaPerfilUsuario({ usuario }: Props) {
  const mostrarCodigo = usuario.rol === 'Operador' || usuario.rol === 'Administrador'
  const mostrarAlias = usuario.rol === 'Participante'

  return (
    <section className="perfil-usuario">
      <header className="perfil-usuario-cabecera">
        <div>
          <h2>{usuario.nombre} {usuario.apellido}</h2>
          <span className="perfil-usuario-username">@{usuario.nombreUsuario}</span>
        </div>
        <BadgeEstado estado={usuario.estado} />
      </header>

      <dl className="perfil-usuario-datos">
        <div>
          <dt>Rol</dt>
          <dd>{usuario.rol}</dd>
        </div>
        {mostrarCodigo && (
          <div>
            <dt>Código</dt>
            <dd>{codigoUsuario(usuario)}</dd>
          </div>
        )}
        {mostrarAlias && (
          <div>
            <dt>Alias</dt>
            <dd>{noDisponible(usuario.alias)}</dd>
          </div>
        )}
        <div>
          <dt>Nombre</dt>
          <dd>{noDisponible(usuario.nombre)}</dd>
        </div>
        <div>
          <dt>Apellido</dt>
          <dd>{noDisponible(usuario.apellido)}</dd>
        </div>
        <div>
          <dt>Sexo</dt>
          <dd>{noDisponible(usuario.sexo)}</dd>
        </div>
        <div>
          <dt>Correo</dt>
          <dd>{noDisponible(usuario.correo)}</dd>
        </div>
        <div>
          <dt>Teléfono</dt>
          <dd>{noDisponible(usuario.datosContacto?.telefono)}</dd>
        </div>
        <div>
          <dt>Dirección</dt>
          <dd>{noDisponible(usuario.datosContacto?.direccion)}</dd>
        </div>
        <div>
          <dt>Fecha de nacimiento</dt>
          <dd>{formatearFecha(usuario.fechaNacimiento)}</dd>
        </div>
        <div>
          <dt>Fecha de registro</dt>
          <dd>{formatearFecha(usuario.fechaRegistro)}</dd>
        </div>
      </dl>
    </section>
  )
}
