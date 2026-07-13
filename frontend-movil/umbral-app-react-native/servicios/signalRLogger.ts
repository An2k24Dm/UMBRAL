import * as signalR from "@microsoft/signalr";

function esInicioCanceladoDuranteNegociacion(mensaje: string): boolean {
  return /connection was stopped during negotiation|stopped during negotiation/i.test(
    mensaje,
  );
}

export function crearLoggerSignalRMovil(etiqueta: string): signalR.ILogger {
  return {
    log(nivel, mensaje) {
      if (esInicioCanceladoDuranteNegociacion(mensaje)) return;

      if (nivel >= signalR.LogLevel.Error) {
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
