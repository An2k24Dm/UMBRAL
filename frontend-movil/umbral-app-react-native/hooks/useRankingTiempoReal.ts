import { useCallback, useEffect, useRef } from "react";
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

      const unirse = () =>
        conexion
          .invoke("UnirseASesion", sesionId)
          .catch((error: unknown) => manejarError(error, "UnirseASesion"));

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
        void unirse();
        void callbacksRef.current.onReconectado?.();
      });
      conexion.onreconnecting(manejarError);
      conexion.onclose(manejarError);

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
          if (desmontado) {
            await cerrarConexion();
            return;
          }
          await unirse();
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
}
