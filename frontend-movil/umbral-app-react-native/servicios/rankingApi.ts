import {
  construirUrl,
  leerCuerpoError,
  obtenerEncabezadosAutenticados,
} from "./clienteHttp";

export interface RankingParticipanteDto {
  posicion: number;
  participanteSesionId: string;
  participanteIdentidadId: string;
  equipoId: string | null;
  alias: string;
  puntaje: number;
}

export interface AporteParticipanteEquipoDto {
  posicion: number;
  participanteSesionId: string;
  participanteIdentidadId: string;
  alias: string;
  puntaje: number;
}

export interface RankingEquipoDto {
  posicion: number;
  equipoId: string;
  nombreEquipo: string;
  puntaje: number;
  participantes: AporteParticipanteEquipoDto[];
}

export interface RankingGlobalDto {
  posicion: number;
  participanteIdentidadId: string;
  alias: string;
  puntaje: number;
}

// Desglose del puntaje del participante autenticado por misión y etapa. La
// fuente es sesiones-servicio (PuntosGanados, fijado con el valor real que
// calcula ranking); los nombres/orden vienen enriquecidos desde juegos.
export interface DesgloseEtapaDto {
  etapaId: string;
  orden: number;
  nombre: string;
  tipo: string;
  puntaje: number;
}

export interface DesgloseMisionDto {
  misionId: string;
  orden: number;
  nombre: string;
  puntajeTotal: number;
  etapas: DesgloseEtapaDto[];
}

export interface MiDesgloseSesionDto {
  participanteIdentidadId: string;
  puntajeTotal: number;
  misiones: DesgloseMisionDto[];
}

async function lanzarErrorRanking(respuesta: Response): Promise<never> {
  const cuerpo = await leerCuerpoError(respuesta);
  const mensaje =
    cuerpo?.mensaje ?? `Error ${respuesta.status} al consultar el ranking.`;
  const err = new Error(mensaje) as Error & { estadoHttp: number };
  err.estadoHttp = respuesta.status;
  throw err;
}

export async function obtenerRankingParticipantesSesionApi(
  tokenAcceso: string,
  sesionId: string,
): Promise<RankingParticipanteDto[]> {
  const respuesta = await fetch(
    construirUrl(`/api/ranking/sesiones/${sesionId}/participantes`),
    { method: "GET", headers: obtenerEncabezadosAutenticados(tokenAcceso) },
  );
  if (!respuesta.ok) await lanzarErrorRanking(respuesta);
  return (await respuesta.json()) as RankingParticipanteDto[];
}

export async function obtenerRankingEquiposSesionApi(
  tokenAcceso: string,
  sesionId: string,
): Promise<RankingEquipoDto[]> {
  const respuesta = await fetch(
    construirUrl(`/api/ranking/sesiones/${sesionId}/equipos`),
    { method: "GET", headers: obtenerEncabezadosAutenticados(tokenAcceso) },
  );
  if (!respuesta.ok) await lanzarErrorRanking(respuesta);
  return (await respuesta.json()) as RankingEquipoDto[];
}

export async function obtenerRankingGlobalApi(
  tokenAcceso: string,
  top = 50,
): Promise<RankingGlobalDto[]> {
  const respuesta = await fetch(
    construirUrl(`/api/ranking/global?top=${encodeURIComponent(String(top))}`),
    { method: "GET", headers: obtenerEncabezadosAutenticados(tokenAcceso) },
  );
  if (!respuesta.ok) await lanzarErrorRanking(respuesta);
  return (await respuesta.json()) as RankingGlobalDto[];
}

// El desglose por etapa lo sirve sesiones-servicio (dueño del contexto por
// misión/etapa); ranking sigue siendo la fuente del puntaje total y del ranking.
export async function obtenerMiDesgloseSesionApi(
  tokenAcceso: string,
  sesionId: string,
): Promise<MiDesgloseSesionDto> {
  const respuesta = await fetch(
    construirUrl(`/api/sesiones/participante/disponibles/${sesionId}/mi-desglose`),
    { method: "GET", headers: obtenerEncabezadosAutenticados(tokenAcceso) },
  );
  if (!respuesta.ok) await lanzarErrorRanking(respuesta);
  return (await respuesta.json()) as MiDesgloseSesionDto;
}
