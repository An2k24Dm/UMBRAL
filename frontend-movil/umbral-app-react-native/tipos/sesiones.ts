// Tipos del flujo móvil de sesiones disponibles para Participante.
//
// Se mantienen alineados con los DTOs expuestos por sesiones-servicio en:
//   * GET /api/sesiones/participante/disponibles
//   * GET /api/sesiones/participante/disponibles/{sesionId}
//
// El móvil no genera estos tipos a partir del backend; se replican a mano
// para que el editor avise si en algún momento divergen.

// El backend serializa el discriminador como string "Individual" | "Grupal".
export type ModoSesion = "Individual" | "Grupal";

// "Todas" es un valor exclusivo de la UI para indicar "sin filtro de modo".
// El cliente HTTP NO lo envía al backend: se traduce a omitir el query param.
export type FiltroModoSesion = "Todas" | ModoSesion;

// Estados que el Participante puede llegar a ver en el listado/detalle. El
// backend solo devuelve sesiones en estos estados (defensa en profundidad).
// Se incluye el catálogo completo porque el detalle también muestra el
// estado actual y, eventualmente, otros estados podrían llegar si el
// Operador transiciona la sesión durante la consulta.
export type EstadoSesion =
  | "Programada"
  | "EnPreparacion"
  | "Activa"
  | "Pausada"
  | "Finalizada"
  | "Cancelada";

// DTO del listado móvil. Solo expone los datos pensados para el Participante:
// los identificadores administrativos (OperadorCreadorId, fechas internas,
// código de acceso) NUNCA se incluyen aquí.
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

// Etapa de una misión vista desde móvil. Mantiene los nombres del backend
// (TipoModoDeJuego, NombreModoDeJuego, TiempoEstimadoSegundos) pero
// camelCase al estilo TypeScript.
export interface EtapaSesionMovilDto {
  id: string;
  orden: number;
  // Discriminador del modo: "Trivia" o "BusquedaDelTesoro".
  tipoModoDeJuego: string;
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
