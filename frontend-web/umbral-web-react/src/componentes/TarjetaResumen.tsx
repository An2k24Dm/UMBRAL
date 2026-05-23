import type { ReactNode } from 'react'
import { Link } from 'react-router-dom'

interface Props {
  titulo: string
  descripcion: string
  destino?: string
  textoAccion?: string
  deshabilitado?: boolean
  insignia?: string
  icono?: ReactNode
}

export function TarjetaResumen({
  titulo,
  descripcion,
  destino,
  textoAccion = 'Abrir',
  deshabilitado,
  insignia,
  icono
}: Props) {
  return (
    <article className={`tarjeta-resumen${deshabilitado ? ' tarjeta-deshabilitada' : ''}`}>
      <header className="tarjeta-resumen-cabecera">
        {icono && <span className="tarjeta-resumen-icono" aria-hidden>{icono}</span>}
        <h3>{titulo}</h3>
        {insignia && <span className="tarjeta-resumen-insignia">{insignia}</span>}
      </header>
      <p className="tarjeta-resumen-descripcion">{descripcion}</p>
      {destino && !deshabilitado ? (
        <Link to={destino} className="boton-ui boton-primario tarjeta-resumen-accion">
          {textoAccion}
        </Link>
      ) : (
        <span className="tarjeta-resumen-pendiente">No disponible</span>
      )}
    </article>
  )
}
