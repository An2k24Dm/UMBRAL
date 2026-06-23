import { useCallback, useEffect, useState } from "react";
import { useAutenticacion } from "../autenticacion/ContextoAutenticacion";
import { ErrorCrearEquipo, listarEquiposSesionApi } from "../servicios/equiposApi";
import type { EquipoSesionListado } from "../tipos/equipos";

interface EstadoUseEquiposSesion {
  equipos: EquipoSesionListado[];
  cargando: boolean;
  error: string | null;
  sesionExpirada: boolean;
  refrescar: () => Promise<void>;
}

// HU43 — Lista de equipos de una sesión.
export function useEquiposSesion(
  sesionId: string | null | undefined,
): EstadoUseEquiposSesion {
  const { sesion } = useAutenticacion();
  const token = sesion?.tokenAcceso ?? null;

  const [equipos, setEquipos] = useState<EquipoSesionListado[]>([]);
  const [cargando, setCargando] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [sesionExpirada, setSesionExpirada] = useState(false);

  const cargar = useCallback(async () => {
    if (!token || !sesionId) return;
    setCargando(true);
    setError(null);
    setSesionExpirada(false);
    try {
      const datos = await listarEquiposSesionApi(token, sesionId);
      setEquipos(datos);
    } catch (e) {
      if (e instanceof ErrorCrearEquipo) {
        if (e.codigo === "NO_AUTORIZADO") setSesionExpirada(true);
        setError(e.message);
      } else if (e instanceof Error) {
        setError(e.message);
      } else {
        setError("No fue posible cargar los equipos.");
      }
      setEquipos([]);
    } finally {
      setCargando(false);
    }
  }, [token, sesionId]);

  useEffect(() => {
    if (token && sesionId) cargar();
  }, [token, sesionId, cargar]);

  return { equipos, cargando, error, sesionExpirada, refrescar: cargar };
}
