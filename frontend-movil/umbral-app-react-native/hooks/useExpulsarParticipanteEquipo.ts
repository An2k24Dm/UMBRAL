import { useCallback, useState } from "react";
import { useAutenticacion } from "../autenticacion/ContextoAutenticacion";
import {
  ErrorCrearEquipo,
  expulsarParticipanteEquipoApi,
} from "../servicios/equiposApi";

interface EstadoUseExpulsarParticipanteEquipo {
  expulsando: boolean;
  error: string | null;
  sesionExpirada: boolean;
  expulsarParticipanteEquipo: (
    sesionId: string,
    equipoId: string,
    participanteSesionId: string,
  ) => Promise<boolean>;
  limpiarError: () => void;
}

// HU45 — Expulsar a un integrante del equipo (solo el líder desde móvil).
// Devuelve true si fue exitoso; los errores quedan en `error`.
export function useExpulsarParticipanteEquipo(): EstadoUseExpulsarParticipanteEquipo {
  const { sesion } = useAutenticacion();
  const token = sesion?.tokenAcceso ?? null;

  const [expulsando, setExpulsando] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [sesionExpirada, setSesionExpirada] = useState(false);

  const limpiarError = useCallback(() => setError(null), []);

  const expulsarParticipanteEquipo = useCallback(
    async (
      sesionId: string,
      equipoId: string,
      participanteSesionId: string,
    ): Promise<boolean> => {
      if (!token || !sesionId || !equipoId || !participanteSesionId) return false;
      setExpulsando(true);
      setError(null);
      setSesionExpirada(false);
      try {
        await expulsarParticipanteEquipoApi(
          token, sesionId, equipoId, participanteSesionId);
        return true;
      } catch (e) {
        if (e instanceof ErrorCrearEquipo) {
          if (e.codigo === "NO_AUTORIZADO") setSesionExpirada(true);
          setError(e.message);
        } else if (e instanceof Error) {
          setError(e.message);
        } else {
          setError("No fue posible expulsar al participante.");
        }
        return false;
      } finally {
        setExpulsando(false);
      }
    },
    [token],
  );

  return { expulsando, error, sesionExpirada, expulsarParticipanteEquipo, limpiarError };
}
