import { useCallback, useRef, useState } from "react";
import { useAutenticacion } from "../autenticacion/ContextoAutenticacion";
import {
  ErrorConsultaSesiones,
  ingresarSesionIndividualApi,
  ingresarSesionPorCodigoApi,
} from "../servicios/sesionesApi";
import { notificarMembresiaTiempoRealActualizada } from "../servicios/membresiaTiempoReal";
import type { IngresarSesionRespuestaDto } from "../tipos/sesiones";

export function useIngresoSesion() {
  const { sesion } = useAutenticacion();
  const token = sesion?.tokenAcceso ?? null;
  const enCurso = useRef(false);
  const [ingresando, setIngresando] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [sesionExpirada, setSesionExpirada] = useState(false);

  const ejecutar = useCallback(
    async (
      accion: (tokenAcceso: string) => Promise<IngresarSesionRespuestaDto>,
    ): Promise<IngresarSesionRespuestaDto | null> => {
      if (!token || enCurso.current) return null;
      enCurso.current = true;
      setIngresando(true);
      setError(null);
      setSesionExpirada(false);
      try {
        const resultado = await accion(token);
        // #8: la conexión SignalR global re-sincroniza sus grupos para unirse de
        // inmediato al GrupoSesion de la sesión recién ingresada.
        notificarMembresiaTiempoRealActualizada();
        return resultado;
      } catch (e) {
        if (e instanceof ErrorConsultaSesiones) {
          if (e.codigo === "NO_AUTORIZADO") setSesionExpirada(true);
          setError(e.message);
        } else if (e instanceof Error) {
          setError(e.message);
        } else {
          setError("No fue posible ingresar a la sesión.");
        }
        return null;
      } finally {
        enCurso.current = false;
        setIngresando(false);
      }
    },
    [token],
  );

  const ingresarPorCodigo = useCallback(
    (codigo: string) => ejecutar((t) => ingresarSesionPorCodigoApi(t, codigo)),
    [ejecutar],
  );
  const ingresarIndividual = useCallback(
    (sesionId: string) => ejecutar((t) => ingresarSesionIndividualApi(t, sesionId)),
    [ejecutar],
  );
  const limpiarError = useCallback(() => setError(null), []);

  return {
    ingresando,
    error,
    sesionExpirada,
    ingresarPorCodigo,
    ingresarIndividual,
    limpiarError,
  };
}
