const URL_API: string =
  process.env.EXPO_PUBLIC_API_URL ?? "http://localhost:5000";

export interface UsuarioAutenticado {
  id: string;
  nombreUsuario: string;
  rol: "Administrador" | "Operador" | "Participante";
  nombre: string;
  apellido: string;
}

export interface ResultadoInicioSesion {
  tokenAcceso: string;
  tokenRefresco: string;
  expiraEn: number;
  tipoToken: string;
  usuario: UsuarioAutenticado;
  rutaRedireccion: string;
}

// HU03 — datos que el formulario móvil envía para registrar un Participante.
// La forma del JSON debe coincidir con RegistrarParticipanteDto del backend.
export interface DatosRegistroParticipante {
  alias: string;
  nombreUsuario: string;
  correo: string;
  contrasena: string;
  nombre: string;
  apellido: string;
  sexo: "Masculino" | "Femenino" | "Indefinido" | "Otro";
  fechaNacimiento: string;
  direccion: string;
  telefono: string;
}

// Respuesta tipada del backend al crear un usuario (HU02 y HU03 comparten
// CrearUsuarioRespuestaDto). El Participante recibe siempre codigo = null.
export interface RespuestaCrearUsuario {
  id: string;
  nombreUsuario: string;
  correo: string;
  rol: "Administrador" | "Operador" | "Participante";
  estado: string;
  codigo: string | null;
  mensaje: string;
}

// Error por campo que devuelve el backend cuando la validación falla.
export interface ErrorCampo {
  campo: string;
  mensaje: string;
}

// HU04 — el backend devuelve { codigo, mensaje } en errores del login.
// Se transporta el código para que la pantalla muestre mensajes específicos
// (credenciales incorrectas, acceso no permitido en móvil, cuenta desactivada).
export type CodigoErrorInicioSesion =
  | "DATOS_INVALIDOS"
  | "ACCESO_NO_PERMITIDO"
  | "CUENTA_DESACTIVADA"
  | "ROL_NO_VALIDO"
  | "ERROR_INTERNO"
  | "DESCONOCIDO";

export class ErrorInicioSesion extends Error {
  codigo: CodigoErrorInicioSesion;
  estadoHttp: number;
  constructor(
    mensaje: string,
    codigo: CodigoErrorInicioSesion,
    estadoHttp: number,
  ) {
    super(mensaje);
    this.codigo = codigo;
    this.estadoHttp = estadoHttp;
  }
}

// Excepción que transporta los errores por campo del backend para que la
// pantalla pueda renderizarlos junto a cada input.
export class ErrorValidacionRegistro extends Error {
  errores: ErrorCampo[];
  constructor(mensaje: string, errores: ErrorCampo[]) {
    super(mensaje);
    this.errores = errores;
  }
}

export async function iniciarSesionApi(
  nombreUsuario: string,
  contrasena: string,
): Promise<ResultadoInicioSesion> {
  const respuesta = await fetch(`${URL_API}/api/autenticacion/login-movil`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ nombreUsuario, contrasena }),
  });

  if (!respuesta.ok) {
    const cuerpo = (await respuesta.json().catch(() => null)) as {
      codigo?: string;
      mensaje?: string;
    } | null;
    const codigo = (cuerpo?.codigo as CodigoErrorInicioSesion | undefined) ??
      "DESCONOCIDO";
    throw new ErrorInicioSesion(
      cuerpo?.mensaje ?? "No fue posible iniciar sesión.",
      codigo,
      respuesta.status,
    );
  }

  return (await respuesta.json()) as ResultadoInicioSesion;
}

// HU04 — Perfil del Participante autenticado.
// El backend expone GET /api/autenticacion/perfil-actual, que toma el id del
// usuario desde el token. La app móvil nunca debe pedir perfiles por id ni
// permitir consultar perfiles de otros Participantes.
export interface DatosContactoPerfil {
  direccion?: string | null;
  telefono?: string | null;
}

export interface PerfilParticipante {
  id: string;
  nombreUsuario: string;
  correo: string;
  rol: "Participante";
  estado: string;
  nombre: string;
  apellido: string;
  datosContacto: DatosContactoPerfil;
  sexo: string;
  fechaNacimiento: string;
  fechaRegistro: string;
  alias: string;
}

export type CodigoErrorConsultaPerfil =
  | "NO_AUTORIZADO"
  | "ACCESO_NO_PERMITIDO"
  | "ERROR_INTERNO"
  | "DESCONOCIDO";

