export interface ErrorCampo {
  campo: string;
  mensaje: string;
}

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

export class ErrorValidacionRegistro extends Error {
  errores: ErrorCampo[];
  constructor(mensaje: string, errores: ErrorCampo[]) {
    super(mensaje);
    this.errores = errores;
  }
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

export type CodigoErrorEliminarCuenta =
  | "NO_AUTORIZADO"
  | "ACCESO_NO_PERMITIDO"
  | "CUENTA_DESACTIVADA"
  | "PARTICIPANTE_NO_ENCONTRADO"
  | "ERROR_INTERNO"
  | "DESCONOCIDO";

export class ErrorEliminarCuenta extends Error {
  codigo: CodigoErrorEliminarCuenta;
  estadoHttp: number;
  cuentaEliminada: boolean;
  constructor(
    mensaje: string,
    codigo: CodigoErrorEliminarCuenta,
    estadoHttp: number,
    cuentaEliminada: boolean,
  ) {
    super(mensaje);
    this.codigo = codigo;
    this.estadoHttp = estadoHttp;
    this.cuentaEliminada = cuentaEliminada;
  }
}
