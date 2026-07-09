import { useCallback, useEffect, useRef, useState } from "react";
import { useAutenticacion } from "../autenticacion/ContextoAutenticacion";
import {
  ErrorConsultaSesiones,
  obtenerDetalleSesionDisponibleApi,
} from "../servicios/sesionesApi";
import type { EstadoSesion, SesionDetalleMovilDto } from "../tipos/sesiones";

interface EstadoUseDetalleSesionDisponible {
  detalle: SesionDetalleMovilDto | null;
  cargando: boolean;
  error: string | null;
  sesionNoDisponible: boolean;
  sesionExpirada: boolean;
  refrescar: () => Promise<void>;
  // Actualiza el estado en detalle localmente sin refetch (para eventos SignalR).
  actualizarEstadoLocal: (estado: string) => void;
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

  // Rastrea el último estado conocido para no limpiar detalle al recibir 404
  // después de que la sesión ya fue marcada como Finalizada.
  const ultimoEstadoRef = useRef<string | null>(null);

  const actualizarEstadoLocal = useCallback((nuevoEstado: string) => {
    ultimoEstadoRef.current = nuevoEstado;
    setDetalle((prev) =>
      prev ? { ...prev, estado: nuevoEstado as EstadoSesion } : prev,
    );
  }, []);

  const cargar = useCallback(async () => {
    if (!token || !sesionId) return;
    setCargando(true);
    setError(null);
    setSesionNoDisponible(false);
    setSesionExpirada(false);
    try {
      const datos = await obtenerDetalleSesionDisponibleApi(token, sesionId);
      ultimoEstadoRef.current = datos.estado;
      setDetalle(datos);
    } catch (e) {
      if (e instanceof ErrorConsultaSesiones) {
        if (e.codigo === "NO_AUTORIZADO") {
          setSesionExpirada(true);
          setDetalle(null);
        } else if (e.codigo === "SESION_NO_DISPONIBLE") {
          if (ultimoEstadoRef.current === "Finalizada") {
            // La sesión ya fue finalizada — preservamos el detalle que tenemos
            // para que el participante pueda ver sus resultados.
          } else {
            setSesionNoDisponible(true);
            setError(e.message);
            setDetalle(null);
          }
        } else {
          setError(e.message);
          setDetalle(null);
        }
      } else if (e instanceof Error) {
        setError(e.message);
        setDetalle(null);
      } else {
        setError("No fue posible consultar el detalle de la sesión.");
        setDetalle(null);
      }
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
    actualizarEstadoLocal,
  };
}
