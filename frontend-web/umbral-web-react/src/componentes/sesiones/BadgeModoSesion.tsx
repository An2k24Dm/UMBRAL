interface Props {
  modo?: string | null
}

// Badge compacto para mostrar el modo/tipo de sesión en tablas.
// Reusa las clases existentes del panel (badge + variantes neutras).
export function BadgeModoSesion({ modo }: Props) {
  const valor = (modo ?? '').toString().trim()
  if (!valor) return <span className="badge badge-neutro">No disponible</span>
  const clase = valor === 'Grupal' ? 'badge-modo-grupal' : 'badge-modo-individual'
  return <span className={`badge ${clase}`}>{valor}</span>
}
