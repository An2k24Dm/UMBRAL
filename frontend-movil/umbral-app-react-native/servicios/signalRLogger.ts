import * as signalR from "@microsoft/signalr";

// Clasificación de mensajes de log que emite @microsoft/signalr.
//
// Objetivo (ver requerimiento de observabilidad): un timeout de keep-alive que
// se recupera solo con la reconexión automática NO debe pintarse como error
// grave (caja roja de Expo). A la vez, un fallo terminal real SÍ debe seguir
// siendo Error. La clasificación se hace por listas de mensajes nombradas y
// funciones puras y testeables, nunca ocultando todo.

export type CategoriaErrorSignalR =
  | "cancelacion" // desmontaje/cleanup intencional: no es un error
  | "transitorio" // interrupción recuperable; la reconexión lo resuelve
  | "autenticacion" // 401/token: dispara el flujo de seguridad del hook
  | "fatal"; // fallo terminal o mensaje grave no reconocido

// Cancelación intencional (la pantalla se desmontó mientras negociaba). No es un
// error del sistema; se ignora por completo.
export function esInicioCanceladoDuranteNegociacion(mensaje: string): boolean {
  return /connection was stopped during negotiation|stopped during negotiation/i.test(
    mensaje,
  );
}

// Autenticación: token inválido/expirado. NUNCA se debe confundir con un
// timeout transitorio; el hook correspondiente ejecuta la seguridad existente.
export function esErrorSignalRAutenticacion(mensaje: string): boolean {
  return /\b401\b|unauthorized|no autenticado|no_autenticado/i.test(mensaje);
}

// Interrupciones recuperables típicas del transporte. La conexión sigue viva
// para la app porque withAutomaticReconnect vuelve a levantarla.
const PATRONES_TRANSITORIOS: readonly RegExp[] = [
  // Server timeout elapsed without receiving a message from the server.
  /server timeout elapsed/i,
  // Connection disconnected with error: '... timeout ...'
  /timeout elapsed without receiving/i,
  // WebSocket closed with status code: 1001 (going away) u otros cierres de
  // transporte que la reconexión automática resuelve.
  /websocket closed with status code/i,
  /\b1001\b/,
  // Stream end encountered.
  /stream end encountered/i,
  // Network request failed (pérdida momentánea de red en el dispositivo).
  /network request failed/i,
  // Failed to complete negotiation with the server (con reconexión activa).
  /failed to complete negotiation/i,
  // La conexión se perdió mientras se reconectaba.
  /connection (?:disconnected|closed) with error/i,
];

export function esErrorSignalRTransitorio(mensaje: string): boolean {
  if (esErrorSignalRAutenticacion(mensaje)) return false;
  return PATRONES_TRANSITORIOS.some((patron) => patron.test(mensaje));
}

// Fatal: no es cancelación, ni autenticación, ni un transitorio reconocido.
// Se mantiene como Error real (no se oculta).
export function esErrorSignalRFatal(mensaje: string): boolean {
  return (
    !esInicioCanceladoDuranteNegociacion(mensaje) &&
    !esErrorSignalRAutenticacion(mensaje) &&
    !esErrorSignalRTransitorio(mensaje)
  );
}

export function clasificarErrorSignalR(mensaje: string): CategoriaErrorSignalR {
  if (esInicioCanceladoDuranteNegociacion(mensaje)) return "cancelacion";
  if (esErrorSignalRAutenticacion(mensaje)) return "autenticacion";
  if (esErrorSignalRTransitorio(mensaje)) return "transitorio";
  return "fatal";
}

export function crearLoggerSignalRMovil(etiqueta: string): signalR.ILogger {
  return {
    log(nivel, mensaje) {
      if (nivel >= signalR.LogLevel.Error) {
        const categoria = clasificarErrorSignalR(mensaje);

        // Cancelación intencional por desmontaje: ni siquiera se registra.
        if (categoria === "cancelacion") return;

        // Transitorio recuperable: como máximo un warn de diagnóstico en dev.
        // Nunca console.error → no dispara la caja roja de Expo por algo que la
        // reconexión automática va a resolver.
        if (categoria === "transitorio") {
          if (__DEV__) {
            console.warn(
              `[SignalR][${etiqueta}] Conexión interrumpida temporalmente; se ` +
                `intentará reconectar. Detalle técnico: ${mensaje}`,
            );
          }
          return;
        }

        // Autenticación o fallo terminal real: SÍ es Error (se conserva la
        // capacidad de diagnóstico). El hook decide la seguridad/UI aparte.
        console.error(`[SignalR][${etiqueta}] ${mensaje}`);
        return;
      }

      if (nivel >= signalR.LogLevel.Warning) {
        console.warn(`[SignalR][${etiqueta}] ${mensaje}`);
        return;
      }

      if (__DEV__ && nivel >= signalR.LogLevel.Information) {
        console.info(`[SignalR][${etiqueta}] ${mensaje}`);
      }
    },
  };
}
