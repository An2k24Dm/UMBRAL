import { useCallback, useState } from "react";
import { useAutenticacion } from "../autenticacion/ContextoAutenticacion";
import { ErrorCrearEquipo, modificarEquipoApi } from "../servicios/equiposApi";
import type { CrearEquipoRespuesta, CrearEquipoSolicitud } from "../tipos/equipos";

interface EstadoUseModificarEquipo {
  guardando: boolean;
  error: string | null;
  equipoActualizado: CrearEquipoRespuesta | null;
  sesionExpirada: boolean;
  modificar: (solicitud: CrearEquipoSolicitud) => Promise<boolean>;
}

// HU41 — Modificar un equipo (solo el líder).
export function useModificarEquipo(
  sesionId: string | null | undefined,
  equipoId: string | null | undefined,
): EstadoUseModificarEquipo {
  const { sesion } = useAutenticacion();
  const token = sesion?.tokenAcceso ?? null;

  const [guardando, setGuardando] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [equipoActualizado, setEquipoActualizado] =
    useState<CrearEquipoRespuesta | null>(null);
  const [sesionExpirada, setSesionExpirada] = useState(false);

  const modificar = useCallback(
    async (solicitud: CrearEquipoSolicitud): Promise<boolean> => {
      if (!token || !sesionId || !equipoId) return false;
      setGuardando(true);
      setError(null);
      setSesionExpirada(false);
      try {
        const actualizado = await modificarEquipoApi(
          token, sesionId, equipoId, solicitud);
        setEquipoActualizado(actualizado);
        return true;
      } catch (e) {
        if (e instanceof ErrorCrearEquipo) {
          if (e.codigo === "NO_AUTORIZADO") setSesionExpirada(true);
          setError(e.message);
        } else if (e instanceof Error) {
          setError(e.message);
        } else {
          setError("No fue posible modificar el equipo.");
        }
        return false;
      } finally {
        setGuardando(false);
      }
    },
    [token, sesionId, equipoId],
  );

  return { guardando, error, equipoActualizado, sesionExpirada, modificar };
}
