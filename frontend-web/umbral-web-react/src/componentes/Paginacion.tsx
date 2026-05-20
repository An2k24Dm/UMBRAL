import { Boton } from './Boton'

interface Props {
  pagina: number
  tamanioPagina: number
  total: number
  alCambiarPagina: (pagina: number) => void
}

export function Paginacion({ pagina, tamanioPagina, total, alCambiarPagina }: Props) {
  const totalPaginas = Math.max(1, Math.ceil(total / tamanioPagina))
  const haySiguiente = pagina < totalPaginas
  const hayAnterior = pagina > 1

  const desde = total === 0 ? 0 : (pagina - 1) * tamanioPagina + 1
  const hasta = Math.min(pagina * tamanioPagina, total)

  return (
    <div className="paginacion">
      <span className="paginacion-info">
        {total === 0
          ? 'Sin registros'
          : `Mostrando ${desde}-${hasta} de ${total}`}
      </span>
      <div className="paginacion-controles">
        <Boton variante="secundario" disabled={!hayAnterior} onClick={() => alCambiarPagina(pagina - 1)}>
          Anterior
        </Boton>
        <span className="paginacion-pagina">Página {pagina} de {totalPaginas}</span>
        <Boton variante="secundario" disabled={!haySiguiente} onClick={() => alCambiarPagina(pagina + 1)}>
          Siguiente
        </Boton>
      </div>
    </div>
  )
}
