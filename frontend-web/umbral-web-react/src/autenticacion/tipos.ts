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
