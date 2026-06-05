import type { ErrorCampo } from "../tipos/errores";

export const URL_API: string =
  process.env.EXPO_PUBLIC_API_URL ?? "http://localhost:5000";

export function obtenerEncabezadosBase(): Record<string, string> {
  return { "Content-Type": "application/json" };
}

export function obtenerEncabezadosAutenticados(
  token: string,
): Record<string, string> {
  return {
    ...obtenerEncabezadosBase(),
    Authorization: `Bearer ${token}`,
  };
}

export interface CuerpoErrorBackend {
  codigo?: string;
  mensaje?: string;
  errores?: ErrorCampo[];
}

export async function leerCuerpoError(
  respuesta: Response,
): Promise<CuerpoErrorBackend | null> {
  return (await respuesta.json().catch(() => null)) as CuerpoErrorBackend | null;
}

export function construirUrl(ruta: string): string {
  const rutaNormalizada = ruta.startsWith("/") ? ruta : `/${ruta}`;
  return `${URL_API}${rutaNormalizada}`;
}