export class ErrorConsultaPerfil extends Error {
  codigo: CodigoErrorConsultaPerfil;
  estadoHttp: number;
  constructor(
    mensaje: string,
    codigo: CodigoErrorConsultaPerfil,
    estadoHttp: number,
  ) {
    super(mensaje);
    this.codigo = codigo;
    this.estadoHttp = estadoHttp;
  }
}

export async function obtenerPerfilActualApi(
  tokenAcceso: string,
): Promise<PerfilParticipante> {
  const respuesta = await fetch(
    `${URL_API}/api/autenticacion/perfil-actual`,
    {
      method: "GET",
      headers: obtenerEncabezadosAutenticados(tokenAcceso),
    },
  );

  if (!respuesta.ok) {
    const cuerpo = (await respuesta.json().catch(() => null)) as {
      codigo?: string;
      mensaje?: string;
    } | null;

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

    const codigo = (cuerpo?.codigo as CodigoErrorConsultaPerfil | undefined) ??
      "DESCONOCIDO";
    throw new ErrorConsultaPerfil(
      cuerpo?.mensaje ?? "No fue posible consultar tu perfil.",
      codigo,
      respuesta.status,
    );
  }

  return (await respuesta.json()) as PerfilParticipante;
}

// HU04 — helper para futuras peticiones autenticadas desde la app móvil.
// También lo usa obtenerPerfilActualApi para no duplicar la construcción
// del encabezado Authorization.
export function obtenerEncabezadosAutenticados(
  token: string,
): Record<string, string> {
  return {
    "Content-Type": "application/json",
    Authorization: `Bearer ${token}`,
  };
}

// HU03 — registro público de Participante desde la app móvil. No envía token:
// el backend asigna RolUsuario.Participante internamente y nunca permite
// crear Operador/Administrador por esta vía.
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
    `${URL_API}/api/usuarios/participantes/registro`,
    {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(cuerpo),
    },
  );

  if (!respuesta.ok) {
    const cuerpoError = (await respuesta.json().catch(() => null)) as {
      mensaje?: string;
      errores?: ErrorCampo[];
    } | null;
    if (cuerpoError?.errores && cuerpoError.errores.length > 0) {
      throw new ErrorValidacionRegistro(
        cuerpoError.mensaje ??
          "No fue posible registrar la cuenta. Revise los campos marcados.",
        cuerpoError.errores,
      );
    }
    throw new Error(
      cuerpoError?.mensaje ?? "No fue posible registrar la cuenta.",
    );
  }

  return (await respuesta.json()) as RespuestaCrearUsuario;
}

// HU10 — edición del propio perfil del Participante desde la app móvil.
//
// El backend identifica al Participante autenticado por el sub del token;
// la app NUNCA envía un id de Participante. La contraseña, si se envía,
// viaja sólo a Keycloak — no se guarda ni se devuelve en la respuesta.
export interface ModificarPerfilParticipantePayload {
  // HU10 — alias del Participante. Solo se incluye si cambió.
  alias?: string;
  nombreUsuario?: string;
  correo?: string;
  nombre?: string;
  apellido?: string;
  sexo?: string;
  fechaNacimiento?: string;
  datosContacto?: {
    direccion?: string;
    telefono?: string;
  };
  nuevaContrasena?: string;
  confirmacionContrasena?: string;
}

export interface RespuestaModificarParticipante {
  huboCambios: boolean;
  camposActualizados: string[];
  mensaje: string;
  participante: PerfilParticipante;
}

export async function modificarPerfilParticipanteApi(
  tokenAcceso: string,
  cambios: ModificarPerfilParticipantePayload,
): Promise<RespuestaModificarParticipante> {
  const respuesta = await fetch(
    `${URL_API}/api/usuarios/participantes/perfil`,
    {
      method: "PATCH",
      headers: obtenerEncabezadosAutenticados(tokenAcceso),
      body: JSON.stringify(cambios),
    },
  );

  if (!respuesta.ok) {
    const cuerpo = (await respuesta.json().catch(() => null)) as {
      mensaje?: string;
      errores?: ErrorCampo[];
    } | null;
    if (cuerpo?.errores && cuerpo.errores.length > 0) {
      throw new ErrorValidacionRegistro(
        cuerpo.mensaje ??
          "No fue posible guardar los cambios. Revise los campos marcados.",
        cuerpo.errores,
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
