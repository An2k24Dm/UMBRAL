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

export function crearConexionSesionesTiempoReal(token: string) {
  return new signalR.HubConnectionBuilder()
    .withUrl(`${URL_API}/hubs/sesiones`, {
      accessTokenFactory: () => token,
      transport: signalR.HttpTransportType.WebSockets,
    })
    .withAutomaticReconnect()
    .build();
}
