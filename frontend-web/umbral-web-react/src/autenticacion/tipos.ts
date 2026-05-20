export type Rol = 'Administrador' | 'Operador' | 'Participante'

export interface UsuarioAutenticado {
  id: string
  nombreUsuario: string
  rol: Rol
  nombre: string
  apellido: string
}

export interface ResultadoInicioSesion {
  tokenAcceso: string
  tokenRefresco: string
  expiraEn: number
  tipoToken: string
  usuario: UsuarioAutenticado
  rutaRedireccion: string
}

export interface RespuestaError {
  codigo: string
  mensaje: string
}

// ---------------------------------------------------------------------------
// Tipos compartidos para HU06, HU07, HU08
// ---------------------------------------------------------------------------

export type EstadoUsuario = 'Activo' | 'Inactivo' | string

// Participante listado (HU07). El backend debería entregar al menos estos
// campos; los opcionales se renderizan como "No disponible" si vienen vacíos.
export interface UsuarioListadoParticipante {
  id: string
  alias?: string | null
  nombreUsuario: string
  nombre: string
  apellido: string
  estado: EstadoUsuario
  sexo?: string | null
}

// Operador/Administrador listado (HU08). La columna "Código" puede llegar
// como codigoOperador o codigoAdministrador según el rol.
export interface UsuarioListadoInterno {
  id: string
  codigoOperador?: string | null
  codigoAdministrador?: string | null
  nombreUsuario: string
  nombre: string
  apellido: string
  rol: Rol
  estado: EstadoUsuario
  sexo?: string | null
}

// Detalle/perfil completo de un usuario para HU06 y ver-perfil de HU07/HU08.
export interface DatosContacto {
  direccion?: string | null;
  telefono?: string | null;
}

export interface UsuarioDetalle {
  id: string;
  rol: Rol;
  estado: EstadoUsuario;
  nombreUsuario: string;
  nombre: string;
  apellido: string;
  alias?: string | null;
  codigoOperador?: string | null;
  codigoAdministrador?: string | null;
  sexo?: string | null;
  correo?: string | null;
  datosContacto?: DatosContacto | null;
  fechaNacimiento?: string | null;
  fechaRegistro?: string | null;
}

export interface ResultadoPaginado<T> {
  elementos: T[]
  pagina: number
  tamanioPagina: number
  total: number
}

export type OrdenEstado = 'asc' | 'desc' | null

export interface FiltrosParticipantes {
  pagina: number
  tamanioPagina: number
  ordenEstado?: OrdenEstado
}

export type FiltroRolInterno = 'Todos' | 'Operador' | 'Administrador'

export interface FiltrosUsuariosInternos {
  pagina: number
  tamanioPagina: number
  rol?: FiltroRolInterno
  ordenEstado?: OrdenEstado
}
