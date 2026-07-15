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
  fechaInicioUtc: string | null;
  duracionSegundosLimite: number | null;
  ejecucionActual: EjecucionActualSesionDto | null;
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

export interface EjecucionActualSesionDto {
  misionId: string;
  etapaId: string;
  modoDeJuegoId: string;
  tipoEtapa: string;
  ordenGlobal: number;
  fechaInicioUtc: string;
  duracionSegundos: number;
  duracionPausasAcumuladaMs: number;
  fechaInicioPausaUtc: string | null;
  segundosRestantes: number;
}

// Progreso combinado (trivia + tesoro) de cada participante en la sesión.
export interface ProgresoSesionParticipanteDto {
  participanteIdentidadId: string;
  equipoId?: string | null;
  triviaEtapasCompletadas: number;
  triviaRespondidas: number;
  triviaCorrectas: number;
  triviaIncorrectas: number;
  tesoroIntentosEnviados: number;
  tesoroEtapasCompletadas: number;
}

export interface ProgresoSecuencialSesionDto {
  etapasCompletadasGlobalmenteIds?: string[];
  etapasCompletadasIds: string[];
  misionActualId: string | null;
  etapaActualId: string | null;
  tipoEtapaActual: string | null;
  modoDeJuegoId: string | null;
  ordenGlobalActual: number | null;
  // Preparación entre etapas/misiones. "Preparacion" ⇒ etapa aún NO jugable.
  faseEtapaActual?: string | null;
  fechaInicioProgramadaEtapaUtc?: string | null;
  segundosRestantesPreparacion?: number | null;
  duracionPreparacionSegundos?: number | null;
  numeroMisionActual?: number | null;
  numeroEtapaActual?: number | null;
  esNuevaMision?: boolean;
  fechaInicioEtapaUtc?: string | null;
  duracionEtapaSegundos?: number | null;
  duracionPausasAcumuladaMs?: number;
  fechaInicioPausaUtc?: string | null;
  segundosRestantesEtapa?: number | null;
  tiempoActivoEtapaMs?: number | null;
  triviaPreguntaActualId?: string | null;
  triviaPreguntasExpiradasIds?: string[];
  triviaTiempoRestantePreguntaMs?: number | null;
  triviaTiempoTranscurridoPreguntaMs?: number | null;
  triviaAgotada?: boolean;
  // Ventana de feedback autoritativa entre preguntas (mínimo 5 s).
  triviaEnTransicionEntrePreguntas?: boolean;
  triviaTiempoRestanteTransicionMs?: number | null;
  triviaSiguientePreguntaId?: string | null;
  jugadorActualCompletoEtapaActual?: boolean;
  esperandoOtrosJugadores?: boolean;
  todoCompletado: boolean;
}

// Historial de participaciones finalizadas del participante.
export interface MiParticipacionDto {
  sesionId: string;
  nombreSesion: string;
  modo: string;
  fechaInicioUtc: string | null;
  fechaFinalizacionUtc: string | null;
  puntajeObtenido: number;
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
