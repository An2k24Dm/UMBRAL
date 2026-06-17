// Fachada de acceso HTTP a sesiones-servicio.
//
// Re-exporta las funciones del adaptador HTTP existente para que las
// páginas y hooks importen desde `servicios/` y no desde `autenticacion/`.
// Mantiene una única implementación de fetch + manejo de errores.

export {
  crearSesion,
  listarSesiones,
  obtenerSesion,
  actualizarSesion
} from '../autenticacion/clienteApiSesiones'

export type {
  CrearSesionRespuestaDto,
  CrearSesionSolicitudDto,
  FiltrosListadoSesiones,
  ModificarSesionSolicitud,
  SesionDetalleDto,
  SesionListadoDto
} from '../tipos/sesiones'
