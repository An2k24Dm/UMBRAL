import * as signalR from "@microsoft/signalr";
import { URL_API } from "./clienteHttp";
import { crearLoggerSignalRMovil } from "./signalRLogger";

export interface PuntajeCalculadoEvento {
  eventoIdOrigen?: string;
  EventoIdOrigen?: string;
  sesionId?: string;
  SesionId?: string;
  participanteSesionId?: string;
  ParticipanteSesionId?: string;
  participanteIdentidadId?: string;
  ParticipanteIdentidadId?: string;
  equipoId?: string | null;
  EquipoId?: string | null;
  puntajeGanado?: number;
  PuntajeGanado?: number;
  puntajeTotalParticipante?: number;
  PuntajeTotalParticipante?: number;
  puntajeTotalEquipo?: number | null;
  PuntajeTotalEquipo?: number | null;
  calculadoEnUtc?: string;
  CalculadoEnUtc?: string;
}

export function crearConexionRankingTiempoReal(token: string) {
  const tokenLimpio = token.trim();
  if (!tokenLimpio) {
    throw new Error("No se puede crear conexión SignalR sin token de acceso.");
  }

  const conexion = new signalR.HubConnectionBuilder()
    .withUrl(`${URL_API}/hubs/ranking`, {
      accessTokenFactory: () => tokenLimpio,
    })
    .withAutomaticReconnect([0, 2000, 5000, 10000, 30000])
    .configureLogging(crearLoggerSignalRMovil("Ranking"))
    .build();

  conexion.serverTimeoutInMilliseconds = 60000;
  conexion.keepAliveIntervalInMilliseconds = 15000;
  return conexion;
}

export function obtenerEventoIdOrigen(evento: PuntajeCalculadoEvento): string {
  return evento.eventoIdOrigen ?? evento.EventoIdOrigen ?? "";
}

export function obtenerPuntajeGanado(evento: PuntajeCalculadoEvento): number {
  return evento.puntajeGanado ?? evento.PuntajeGanado ?? 0;
}
