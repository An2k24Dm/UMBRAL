import type {
  FiltroModoSesion,
  IngresarSesionRespuestaDto,
  MiParticipacionDto,
  ProgresoSesionParticipanteDto,
  ProgresoSecuencialSesionDto,
  SesionDetalleMovilDto,
  SesionDisponibleMovilDto,
} from "../tipos/sesiones";
import {
  construirUrl,
  leerCuerpoError,
  obtenerEncabezadosAutenticados,
} from "./clienteHttp";

export type CodigoErrorConsultaSesiones =
  | "NO_AUTORIZADO"
  | "ACCESO_NO_PERMITIDO"
  | "SESION_NO_DISPONIBLE"
  | "ERROR_INTERNO"
  | "DESCONOCIDO";

export class ErrorConsultaSesiones extends Error {
  codigo: CodigoErrorConsultaSesiones;
  estadoHttp: number;
  constructor(
    mensaje: string,
    codigo: CodigoErrorConsultaSesiones,
    estadoHttp: number,
  ) {
    super(mensaje);
    this.codigo = codigo;
    this.estadoHttp = estadoHttp;
  }
}

interface ParametrosListado {
  busqueda?: string | null;
  modo?: FiltroModoSesion | null;
}

function construirRutaListado(parametros: ParametrosListado): string {
  const partes: string[] = [];
  const busqueda = parametros.busqueda?.trim();
  if (busqueda && busqueda.length > 0) {
    partes.push(`busqueda=${encodeURIComponent(busqueda)}`);
  }
  if (parametros.modo && parametros.modo !== "Todas") {
    partes.push(`modo=${encodeURIComponent(parametros.modo)}`);
  }
  const consulta = partes.length > 0 ? `?${partes.join("&")}` : "";
  return `/api/sesiones/participante/disponibles${consulta}`;
}

function mapearError(
  estadoHttp: number,
  mensajeBackend: string | undefined,
  cuerpoCrudo: unknown,
): ErrorConsultaSesiones {
  if (typeof __DEV__ !== "undefined" && __DEV__) {
    console.warn(
      `[sesionesApi] Respuesta de error ${estadoHttp}:`,
      cuerpoCrudo,
    );
  }

  if (estadoHttp === 401) {
    return new ErrorConsultaSesiones(
      "Tu sesión expiró. Inicia sesión nuevamente.",
      "NO_AUTORIZADO",
      401,
    );
  }
  if (estadoHttp === 403) {
    return new ErrorConsultaSesiones(
      mensajeBackend ?? "No tienes permisos para consultar sesiones.",
      "ACCESO_NO_PERMITIDO",
      403,
    );
  }
  if (estadoHttp === 404) {
    return new ErrorConsultaSesiones(
      mensajeBackend ?? "La sesión no está disponible para consulta.",
      "SESION_NO_DISPONIBLE",
      404,
    );
  }
  if (estadoHttp >= 500) {
    return new ErrorConsultaSesiones(
      mensajeBackend ?? "Ocurrió un error en el servidor. Intente nuevamente.",
      "ERROR_INTERNO",
      estadoHttp,
    );
  }
  return new ErrorConsultaSesiones(
    mensajeBackend ?? "No fue posible consultar las sesiones.",
    "DESCONOCIDO",
    estadoHttp,
  );
}

export async function listarSesionesDisponiblesApi(
  tokenAcceso: string,
  parametros: ParametrosListado,
): Promise<SesionDisponibleMovilDto[]> {
  const respuesta = await fetch(construirUrl(construirRutaListado(parametros)), {
    method: "GET",
    headers: obtenerEncabezadosAutenticados(tokenAcceso),
  });

  if (!respuesta.ok) {
    const cuerpo = await leerCuerpoError(respuesta);
    throw mapearError(respuesta.status, cuerpo?.mensaje, cuerpo);
  }

  return (await respuesta.json()) as SesionDisponibleMovilDto[];
}

export async function obtenerDetalleSesionDisponibleApi(
  tokenAcceso: string,
  sesionId: string,
): Promise<SesionDetalleMovilDto> {
  const respuesta = await fetch(
    construirUrl(`/api/sesiones/participante/disponibles/${sesionId}`),
    {
      method: "GET",
      headers: obtenerEncabezadosAutenticados(tokenAcceso),
    },
  );

  if (!respuesta.ok) {
    const cuerpo = await leerCuerpoError(respuesta);
    throw mapearError(respuesta.status, cuerpo?.mensaje, cuerpo);
  }

  return (await respuesta.json()) as SesionDetalleMovilDto;
}

async function ejecutarIngreso(
  tokenAcceso: string,
  ruta: string,
  cuerpo?: object,
): Promise<IngresarSesionRespuestaDto> {
  const respuesta = await fetch(construirUrl(ruta), {
    method: "POST",
    headers: obtenerEncabezadosAutenticados(tokenAcceso),
    body: cuerpo ? JSON.stringify(cuerpo) : undefined,
  });

  if (!respuesta.ok) {
    const error = await leerCuerpoError(respuesta);
    throw mapearError(respuesta.status, error?.mensaje, error);
  }

  return (await respuesta.json()) as IngresarSesionRespuestaDto;
}

export function ingresarSesionPorCodigoApi(
  tokenAcceso: string,
  codigoSesion: string,
): Promise<IngresarSesionRespuestaDto> {
  return ejecutarIngreso(tokenAcceso, "/api/sesiones/participante/ingresar", {
    codigoSesion: codigoSesion.trim().toUpperCase(),
  });
}

