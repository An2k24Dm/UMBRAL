// Servicio HTTP de los flujos del Participante (registro y perfil).
//
// El backend identifica al Participante autenticado por el sub del JWT;
// estas funciones NUNCA reciben ni envían un id de Participante.

import type {
  CodigoErrorConsultaPerfil,
  CodigoErrorEliminarCuenta,
  ErrorCampo,
} from "../tipos/errores";
import {
  ErrorConsultaPerfil,
  ErrorEliminarCuenta,
  ErrorValidacionRegistro,
} from "../tipos/errores";
import type {
  DatosRegistroParticipante,
  ModificarPerfilParticipantePayload,
  PerfilParticipante,
  RespuestaCrearUsuario,
  RespuestaEliminarCuentaParticipante,
  RespuestaModificarParticipante,
} from "../tipos/participantes";
import {
  construirUrl,
  leerCuerpoError,
  obtenerEncabezadosAutenticados,
  obtenerEncabezadosBase,
} from "./clienteHttp";

// HU03 — registro público de Participante. No envía token: el backend
// asigna Rol.Participante internamente y nunca permite crear
// Operador/Administrador por esta vía.
export async function registrarParticipanteApi(
  datos: DatosRegistroParticipante,
): Promise<RespuestaCrearUsuario> {
  const cuerpo = {
    alias: datos.alias,
    nombreUsuario: datos.nombreUsuario,
    correo: datos.correo,
    contrasena: datos.contrasena,
    nombre: datos.nombre,
    apellido: datos.apellido,
    sexo: datos.sexo,
    fechaNacimiento: datos.fechaNacimiento,
    datosContacto: {
      direccion: datos.direccion,
      telefono: datos.telefono,
    },
  };

  const respuesta = await fetch(
    construirUrl("/api/usuarios/participantes/registro"),
    {
      method: "POST",
      headers: obtenerEncabezadosBase(),
      body: JSON.stringify(cuerpo),
    },
  );

  if (!respuesta.ok) {
    const cuerpoError = await leerCuerpoError(respuesta);
    if (cuerpoError?.errores && cuerpoError.errores.length > 0) {
      throw new ErrorValidacionRegistro(
        cuerpoError.mensaje ??
          "No fue posible registrar la cuenta. Revise los campos marcados.",
        cuerpoError.errores as ErrorCampo[],
      );
    }
    throw new Error(
      cuerpoError?.mensaje ?? "No fue posible registrar la cuenta.",
    );
  }

  return (await respuesta.json()) as RespuestaCrearUsuario;
}

// HU04 — Perfil del Participante autenticado.
// El backend expone GET /api/autenticacion/perfil-actual y toma el id del
// usuario desde el token. La app móvil nunca debe pedir perfiles por id ni
// permitir consultar perfiles de otros Participantes.
export async function obtenerPerfilActualApi(
  tokenAcceso: string,
): Promise<PerfilParticipante> {
  const respuesta = await fetch(
    construirUrl("/api/autenticacion/perfil-actual"),
    {
      method: "GET",
      headers: obtenerEncabezadosAutenticados(tokenAcceso),
    },
  );

  if (!respuesta.ok) {
    const cuerpo = await leerCuerpoError(respuesta);

    if (respuesta.status === 401) {
      throw new ErrorConsultaPerfil(
        "Tu sesión expiró. Inicia sesión nuevamente.",
        "NO_AUTORIZADO",
        401,
      );
    }
    if (respuesta.status === 403) {
      throw new ErrorConsultaPerfil(
        cuerpo?.mensaje ?? "No tienes permisos para consultar este perfil.",
        "ACCESO_NO_PERMITIDO",
        403,
      );
    }

    const codigo =
      (cuerpo?.codigo as CodigoErrorConsultaPerfil | undefined) ?? "DESCONOCIDO";
    throw new ErrorConsultaPerfil(
      cuerpo?.mensaje ?? "No fue posible consultar tu perfil.",
      codigo,
      respuesta.status,
    );
  }

  return (await respuesta.json()) as PerfilParticipante;
}

// HU10 — modificar el propio perfil del Participante.
export async function modificarPerfilParticipanteApi(
  tokenAcceso: string,
  cambios: ModificarPerfilParticipantePayload,
): Promise<RespuestaModificarParticipante> {
  const respuesta = await fetch(
    construirUrl("/api/usuarios/participantes/perfil"),
    {
      method: "PATCH",
      headers: obtenerEncabezadosAutenticados(tokenAcceso),
      body: JSON.stringify(cambios),
    },
  );

  if (!respuesta.ok) {
    const cuerpo = await leerCuerpoError(respuesta);
    if (cuerpo?.errores && cuerpo.errores.length > 0) {
      throw new ErrorValidacionRegistro(
        cuerpo.mensaje ??
          "No fue posible guardar los cambios. Revise los campos marcados.",
        cuerpo.errores as ErrorCampo[],
      );
    }
    if (respuesta.status === 401) {
      throw new Error("Tu sesión expiró. Inicia sesión nuevamente.");
    }
    if (respuesta.status === 403) {
      throw new Error("No tienes permisos para modificar este perfil.");
    }
    throw new Error(
      cuerpo?.mensaje ?? "No fue posible guardar los cambios del perfil.",
    );
  }

  return (await respuesta.json()) as RespuestaModificarParticipante;
}

// HU11 — eliminar la cuenta del Participante autenticado.
export async function eliminarCuentaParticipanteApi(
  tokenAcceso: string,
): Promise<RespuestaEliminarCuentaParticipante> {
  const respuesta = await fetch(
    construirUrl("/api/usuarios/participantes/perfil"),
    {
      method: "DELETE",
      headers: obtenerEncabezadosAutenticados(tokenAcceso),
    },
  );

  if (!respuesta.ok) {
    const cuerpo = await leerCuerpoError(respuesta);

    if (respuesta.status === 401) {
      throw new ErrorEliminarCuenta(
        cuerpo?.mensaje ?? "Tu sesión expiró. Inicia sesión nuevamente.",
        "NO_AUTORIZADO",
        401,
        false,
      );
    }
    if (respuesta.status === 403) {
      const codigo: CodigoErrorEliminarCuenta =
        cuerpo?.codigo === "CUENTA_DESACTIVADA"
          ? "CUENTA_DESACTIVADA"
          : "ACCESO_NO_PERMITIDO";
      throw new ErrorEliminarCuenta(
        cuerpo?.mensaje ?? "No es posible eliminar la cuenta en este momento.",
        codigo,
        403,
        false,
      );
    }
    if (respuesta.status === 404) {
      throw new ErrorEliminarCuenta(
        cuerpo?.mensaje ?? "No se encontró el participante autenticado.",
        "PARTICIPANTE_NO_ENCONTRADO",
        404,
        false,
      );
    }

    const codigo =
      (cuerpo?.codigo as CodigoErrorEliminarCuenta | undefined) ??
      "DESCONOCIDO";
    throw new ErrorEliminarCuenta(
      cuerpo?.mensaje ?? "No fue posible eliminar la cuenta.",
      codigo,
      respuesta.status,
      false,
    );
  }

  return (await respuesta.json()) as RespuestaEliminarCuentaParticipante;
}
