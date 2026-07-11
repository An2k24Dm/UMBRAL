import { useCallback, useState } from "react";
import { useAutenticacion } from "../autenticacion/ContextoAutenticacion";
import { ErrorCrearEquipo, ingresarEquipoApi } from "../servicios/equiposApi";
import { notificarMembresiaTiempoRealActualizada } from "../servicios/membresiaTiempoReal";
import type { IngresarEquipoRespuesta } from "../tipos/equipos";

interface EstadoUseIngresarEquipo {
  ingresando: boolean;
  error: string | null;
  sesionExpirada: boolean;
  ingresarEquipo: (
    sesionId: string,
    equipoId: string,
    contrasena?: string | null,
  ) => Promise<IngresarEquipoRespuesta | null>;
  limpiarError: () => void;
}

// HU47 — Ingresar a un equipo de una sesión grupal. Equipos públicos entran
// sin contraseña; los privados la exigen y el backend la verifica contra el
// hash. Devuelve la respuesta del backend en éxito o null si falló.
export function useIngresarEquipo(): EstadoUseIngresarEquipo {
  const { sesion } = useAutenticacion();
  const token = sesion?.tokenAcceso ?? null;

  const [ingresando, setIngresando] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [sesionExpirada, setSesionExpirada] = useState(false);

  const limpiarError = useCallback(() => setError(null), []);

  const ingresarEquipo = useCallback(
    async (
      sesionId: string,
      equipoId: string,
      contrasena?: string | null,
    ): Promise<IngresarEquipoRespuesta | null> => {
      if (!token || !sesionId || !equipoId) return null;
      setIngresando(true);
      setError(null);
      setSesionExpirada(false);
      try {
        const resultado = await ingresarEquipoApi(token, sesionId, equipoId, {
          contrasena: contrasena ?? null,
        });
        // #8: re-sincroniza la conexión global (UnirseASesion + UnirseAEquipo).
        notificarMembresiaTiempoRealActualizada();
        return resultado;
      } catch (e) {
        if (e instanceof ErrorCrearEquipo) {
          if (e.codigo === "NO_AUTORIZADO") setSesionExpirada(true);
          setError(e.message);
        } else if (e instanceof Error) {
          setError(e.message);
        } else {
          setError("No fue posible ingresar al equipo.");
        }
        return null;
      } finally {
        setIngresando(false);
      }
    },
    [token],
  );

  return { ingresando, error, sesionExpirada, ingresarEquipo, limpiarError };
}
