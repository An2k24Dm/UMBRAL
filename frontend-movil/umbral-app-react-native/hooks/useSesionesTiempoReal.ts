import { useCallback, useEffect, useRef } from "react";
import { useFocusEffect } from "expo-router";
import * as signalR from "@microsoft/signalr";
import { useAutenticacion } from "../autenticacion/ContextoAutenticacion";
import {
  crearConexionSesionesTiempoReal,
  esErrorNoAutenticadoTiempoReal,
  obtenerEquipoIdEvento,
  obtenerEstadoEvento,
  obtenerSesionIdEvento,
  registrarEventoConexionSesionesTiempoReal,
  registrarErrorConexionTiempoRealDev,
  type EventoSesionTiempoReal,
} from "../servicios/sesionesTiempoReal";

interface OpcionesUseSesionesTiempoReal {
  sesionId?: string | null;
  equipoId?: string | null;
  origen?: string;
  onParticipantesSesionActualizados?: () => void | Promise<void>;
  onEquiposSesionActualizados?: () => void | Promise<void>;
  onEquipoActualizado?: () => void | Promise<void>;
  onSesionActualizada?: (estado: string | undefined) => void | Promise<void>;
  onRespuestaRegistrada?: () => void | Promise<void>;
  onEtapaCompletada?: () => void | Promise<void>;
  onEtapaIniciada?: () => void | Promise<void>;
  onProgresoSecuencialActualizado?: () => void | Promise<void>;
}

type CallbacksSesionesTiempoReal = Pick<
  OpcionesUseSesionesTiempoReal,
  | "onParticipantesSesionActualizados"
  | "onEquiposSesionActualizados"
  | "onEquipoActualizado"
  | "onSesionActualizada"
  | "onRespuestaRegistrada"
  | "onEtapaCompletada"
  | "onEtapaIniciada"
  | "onProgresoSecuencialActualizado"
>;

