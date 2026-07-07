import * as signalR from "@microsoft/signalr";
import { URL_API } from "./clienteHttp";

export function crearConexionPartidasTiempoReal(token: string) {
  const tokenLimpio = token?.trim();
  if (!tokenLimpio) {
    throw new Error("No se puede crear conexión SignalR sin token de acceso.");
  }
  return new signalR.HubConnectionBuilder()
    .withUrl(`${URL_API}/hubs/partidas`, {
      accessTokenFactory: () => tokenLimpio,
    })
    .withAutomaticReconnect()
    .build();
}
