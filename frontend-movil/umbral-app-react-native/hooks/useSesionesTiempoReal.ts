import { useEffect } from "react";
import * as signalR from "@microsoft/signalr";
import { useAutenticacion } from "../autenticacion/ContextoAutenticacion";
import {
  crearConexionSesionesTiempoReal,
  esErrorNoAutenticadoTiempoReal,
  obtenerEquipoIdEvento,
  obtenerSesionIdEvento,
  type EventoSesionTiempoReal,
} from "../servicios/sesionesTiempoReal";

interface OpcionesUseSesionesTiempoReal {
  sesionId?: string | null;
  equipoId?: string | null;
  onParticipantesSesionActualizados?: () => void | Promise<void>;
  onEquiposSesionActualizados?: () => void | Promise<void>;
  onEquipoActualizado?: () => void | Promise<void>;
}

export function useSesionesTiempoReal({
  sesionId,
  equipoId,
  onParticipantesSesionActualizados,
  onEquiposSesionActualizados,
  onEquipoActualizado,
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
      if (coincideSesion(evento)) void onParticipantesSesionActualizados?.();
    };

    const manejarEquipos = (evento: EventoSesionTiempoReal) => {
      if (coincideSesion(evento)) void onEquiposSesionActualizados?.();
    };

    const manejarEquipo = (evento: EventoSesionTiempoReal) => {
      if (coincideSesion(evento) && coincideEquipo(evento)) {
        void onEquipoActualizado?.();
      }
    };

    conexion.on("ParticipantesSesionActualizados", manejarParticipantes);
    conexion.on("EquiposSesionActualizados", manejarEquipos);
    conexion.on("EquipoActualizado", manejarEquipo);

    conexion.onreconnected(async () => {
      if (desmontado) {
        await conexion.stop().catch(() => undefined);
        return;
      }
      if (sesionId) {
        await conexion
          .invoke("UnirseASesion", sesionId)
          .catch((error: unknown) => manejarErrorConexion(error));
      }
      if (equipoId) {
        await conexion
          .invoke("UnirseAEquipo", equipoId)
          .catch((error: unknown) => manejarErrorConexion(error));
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
        if (desmontado) {
          await conexion.stop().catch(() => undefined);
          return;
        }
        if (sesionId) {
          await conexion
            .invoke("UnirseASesion", sesionId)
            .catch((error: unknown) => manejarErrorConexion(error));
        }
        if (equipoId) {
          await conexion
            .invoke("UnirseAEquipo", equipoId)
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
  ]);
}
