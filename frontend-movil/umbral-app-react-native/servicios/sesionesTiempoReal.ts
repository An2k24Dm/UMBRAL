import * as signalR from "@microsoft/signalr";
import { URL_API } from "./clienteHttp";
import {
  crearLoggerSignalRMovil,
  esErrorSignalRTransitorio,
} from "./signalRLogger";

interface DiagnosticoConexionTiempoReal {
  origen: string;
  id: string;
}

const diagnosticosConexiones = new WeakMap<
  signalR.HubConnection,
  DiagnosticoConexionTiempoReal
>();

const EVENTOS_SESIONES_SIN_HANDLER_REQUERIDO = [
  "ParticipantesSesionActualizados",
  "EquiposSesionActualizados",
  "EquipoActualizado",
  "SesionActualizada",
  "ParticipanteExpulsadoSesion",
  "EquipoExpulsadoSesion",
  "RespuestaRegistrada",
  "EtapaCompletada",
  "EtapaPorComenzar",
  "EtapaIniciada",
  "ProgresoSecuencialActualizado",
  "PistaLiberada",
  "UbicacionActualizada",
] as const;

function crearIdDiagnostico(): string {
  return Math.random().toString(36).slice(2, 8);
}

export interface EventoSesionTiempoReal {
  sesionId?: string;
  SesionId?: string;
  equipoId?: string | null;
  EquipoId?: string | null;
  // Estado del ciclo de vida: solo lo trae el evento "SesionActualizada".
  estado?: string;
  Estado?: string;
  tipoEvento?: string;
  TipoEvento?: string;
  fechaEventoUtc?: string;
  FechaEventoUtc?: string;
}

// Evento "EtapaPorComenzar": la siguiente etapa está programada y comenzará en
// DuracionPreparacionSegundos. Aún NO es jugable (eso lo indica EtapaIniciada).
export interface EventoEtapaPorComenzar {
  sesionId?: string;
  SesionId?: string;
  misionId?: string;
  MisionId?: string;
  etapaId?: string;
  EtapaId?: string;
  tipoEtapa?: string;
  TipoEtapa?: string;
  modoDeJuegoId?: string;
  ModoDeJuegoId?: string;
  numeroMision?: number;
  NumeroMision?: number;
  numeroEtapa?: number;
  NumeroEtapa?: number;
  ordenGlobal?: number;
  OrdenGlobal?: number;
  esNuevaMision?: boolean;
  EsNuevaMision?: boolean;
  fechaInicioProgramadaUtc?: string;
  FechaInicioProgramadaUtc?: string;
  duracionPreparacionSegundos?: number;
  DuracionPreparacionSegundos?: number;
  fechaEventoUtc?: string;
  FechaEventoUtc?: string;
}

export function obtenerSesionIdEvento(evento: EventoSesionTiempoReal): string {
  return evento.sesionId ?? evento.SesionId ?? "";
}

// Normaliza el evento EtapaPorComenzar (camelCase o PascalCase) a un estado de
// banner tipado. Fuente de verdad del countdown: fechaInicioProgramadaUtc.
export interface EstadoBannerEtapaPorComenzar {
  sesionId: string;
  mensaje: string;
  numeroMision: number;
  numeroEtapa: number;
  esNuevaMision: boolean;
  fechaInicioProgramadaUtc: string;
  duracionPreparacionSegundos: number;
}

export function mapearEtapaPorComenzar(
  evento: EventoEtapaPorComenzar,
): EstadoBannerEtapaPorComenzar | null {
  const sesionId = evento.sesionId ?? evento.SesionId ?? "";
  const fechaInicioProgramadaUtc =
    evento.fechaInicioProgramadaUtc ?? evento.FechaInicioProgramadaUtc ?? "";
  if (!sesionId || !fechaInicioProgramadaUtc) return null;

  const esNuevaMision = evento.esNuevaMision ?? evento.EsNuevaMision ?? false;
  const numeroMision = evento.numeroMision ?? evento.NumeroMision ?? 0;
  const numeroEtapa = evento.numeroEtapa ?? evento.NumeroEtapa ?? 0;
  const duracionPreparacionSegundos =
    evento.duracionPreparacionSegundos ?? evento.DuracionPreparacionSegundos ?? 10;

  return {
    sesionId,
    mensaje: esNuevaMision
      ? `Misión ${numeroMision} está por comenzar`
      : `Etapa ${numeroEtapa} está por comenzar`,
    numeroMision,
    numeroEtapa,
    esNuevaMision,
    fechaInicioProgramadaUtc,
    duracionPreparacionSegundos,
  };
}

export function obtenerEstadoEvento(
  evento: EventoSesionTiempoReal,
): string | undefined {
  return evento.estado ?? evento.Estado;
}

export function obtenerEquipoIdEvento(evento: EventoSesionTiempoReal): string {
  return evento.equipoId ?? evento.EquipoId ?? "";
}

export function esErrorNoAutenticadoTiempoReal(error: unknown): boolean {
  if (!error) return false;

  const candidato = error as {
    statusCode?: unknown;
    status?: unknown;
    message?: unknown;
  };

  if (candidato.statusCode === 401 || candidato.status === 401) return true;

  const mensaje =
    typeof candidato.message === "string"
      ? candidato.message
      : typeof error === "string"
        ? error
        : "";

  return /(?:\b401\b|unauthorized|no_autenticado|no autenticado)/i.test(
    mensaje,
  );
}