export function useSesionesTiempoReal({
  sesionId,
  equipoId,
  origen = "Detalle",
  onParticipantesSesionActualizados,
  onEquiposSesionActualizados,
  onEquipoActualizado,
  onSesionActualizada,
  onRespuestaRegistrada,
  onEtapaCompletada,
  onEtapaIniciada,
  onProgresoSecuencialActualizado,
}: OpcionesUseSesionesTiempoReal) {
  const { sesion, cargandoSesion, estaAutenticado, cerrarSesion } =
    useAutenticacion();
  const token = sesion?.tokenAcceso ?? null;
  const callbacksRef = useRef<CallbacksSesionesTiempoReal>({});

  useEffect(() => {
    callbacksRef.current = {
      onParticipantesSesionActualizados,
      onEquiposSesionActualizados,
      onEquipoActualizado,
      onSesionActualizada,
      onRespuestaRegistrada,
      onEtapaCompletada,
      onEtapaIniciada,
      onProgresoSecuencialActualizado,
    };
  }, [
    onParticipantesSesionActualizados,
    onEquiposSesionActualizados,
    onEquipoActualizado,
    onSesionActualizada,
    onRespuestaRegistrada,
    onEtapaCompletada,
    onEtapaIniciada,
    onProgresoSecuencialActualizado,
  ]);

  useFocusEffect(
    useCallback(() => {
      if (
        cargandoSesion ||
        !token ||
        !estaAutenticado ||
        (!sesionId && !equipoId)
      ) {
        return undefined;
      }

      let desmontado = false;
      let invalidandoSesion = false;
      let cerrando = false;
      let cierreRegistrado = false;
      let inicioPromise: Promise<void> | null = null;
      const conexion = crearConexionSesionesTiempoReal(token, origen);
      const sesionActual = (sesionId ?? "").toLowerCase();
      const equipoActual = (equipoId ?? "").toLowerCase();

      const logDev = (mensaje: string) => {
        registrarEventoConexionSesionesTiempoReal(conexion, mensaje);
      };

      const registrarCerrado = () => {
        if (cierreRegistrado) return;
        cierreRegistrado = true;
        logDev("cerrado");
      };

      const manejarErrorConexion = async (error: unknown, contexto?: string) => {
        if (desmontado || invalidandoSesion || !error) return;

        // Solo un 401 real invalida la sesión. Transporte/timeout/rechazo se
        // registran como diagnóstico (dev) sin cerrar sesión ni caja roja.
        if (!esErrorNoAutenticadoTiempoReal(error)) {
          registrarErrorConexionTiempoRealDev(error, contexto);
          return;
        }

        invalidandoSesion = true;
        await cerrarSesion();
      };

      const coincideSesion = (evento: EventoSesionTiempoReal) =>
        !sesionActual ||
        obtenerSesionIdEvento(evento).toLowerCase() === sesionActual;

      const coincideEquipo = (evento: EventoSesionTiempoReal) =>
        !equipoActual ||
        obtenerEquipoIdEvento(evento).toLowerCase() === equipoActual;

      const manejarParticipantes = (evento: EventoSesionTiempoReal) => {
        if (coincideSesion(evento)) {
          void callbacksRef.current.onParticipantesSesionActualizados?.();
        }
      };

      const manejarEquipos = (evento: EventoSesionTiempoReal) => {
        if (coincideSesion(evento)) {
          void callbacksRef.current.onEquiposSesionActualizados?.();
        }
      };

      const manejarEquipo = (evento: EventoSesionTiempoReal) => {
        if (coincideSesion(evento) && coincideEquipo(evento)) {
          void callbacksRef.current.onEquipoActualizado?.();
        }
      };

      const manejarSesion = (evento: EventoSesionTiempoReal) => {
        if (coincideSesion(evento)) {
          void callbacksRef.current.onSesionActualizada?.(obtenerEstadoEvento(evento));
        }
      };

      const manejarRespuesta = (evento: EventoSesionTiempoReal) => {
        if (coincideSesion(evento)) {
          void callbacksRef.current.onRespuestaRegistrada?.();
        }
      };

      const manejarEtapa = (evento: EventoSesionTiempoReal) => {
        if (coincideSesion(evento)) {
          void callbacksRef.current.onEtapaCompletada?.();
        }
      };

      const manejarEtapaIniciada = (evento: EventoSesionTiempoReal) => {
        if (coincideSesion(evento)) {
          void callbacksRef.current.onEtapaIniciada?.();
        }
      };

      const manejarProgresoSecuencial = (evento: EventoSesionTiempoReal) => {
        if (coincideSesion(evento) && coincideEquipo(evento)) {
          void callbacksRef.current.onProgresoSecuencialActualizado?.();
        }
      };

      const unirseASesion = async () => {
        if (!sesionId || desmontado) return;
        try {
          await conexion.invoke("UnirseASesion", sesionId);
          logDev("unido a sesion");
        } catch (error: unknown) {
          await manejarErrorConexion(error, "UnirseASesion");
        }
      };

      const unirseAEquipo = async () => {
        if (!equipoId || desmontado) return;
        try {
          await conexion.invoke("UnirseAEquipo", equipoId);
          logDev("unido a equipo");
        } catch (error: unknown) {
          await manejarErrorConexion(error, "UnirseAEquipo");
        }
      };

      conexion.on("ParticipantesSesionActualizados", manejarParticipantes);
      conexion.on("EquiposSesionActualizados", manejarEquipos);
      conexion.on("EquipoActualizado", manejarEquipo);
      conexion.on("SesionActualizada", manejarSesion);
      conexion.on("RespuestaRegistrada", manejarRespuesta);
      conexion.on("EtapaCompletada", manejarEtapa);
      conexion.on("EtapaIniciada", manejarEtapaIniciada);
      conexion.on("ProgresoSecuencialActualizado", manejarProgresoSecuencial);

      conexion.onreconnecting((error) => {
        logDev("reconectando");
        void manejarErrorConexion(error, "reconectando");
      });

      conexion.onreconnected(async () => {
        if (desmontado) {
          await cerrarConexion();
          return;
        }
        logDev("reconectado");
        await unirseASesion();
        await unirseAEquipo();
        if (!desmontado) {
          void callbacksRef.current.onSesionActualizada?.(undefined);
        }
      });

      conexion.onclose((error) => {
        registrarCerrado();
        void manejarErrorConexion(error, "onclose");
      });

      const cerrarConexion = async () => {
        if (cerrando) return;
        cerrando = true;
        logDev("cerrando");

        await inicioPromise?.catch(() => undefined);

        if (conexion.state === signalR.HubConnectionState.Connected) {
          await Promise.all([
            sesionId
              ? conexion.invoke("SalirDeSesion", sesionId).catch(() => undefined)
              : Promise.resolve(),
            equipoId
              ? conexion.invoke("SalirDeEquipo", equipoId).catch(() => undefined)
              : Promise.resolve(),
          ]);
        }

        if (conexion.state !== signalR.HubConnectionState.Disconnected) {
          await conexion.stop().catch(() => undefined);
        }

        if (conexion.state === signalR.HubConnectionState.Disconnected) {
          registrarCerrado();
        }
      };

      inicioPromise = conexion.start();
      void inicioPromise
        .then(async () => {
          logDev("conectado");
          if (desmontado) {
            await cerrarConexion();
            return;
          }
          await unirseASesion();
          await unirseAEquipo();
        })
        .catch((error: unknown) => {
          if (desmontado) return;
          void manejarErrorConexion(error, "start");
        });

      const limpiar = async () => {
        conexion.off("ParticipantesSesionActualizados", manejarParticipantes);
        conexion.off("EquiposSesionActualizados", manejarEquipos);
        conexion.off("EquipoActualizado", manejarEquipo);
        conexion.off("SesionActualizada", manejarSesion);
        conexion.off("RespuestaRegistrada", manejarRespuesta);
        conexion.off("EtapaCompletada", manejarEtapa);
        conexion.off("EtapaIniciada", manejarEtapaIniciada);
        conexion.off("ProgresoSecuencialActualizado", manejarProgresoSecuencial);
        await cerrarConexion();
      };

      return () => {
        desmontado = true;
        void limpiar();
      };
    }, [
      token,
      cargandoSesion,
      estaAutenticado,
      cerrarSesion,
      sesionId,
      equipoId,
      origen,
    ]),
  );
}
