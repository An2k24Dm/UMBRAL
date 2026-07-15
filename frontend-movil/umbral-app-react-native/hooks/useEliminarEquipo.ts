import { useCallback, useState } from "react";
import { useAutenticacion } from "../autenticacion/ContextoAutenticacion";
import { ErrorCrearEquipo, eliminarEquipoApi } from "../servicios/equiposApi";

interface EstadoUseEliminarEquipo {
  eliminando: boolean;
  error: string | null;
  noExiste: boolean;
  sesionExpirada: boolean;
  eliminar: () => Promise<boolean>;
}

// HU42 — Eliminar un equipo (solo el líder).
export function useEliminarEquipo(
  sesionId: string | null | undefined,
  equipoId: string | null | undefined,
): EstadoUseEliminarEquipo {
  const { sesion } = useAutenticacion();
  const token = sesion?.tokenAcceso ?? null;

  const [eliminando, setEliminando] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [noExiste, setNoExiste] = useState(false);
  const [sesionExpirada, setSesionExpirada] = useState(false);

  const eliminar = useCallback(async (): Promise<boolean> => {
    if (!token || !sesionId || !equipoId) return false;
    setEliminando(true);
    setError(null);
    setNoExiste(false);
    setSesionExpirada(false);
    try {
      await eliminarEquipoApi(token, sesionId, equipoId);
      return true;
    } catch (e) {
      if (e instanceof ErrorCrearEquipo) {
        if (e.codigo === "NO_AUTORIZADO") setSesionExpirada(true);
        if (e.estadoHttp === 404) setNoExiste(true);
        setError(e.message);
      } else if (e instanceof Error) {
        setError(e.message);
      } else {
        setError("No fue posible eliminar el equipo.");
      }
      return false;
    } finally {
      setEliminando(false);
    }
  }, [token, sesionId, equipoId]);

  return { eliminando, error, noExiste, sesionExpirada, eliminar };
}
