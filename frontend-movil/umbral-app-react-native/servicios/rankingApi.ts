import {
  construirUrl,
  leerCuerpoError,
  obtenerEncabezadosAutenticados,
} from "./clienteHttp";

export interface EntradaRankingGlobalDto {
  posicion: number;
  participanteIdentidadId: string;
  nombreParticipante: string;
  puntajeAcumulado: number;
  sesionesJugadas: number;
  etapasCompletadasTotal: number;
}

export interface EntradaRankingParticipanteDto {
  sesionId: string;
  participanteIdentidadId: string;
  nombreParticipante: string;
  puntajeTotal: number;
  respuestasCorrectas: number;
  respuestasTotales: number;
  etapasCompletadas: number;
  posicion: number;
}

export interface EntradaRankingEquipoDto {
  sesionId: string;
  equipoId: string;
  nombreEquipo: string;
  puntajeTotal: number;
  etapasCompletadas: number;
  posicion: number;
}

async function lanzarErrorRanking(respuesta: Response): Promise<never> {
  const cuerpo = await leerCuerpoError(respuesta);
  const mensaje =
    cuerpo?.mensaje ?? `Error ${respuesta.status} al consultar el ranking.`;
  const err = new Error(mensaje) as Error & { estadoHttp: number };
  err.estadoHttp = respuesta.status;
  throw err;
}

export async function obtenerRankingGlobalApi(
  tokenAcceso: string,
  top = 20,
): Promise<EntradaRankingGlobalDto[]> {
  const respuesta = await fetch(
    construirUrl(`/api/ranking/global?top=${top}`),
    { method: "GET", headers: obtenerEncabezadosAutenticados(tokenAcceso) },
  );
  if (!respuesta.ok) await lanzarErrorRanking(respuesta);
  return (await respuesta.json()) as EntradaRankingGlobalDto[];
}

export async function obtenerRankingParticipantesSesionApi(
  tokenAcceso: string,
  sesionId: string,
): Promise<EntradaRankingParticipanteDto[]> {
  const respuesta = await fetch(
    construirUrl(`/api/ranking/sesiones/${sesionId}/participantes`),
    { method: "GET", headers: obtenerEncabezadosAutenticados(tokenAcceso) },
  );
  if (!respuesta.ok) await lanzarErrorRanking(respuesta);
  return (await respuesta.json()) as EntradaRankingParticipanteDto[];
}

export async function obtenerRankingEquiposSesionApi(
  tokenAcceso: string,
  sesionId: string,
): Promise<EntradaRankingEquipoDto[]> {
  const respuesta = await fetch(
    construirUrl(`/api/ranking/sesiones/${sesionId}/equipos`),
    { method: "GET", headers: obtenerEncabezadosAutenticados(tokenAcceso) },
  );
  if (!respuesta.ok) await lanzarErrorRanking(respuesta);
  return (await respuesta.json()) as EntradaRankingEquipoDto[];
}
