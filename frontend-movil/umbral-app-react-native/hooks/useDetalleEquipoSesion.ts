import { useCallback, useEffect, useState } from "react";
import { useAutenticacion } from "../autenticacion/ContextoAutenticacion";
import {
  ErrorCrearEquipo,
  obtenerDetalleEquipoSesionApi,
} from "../servicios/equiposApi";
import type { EquipoSesionDetalle } from "../tipos/equipos";

interface EstadoUseDetalleEquipoSesion {
  equipo: EquipoSesionDetalle | null;
  cargando: boolean;
  error: string | null;
  sesionExpirada: boolean;
  refrescar: () => Promise<void>;
}

// HU43 — Detalle de un equipo de una sesión.
export function useDetalleEquipoSesion(
  sesionId: string | null | undefined,
  equipoId: string | null | undefined,
): EstadoUseDetalleEquipoSesion {
  const { sesion } = useAutenticacion();
  const token = sesion?.tokenAcceso ?? null;

  const [equipo, setEquipo] = useState<EquipoSesionDetalle | null>(null);
  const [cargando, setCargando] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [sesionExpirada, setSesionExpirada] = useState(false);

  const cargar = useCallback(async () => {
    if (!token || !sesionId || !equipoId) return;
    setCargando(true);
    setError(null);
    setSesionExpirada(false);
    try {
      const datos = await obtenerDetalleEquipoSesionApi(token, sesionId, equipoId);
      setEquipo(datos);
    } catch (e) {
      if (e instanceof ErrorCrearEquipo) {
        if (e.codigo === "NO_AUTORIZADO") setSesionExpirada(true);
        setError(e.message);
      } else if (e instanceof Error) {
        setError(e.message);
      } else {
        setError("No fue posible cargar el equipo.");
      }
      setEquipo(null);
    } finally {
      setCargando(false);
    }
  }, [token, sesionId, equipoId]);

  useEffect(() => {
    if (token && sesionId && equipoId) cargar();
  }, [token, sesionId, equipoId, cargar]);

  return { equipo, cargando, error, sesionExpirada, refrescar: cargar };
}
