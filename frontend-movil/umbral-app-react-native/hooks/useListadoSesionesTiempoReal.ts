import { useEffect } from "react";
import * as signalR from "@microsoft/signalr";
import { useAutenticacion } from "../autenticacion/ContextoAutenticacion";
import {
  crearConexionSesionesTiempoReal,
  esErrorNoAutenticadoTiempoReal,
} from "../servicios/sesionesTiempoReal";

interface OpcionesUseListadoSesionesTiempoReal {
  onListadoActualizado: () => void | Promise<void>;
}

// Mantiene el listado móvil de sesiones disponibles sincronizado en vivo: se
// une al grupo de listado del hub y refresca cuando el backend emite
// "SesionActualizada" (cambio de estado o de conteo por ingreso/abandono/
// equipos). Si SignalR no conecta, el listado sigue funcionando por HTTP con
// el refresco manual (pull-to-refresh) como respaldo.
export function useListadoSesionesTiempoReal({
  onListadoActualizado,
}: OpcionesUseListadoSesionesTiempoReal) {
  const { sesion, cargandoSesion, estaAutenticado, cerrarSesion } =
    useAutenticacion();
  const token = sesion?.tokenAcceso ?? null;

  useEffect(() => {
    if (cargandoSesion || !token || !estaAutenticado) return;

    let desmontado = false;
    let invalidandoSesion = false;
    const conexion = crearConexionSesionesTiempoReal(token);

    const manejarErrorConexion = async (error: unknown) => {
      if (desmontado || invalidandoSesion || !esErrorNoAutenticadoTiempoReal(error)) {
        return;
      }
      invalidandoSesion = true;
      await cerrarSesion();
    };

    const manejarListado = () => {
      if (__DEV__) {
        console.log("[SignalR Movil Listado] SesionActualizada recibida");
        console.log("[SignalR Movil Listado] refrescando listado");
      }
      void onListadoActualizado();
    };

    conexion.on("SesionActualizada", manejarListado);

    conexion.onreconnected(async () => {
      if (desmontado) {
        await conexion.stop().catch(() => undefined);
        return;
      }
      if (__DEV__) {
        console.log("[SignalR Movil Listado] reconectado");
      }
      await conexion
        .invoke("UnirseAListadoSesiones")
        .then(() => {
          if (__DEV__) {
            console.log("[SignalR Movil Listado] unido al listado movil");
            console.log("[SignalR Movil Listado] refrescando listado");
          }
          return onListadoActualizado();
        })
        .catch((error: unknown) => manejarErrorConexion(error));
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
          console.log("[SignalR Movil Listado] conexion SignalR movil creada");
        }
        if (desmontado) {
          await conexion.stop().catch(() => undefined);
          return;
        }
        await conexion
          .invoke("UnirseAListadoSesiones")
          .then(() => {
            if (__DEV__) {
              console.log("[SignalR Movil Listado] unido al listado movil");
            }
          })
          .catch((error: unknown) => manejarErrorConexion(error));
      })
      .catch((error: unknown) => {
        void manejarErrorConexion(error);
        // Sin pantalla roja en Expo: el listado sigue por HTTP.
      });

    return () => {
      desmontado = true;
      conexion.off("SesionActualizada", manejarListado);

      if (conexion.state === signalR.HubConnectionState.Connected) {
        conexion
          .invoke("SalirDeListadoSesiones")
          .catch(() => undefined)
          .finally(() => {
            conexion.stop().catch(() => undefined);
          });
      }
    };
  }, [token, cargandoSesion, estaAutenticado, cerrarSesion, onListadoActualizado]);
}
