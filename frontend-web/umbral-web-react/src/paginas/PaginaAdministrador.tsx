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
            <h2>Resumen general</h2>
            <p>Métricas operativas. Las cifras se cargarán desde el backend cuando los endpoints estén disponibles.</p>
          </div>
        </div>
        <div className="rejilla-tarjetas">
          {/* TODO backend: reemplazar por consulta de métricas reales (HU futuras). */}
          <TarjetaResumen
            titulo="Participantes registrados"
            descripcion="Pendiente de backend. La métrica se calculará cuando el endpoint esté disponible."
            insignia="Pendiente"
            deshabilitado
          />
          <TarjetaResumen
            titulo="Operadores activos"
            descripcion="Pendiente de backend. La métrica se calculará cuando el endpoint esté disponible."
            insignia="Pendiente"
            deshabilitado
          />
          <TarjetaResumen
            titulo="Sesiones del día"
            descripcion="Módulo Sesiones no disponible todavía."
            insignia="Próximamente"
            deshabilitado
          />
        </div>
      </section>

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
            titulo="Mi perfil"
            descripcion="Revisar los datos personales asociados a su cuenta."
            destino="/administrador/perfil"
            textoAccion="Ver perfil"
          />
        </div>
      </section>

      <section className="seccion">
        <div className="seccion-cabecera">
          <div>
            <h2>Módulos próximos</h2>
            <p>Funcionalidades planificadas para iteraciones posteriores.</p>
          </div>
        </div>
        <div className="rejilla-tarjetas">
          <TarjetaResumen titulo="Trivias" descripcion="Configuración de trivias del juego." insignia="Próximamente" deshabilitado />
          <TarjetaResumen titulo="Misiones" descripcion="Diseño y publicación de misiones." insignia="Próximamente" deshabilitado />
          <TarjetaResumen titulo="Sesiones" descripcion="Gestión y monitoreo de sesiones activas." insignia="Próximamente" deshabilitado />
          <TarjetaResumen titulo="Ranking" descripcion="Consulta de ranking de participantes." insignia="Próximamente" deshabilitado />
          <TarjetaResumen titulo="Logs" descripcion="Bitácora de eventos del sistema." insignia="Próximamente" deshabilitado />
        </div>
      </section>
    </LayoutPanel>
  )
}
