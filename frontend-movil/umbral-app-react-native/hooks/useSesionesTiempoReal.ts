import { useEffect } from "react";
import * as signalR from "@microsoft/signalr";
import { useAutenticacion } from "../autenticacion/ContextoAutenticacion";
import {
  crearConexionSesionesTiempoReal,
  esErrorNoAutenticadoTiempoReal,
  obtenerEquipoIdEvento,
  obtenerEstadoEvento,
  obtenerSesionIdEvento,
  type EventoSesionTiempoReal,
} from "../servicios/sesionesTiempoReal";

interface OpcionesUseSesionesTiempoReal {
  sesionId?: string | null;
  equipoId?: string | null;
  onParticipantesSesionActualizados?: () => void | Promise<void>;
  onEquiposSesionActualizados?: () => void | Promise<void>;
  onEquipoActualizado?: () => void | Promise<void>;
  // HU52 — cambio de estado del ciclo de vida (iniciar/pausar/reanudar/
  // cancelar/finalizar). Recibe el nuevo estado para que la pantalla decida
  // (banner de pausa, mensaje de cancelación, etc.).
  onSesionActualizada?: (estado: string | undefined) => void | Promise<void>;
  onRespuestaRegistrada?: () => void | Promise<void>;
  onEtapaCompletada?: () => void | Promise<void>;
}

