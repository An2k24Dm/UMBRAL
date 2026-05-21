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
    const cuerpo = await respuesta.json().catch(() => null);
    throw new Error(cuerpo?.mensaje ?? "No fue posible iniciar sesión.");
  }

  return (await respuesta.json()) as ResultadoInicioSesion;
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
