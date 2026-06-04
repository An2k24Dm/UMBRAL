import type {
  CodigoErrorInicioSesion,
} from "../tipos/errores";
import { ErrorInicioSesion } from "../tipos/errores";
import type { ResultadoInicioSesion } from "../tipos/autenticacion";
import {
  construirUrl,
  leerCuerpoError,
  obtenerEncabezadosBase,
} from "./clienteHttp";

export async function iniciarSesionApi(
  nombreUsuario: string,
  contrasena: string,
): Promise<ResultadoInicioSesion> {
  const respuesta = await fetch(construirUrl("/api/autenticacion/login-movil"), {
    method: "POST",
    headers: obtenerEncabezadosBase(),
    body: JSON.stringify({ nombreUsuario, contrasena }),
  });

  if (!respuesta.ok) {
    const cuerpo = await leerCuerpoError(respuesta);
    const codigo =
      (cuerpo?.codigo as CodigoErrorInicioSesion | undefined) ?? "DESCONOCIDO";
    throw new ErrorInicioSesion(
      cuerpo?.mensaje ?? "No fue posible iniciar sesión.",
      codigo,
      respuesta.status,
    );
  }

  return (await respuesta.json()) as ResultadoInicioSesion;
}
