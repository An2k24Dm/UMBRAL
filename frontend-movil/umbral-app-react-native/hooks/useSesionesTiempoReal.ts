import { useEffect } from "react";
import { useAutenticacion } from "../autenticacion/ContextoAutenticacion";
import {
  crearConexionSesionesTiempoReal,
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
  const { sesion } = useAutenticacion();
  const token = sesion?.tokenAcceso ?? null;

  useEffect(() => {
    if (!token || (!sesionId && !equipoId)) return;

    let desmontado = false;
    const conexion = crearConexionSesionesTiempoReal(token);
    const sesionActual = (sesionId ?? "").toLowerCase();
    const equipoActual = (equipoId ?? "").toLowerCase();

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

    conexion
      .start()
      .then(async () => {
        if (desmontado) return;
        if (sesionId) await conexion.invoke("UnirseASesion", sesionId);
        if (equipoId) await conexion.invoke("UnirseAEquipo", equipoId);
      })
      .catch(() => undefined);

    return () => {
      desmontado = true;
      conexion.off("ParticipantesSesionActualizados", manejarParticipantes);
      conexion.off("EquiposSesionActualizados", manejarEquipos);
      conexion.off("EquipoActualizado", manejarEquipo);

      Promise.all([
        sesionId ? conexion.invoke("SalirDeSesion", sesionId).catch(() => undefined) : Promise.resolve(),
        equipoId ? conexion.invoke("SalirDeEquipo", equipoId).catch(() => undefined) : Promise.resolve(),
      ]).finally(() => {
        conexion.stop().catch(() => undefined);
      });
    };
  }, [
    token,
    sesionId,
    equipoId,
    onParticipantesSesionActualizados,
    onEquiposSesionActualizados,
    onEquipoActualizado,
  ]);
}
