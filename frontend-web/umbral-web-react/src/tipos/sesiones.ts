// Tipos centralizados del dominio "sesiones" en el frontend.
//
// Re-exportamos los DTOs del cliente HTTP para que las páginas y los
// hooks no tengan que importar desde `autenticacion/`. La intención es
// que `autenticacion/clienteApiSesiones.ts` siga siendo el adaptador
// HTTP (fetch + manejo de errores) y este módulo sea la única puerta
// de tipos compartidos entre vistas, componentes y hooks.

import type {
  CrearSesionRespuestaDto,
  CrearSesionSolicitud,
  EquipoSesionDetalleDto,
  EquipoSesionDto,
  EquipoSesionListadoDto,
  EstadoSesionApi,
  FiltrosListadoSesiones,
  ModificarSesionSolicitud,
  ModoSesionApi,
  IntegranteEquipoDto,
  ParticipanteEquipoDto,
  ParticipanteSesionDto,
  SesionDetalleDto,
  SesionListadoDto,
  SesionMisionDto
} from '../autenticacion/clienteApiSesiones'

export type ModoSesion = ModoSesionApi
export type EstadoSesion = EstadoSesionApi

export type {
  CrearSesionRespuestaDto,
  CrearSesionSolicitud as CrearSesionSolicitudDto,
  EquipoSesionDetalleDto,
  EquipoSesionDto,
  EquipoSesionListadoDto,
  FiltrosListadoSesiones,
  ModificarSesionSolicitud,
  IntegranteEquipoDto,
  ParticipanteEquipoDto,
  ParticipanteSesionDto,
  SesionDetalleDto,
  SesionListadoDto,
  SesionMisionDto
}
