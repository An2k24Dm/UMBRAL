import {
  construirUrl,
  leerCuerpoError,
  obtenerEncabezadosAutenticados,
} from "./clienteHttp";

export interface OpcionTrivia {
  id: string;
  texto: string;
}

export interface PreguntaTrivia {
  id: string;
  enunciado: string;
  puntajeAsignado: number;
  tiempoEstimado: number;
  opciones: OpcionTrivia[];
}

export interface TriviaParticipante {
  id: string;
  nombre: string;
  descripcion: string;
  tiempoLimitePorPregunta: number;
  preguntas: PreguntaTrivia[];
}

export interface RespuestaTriviaResultado {
  esCorrecta: boolean;
  puntosGanados: number;
  etapaCompletada: boolean;
}

export async function obtenerTriviaParticipante(
  sesionId: string,
  triviaId: string,
  token: string,
): Promise<TriviaParticipante> {
  const respuesta = await fetch(
    construirUrl(`/api/sesiones/${sesionId}/trivia/${triviaId}`),
    { headers: obtenerEncabezadosAutenticados(token) },
  );

  if (!respuesta.ok) {
    const cuerpo = await leerCuerpoError(respuesta);
    throw new Error(cuerpo?.mensaje ?? `Error ${respuesta.status}`);
  }

  return (await respuesta.json()) as TriviaParticipante;
}

export async function enviarRespuestaTrivia(
  sesionId: string,
  misionId: string,
  etapaId: string,
  triviaId: string,
  preguntaId: string,
  opcionSeleccionadaId: string,
  tiempoTardadoMs: number,
  totalPreguntasEtapa: number,
  token: string,
): Promise<RespuestaTriviaResultado> {
  const respuesta = await fetch(
    construirUrl(
      `/api/sesiones/${sesionId}/misiones/${misionId}/etapas/${etapaId}/trivia/${triviaId}/respuestas`,
    ),
    {
      method: "POST",
      headers: obtenerEncabezadosAutenticados(token),
      body: JSON.stringify({
        preguntaId,
        opcionSeleccionadaId,
        tiempoTardadoMs,
        totalPreguntasEtapa,
      }),
    },
  );

  if (respuesta.status === 409) {
    return { esCorrecta: false, puntosGanados: 0, etapaCompletada: false };
  }

  if (!respuesta.ok) {
    const cuerpo = await leerCuerpoError(respuesta);
    throw new Error(cuerpo?.mensaje ?? `Error ${respuesta.status}`);
  }

  return (await respuesta.json()) as RespuestaTriviaResultado;
}

export async function obtenerPreguntasRespondidas(
  sesionId: string,
  misionId: string,
  etapaId: string,
  token: string,
): Promise<string[]> {
  const respuesta = await fetch(
    construirUrl(
      `/api/sesiones/${sesionId}/misiones/${misionId}/etapas/${etapaId}/preguntas-respondidas`,
    ),
    { headers: obtenerEncabezadosAutenticados(token) },
  );

  if (!respuesta.ok) return [];

  return (await respuesta.json()) as string[];
}
