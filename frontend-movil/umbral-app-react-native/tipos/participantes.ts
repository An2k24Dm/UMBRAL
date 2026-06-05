export interface DatosContactoPerfil {
  direccion?: string | null;
  telefono?: string | null;
}

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

export interface RespuestaCrearUsuario {
  id: string;
  nombreUsuario: string;
  correo: string;
  rol: "Administrador" | "Operador" | "Participante";
  estado: string;
  codigo: string | null;
  mensaje: string;
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

export interface ModificarPerfilParticipantePayload {
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

export interface RespuestaEliminarCuentaParticipante {
  eliminada: boolean;
  mensaje: string;
}
