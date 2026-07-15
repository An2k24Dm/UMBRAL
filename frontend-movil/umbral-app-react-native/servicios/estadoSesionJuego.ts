export type EstadoSesionJuego =
  | "Activa"
  | "Pausada"
  | "Cancelada"
  | "Finalizada"
  | "EnPreparacion"
  | "Desconocida";

export function mapearEstadoSesionJuego(
  estado: string | undefined | null,
): EstadoSesionJuego {
  switch (estado) {
    case "Activa":
      return "Activa";
    case "Pausada":
      return "Pausada";
    case "Cancelada":
      return "Cancelada";
    case "Finalizada":
      return "Finalizada";
    case "EnPreparacion":
      return "EnPreparacion";
    default:
      return "Desconocida";
  }
}
