// Tipos del flujo móvil de creación de equipos (HU40). Alineados con los
// DTOs de sesiones-servicio en POST /api/sesiones/{sesionId}/equipos.

export type TipoEquipo = "Publico" | "Privado";

// Solicitud de creación. La contraseña solo viaja cuando el equipo es
// privado; el líder lo resuelve el backend desde el usuario autenticado.
export interface CrearEquipoSolicitud {
  nombre: string;
  tipo: TipoEquipo;
  contrasena?: string | null;
}

// Respuesta del backend. Nunca incluye contraseña ni hash.
export interface CrearEquipoRespuesta {
  id: string;
  sesionId: string;
  nombre: string;
  tipo: TipoEquipo;
  capacidadMaxima: number;
  cantidadParticipantes: number;
  liderParticipanteId: string;
  puntaje: number;
  fechaCreacion: string;
}
