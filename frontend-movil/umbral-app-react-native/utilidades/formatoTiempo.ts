// Formatea una duración en segundos como un texto corto y legible.
// Reglas de negocio (sección 18 del ERS):
//   * < 60 s → "N s"
//   * múltiplo exacto de minuto → "N min"
//   * mixto → "M min S s"
// Los segundos negativos o no finitos se muestran como "—" para no
// confundir al Participante con un dato corrupto.

export function formatearSegundos(segundos?: number | null): string {
  if (
    segundos === null ||
    segundos === undefined ||
    !Number.isFinite(segundos) ||
    segundos < 0
  ) {
    return "—";
  }

  const total = Math.round(segundos);
  if (total < 60) return `${total} s`;

  const minutos = Math.floor(total / 60);
  const restoSegundos = total % 60;
  if (restoSegundos === 0) return `${minutos} min`;
  return `${minutos} min ${restoSegundos} s`;
}