function extraerMensajeErrorTiempoReal(error: unknown): string {
  const candidato = error as { message?: unknown };
  if (typeof candidato?.message === "string") return candidato.message;
  if (typeof error === "string") return error;
  return "";
}

export function mensajeSeguroErrorTiempoReal(error: unknown): string {
  const candidato = error as {
    statusCode?: unknown;
    status?: unknown;
    message?: unknown;
  };
  const estado = candidato?.statusCode ?? candidato?.status;
  const mensaje =
    typeof candidato?.message === "string"
      ? candidato.message
      : typeof error === "string"
        ? error
        : "Solicitud rechazada por el servidor.";

  return estado ? `${estado}: ${mensaje}` : mensaje;
}

// Clasificación honesta de un error de conexión SignalR a nivel de OBJETO (no
// solo del mensaje). Distingue autenticación (401) de una interrupción de
// transporte recuperable (timeout/WebSocket cerrado/network failed) para no
// etiquetar un fallo de red como "acceso a grupo rechazado" (ver requerimiento).
// No es posible distinguir de forma fiable en el cliente un rechazo real del
// servidor de un fallo terminal de transporte: se agrupan como "grave" y el
// contexto de la operación se registra explícitamente en el log.
export type CategoriaErrorConexionTiempoReal =
  | "autenticacion"
  | "transitorio"
  | "grave";

export function esErrorConexionTransitorioTiempoReal(error: unknown): boolean {
  if (!error) return false;
  if (esErrorNoAutenticadoTiempoReal(error)) return false;
  return esErrorSignalRTransitorio(extraerMensajeErrorTiempoReal(error));
}

export function clasificarErrorConexionTiempoReal(
  error: unknown,
): CategoriaErrorConexionTiempoReal {
  if (esErrorNoAutenticadoTiempoReal(error)) return "autenticacion";
  if (esErrorConexionTransitorioTiempoReal(error)) return "transitorio";
  return "grave";
}

// Registro de diagnóstico (solo __DEV__) de un error de conexión que el hook NO
// trata como 401. Nunca usa console.error: un fallo terminal real ya lo escala
// el propio logger de SignalR (crearLoggerSignalRMovil). Aquí solo aportamos
// contexto de la operación (unión a grupo, reconexión, cierre) sin caja roja.
export function registrarErrorConexionTiempoRealDev(
  error: unknown,
  contexto?: string,
) {
  if (!__DEV__ || !error) return;

  const categoria = clasificarErrorConexionTiempoReal(error);
  // El 401 lo maneja el flujo de seguridad del hook; no es ruido de diagnóstico.
  if (categoria === "autenticacion") return;

  const sufijo = contexto ? `[${contexto}] ` : "";
  const mensaje = mensajeSeguroErrorTiempoReal(error);

  if (categoria === "transitorio") {
    console.warn(
      `[SignalR Movil] ${sufijo}conexión interrumpida temporalmente; ` +
        `se intentará reconectar. Detalle: ${mensaje}`,
    );
    return;
  }

  console.warn(
    `[SignalR Movil] ${sufijo}error de conexión en tiempo real: ${mensaje}`,
  );
}

export function registrarEventoConexionSesionesTiempoReal(
  conexion: signalR.HubConnection,
  mensaje: string,
) {
  if (!__DEV__) return;

  const diagnostico = diagnosticosConexiones.get(conexion);
  const origen = diagnostico?.origen ?? "General";
  const id = diagnostico?.id ?? "sin-id";
  console.log(`[SignalR][${origen}][${id}] ${mensaje}`);
}

export function crearConexionSesionesTiempoReal(
  token: string,
  origen = "General",
) {
  // Nunca abrir una conexión sin token válido (evita 401 no controlados).
  const tokenLimpio = token?.trim();
  if (!tokenLimpio) {
    throw new Error("No se puede crear conexión SignalR sin token de acceso.");
  }
  const diagnostico = {
    origen,
    id: crearIdDiagnostico(),
  };
  // Dejamos que SignalR negocie el transporte automáticamente (no forzamos
  // WebSockets): es más robusto en Expo y evita fallos de negociación.
  const conexion = new signalR.HubConnectionBuilder()
    .withUrl(`${URL_API}/hubs/sesiones`, {
      accessTokenFactory: () => tokenLimpio,
    })
    .withAutomaticReconnect([0, 2000, 5000, 10000, 30000])
    .configureLogging(
      crearLoggerSignalRMovil(`Sesiones:${origen}:${diagnostico.id}`),
    )
    .build();

  conexion.serverTimeoutInMilliseconds = 60000;
  conexion.keepAliveIntervalInMilliseconds = 15000;

  for (const evento of EVENTOS_SESIONES_SIN_HANDLER_REQUERIDO) {
    conexion.on(evento, () => undefined);
  }

  diagnosticosConexiones.set(conexion, diagnostico);
  registrarEventoConexionSesionesTiempoReal(conexion, "creando");

  return conexion;
}
