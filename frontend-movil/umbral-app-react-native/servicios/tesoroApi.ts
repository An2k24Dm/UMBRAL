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
    // Ya envió evidencia — idempotente: se trata como completado sin puntos nuevos.
    return { esValida: false, puntosGanados: 0, etapaCompletada: false };
  }

  if (!respuesta.ok) {
    const cuerpo = await leerCuerpoError(respuesta);
    throw new Error(cuerpo?.mensaje ?? `Error ${respuesta.status}`);
  }

  return (await respuesta.json()) as EvidenciaTesoroResultado;
}
