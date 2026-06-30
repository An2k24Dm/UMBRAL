import * as signalR from "@microsoft/signalr";
import { URL_API } from "./clienteHttp";

export interface EventoSesionTiempoReal {
  sesionId?: string;
  SesionId?: string;
  equipoId?: string | null;
  EquipoId?: string | null;
  tipoEvento?: string;
  TipoEvento?: string;
  fechaEventoUtc?: string;
  FechaEventoUtc?: string;
}

export function obtenerSesionIdEvento(evento: EventoSesionTiempoReal): string {
  return evento.sesionId ?? evento.SesionId ?? "";
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

export function crearConexionSesionesTiempoReal(token: string) {
  // Dejamos que SignalR negocie el transporte automáticamente (no forzamos
  // WebSockets): es más robusto en Expo y evita fallos de negociación.
  return new signalR.HubConnectionBuilder()
    .withUrl(`${URL_API}/hubs/sesiones`, {
      accessTokenFactory: () => token,
    })
    .withAutomaticReconnect()
    .build();
}
