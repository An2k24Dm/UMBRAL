import { useCallback, useEffect, useRef, useState } from "react";
import {
  obtenerEventoIdOrigen,
  obtenerPuntajeGanado,
  type PuntajeCalculadoEvento,
} from "../servicios/rankingTiempoReal";

const GUID_VACIO = "00000000-0000-0000-0000-000000000000";

export const TEXTO_PUNTAJE_CALCULANDO = "Puntaje calculándose...";
export const TEXTO_PUNTAJE_FALLBACK = "El puntaje se actualizará en el ranking.";

// El puntaje real se calcula en ranking-servicio y llega por SignalR
// (PuntajeCalculado). Como el evento puede llegar ANTES de que resuelva el HTTP
// de la respuesta, se guarda temporalmente por eventoIdOrigen y se consume al
// conocer el resultado.eventoId. Es memoria local de corta duración (no global).
const TTL_BUFFER_MS = 15000;

interface OpcionesCorrelacionPuntaje {
  // Máximo que se espera el evento de puntaje antes de mostrar el texto de
  // respaldo. No bloquea la ejecución de la sesión: solo afecta el +X visual.
  esperaMaximaMs?: number;
}

// Correlaciona el resultado HTTP de una acción (resultado.eventoId) con el
// evento PuntajeCalculado (eventoIdOrigen) para mostrar el puntaje REAL ganado
// (+X pts), resolviendo la carrera "SignalR llega antes que el HTTP" mediante un
// buffer temporal indexado por eventoIdOrigen.
export function useCorrelacionPuntaje(
  { esperaMaximaMs = 6000 }: OpcionesCorrelacionPuntaje = {},
) {
  const [feedbackPuntaje, setFeedbackPuntaje] = useState<string>(
    TEXTO_PUNTAJE_CALCULANDO,
  );
  // true cuando ya se mostró un resultado definitivo (+X pts o el respaldo).
  const resueltoRef = useRef(false);
  // eventoId que se está esperando por SignalR (null = ninguno).
  const esperadoRef = useRef<string | null>(null);
  const timeoutRef = useRef<ReturnType<typeof setTimeout> | null>(null);
  // Eventos PuntajeCalculado que llegaron antes de conocer el eventoId del HTTP.
  const bufferRef = useRef<Map<string, { puntaje: number; ts: number }>>(
    new Map(),
  );

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

  // Se pasa a useRankingTiempoReal.onPuntajeCalculado.
  const alRecibirPuntajeCalculado = useCallback(
    (evento: PuntajeCalculadoEvento) => {
      const origen = obtenerEventoIdOrigen(evento);
      if (!origen || origen === GUID_VACIO) return;
      const puntaje = obtenerPuntajeGanado(evento);

      // Coincide con la acción que se está esperando: mostrar +X de inmediato.
      if (esperadoRef.current && origen === esperadoRef.current) {
        esperadoRef.current = null;
        limpiarTimeout();
        resueltoRef.current = true;
        setFeedbackPuntaje(`+${puntaje} pts`);
        return;
      }

      // Todavía no se espera este evento (SignalR llegó antes que el HTTP): se
      // guarda por eventoIdOrigen para consumirlo al resolver el HTTP.
      podarBuffer();
      bufferRef.current.set(origen, { puntaje, ts: Date.now() });
    },
    [limpiarTimeout, podarBuffer],
  );

  // Se llama tras la respuesta HTTP con resultado.eventoId.
  const esperarPuntaje = useCallback(
    (eventoId?: string | null) => {
      limpiarTimeout();
      resueltoRef.current = false;

      if (!eventoId || eventoId === GUID_VACIO) {
        esperadoRef.current = null;
        resueltoRef.current = true;
        setFeedbackPuntaje(TEXTO_PUNTAJE_FALLBACK);
        return;
      }

      // ¿El evento ya había llegado por SignalR antes que el HTTP? (carrera)
      const enBuffer = bufferRef.current.get(eventoId);
      if (enBuffer) {
        bufferRef.current.delete(eventoId);
        esperadoRef.current = null;
        resueltoRef.current = true;
        setFeedbackPuntaje(`+${enBuffer.puntaje} pts`);
        return;
      }

      esperadoRef.current = eventoId;
      setFeedbackPuntaje(TEXTO_PUNTAJE_CALCULANDO);
      timeoutRef.current = setTimeout(() => {
        if (esperadoRef.current === eventoId) {
          esperadoRef.current = null;
          resueltoRef.current = true;
          setFeedbackPuntaje(TEXTO_PUNTAJE_FALLBACK);
        }
      }, esperaMaximaMs);
    },
    [esperaMaximaMs, limpiarTimeout],
  );

  useEffect(() => () => limpiarTimeout(), [limpiarTimeout]);

  return {
    feedbackPuntaje,
    resueltoRef,
    alRecibirPuntajeCalculado,
    esperarPuntaje,
  };
}
