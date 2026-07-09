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
  // Presente SOLO cuando el backend responde 409 con un código de respuesta
  // duplicada real ("YA_RESPONDIDA"/"EQUIPO_YA_RESPONDIO"):
  // "equipo"     → otro integrante del equipo respondió primero (grupal).
  // "individual" → el propio participante ya había respondido.
  conflicto?: "equipo" | "individual";
}

// Error tipado de negocio al enviar una respuesta. Conserva el `codigo` del
// backend para que la pantalla decida (p. ej. "OPERACION_SESION_INVALIDA" =
// pregunta fuera de su ventana temporal → resincronizar, NO "ya respondida").
export class ErrorRespuestaTrivia extends Error {
  readonly codigo?: string;
  readonly estadoHttp: number;
  constructor(mensaje: string, estadoHttp: number, codigo?: string) {
    super(mensaje);
    this.name = "ErrorRespuestaTrivia";
    this.estadoHttp = estadoHttp;
    this.codigo = codigo;
  }
}

export async function obtenerTriviaParticipante(
  sesionId: string,
  misionId: string,
  etapaId: string,
  triviaId: string,
  token: string,
): Promise<TriviaParticipante> {
  // El backend valida server-side que esta etapa sea la actual permitida; el
  // contexto completo (misión/etapa) viaja en la ruta pero no es autoridad.
  const respuesta = await fetch(
    construirUrl(
      `/api/sesiones/${sesionId}/misiones/${misionId}/etapas/${etapaId}/trivia/${triviaId}`,
    ),
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
  opcionSeleccionadaId: string | null,
  tiempoTardadoMs: number,
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
      }),
    },
  );

  if (respuesta.status === 409) {
    const cuerpo = await leerCuerpoError(respuesta);
    // SOLO estos códigos son una respuesta duplicada real. No inferir duplicado
    // por el status 409 a secas: el backend también devuelve 409 para otras
    // reglas (p. ej. OPERACION_SESION_INVALIDA cuando la pregunta aún no está en
    // su ventana temporal), que NO deben mostrarse como "Ya respondida".
    if (cuerpo?.codigo === "YA_RESPONDIDA") {
      return { esCorrecta: false, puntosGanados: 0, etapaCompletada: false, conflicto: "individual" };
    }
    if (cuerpo?.codigo === "EQUIPO_YA_RESPONDIO") {
      return { esCorrecta: false, puntosGanados: 0, etapaCompletada: false, conflicto: "equipo" };
    }
    throw new ErrorRespuestaTrivia(
      cuerpo?.mensaje ?? `Error ${respuesta.status}`, 409, cuerpo?.codigo);
  }

  if (!respuesta.ok) {
    const cuerpo = await leerCuerpoError(respuesta);
    throw new ErrorRespuestaTrivia(
      cuerpo?.mensaje ?? `Error ${respuesta.status}`, respuesta.status, cuerpo?.codigo);
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
