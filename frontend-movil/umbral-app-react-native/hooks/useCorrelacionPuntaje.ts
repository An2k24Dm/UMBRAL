import { useCallback, useEffect, useRef, useState } from "react";
import {
  normalizarEventoId,
  obtenerEventoIdOrigen,
  obtenerPuntajeGanado,
  type PuntajeCalculadoEvento,
} from "../servicios/rankingTiempoReal";

const GUID_VACIO = normalizarEventoId("00000000-0000-0000-0000-000000000000");

export const TEXTO_PUNTAJE_CALCULANDO = "Puntaje calculándose...";
export const TEXTO_PUNTAJE_FALLBACK = "El puntaje se actualizará en el ranking.";

const TTL_BUFFER_MS = 15000;

interface OpcionesCorrelacionPuntaje {
  esperaMaximaMs?: number;
  recuperarPuntaje?: (eventoId: string) => Promise<number | null>;
}

export function useCorrelacionPuntaje(
  { esperaMaximaMs = 6000, recuperarPuntaje }: OpcionesCorrelacionPuntaje = {},
) {
  const [feedbackPuntaje, setFeedbackPuntaje] = useState<string>(
    TEXTO_PUNTAJE_CALCULANDO,
  );
  const resueltoRef = useRef(false);
  const esperadoRef = useRef<string | null>(null);
  const timeoutRef = useRef<ReturnType<typeof setTimeout> | null>(null);
  const [versionResolucion, setVersionResolucion] = useState(0);
  const bufferRef = useRef<Map<string, { puntaje: number; ts: number }>>(
    new Map(),
  );

  const logDev = useCallback((mensaje: string, datos?: Record<string, unknown>) => {
    if (typeof __DEV__ !== "undefined" && __DEV__) {
      console.info("[Trivia/Puntaje]", mensaje, datos ?? {});
    }
  }, []);

  const marcarResuelto = useCallback((texto: string) => {
    resueltoRef.current = true;
    setFeedbackPuntaje(texto);
    setVersionResolucion((v) => v + 1);
  }, []);

  const limpiarTimeout = useCallback(() => {
    if (timeoutRef.current) {
      clearTimeout(timeoutRef.current);
      timeoutRef.current = null;
    }
  }, []);

  const podarBuffer = useCallback(() => {
    const ahora = Date.now();
    for (const [clave, valor] of bufferRef.current) {
      if (ahora - valor.ts > TTL_BUFFER_MS) bufferRef.current.delete(clave);
    }
  }, []);

  const alRecibirPuntajeCalculado = useCallback(
    (evento: PuntajeCalculadoEvento) => {
      const origen = obtenerEventoIdOrigen(evento);
      if (!origen || origen === GUID_VACIO) return;
      const puntaje = obtenerPuntajeGanado(evento);
      logDev("PuntajeCalculado recibido", { eventoId: origen, puntaje });

      if (esperadoRef.current && origen === esperadoRef.current) {
        esperadoRef.current = null;
        limpiarTimeout();
        marcarResuelto(`+${puntaje} pts`);
        logDev("Puntaje correlacionado por SignalR", { eventoId: origen, puntaje });
        return;
      }

      podarBuffer();
      bufferRef.current.set(origen, { puntaje, ts: Date.now() });
      logDev("Puntaje guardado en buffer", { eventoId: origen, puntaje });
    },
    [limpiarTimeout, logDev, marcarResuelto, podarBuffer],
  );

  const recuperarPendiente = useCallback(async () => {
    const eventoId = esperadoRef.current;
    if (!eventoId || !recuperarPuntaje) return false;

    try {
      logDev("Recuperando puntaje por HTTP", { eventoId });
      const puntaje = await recuperarPuntaje(eventoId);
      if (puntaje === null || esperadoRef.current !== eventoId) return false;
      esperadoRef.current = null;
      limpiarTimeout();
      marcarResuelto(`+${puntaje} pts`);
      logDev("Puntaje recuperado por HTTP", { eventoId, puntaje });
      return true;
    } catch (error) {
      logDev("Recuperación de puntaje falló", {
        eventoId,
        error: error instanceof Error ? error.message : String(error),
      });
      return false;
    }
  }, [limpiarTimeout, logDev, marcarResuelto, recuperarPuntaje]);

  const esperarPuntaje = useCallback(
    (eventoId?: string | null) => {
      limpiarTimeout();
      resueltoRef.current = false;

      const eventoNormalizado = normalizarEventoId(eventoId);

      if (!eventoNormalizado || eventoNormalizado === GUID_VACIO) {
        esperadoRef.current = null;
        marcarResuelto(TEXTO_PUNTAJE_FALLBACK);
        return;
      }

      const enBuffer = bufferRef.current.get(eventoNormalizado);
      if (enBuffer) {
        bufferRef.current.delete(eventoNormalizado);
        esperadoRef.current = null;
        marcarResuelto(`+${enBuffer.puntaje} pts`);
        logDev("Puntaje tomado desde buffer", {
          eventoId: eventoNormalizado,
          puntaje: enBuffer.puntaje,
        });
        return;
      }

      esperadoRef.current = eventoNormalizado;
      setFeedbackPuntaje(TEXTO_PUNTAJE_CALCULANDO);
      logDev("Esperando puntaje", { eventoId: eventoNormalizado });
      timeoutRef.current = setTimeout(() => {
        void (async () => {
          if (esperadoRef.current !== eventoNormalizado) return;
          const recuperado = await recuperarPendiente();
          if (recuperado || esperadoRef.current !== eventoNormalizado) return;
          esperadoRef.current = null;
          marcarResuelto(TEXTO_PUNTAJE_FALLBACK);
          logDev("Timeout esperando puntaje", { eventoId: eventoNormalizado });
        })();
      }, esperaMaximaMs);
    },
    [esperaMaximaMs, limpiarTimeout, logDev, marcarResuelto, recuperarPendiente],
  );

  useEffect(() => () => limpiarTimeout(), [limpiarTimeout]);

  return {
    feedbackPuntaje,
    versionResolucion,
    resueltoRef,
    alRecibirPuntajeCalculado,
    esperarPuntaje,
    recuperarPendiente,
  };
}
