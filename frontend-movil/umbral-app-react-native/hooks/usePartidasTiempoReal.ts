import { useEffect } from "react";
import * as signalR from "@microsoft/signalr";
import { useAutenticacion } from "../autenticacion/ContextoAutenticacion";
import { crearConexionPartidasTiempoReal } from "../servicios/partidasTiempoReal";
import { esErrorNoAutenticadoTiempoReal } from "../servicios/sesionesTiempoReal";
import type { RankingEntradaDto } from "../tipos/partidas";

interface OpcionesUsePartidasTiempoReal {
  sesionId?: string | null;
  onEstadoCambiado?: (estado: string) => void;
  onRespuestaRegistrada?: (preguntaId: string, esCorrecta: boolean, puntosGanados: number) => void;
  onPuntajeActualizado?: (ranking: RankingEntradaDto[]) => void;
}

export function usePartidasTiempoReal({
  sesionId,
  onEstadoCambiado,
  onRespuestaRegistrada,
  onPuntajeActualizado,
}: OpcionesUsePartidasTiempoReal) {
  const { sesion, cargandoSesion, estaAutenticado, cerrarSesion } =
    useAutenticacion();
  const token = sesion?.tokenAcceso ?? null;

  useEffect(() => {
    if (cargandoSesion || !token || !estaAutenticado || !sesionId) return;

    let desmontado = false;
    let invalidandoSesion = false;
    const conexion = crearConexionPartidasTiempoReal(token);

    const manejarErrorConexion = async (error: unknown) => {
      if (desmontado || invalidandoSesion || !esErrorNoAutenticadoTiempoReal(error))
        return;
      invalidandoSesion = true;
      await cerrarSesion();
    };

    const manejarEstado = (payload: { estado: string }) => {
      onEstadoCambiado?.(payload.estado);
    };

    const manejarRespuesta = (payload: {
      preguntaId: string;
      esCorrecta: boolean;
      puntosGanados: number;
    }) => {
      onRespuestaRegistrada?.(payload.preguntaId, payload.esCorrecta, payload.puntosGanados);
    };

    const manejarPuntaje = (payload: { ranking: RankingEntradaDto[] }) => {
      onPuntajeActualizado?.(payload.ranking);
    };

    conexion.on("EstadoPartidaCambiado", manejarEstado);
    conexion.on("RespuestaRegistrada", manejarRespuesta);
    conexion.on("PuntajeActualizado", manejarPuntaje);

    conexion.onreconnected(async () => {
      if (desmontado) {
        await conexion.stop().catch(() => undefined);
        return;
      }
      await conexion
        .invoke("UnirseAPartida", sesionId)
        .catch((error: unknown) => manejarErrorConexion(error));
    });

    conexion.onreconnecting((error) => void manejarErrorConexion(error));
    conexion.onclose((error) => void manejarErrorConexion(error));

    conexion
      .start()
      .then(async () => {
        if (desmontado) {
          await conexion.stop().catch(() => undefined);
          return;
        }
        await conexion
          .invoke("UnirseAPartida", sesionId)
          .catch((error: unknown) => manejarErrorConexion(error));
      })
      .catch((error: unknown) => void manejarErrorConexion(error));

    return () => {
      desmontado = true;
      conexion.off("EstadoPartidaCambiado", manejarEstado);
      conexion.off("RespuestaRegistrada", manejarRespuesta);
      conexion.off("PuntajeActualizado", manejarPuntaje);

      if (conexion.state === signalR.HubConnectionState.Connected) {
        conexion
          .invoke("SalirDePartida", sesionId)
          .catch(() => undefined)
          .finally(() => conexion.stop().catch(() => undefined));
      }
    };
  }, [
    token,
    cargandoSesion,
    estaAutenticado,
    cerrarSesion,
    sesionId,
    onEstadoCambiado,
    onRespuestaRegistrada,
    onPuntajeActualizado,
  ]);
}
