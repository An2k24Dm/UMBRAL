import { useCallback, useEffect, useRef, useState } from "react";
import { useFocusEffect } from "expo-router";
import * as signalR from "@microsoft/signalr";
import { useAutenticacion } from "../autenticacion/ContextoAutenticacion";
import {
  crearConexionRankingTiempoReal,
  type PuntajeCalculadoEvento,
} from "../servicios/rankingTiempoReal";
import {
  esErrorNoAutenticadoTiempoReal,
  registrarErrorConexionTiempoRealDev,
} from "../servicios/sesionesTiempoReal";

interface OpcionesUseRankingTiempoReal {
  sesionId?: string | null;
  onPuntajeCalculado?: (evento: PuntajeCalculadoEvento) => void | Promise<void>;
  onRankingParticipantesActualizado?: () => void | Promise<void>;
  onRankingEquiposActualizado?: () => void | Promise<void>;
  onReconectado?: () => void | Promise<void>;
}

type CallbacksRanking = Omit<OpcionesUseRankingTiempoReal, "sesionId">;

export type EstadoRankingTiempoReal =
  | "desconectado"
  | "conectando"
  | "conectado"
  | "unido"
  | "reconectando";

export function useRankingTiempoReal({
  sesionId,
  onPuntajeCalculado,
  onRankingParticipantesActualizado,
  onRankingEquiposActualizado,
  onReconectado,
}: OpcionesUseRankingTiempoReal) {
  const { sesion, cargandoSesion, estaAutenticado, cerrarSesion } =
    useAutenticacion();
  const token = sesion?.tokenAcceso ?? null;
  const callbacksRef = useRef<CallbacksRanking>({});
  const [estado, setEstado] = useState<EstadoRankingTiempoReal>("desconectado");

  useEffect(() => {
    callbacksRef.current = {
      onPuntajeCalculado,
      onRankingParticipantesActualizado,
      onRankingEquiposActualizado,
      onReconectado,
    };
  }, [
    onPuntajeCalculado,
    onRankingParticipantesActualizado,
    onRankingEquiposActualizado,
    onReconectado,
  ]);

  useFocusEffect(
    useCallback(() => {
      if (cargandoSesion || !token || !estaAutenticado || !sesionId) {
        return undefined;
      }

      let desmontado = false;
      let cerrando = false;
      let inicioPromise: Promise<void> | null = null;
      const conexion = crearConexionRankingTiempoReal(token);
      setEstado("conectando");

      const logDev = (mensaje: string, datos?: Record<string, unknown>) => {
        if (typeof __DEV__ !== "undefined" && __DEV__) {
          console.info("[Ranking SignalR]", mensaje, {
            sesionId,
            connectionId: conexion.connectionId,
            estado: conexion.state,
            ...datos,
          });
        }
      };

      const manejarError = async (error: unknown, contexto?: string) => {
        if (!error || desmontado) return;
        // Solo el 401 cierra sesión; el transporte transitorio se registra como
        // diagnóstico (dev) y la reconexión automática se encarga del resto.
        if (esErrorNoAutenticadoTiempoReal(error)) {
          await cerrarSesion();
          return;
        }
        registrarErrorConexionTiempoRealDev(error, contexto ?? "Ranking");
      };

      const unirse = async () => {
        await conexion.invoke("UnirseASesion", sesionId);
        setEstado("unido");
        logDev("unido a sesión");
      };

      conexion.on("PuntajeCalculado", (evento: PuntajeCalculadoEvento) => {
        void callbacksRef.current.onPuntajeCalculado?.(evento);
      });
      conexion.on("RankingParticipantesActualizado", () => {
        void callbacksRef.current.onRankingParticipantesActualizado?.();
      });
      conexion.on("RankingEquiposActualizado", () => {
        void callbacksRef.current.onRankingEquiposActualizado?.();
      });

      conexion.onreconnected(() => {
        if (desmontado) return;
        setEstado("conectado");
        logDev("reconectado");
        void unirse()
          .then(() => callbacksRef.current.onReconectado?.())
          .catch((error: unknown) => manejarError(error, "UnirseASesion"));
      });
      conexion.onreconnecting((error) => {
        setEstado("reconectando");
        void manejarError(error, "reconectando");
      });
      conexion.onclose((error) => {
        setEstado("desconectado");
        void manejarError(error, "onclose");
      });

      const cerrarConexion = async () => {
        if (cerrando) return;
        cerrando = true;

        await inicioPromise?.catch(() => undefined);

        if (conexion.state === signalR.HubConnectionState.Connected) {
          await conexion
            .invoke("SalirDeSesion", sesionId)
            .catch(() => undefined);
        }

        if (conexion.state !== signalR.HubConnectionState.Disconnected) {
          await conexion.stop().catch(() => undefined);
        }
      };

      inicioPromise = conexion.start();
      void inicioPromise
        .then(async () => {
          setEstado("conectado");
          logDev("conectado");
          if (desmontado) {
            await cerrarConexion();
            return;
          }
          await unirse().catch((error: unknown) => manejarError(error, "UnirseASesion"));
        })
        .catch((error: unknown) => {
          if (desmontado) return;
          void manejarError(error);
        });

      return () => {
        desmontado = true;
        conexion.off("PuntajeCalculado");
        conexion.off("RankingParticipantesActualizado");
        conexion.off("RankingEquiposActualizado");
        void cerrarConexion();
      };
    }, [cargandoSesion, token, estaAutenticado, sesionId, cerrarSesion]),
  );

  return { estado, unidoASesion: estado === "unido" };
}
