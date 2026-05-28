import { LayoutPanel } from '../componentes/LayoutPanel'
import { TarjetaResumen } from '../componentes/TarjetaResumen'
import { usarAutenticacion } from '../autenticacion/ProveedorAutenticacion'

export function PaginaAdministrador() {
  const { usuario } = usarAutenticacion()

  return (
    <LayoutPanel
      titulo="Dashboard"
      descripcion={`Bienvenido, ${usuario?.nombre ?? ''} ${usuario?.apellido ?? ''}.`}
    >
      <section className="seccion">
        <div className="seccion-cabecera">
          <div>
            <h2>Gestión de usuarios</h2>
            <p>Atajos a las funcionalidades disponibles para el rol Administrador.</p>
          </div>
        </div>
        <div className="rejilla-tarjetas">
          <TarjetaResumen
            titulo="Registrar usuario"
            descripcion="Crear cuentas de Operador o Administrador. El código se genera automáticamente."
            destino="/administrador/usuarios/registrar"
            textoAccion="Ir al registro"
          />
          <TarjetaResumen
            titulo="Participantes"
            descripcion="Consultar las cuentas de Participantes registrados en la plataforma."
            destino="/administrador/usuarios/participantes"
            textoAccion="Ver participantes"
          />
          <TarjetaResumen
            titulo="Operadores y Administradores"
            descripcion="Consultar las cuentas internas del sistema."
            destino="/administrador/usuarios/internos"
            textoAccion="Ver usuarios internos"
          />
          <TarjetaResumen
            titulo="Trivias"
            descripcion="Crear y gestionar trivias: agregar, modificar o eliminar preguntas."
            destino="/administrador/trivias"
            textoAccion="Gestionar trivias"
          />
          <TarjetaResumen
            titulo="Búsquedas del tesoro"
            descripcion="Crear y gestionar búsquedas del tesoro con etapas y misiones."
            destino="/administrador/busquedas"
            textoAccion="Gestionar búsquedas"
          />
          <TarjetaResumen
            titulo="Mi perfil"
            descripcion="Revisar los datos personales asociados a su cuenta."
            destino="/administrador/perfil"
            textoAccion="Ver perfil"
          />
        </div>
      </section>
    </LayoutPanel>
  )
}
