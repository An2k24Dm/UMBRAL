import { useEffect } from "react";
import { Alert } from "react-native";
import * as signalR from "@microsoft/signalr";
import { useRouter } from "expo-router";
import { useAutenticacion } from "../autenticacion/ContextoAutenticacion";
import {
  crearConexionSesionesTiempoReal,
  esErrorNoAutenticadoTiempoReal,
} from "../servicios/sesionesTiempoReal";

export function useAvisosSesionTiempoReal() {
  const {
    sesion,
    cargandoSesion,
    estaAutenticado,
    cerrarSesion,
  } = useAutenticacion();
  const token = sesion?.tokenAcceso ?? null;
  const enrutador = useRouter();

  useEffect(() => {
    if (cargandoSesion || !token || !estaAutenticado) return;

    let desmontado = false;
    let invalidandoSesion = false;
    const conexion = crearConexionSesionesTiempoReal(token);

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
      if (!desmontado) enrutador.replace("/");
    };

    const manejarParticipanteExpulsado = () => {
      Alert.alert("Sesión", "Fuiste expulsado de esta sesión.");
      enrutador.replace("/participante/sesiones");
    };

    const manejarEquipoExpulsado = () => {
      Alert.alert("Equipo expulsado", "Tu equipo fue expulsado de la sesión.");
      enrutador.replace("/participante/sesiones");
    };

    conexion.on("ParticipanteExpulsadoSesion", manejarParticipanteExpulsado);
    conexion.on("EquipoExpulsadoSesion", manejarEquipoExpulsado);

    conexion
      .start()
      .then(async () => {
        // Si se desmontó mientras negociaba, recién aquí (ya conectada) es
        // seguro detenerla, sin cortar la negociación.
        if (desmontado) await conexion.stop().catch(() => undefined);
      })
      .catch((error: unknown) => {
        void manejarErrorConexion(error);
        // Sin pantalla roja en Expo: el resto de la app sigue por HTTP.
      });

    conexion.onreconnecting((error) => {
      void manejarErrorConexion(error);
    });

    conexion.onclose((error) => {
      void manejarErrorConexion(error);
    });

    return () => {
      desmontado = true;
      conexion.off("ParticipanteExpulsadoSesion", manejarParticipanteExpulsado);
      conexion.off("EquipoExpulsadoSesion", manejarEquipoExpulsado);

      // Solo cerramos si ya está Conectada. Si está Connecting/Reconnecting,
      // no llamamos stop (cortaría la negociación); el then de start ve
      // `desmontado` y la cierra al terminar.
      if (conexion.state === signalR.HubConnectionState.Connected) {
        conexion.stop().catch(() => undefined);
      }
    };
  }, [
    token,
    cargandoSesion,
    estaAutenticado,
    cerrarSesion,
    enrutador,
  ]);
}
