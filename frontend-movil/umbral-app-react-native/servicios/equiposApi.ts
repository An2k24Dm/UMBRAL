import type {
  CrearEquipoRespuesta,
  CrearEquipoSolicitud,
} from "../tipos/equipos";
import {
  construirUrl,
  leerCuerpoError,
  obtenerEncabezadosAutenticados,
} from "./clienteHttp";

export type CodigoErrorEquipo =
  | "NO_AUTORIZADO"
  | "ACCESO_NO_PERMITIDO"
  | "SESION_NO_ENCONTRADA"
  | "VALIDACION"
  | "CONFLICTO"
  | "ERROR_INTERNO"
  | "DESCONOCIDO";

export class ErrorCrearEquipo extends Error {
  codigo: CodigoErrorEquipo;
  estadoHttp: number;
  constructor(mensaje: string, codigo: CodigoErrorEquipo, estadoHttp: number) {
    super(mensaje);
    this.codigo = codigo;
    this.estadoHttp = estadoHttp;
  }
}

function mapearError(
  estadoHttp: number,
  mensajeBackend: string | undefined,
): ErrorCrearEquipo {
  if (estadoHttp === 401) {
    return new ErrorCrearEquipo(
      "Tu sesión expiró. Inicia sesión nuevamente.",
      "NO_AUTORIZADO",
      401,
    );
  }
  if (estadoHttp === 403) {
    return new ErrorCrearEquipo(
      mensajeBackend ?? "Solo un participante puede crear equipos.",
      "ACCESO_NO_PERMITIDO",
      403,
    );
  }
  if (estadoHttp === 404) {
    return new ErrorCrearEquipo(
      mensajeBackend ?? "La sesión no existe.",
      "SESION_NO_ENCONTRADA",
      404,
    );
  }
  if (estadoHttp === 400) {
    return new ErrorCrearEquipo(
      mensajeBackend ?? "Revisa los datos del equipo.",
      "VALIDACION",
      400,
    );
  }
  if (estadoHttp === 409) {
    return new ErrorCrearEquipo(
      mensajeBackend ?? "No se pudo crear el equipo en esta sesión.",
      "CONFLICTO",
      409,
    );
  }
  if (estadoHttp >= 500) {
    return new ErrorCrearEquipo(
      mensajeBackend ?? "Ocurrió un error en el servidor. Intenta nuevamente.",
      "ERROR_INTERNO",
      estadoHttp,
    );
  }
  return new ErrorCrearEquipo(
    mensajeBackend ?? "No fue posible crear el equipo.",
    "DESCONOCIDO",
    estadoHttp,
  );
}

export async function crearEquipoApi(
  tokenAcceso: string,
  sesionId: string,
  solicitud: CrearEquipoSolicitud,
): Promise<CrearEquipoRespuesta> {
  const cuerpo = {
    nombre: solicitud.nombre,
    tipo: solicitud.tipo,
    // Solo se envía contraseña en equipos privados.
    contrasena: solicitud.tipo === "Privado" ? solicitud.contrasena : null,
  };

  const respuesta = await fetch(
    construirUrl(`/api/sesiones/${sesionId}/equipos`),
    {
      method: "POST",
      headers: obtenerEncabezadosAutenticados(tokenAcceso),
      body: JSON.stringify(cuerpo),
    },
  );

  if (!respuesta.ok) {
    const error = await leerCuerpoError(respuesta);
    throw mapearError(respuesta.status, error?.mensaje);
  }

  return (await respuesta.json()) as CrearEquipoRespuesta;
}
