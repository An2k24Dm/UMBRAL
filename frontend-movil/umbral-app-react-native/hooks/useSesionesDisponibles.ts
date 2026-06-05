import { useCallback, useEffect, useState } from "react";
import { useAutenticacion } from "../autenticacion/ContextoAutenticacion";
import {
  ErrorConsultaSesiones,
  listarSesionesDisponiblesApi,
} from "../servicios/sesionesApi";
import type {
  FiltroModoSesion,
  SesionDisponibleMovilDto,
} from "../tipos/sesiones";

// Estado del listado móvil de sesiones disponibles. La UI mantiene los
// filtros (búsqueda + modo) en su propio estado y los pasa al hook; el
// hook decide cuándo llamar al backend.
interface EstadoUseSesionesDisponibles {
  sesiones: SesionDisponibleMovilDto[];
  cargando: boolean;
  error: string | null;
  // Flag para que la pantalla pueda redirigir al login. NO se cierra
  // sesión desde dentro del hook: la pantalla decide cómo reaccionar
  // para mantener el comportamiento consistente con el resto de la app.
  sesionExpirada: boolean;
  refrescar: () => Promise<void>;
}

interface OpcionesUseSesionesDisponibles {
  busqueda: string;
  modo: FiltroModoSesion;
}

export function useSesionesDisponibles(
  opciones: OpcionesUseSesionesDisponibles,
): EstadoUseSesionesDisponibles {
  const { sesion } = useAutenticacion();
  const token = sesion?.tokenAcceso ?? null;

  const [sesiones, setSesiones] = useState<SesionDisponibleMovilDto[]>([]);
  const [cargando, setCargando] = useState<boolean>(false);
  const [error, setError] = useState<string | null>(null);
  const [sesionExpirada, setSesionExpirada] = useState<boolean>(false);

  const { busqueda, modo } = opciones;

  const cargar = useCallback(async () => {
    if (!token) return;
    setCargando(true);
    setError(null);
    setSesionExpirada(false);
    try {
      const datos = await listarSesionesDisponiblesApi(token, {
        busqueda,
        modo,
      });
      setSesiones(datos);
    } catch (e) {
      if (e instanceof ErrorConsultaSesiones) {
        if (e.codigo === "NO_AUTORIZADO") {
          setSesionExpirada(true);
        }
        setError(e.message);
      } else if (e instanceof Error) {
        setError(e.message);
      } else {
        setError("No fue posible consultar las sesiones.");
      }
      // Vaciar la lista en caso de error evita que se muestren datos
      // viejos junto a un mensaje de error nuevo.
      setSesiones([]);
    } finally {
      setCargando(false);
    }
  }, [token, busqueda, modo]);

  useEffect(() => {
    if (token) {
      cargar();
    }
  }, [token, cargar]);

  return {
    sesiones,
    cargando,
    error,
    sesionExpirada,
    refrescar: cargar,
  };
}
