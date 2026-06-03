// Fachada de acceso HTTP a juegos-servicio para misiones.
//
// Las páginas y hooks de sesiones importan desde acá; el adaptador
// HTTP real vive en `autenticacion/clienteApiJuegos.ts`.

export {
  obtenerMisionesActivas,
  obtenerDetalleMision
} from '../autenticacion/clienteApiJuegos'

export type { MisionResumenDto, MisionDetalleDto } from '../tipos/misiones'
