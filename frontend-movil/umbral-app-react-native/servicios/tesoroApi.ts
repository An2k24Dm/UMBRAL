import {
  construirUrl,
  leerCuerpoError,
  obtenerEncabezadosAutenticados,
} from "./clienteHttp";

export interface PistaLiberadaSesion {
  pistaId: string | null;
  contenido: string;
  fechaLiberacionUtc: string;
}

export interface BusquedaConPistas {
  id: string;
  nombre: string;
  descripcion: string;
  tiempoSegundos: number;
  puntajeBase: number;
  pistasLiberadas: PistaLiberadaSesion[];
  yaEnvioEvidencia: boolean;
}

export interface EvidenciaTesoroResultado {
  esValida: boolean;
  puntosGanados: number;
  etapaCompletada: boolean;
  // Presente solo cuando el backend responde 409 (etapa ya completada):
  // "equipo"     → otro integrante del equipo encontró el tesoro primero (grupal).
  // "individual" → el propio participante ya completó la etapa.
  conflicto?: "equipo" | "individual";
}

export async function obtenerBusquedaConPistas(
  sesionId: string,
  etapaId: string,
  busquedaId: string,
  token: string,
): Promise<BusquedaConPistas> {
  const respuesta = await fetch(
    construirUrl(
      `/api/sesiones/${sesionId}/misiones/dummy/etapas/${etapaId}/busqueda-tesoro/${busquedaId}`,
    ),
    { headers: obtenerEncabezadosAutenticados(token) },
  );

  if (!respuesta.ok) {
    const cuerpo = await leerCuerpoError(respuesta);
    throw new Error(cuerpo?.mensaje ?? `Error ${respuesta.status}`);
  }

  return (await respuesta.json()) as BusquedaConPistas;
}

export async function obtenerBusquedaConPistasCompleto(
  sesionId: string,
  misionId: string,
  etapaId: string,
  busquedaId: string,
  token: string,
): Promise<BusquedaConPistas> {
  const respuesta = await fetch(
    construirUrl(
      `/api/sesiones/${sesionId}/misiones/${misionId}/etapas/${etapaId}/busqueda-tesoro/${busquedaId}`,
    ),
    { headers: obtenerEncabezadosAutenticados(token) },
  );

  if (!respuesta.ok) {
    const cuerpo = await leerCuerpoError(respuesta);
    throw new Error(cuerpo?.mensaje ?? `Error ${respuesta.status}`);
  }

  return (await respuesta.json()) as BusquedaConPistas;
}

export async function enviarEvidenciaTesoro(
  sesionId: string,
  misionId: string,
  etapaId: string,
  busquedaId: string,
  codigoEscaneado: string,
  token: string,
): Promise<EvidenciaTesoroResultado> {
  const respuesta = await fetch(
    construirUrl(
      `/api/sesiones/${sesionId}/misiones/${misionId}/etapas/${etapaId}/busqueda-tesoro/${busquedaId}/evidencias`,
    ),
    {
      method: "POST",
      headers: obtenerEncabezadosAutenticados(token),
      body: JSON.stringify({ codigoEscaneado }),
    },
  );

  if (respuesta.status === 409) {
    // La etapa ya estaba completada. Distinguimos si fue el equipo (grupal) o el
    // propio participante para mostrar el mensaje correcto (no "código incorrecto").
    const cuerpo = await leerCuerpoError(respuesta);
    const conflicto: "equipo" | "individual" =
      cuerpo?.codigo === "EQUIPO_YA_COMPLETO_ETAPA" ? "equipo" : "individual";
    return { esValida: false, puntosGanados: 0, etapaCompletada: false, conflicto };
  }

  if (!respuesta.ok) {
    const cuerpo = await leerCuerpoError(respuesta);
    throw new Error(cuerpo?.mensaje ?? `Error ${respuesta.status}`);
  }

  return (await respuesta.json()) as EvidenciaTesoroResultado;
}
