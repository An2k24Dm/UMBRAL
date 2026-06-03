// Tipos compartidos relacionados con misiones que las páginas de
// sesiones necesitan. Re-exporta lo que define el cliente HTTP de
// juegos-servicio para no duplicar definiciones.

import type {
  EtapaDetalleDto,
  MisionDetalleDto,
  MisionResumenDto
} from '../autenticacion/clienteApiJuegos'

export type {
  EtapaDetalleDto,
  MisionDetalleDto,
  MisionResumenDto
}
