export interface OpcionParticipanteDto {
  id: string;
  texto: string;
}

export interface PreguntaParticipanteDto {
  id: string;
  enunciado: string;
  puntajeAsignado: number;
  tiempoEstimado: number;
  opciones: OpcionParticipanteDto[];
}

export interface TriviaParticipanteDto {
  id: string;
  nombre: string;
  descripcion: string;
  tiempoLimitePorPregunta: number;
  preguntas: PreguntaParticipanteDto[];
}

export interface RespuestaTriviaResultadoDto {
  esCorrecta: boolean;
  puntosGanados: number;
  yaRespondida: boolean;
  mensaje: string;
}

export interface RankingEntradaDto {
  posicion: number;
  equipoId: string | null;
  participanteId: string | null;
  nombre: string;
  puntajeTotal: number;
  tiempoTotalMs: number;
  respuestasCorrectas: number;
}

export interface EstadoPartidaDto {
  existe: boolean;
  estado: string | null;
  estaActiva: boolean;
}
