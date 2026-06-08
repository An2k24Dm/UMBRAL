import { useCallback, useEffect, useState } from "react";
import { useAutenticacion } from "../autenticacion/ContextoAutenticacion";
import {
  ErrorConsultaSesiones,
  obtenerDetalleSesionDisponibleApi,
} from "../servicios/sesionesApi";
import type { SesionDetalleMovilDto } from "../tipos/sesiones";

interface EstadoUseDetalleSesionDisponible {
  detalle: SesionDetalleMovilDto | null;
  cargando: boolean;
  error: string | null;
  // 404 → la sesión ya no está disponible (Finalizada/Pausada/Cancelada
  // o id inexistente). La pantalla debe ofrecer volver al listado.
  sesionNoDisponible: boolean;
  sesionExpirada: boolean;
  refrescar: () => Promise<void>;
}

export function useDetalleSesionDisponible(
  sesionId: string | null | undefined,
): EstadoUseDetalleSesionDisponible {
  const { sesion } = useAutenticacion();
  const token = sesion?.tokenAcceso ?? null;

  const [detalle, setDetalle] = useState<SesionDetalleMovilDto | null>(null);
  const [cargando, setCargando] = useState<boolean>(false);
  const [error, setError] = useState<string | null>(null);
  const [sesionNoDisponible, setSesionNoDisponible] = useState<boolean>(false);
  const [sesionExpirada, setSesionExpirada] = useState<boolean>(false);

  const cargar = useCallback(async () => {
    if (!token || !sesionId) return;
    setCargando(true);
    setError(null);
    setSesionNoDisponible(false);
    setSesionExpirada(false);
    try {
      const datos = await obtenerDetalleSesionDisponibleApi(token, sesionId);
      setDetalle(datos);
    } catch (e) {
      if (e instanceof ErrorConsultaSesiones) {
        if (e.codigo === "NO_AUTORIZADO") setSesionExpirada(true);
        if (e.codigo === "SESION_NO_DISPONIBLE") setSesionNoDisponible(true);
        setError(e.message);
      } else if (e instanceof Error) {
        setError(e.message);
      } else {
        setError("No fue posible consultar el detalle de la sesión.");
      }
      setDetalle(null);
    } finally {
      setCargando(false);
    }
  }, [token, sesionId]);

  useEffect(() => {
    if (token && sesionId) {
      cargar();
    }
  }, [token, sesionId, cargar]);

  return {
    detalle,
    cargando,
    error,
    sesionNoDisponible,
    sesionExpirada,
    refrescar: cargar,
  };
}