export function useSesionesTiempoReal({
  sesionId,
  equipoId,
  onParticipantesSesionActualizados,
  onEquiposSesionActualizados,
  onEquipoActualizado,
  onSesionActualizada,
  onRespuestaRegistrada,
  onEtapaCompletada,
}: OpcionesUseSesionesTiempoReal) {
  const {
    sesion,
    cargandoSesion,
    estaAutenticado,
    cerrarSesion,
  } = useAutenticacion();
  const token = sesion?.tokenAcceso ?? null;

  useEffect(() => {
    if (
      cargandoSesion ||
      !token ||
      !estaAutenticado ||
      (!sesionId && !equipoId)
    ) {
      return;
    }

    let desmontado = false;
    let invalidandoSesion = false;
    const conexion = crearConexionSesionesTiempoReal(token);
    const sesionActual = (sesionId ?? "").toLowerCase();
    const equipoActual = (equipoId ?? "").toLowerCase();

    const manejarErrorConexion = async (error: unknown) => {
      if (
        desmontado ||
        invalidandoSesion ||
        !esErrorNoAutenticadoTiempoReal(error)
      ) {
        return;
      }

      invalidandoSesion = true;
      await cerrarSesion();
    };

    const coincideSesion = (evento: EventoSesionTiempoReal) =>
      !sesionActual || obtenerSesionIdEvento(evento).toLowerCase() === sesionActual;

    const coincideEquipo = (evento: EventoSesionTiempoReal) =>
      !equipoActual || obtenerEquipoIdEvento(evento).toLowerCase() === equipoActual;

    const manejarParticipantes = (evento: EventoSesionTiempoReal) => {
      if (coincideSesion(evento)) {
        if (__DEV__) {
          console.log("[SignalR Movil Detalle] ParticipantesSesionActualizados recibido");
        }
        void onParticipantesSesionActualizados?.();
      }
    };

    const manejarEquipos = (evento: EventoSesionTiempoReal) => {
      if (coincideSesion(evento)) {
        if (__DEV__) {
          console.log("[SignalR Movil Detalle] EquiposSesionActualizados recibido");
        }
        void onEquiposSesionActualizados?.();
      }
    };

    const manejarEquipo = (evento: EventoSesionTiempoReal) => {
      if (coincideSesion(evento) && coincideEquipo(evento)) {
        if (__DEV__) {
          console.log("[SignalR Movil Detalle] EquipoActualizado recibido");
        }
        void onEquipoActualizado?.();
      }
    };

    const manejarSesion = (evento: EventoSesionTiempoReal) => {
      if (coincideSesion(evento)) {
        if (__DEV__) {
          console.log("[SignalR Movil Detalle] SesionActualizada recibida");
        }
        void onSesionActualizada?.(obtenerEstadoEvento(evento));
      }
    };

    const manejarRespuesta = (evento: EventoSesionTiempoReal) => {
      if (coincideSesion(evento)) {
        if (__DEV__) {
          console.log("[SignalR Movil Detalle] RespuestaRegistrada recibida");
        }
        void onRespuestaRegistrada?.();
      }
    };

    const manejarEtapa = (evento: EventoSesionTiempoReal) => {
      if (coincideSesion(evento)) {
        if (__DEV__) {
          console.log("[SignalR Movil Detalle] EtapaCompletada recibida");
        }
        void onEtapaCompletada?.();
      }
    };

    conexion.on("ParticipantesSesionActualizados", manejarParticipantes);
    conexion.on("EquiposSesionActualizados", manejarEquipos);
    conexion.on("EquipoActualizado", manejarEquipo);
    conexion.on("SesionActualizada", manejarSesion);
    conexion.on("RespuestaRegistrada", manejarRespuesta);
    conexion.on("EtapaCompletada", manejarEtapa);

    conexion.onreconnected(async () => {
      if (desmontado) {
        await conexion.stop().catch(() => undefined);
        return;
      }
      if (__DEV__) {
        console.log("[SignalR Movil Detalle] reconectado");
      }
      if (sesionId) {
        await conexion
          .invoke("UnirseASesion", sesionId)
          .then(() => {
            if (__DEV__) {
              console.log("[SignalR Movil Detalle] unido a sesion");
            }
          })
          .catch((error: unknown) => manejarErrorConexion(error));
      }
      if (equipoId) {
        await conexion
          .invoke("UnirseAEquipo", equipoId)
          .then(() => {
            if (__DEV__) {
              console.log("[SignalR Movil Detalle] unido a equipo");
            }
          })
          .catch((error: unknown) => manejarErrorConexion(error));
      }
      if (!desmontado) {
        void onSesionActualizada?.(undefined);
      }
    });

    conexion.onreconnecting((error) => {
      void manejarErrorConexion(error);
    });

    conexion.onclose((error) => {
      void manejarErrorConexion(error);
    });

    conexion
      .start()
      .then(async () => {
        if (__DEV__) {
          console.log("[SignalR Movil Detalle] conexion SignalR movil creada");
        }
        if (desmontado) {
          await conexion.stop().catch(() => undefined);
          return;
        }
        if (sesionId) {
          await conexion
            .invoke("UnirseASesion", sesionId)
            .then(() => {
              if (__DEV__) {
                console.log("[SignalR Movil Detalle] unido a sesion");
              }
            })
            .catch((error: unknown) => manejarErrorConexion(error));
        }
        if (equipoId) {
          await conexion
            .invoke("UnirseAEquipo", equipoId)
            .then(() => {
              if (__DEV__) {
                console.log("[SignalR Movil Detalle] unido a equipo");
              }
            })
            .catch((error: unknown) => manejarErrorConexion(error));
        }
      })
      .catch((error: unknown) => {
        void manejarErrorConexion(error);
        // No mostrar pantalla roja en Expo: el detalle sigue por HTTP y el
        // refresco manual queda como respaldo.
      });

    return () => {
      desmontado = true;
      conexion.off("ParticipantesSesionActualizados", manejarParticipantes);
      conexion.off("EquiposSesionActualizados", manejarEquipos);
      conexion.off("EquipoActualizado", manejarEquipo);
      conexion.off("SesionActualizada", manejarSesion);
      conexion.off("RespuestaRegistrada", manejarRespuesta);
      conexion.off("EtapaCompletada", manejarEtapa);

      if (conexion.state === signalR.HubConnectionState.Connected) {
        Promise.all([
          sesionId
            ? conexion.invoke("SalirDeSesion", sesionId).catch(() => undefined)
            : Promise.resolve(),
          equipoId
            ? conexion.invoke("SalirDeEquipo", equipoId).catch(() => undefined)
            : Promise.resolve(),
        ]).finally(() => {
          conexion.stop().catch(() => undefined);
        });
      }
    };
  }, [
    token,
    cargandoSesion,
    estaAutenticado,
    cerrarSesion,
    sesionId,
    equipoId,
    onParticipantesSesionActualizados,
    onEquiposSesionActualizados,
    onEquipoActualizado,
    onSesionActualizada,
    onRespuestaRegistrada,
    onEtapaCompletada,
  ]);
}
