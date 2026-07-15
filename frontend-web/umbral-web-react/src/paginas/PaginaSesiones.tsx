import { useEffect, useMemo, useState } from 'react'
import { useLocation, useNavigate } from 'react-router-dom'
import { LayoutPanel } from '../componentes/LayoutPanel'
import { Alerta } from '../componentes/Alerta'
import { Boton } from '../componentes/Boton'
import { Paginacion } from '../componentes/Paginacion'
import { TablaSesiones } from '../componentes/sesiones/TablaSesiones'
import { usarAutenticacion } from '../autenticacion/ProveedorAutenticacion'
import { useSesiones } from '../hooks/useSesiones'
import { useListadoSesionesTiempoReal } from '../hooks/useListadoSesionesTiempoReal'
import type { EstadoSesion } from '../tipos/sesiones'

// Listado de sesiones con filtros (Estado, Modo, Buscar por nombre).
// La carga vive en `useSesiones` y la tabla en `TablaSesiones`; esta
// página solo arma los filtros + paginación + acciones.

const TAMANIO_PAGINA = 10

const ESTADOS: Array<{ valor: EstadoSesion; etiqueta: string }> = [
  { valor: 'Programada', etiqueta: 'Programada' },
  { valor: 'EnPreparacion', etiqueta: 'En preparación' },
  { valor: 'Activa', etiqueta: 'Activa' },
  { valor: 'Pausada', etiqueta: 'Pausada' },
  { valor: 'Finalizada', etiqueta: 'Finalizada' },
  { valor: 'Cancelada', etiqueta: 'Cancelada' },
]

export function PaginaSesiones() {
  const { token, usuario } = usarAutenticacion()
  const navegar = useNavigate()
  const ubicacion = useLocation()
  const mensajeExito = (ubicacion.state as { mensajeExito?: string } | null)?.mensajeExito
  const esOperador = usuario?.rol === 'Operador'
  const rutaBase =
    usuario?.rol === 'Administrador' ? '/administrador/sesiones' : '/operador/sesiones'

  const [filtroEstado, setFiltroEstado] = useState<EstadoSesion | ''>('')
  const [filtroModo, setFiltroModo] = useState<'' | 'Individual' | 'Grupal'>('')
  const [filtroNombre, setFiltroNombre] = useState('')
  const [pagina, setPagina] = useState(1)

  const { sesiones, cargando, error, refrescar } = useSesiones({ token, estado: filtroEstado })

  // El listado se refresca en vivo cuando cambia el estado de una sesión o su
  // conteo de participantes/equipos (SignalR); si falla, sigue por HTTP.
  useListadoSesionesTiempoReal({ token, onListadoActualizado: refrescar })

  useEffect(() => { setPagina(1) }, [filtroEstado, filtroModo, filtroNombre])

  // Modo y nombre se filtran en memoria porque el backend aún no acepta
  // esos parámetros. Si más adelante los expone, se mueven a la query
  // del hook.
  const sesionesFiltradas = useMemo(() => {
    const nombre = filtroNombre.trim().toLowerCase()
    return sesiones.filter(s => {
      if (filtroModo && s.modo !== filtroModo) return false
      if (nombre && !s.nombre.toLowerCase().includes(nombre)) return false
      return true
    })
  }, [sesiones, filtroModo, filtroNombre])

  const totalElementos = sesionesFiltradas.length
  const totalPaginas = Math.max(1, Math.ceil(totalElementos / TAMANIO_PAGINA))
  const paginaSegura = Math.min(pagina, totalPaginas)
  const inicioNumeracion = (paginaSegura - 1) * TAMANIO_PAGINA + 1

  const sesionesPagina = useMemo(() => {
    const desde = (paginaSegura - 1) * TAMANIO_PAGINA
    return sesionesFiltradas.slice(desde, desde + TAMANIO_PAGINA)
  }, [sesionesFiltradas, paginaSegura])

  const estadoTabla: 'cargando' | 'error' | 'vacio' | 'listo' = cargando
    ? 'cargando'
    : error
      ? 'error'
      : sesionesFiltradas.length === 0
        ? 'vacio'
        : 'listo'

  return (
    <LayoutPanel
      titulo="Sesiones"
      descripcion="Sesiones programadas y en ejecución."
    >
      <section className="seccion">
        <div className="seccion-cabecera">
          <div>
            <h2>Listado de sesiones</h2>
            <p>Puede consultar las sesiones disponibles según su rol.</p>
          </div>
          {esOperador && (
            <div className="cabecera-pagina-acciones">
              <Boton variante="primario" onClick={() => navegar(`${rutaBase}/crear`)}>
                + Crear sesión
              </Boton>
            </div>
          )}
        </div>

        {mensajeExito && <Alerta tono="exito">{mensajeExito}</Alerta>}

        <div className="barra-filtros">
          <div className="campo">
            <label htmlFor="filtro-estado">Estado</label>
            <select
              id="filtro-estado"
              value={filtroEstado}
              onChange={(e) => setFiltroEstado(e.target.value as EstadoSesion | '')}
            >
              <option value="">Todos</option>
              {ESTADOS.map(e => (
                <option key={e.valor} value={e.valor}>{e.etiqueta}</option>
              ))}
            </select>
          </div>
          <div className="campo">
            <label htmlFor="filtro-modo">Tipo de sesión</label>
            <select
              id="filtro-modo"
              value={filtroModo}
              onChange={(e) => setFiltroModo(e.target.value as '' | 'Individual' | 'Grupal')}
            >
              <option value="">Todos</option>
              <option value="Individual">Individual</option>
              <option value="Grupal">Grupal</option>
            </select>
          </div>
          <div className="campo">
            <label htmlFor="filtro-nombre">Buscar por nombre</label>
            <input
              id="filtro-nombre"
              type="search"
              placeholder="Ej. Sesión piloto"
              value={filtroNombre}
              onChange={(e) => setFiltroNombre(e.target.value)}
            />
          </div>
        </div>

        {estadoTabla === 'error' && error && <Alerta tono="error">{error}</Alerta>}

        <TablaSesiones
          sesiones={sesionesPagina}
          inicioNumeracion={inicioNumeracion}
          estado={estadoTabla}
          mensajeError={error ?? undefined}
          alAbrirDetalle={(s) => navegar(`${rutaBase}/${s.id}`)}
        />

        <Paginacion
          pagina={paginaSegura}
          tamanioPagina={TAMANIO_PAGINA}
          total={totalElementos}
          alCambiarPagina={setPagina}
        />
      </section>
    </LayoutPanel>
  )
}
