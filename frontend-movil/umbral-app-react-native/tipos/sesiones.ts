export type ModoSesion = "Individual" | "Grupal";

export type FiltroModoSesion = "Todas" | ModoSesion;

export type EstadoSesion =
  | "Programada"
  | "EnPreparacion"
  | "Activa"
  | "Pausada"
  | "Finalizada"
  | "Cancelada";

export interface SesionDisponibleMovilDto {
  id: string;
  nombre: string;
  descripcion: string;
  modo: ModoSesion;
  estado: EstadoSesion;
  // ISO 8601 (DateTime serializado por ASP.NET).
  fechaProgramada: string;
  cantidadMisiones: number;

  // Solo Individual:
  cantidadParticipantesActuales?: number | null;
  capacidadMaximaParticipantes?: number | null;

  // Solo Grupal:
  cantidadEquiposActuales?: number | null;
  capacidadMaximaEquipos?: number | null;
}

export interface EtapaSesionMovilDto {
  id: string;
  orden: number;
  // Discriminador del modo: "Trivia" o "BusquedaDelTesoro".
  tipoModoDeJuego: string;
  // ID del objeto de juego (triviaId, etc.).
  modoDeJuegoId: string;
  // Nombre amigable que carga juegos-servicio.
  nombreModoDeJuego: string;
  // Tiempo estimado en segundos para esa etapa.
  tiempoEstimadoSegundos: number;
}

// Misión vista desde el detalle de una sesión.
export interface MisionSesionMovilDto {
  id: string;
  orden: number;
  nombre: string;
  descripcion: string;
  dificultad?: string | null;
  totalEtapas: number;
  etapas: EtapaSesionMovilDto[];
}

// Estado de participación del usuario autenticado en la sesión (HU40).
// Lo calcula el backend a partir del token; el móvil no lo infiere solo.
export interface ParticipacionActual {
  estaInscrito: boolean;
  tipo: "Individual" | "Equipo" | null;
  equipoId: string | null;
  equipoNombre: string | null;
  esLider: boolean;
  participanteSesionId: string | null;
}

// DTO del detalle. El código de acceso es parte del flujo posterior
// (unirse a sesión), no se muestra en pantalla en esta iteración.
export interface SesionDetalleMovilDto {
  id: string;
  nombre: string;
  descripcion: string;
  modo: ModoSesion;
  estado: EstadoSesion;
  fechaProgramada: string;
  codigoAcceso: string;
  misiones: MisionSesionMovilDto[];
  participacionActual: ParticipacionActual;

  // Regla de participación única: si es false, el participante ya está en otra
  // sesión (o en esta) y el móvil no debe ofrecer "Unirse".
  puedeIngresar: boolean;
  motivoNoPuedeIngresar: string | null;
  sesionActualId: string | null;
  sesionActualNombre: string | null;
}

// Filtros que la pantalla del listado mantiene en su estado local.
export interface FiltrosListadoSesiones {
  busqueda: string;
  modo: FiltroModoSesion;
}

export interface IngresarSesionDto {
  codigoSesion: string;
}

export interface ContenidoSesionMovilDto {
  misionId: string;
  nombre: string;
  tipo: string;
  orden: number;
  descripcion: string | null;
  tiempoLimite: number | null;
}

export interface IngresarSesionRespuestaDto {
  sesionId: string;
  nombreSesion: string;
  codigoSesion: string;
  estado: EstadoSesion;
  modo: ModoSesion;
  ingresoRegistrado: boolean;
  redirigirADetalle: boolean;
  requiereEquipo: boolean;
  puedeCrearEquipo: boolean;
  yaPertenecia: boolean;
  mensaje: string | null;
  participacionActual: ParticipacionActual | null;
  contenido: ContenidoSesionMovilDto[];
}
