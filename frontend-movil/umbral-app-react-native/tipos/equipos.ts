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

// HU43 — Equipo en el listado de equipos de una sesión.
export interface EquipoSesionListado {
  id: string;
  sesionId: string;
  nombre: string;
  tipo: TipoEquipo;
  puntaje: number;
  cantidadParticipantes: number;
  capacidadMaxima: number;
  estaLleno: boolean;
  fechaCreacion: string;
  esMiEquipo: boolean;
  soyLider: boolean;
}

// HU43 — Integrante de un equipo. Solo datos no sensibles.
export interface IntegranteEquipo {
  participanteSesionId: string;
  participanteIdentidadId: string;
  nombre: string;
  apellido: string;
  alias: string;
  puntaje: number;
  fechaUnion: string;
  esLider: boolean;
}

// HU47 — Solicitud de ingreso a un equipo. La contraseña solo es necesaria
// para equipos privados; el participante lo resuelve el backend desde el JWT.
export interface IngresarEquipoSolicitud {
  contrasena?: string | null;
}

// HU47 — Respuesta del backend al ingresar. Nunca incluye contraseña ni hash.
export interface IngresarEquipoRespuesta {
  sesionId: string;
  equipoId: string;
  equipoNombre: string;
  tipo: TipoEquipo;
  cantidadParticipantes: number;
  capacidadMaxima: number;
  esMiEquipo: boolean;
}

// HU43 — Detalle de un equipo con sus integrantes.
export interface EquipoSesionDetalle {
  id: string;
  sesionId: string;
  nombre: string;
  tipo: TipoEquipo;
  puntaje: number;
  cantidadParticipantes: number;
  capacidadMaxima: number;
  fechaCreacion: string;
  estaLleno: boolean;
  liderParticipanteId: string;
  esMiEquipo: boolean;
  soyLider: boolean;
  participantes: IntegranteEquipo[];
}