export function ingresarSesionIndividualApi(
  tokenAcceso: string,
  sesionId: string,
): Promise<IngresarSesionRespuestaDto> {
  return ejecutarIngreso(
    tokenAcceso,
    `/api/sesiones/${sesionId}/participante/ingresar-individual`,
  );
}

export type CodigoErrorAbandonarSesion =
  | "NO_AUTORIZADO"
  | "ACCESO_NO_PERMITIDO"
  | "NO_ENCONTRADO"
  | "CONFLICTO"
  | "VALIDACION"
  | "ERROR_INTERNO"
  | "DESCONOCIDO";

export class ErrorAbandonarSesion extends Error {
  codigo: CodigoErrorAbandonarSesion;
  estadoHttp: number;
  constructor(
    mensaje: string,
    codigo: CodigoErrorAbandonarSesion,
    estadoHttp: number,
  ) {
    super(mensaje);
    this.codigo = codigo;
    this.estadoHttp = estadoHttp;
  }
}

function mapearErrorAbandonar(
  estadoHttp: number,
  mensajeBackend: string | undefined,
): ErrorAbandonarSesion {
  if (estadoHttp === 401) {
    return new ErrorAbandonarSesion(
      "Tu sesión expiró. Inicia sesión nuevamente.",
      "NO_AUTORIZADO",
      401,
    );
  }
  if (estadoHttp === 403) {
    return new ErrorAbandonarSesion(
      mensajeBackend ?? "Solo un Participante puede abandonar una sesión o un equipo.",
      "ACCESO_NO_PERMITIDO",
      403,
    );
  }
  if (estadoHttp === 404) {
    return new ErrorAbandonarSesion(
      mensajeBackend ?? "La sesión o tu participación ya no existe.",
      "NO_ENCONTRADO",
      404,
    );
  }
  if (estadoHttp === 409) {
    return new ErrorAbandonarSesion(
      mensajeBackend ??
        "Solo puedes abandonar cuando la sesión está en estado En Preparación.",
      "CONFLICTO",
      409,
    );
  }
  if (estadoHttp === 400) {
    return new ErrorAbandonarSesion(
      mensajeBackend ?? "Los datos enviados no son válidos.",
      "VALIDACION",
      400,
    );
  }
  if (estadoHttp >= 500) {
    return new ErrorAbandonarSesion(
      mensajeBackend ?? "Ocurrió un error en el servidor. Intenta nuevamente.",
      "ERROR_INTERNO",
      estadoHttp,
    );
  }
  return new ErrorAbandonarSesion(
    mensajeBackend ?? "No fue posible abandonar la sesión.",
    "DESCONOCIDO",
    estadoHttp,
  );
}

// HU48 — Abandonar la sesión individual o el equipo de la sesión grupal.
// El backend decide según el tipo de sesión; 204 No Content en éxito.
export async function abandonarSesionApi(
  tokenAcceso: string,
  sesionId: string,
): Promise<void> {
  const respuesta = await fetch(
    construirUrl(`/api/sesiones/${sesionId}/abandonar`),
    { method: "DELETE", headers: obtenerEncabezadosAutenticados(tokenAcceso) },
  );

  if (!respuesta.ok) {
    const error = await leerCuerpoError(respuesta);
    throw mapearErrorAbandonar(respuesta.status, error?.mensaje);
  }
}

export async function obtenerProgresoSesionParticipanteApi(
  tokenAcceso: string,
  sesionId: string,
): Promise<ProgresoSesionParticipanteDto[]> {
  const respuesta = await fetch(
    construirUrl(`/api/sesiones/participante/disponibles/${sesionId}/progreso`),
    { method: "GET", headers: obtenerEncabezadosAutenticados(tokenAcceso) },
  );
  if (!respuesta.ok) {
    const cuerpo = await leerCuerpoError(respuesta);
    throw mapearError(respuesta.status, cuerpo?.mensaje, cuerpo);
  }
  return (await respuesta.json()) as ProgresoSesionParticipanteDto[];
}

export async function obtenerProgresoSecuencialSesionApi(
  tokenAcceso: string,
  sesionId: string,
): Promise<ProgresoSecuencialSesionDto> {
  const respuesta = await fetch(
    construirUrl(
      `/api/sesiones/participante/disponibles/${sesionId}/progreso-secuencial`,
    ),
    { method: "GET", headers: obtenerEncabezadosAutenticados(tokenAcceso) },
  );
  if (!respuesta.ok) {
    const cuerpo = await leerCuerpoError(respuesta);
    throw mapearError(respuesta.status, cuerpo?.mensaje, cuerpo);
  }
  return (await respuesta.json()) as ProgresoSecuencialSesionDto;
}

export async function obtenerMisParticipacionesApi(
  tokenAcceso: string,
  limite = 20,
): Promise<MiParticipacionDto[]> {
  const respuesta = await fetch(
    construirUrl(`/api/sesiones/participante/disponibles/finalizadas?limite=${limite}`),
    { method: "GET", headers: obtenerEncabezadosAutenticados(tokenAcceso) },
  );

  if (!respuesta.ok) {
    const cuerpo = await leerCuerpoError(respuesta);
    throw mapearError(respuesta.status, cuerpo?.mensaje, cuerpo);
  }

  return (await respuesta.json()) as MiParticipacionDto[];
}
