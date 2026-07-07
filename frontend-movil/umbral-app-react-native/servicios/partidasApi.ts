import type {
  EstadoPartidaDto,
  RankingEntradaDto,
  RespuestaTriviaResultadoDto,
  TriviaParticipanteDto,
} from "../tipos/partidas";
import {
  construirUrl,
  leerCuerpoError,
  obtenerEncabezadosAutenticados,
} from "./clienteHttp";

// ---------------------------------------------------------------------------
// Trivia para participante (sin revelar esCorrecta)
// ---------------------------------------------------------------------------
export async function obtenerTriviaParticipanteApi(
  triviaId: string,
  tokenAcceso: string,
): Promise<TriviaParticipanteDto> {
  const respuesta = await fetch(
    construirUrl(`/api/juegos/trivias/${triviaId}/participante`),
    { headers: obtenerEncabezadosAutenticados(tokenAcceso) },
  );
  if (!respuesta.ok) {
    const error = await leerCuerpoError(respuesta);
    throw new Error(error?.mensaje ?? `Error ${respuesta.status} al cargar la trivia.`);
  }
  return (await respuesta.json()) as TriviaParticipanteDto;
}

// ---------------------------------------------------------------------------
// Enviar respuesta de trivia
// ---------------------------------------------------------------------------
export async function enviarRespuestaTriviaApi(
  tokenAcceso: string,
  sesionId: string,
  misionId: string,
  etapaId: string,
  triviaId: string,
  preguntaId: string,
  opcionSeleccionadaId: string,
  tiempoTardadoMs: number,
): Promise<RespuestaTriviaResultadoDto> {
  const ruta =
    `/api/partidas/sesiones/${sesionId}/misiones/${misionId}` +
    `/etapas/${etapaId}/trivia/${triviaId}/respuestas`;

  const respuesta = await fetch(construirUrl(ruta), {
    method: "POST",
    headers: obtenerEncabezadosAutenticados(tokenAcceso),
    body: JSON.stringify({ preguntaId, opcionSeleccionadaId, tiempoTardadoMs }),
  });

  if (!respuesta.ok) {
    const error = await leerCuerpoError(respuesta);
    throw new Error(error?.mensaje ?? `Error ${respuesta.status} al enviar respuesta.`);
  }
  return (await respuesta.json()) as RespuestaTriviaResultadoDto;
}

// ---------------------------------------------------------------------------
// Ranking
// ---------------------------------------------------------------------------
export async function obtenerRankingApi(
  sesionId: string,
  tokenAcceso: string,
): Promise<RankingEntradaDto[]> {
  const respuesta = await fetch(
    construirUrl(`/api/partidas/sesiones/${sesionId}/ranking`),
    { headers: obtenerEncabezadosAutenticados(tokenAcceso) },
  );
  if (!respuesta.ok) {
    const error = await leerCuerpoError(respuesta);
    throw new Error(error?.mensaje ?? `Error ${respuesta.status} al cargar el ranking.`);
  }
  return (await respuesta.json()) as RankingEntradaDto[];
}

// ---------------------------------------------------------------------------
// Preguntas ya respondidas por el participante/equipo en una etapa
// ---------------------------------------------------------------------------
export async function obtenerPreguntasRespondidasApi(
  sesionId: string,
  misionId: string,
  etapaId: string,
  tokenAcceso: string,
): Promise<string[]> {
  const ruta =
    `/api/partidas/sesiones/${sesionId}/misiones/${misionId}/etapas/${etapaId}/preguntas-respondidas`;
  const respuesta = await fetch(construirUrl(ruta), {
    headers: obtenerEncabezadosAutenticados(tokenAcceso),
  });
  if (!respuesta.ok) return [];
  return (await respuesta.json()) as string[];
}

// ---------------------------------------------------------------------------
// Estado de la partida (para saber si está activa)
// ---------------------------------------------------------------------------
export async function obtenerEstadoPartidaApi(
  sesionId: string,
  tokenAcceso: string,
): Promise<EstadoPartidaDto> {
  const respuesta = await fetch(
    construirUrl(`/api/partidas/sesiones/${sesionId}/estado`),
    { headers: obtenerEncabezadosAutenticados(tokenAcceso) },
  );
  if (!respuesta.ok) {
    const error = await leerCuerpoError(respuesta);
    throw new Error(error?.mensaje ?? `Error ${respuesta.status} al consultar la partida.`);
  }
  return (await respuesta.json()) as EstadoPartidaDto;
}
