import { useCallback, useState } from "react";
import { useAutenticacion } from "../autenticacion/ContextoAutenticacion";
import { ErrorCrearEquipo, crearEquipoApi } from "../servicios/equiposApi";
import type {
  CrearEquipoRespuesta,
  CrearEquipoSolicitud,
} from "../tipos/equipos";

interface EstadoUseCrearEquipo {
  creando: boolean;
  error: string | null;
  equipoCreado: CrearEquipoRespuesta | null;
  sesionExpirada: boolean;
  // Devuelve el equipo creado (para navegar a su detalle) o null si falló.
  crear: (solicitud: CrearEquipoSolicitud) => Promise<CrearEquipoRespuesta | null>;
  reiniciar: () => void;
}

export function useCrearEquipo(sesionId: string | null | undefined): EstadoUseCrearEquipo {
  const { sesion } = useAutenticacion();
  const token = sesion?.tokenAcceso ?? null;

  const [creando, setCreando] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [equipoCreado, setEquipoCreado] = useState<CrearEquipoRespuesta | null>(null);
  const [sesionExpirada, setSesionExpirada] = useState(false);

  const crear = useCallback(
    async (solicitud: CrearEquipoSolicitud): Promise<CrearEquipoRespuesta | null> => {
      if (!token || !sesionId) return null;
      setCreando(true);
      setError(null);
      setSesionExpirada(false);
      try {
        const creado = await crearEquipoApi(token, sesionId, solicitud);
        setEquipoCreado(creado);
        return creado;
      } catch (e) {
        if (e instanceof ErrorCrearEquipo) {
          if (e.codigo === "NO_AUTORIZADO") setSesionExpirada(true);
          setError(e.message);
        } else if (e instanceof Error) {
          setError(e.message);
        } else {
          setError("No fue posible crear el equipo.");
        }
        return null;
      } finally {
        setCreando(false);
      }
    },
    [token, sesionId],
  );

  const reiniciar = useCallback(() => {
    setError(null);
    setEquipoCreado(null);
    setSesionExpirada(false);
  }, []);

  return { creando, error, equipoCreado, sesionExpirada, crear, reiniciar };
}
