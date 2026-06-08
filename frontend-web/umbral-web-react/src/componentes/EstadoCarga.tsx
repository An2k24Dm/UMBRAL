interface Props {
  mensaje?: string
}

export function EstadoCarga({ mensaje = 'Cargando…' }: Props) {
  return (
    <div className="estado-carga" role="status" aria-label={mensaje}>
      <span className="estado-carga-circulo" aria-hidden="true" />
      <span>{mensaje}</span>
    </div>
  )
}
