import { useCallback, useEffect, useRef } from "react";
import { useFocusEffect } from "expo-router";
import * as signalR from "@microsoft/signalr";
import { useAutenticacion } from "../autenticacion/ContextoAutenticacion";
import {
  crearConexionSesionesTiempoReal,
  esErrorNoAutenticadoTiempoReal,
  registrarEventoConexionSesionesTiempoReal,
  registrarErrorConexionTiempoRealDev,
} from "../servicios/sesionesTiempoReal";

interface OpcionesUseListadoSesionesTiempoReal {
  onListadoActualizado: () => void | Promise<void>;
  activo?: boolean;
}

// Mantiene el listado movil de sesiones disponibles sincronizado en vivo: se
// une al grupo general y refresca por HTTP cuando llegan cambios.
export function useListadoSesionesTiempoReal({
  onListadoActualizado,
  activo = true,
}: OpcionesUseListadoSesionesTiempoReal) {
  const { sesion, cargandoSesion, estaAutenticado, cerrarSesion } =
    useAutenticacion();
  const token = sesion?.tokenAcceso ?? null;
  const onListadoActualizadoRef = useRef(onListadoActualizado);

  useEffect(() => {
    onListadoActualizadoRef.current = onListadoActualizado;
  }, [onListadoActualizado]);

  useFocusEffect(
    useCallback(() => {
      if (!activo || cargandoSesion || !token || !estaAutenticado) {
        return undefined;
      }

      let desmontado = false;
      let invalidandoSesion = false;
      let cerrando = false;
      let cierreRegistrado = false;
      let inicioPromise: Promise<void> | null = null;
      const conexion = crearConexionSesionesTiempoReal(token, "Listado");

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

        // Solo un 401 real invalida la sesión; el resto es diagnóstico (dev).
        if (!esErrorNoAutenticadoTiempoReal(error)) {
          registrarErrorConexionTiempoRealDev(error, contexto);
          return;
        }

        invalidandoSesion = true;
        await cerrarSesion();
      };

      const unirseAListado = async () => {
        if (desmontado) return;
        try {
          await conexion.invoke("UnirseAListadoSesiones");
          logDev("unido a listado");
        } catch (error: unknown) {
          await manejarErrorConexion(error, "UnirseAListadoSesiones");
        }
      };

      const manejarListado = () => {
        void onListadoActualizadoRef.current();
      };

      conexion.on("SesionActualizada", manejarListado);

      conexion.onreconnecting((error) => {
        logDev("reconectando");
        void manejarErrorConexion(error, "reconectando");
      });

      conexion.onreconnected(async () => {
        if (desmontado) {
          await conexion.stop().catch(() => undefined);
          return;
        }
        logDev("reconectado");
        await unirseAListado();
        if (!desmontado) {
          void onListadoActualizadoRef.current();
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
          await conexion
            .invoke("SalirDeListadoSesiones")
            .catch(() => undefined);
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
          await unirseAListado();
        })
        .catch((error: unknown) => {
          if (desmontado) return;
          void manejarErrorConexion(error, "start");
        });

      const limpiar = async () => {
        conexion.off("SesionActualizada", manejarListado);
        await cerrarConexion();
      };

      return () => {
        desmontado = true;
        void limpiar();
      };
    }, [activo, token, cargandoSesion, estaAutenticado, cerrarSesion]),
  );
}
