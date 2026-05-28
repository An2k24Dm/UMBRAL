import { LayoutPanel } from '../componentes/LayoutPanel'
import { TarjetaResumen } from '../componentes/TarjetaResumen'
import { usarAutenticacion } from '../autenticacion/ProveedorAutenticacion'

export function PaginaOperador() {
  const { usuario } = usarAutenticacion()

  return (
    <LayoutPanel
      titulo="Dashboard"
      descripcion={`Bienvenido, ${usuario?.nombre ?? ''} ${usuario?.apellido ?? ''}.`}
    >
      <section className="seccion">
        <div className="seccion-cabecera">
          <div>
            <h2>Acciones disponibles</h2>
            <p>Atajos a las funcionalidades habilitadas para el rol Operador.</p>
          </div>
        </div>
        <div className="rejilla-tarjetas">
          <TarjetaResumen
            titulo="Trivias"
            descripcion="Crear y gestionar trivias: agregar, modificar o eliminar preguntas."
            destino="/operador/trivias"
            textoAccion="Gestionar trivias"
          />
          <TarjetaResumen
            titulo="Participantes"
            descripcion="Consultar las cuentas de Participantes registrados."
            destino="/operador/usuarios/participantes"
            textoAccion="Ver participantes"
          />
          <TarjetaResumen
            titulo="Mi perfil"
            descripcion="Revisar los datos personales asociados a su cuenta."
            destino="/operador/perfil"
            textoAccion="Ver perfil"
          />
        </div>
      </section>
    </LayoutPanel>
  )
}
