import type { ReactNode } from 'react'
import { Boton } from './Boton'

export interface ColumnaTabla<T> {
  clave: string
  encabezado: string
  // Si se provee, se renderiza con esta función; si no, se intenta usar `clave` directamente.
  render?: (fila: T, indice: number) => ReactNode
  ordenable?: boolean
}

export type DireccionOrden = 'asc' | 'desc' | null

interface Props<T> {
  columnas: ColumnaTabla<T>[]
  filas: T[]
  obtenerId: (fila: T) => string
  estadoCarga?: 'cargando' | 'error' | 'vacio' | 'listo'
  mensajeError?: string
  mensajeVacio?: string
  // Numeración por posición considerando paginación (HU07).
  inicioNumeracion?: number
  mostrarColumnaNumero?: boolean
  // Acción "Ver perfil" — el padre decide a dónde navegar.
  alVerPerfil?: (fila: T) => void
  // Ordenamiento controlado por el padre.
  columnaOrdenada?: string
  direccionOrden?: DireccionOrden
  alOrdenar?: (clave: string) => void
}

export function TablaUsuarios<T>({
  columnas,
  filas,
  obtenerId,
  estadoCarga = 'listo',
  mensajeError,
  mensajeVacio = 'No hay registros para mostrar.',
  inicioNumeracion,
  mostrarColumnaNumero = false,
  alVerPerfil,
  columnaOrdenada,
  direccionOrden,
  alOrdenar
}: Props<T>) {
  const totalColumnas =
    columnas.length + (mostrarColumnaNumero ? 1 : 0) + (alVerPerfil ? 1 : 0)

  return (
    <div className="tabla-contenedor">
      <table className="tabla-usuarios">
        <thead>
          <tr>
            {mostrarColumnaNumero && <th className="columna-numero">#</th>}
            {columnas.map((columna) => {
              const activo = columnaOrdenada === columna.clave
              const flecha = activo ? (direccionOrden === 'asc' ? ' ▲' : direccionOrden === 'desc' ? ' ▼' : '') : ''
              return (
                <th key={columna.clave}>
                  {columna.ordenable && alOrdenar ? (
                    <button
                      type="button"
                      className={`encabezado-ordenable${activo ? ' encabezado-ordenable-activo' : ''}`}
                      onClick={() => alOrdenar(columna.clave)}
                    >
                      {columna.encabezado}{flecha}
                    </button>
                  ) : (
                    columna.encabezado
                  )}
                </th>
              )
            })}
            {alVerPerfil && <th className="columna-acciones">Acciones</th>}
          </tr>
        </thead>
        <tbody>
          {estadoCarga === 'cargando' && (
            <tr>
              <td colSpan={totalColumnas} className="celda-estado">
                Cargando registros…
              </td>
            </tr>
          )}
          {estadoCarga === 'error' && (
            <tr>
              <td colSpan={totalColumnas} className="celda-estado celda-estado-error">
                {mensajeError ?? 'No fue posible cargar los registros.'}
              </td>
            </tr>
          )}
          {estadoCarga === 'vacio' && (
            <tr>
              <td colSpan={totalColumnas} className="celda-estado">
                {mensajeVacio}
              </td>
            </tr>
          )}
          {estadoCarga === 'listo' && filas.map((fila, indice) => (
            <tr key={obtenerId(fila)}>
              {mostrarColumnaNumero && (
                <td className="columna-numero">{(inicioNumeracion ?? 1) + indice}</td>
              )}
              {columnas.map((columna) => (
                <td key={columna.clave}>
                  {columna.render
                    ? columna.render(fila, indice)
                    : ((fila as unknown as Record<string, ReactNode>)[columna.clave] ?? '—')}
                </td>
              ))}
              {alVerPerfil && (
                <td className="columna-acciones">
                  <Boton variante="fantasma" onClick={() => alVerPerfil(fila)}>
                    Ver perfil
                  </Boton>
                </td>
              )}
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  )
}
