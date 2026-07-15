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
            titulo="Sesiones"
            descripcion="Crear y gestionar tus sesiones programadas o en ejecución."
            destino="/operador/sesiones"
            textoAccion="Gestionar sesiones"
          />
          <TarjetaResumen
            titulo="Crear sesión"
            descripcion="Programa una nueva sesión a partir de contenido activo disponible."
            destino="/operador/sesiones/crear"
            textoAccion="Crear sesión"
          />
          <TarjetaResumen
            titulo="Participantes"
            descripcion="Consultar participantes vinculados a tus sesiones."
            destino="/operador/usuarios/participantes"
            textoAccion="Ver participantes"
          />
          <TarjetaResumen
            titulo="Mi perfil"
            descripcion="Revisar tus datos personales asociados a la cuenta."
            destino="/operador/perfil"
            textoAccion="Ver perfil"
          />
        </div>
      </section>
    </LayoutPanel>
  )
}
