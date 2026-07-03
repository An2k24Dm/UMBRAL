import { useCallback, useState } from "react";
import { useAutenticacion } from "../autenticacion/ContextoAutenticacion";
import {
  ErrorAbandonarSesion,
  abandonarSesionApi,
} from "../servicios/sesionesApi";

interface EstadoUseAbandonarSesion {
  abandonando: boolean;
  error: string | null;
  sesionExpirada: boolean;
  abandonarSesion: (sesionId: string) => Promise<boolean>;
  limpiarError: () => void;
}

// HU48 — Abandonar la sesión individual o el equipo de la sesión grupal
// (el backend decide según el tipo). Devuelve true si fue exitoso.
export function useAbandonarSesion(): EstadoUseAbandonarSesion {
  const { sesion } = useAutenticacion();
  const token = sesion?.tokenAcceso ?? null;

  const [abandonando, setAbandonando] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [sesionExpirada, setSesionExpirada] = useState(false);

  const limpiarError = useCallback(() => setError(null), []);

  const abandonarSesion = useCallback(
    async (sesionId: string): Promise<boolean> => {
      if (!token || !sesionId) {
        setError("No fue posible abandonar la sesión.");
        return false;
      }
      setAbandonando(true);
      setError(null);
      setSesionExpirada(false);
      try {
        await abandonarSesionApi(token, sesionId);
        return true;
      } catch (e) {
        if (e instanceof ErrorAbandonarSesion) {
          if (e.codigo === "NO_AUTORIZADO") setSesionExpirada(true);
          setError(e.message);
        } else if (e instanceof Error) {
          setError(e.message);
        } else {
          setError("No fue posible abandonar la sesión.");
        }
        return false;
      } finally {
        setAbandonando(false);
      }
    },
    [token],
  );

  return { abandonando, error, sesionExpirada, abandonarSesion, limpiarError };
}
