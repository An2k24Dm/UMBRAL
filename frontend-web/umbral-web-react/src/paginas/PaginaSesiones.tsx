import { useEffect, useMemo, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { LayoutPanel } from '../componentes/LayoutPanel'
import { Alerta } from '../componentes/Alerta'
import { Boton } from '../componentes/Boton'
import { TablaUsuarios, type ColumnaTabla } from '../componentes/TablaUsuarios'
import { Paginacion } from '../componentes/Paginacion'
import { BadgeEstadoSesion } from '../componentes/BadgeEstadoSesion'
import {
  listarSesiones,
  type EstadoSesionApi,
  type FiltrosListadoSesiones,
  type SesionListadoDto,
  type TipoJuegoSesion
} from '../autenticacion/clienteApiSesiones'
import { usarAutenticacion } from '../autenticacion/ProveedorAutenticacion'
import {
  formatearFechaSesion,
  nombreTipoJuego
} from '../utilidades/formatoSesiones'

// HU34 — Listado de sesiones rediseñado para alinearse con la pantalla
// de Participantes registrados: cabecera de página, filtros estilizados,
// tabla con encabezados, badge por estado, acción "Ver detalle" y
// paginación al pie de la tarjeta.
//
// La visibilidad real la decide el backend (Administrador ve todo,
// Operador ve propias + creadas por algún Administrador). El frontend
// se limita a renderizar lo que recibe y a paginar/filtrar visualmente.

const TAMANIO_PAGINA = 10

const TIPOS_JUEGO: Array<{ valor: TipoJuegoSesion; etiqueta: string }> = [
  { valor: 'Trivia', etiqueta: 'Trivia' },
  { valor: 'BusquedaTesoro', etiqueta: 'Búsqueda del Tesoro' }
]

const ESTADOS: Array<{ valor: EstadoSesionApi; etiqueta: string }> = [
  { valor: 'Programada', etiqueta: 'Programada' },
  { valor: 'EnPreparacion', etiqueta: 'En preparación' },
  { valor: 'Activa', etiqueta: 'Activa' },
  { valor: 'Pausada', etiqueta: 'Pausada' },
  { valor: 'Finalizada', etiqueta: 'Finalizada' },
  { valor: 'Cancelada', etiqueta: 'Cancelada' }
]

export function PaginaSesiones() {
  const { token, usuario } = usarAutenticacion()
  const navegar = useNavigate()
  const rutaBase = usuario?.rol === 'Administrador'
    ? '/administrador/sesiones'
    : '/operador/sesiones'

  const [estado, setEstado] = useState<'cargando' | 'error' | 'vacio' | 'listo'>('cargando')
  const [mensajeError, setMensajeError] = useState<string | null>(null)
  const [sesiones, setSesiones] = useState<SesionListadoDto[]>([])

  const [filtroTipo, setFiltroTipo] = useState<TipoJuegoSesion | ''>('')
  const [filtroEstado, setFiltroEstado] = useState<EstadoSesionApi | ''>('')
  const [pagina, setPagina] = useState(1)

  // Recargar cuando cambien filtros. El backend ya soporta query
  // params, así que mandamos los filtros allá. La paginación es por
  // ahora en frontend porque el endpoint no expone aún el total.
  useEffect(() => {
    const ref = { cancelado: false }
    async function cargar() {
      if (!token) { setEstado('error'); setMensajeError('Debe iniciar sesión.'); return }
      setEstado('cargando')
      setMensajeError(null)
      const filtros: FiltrosListadoSesiones = {}
      if (filtroTipo) filtros.tipoJuego = filtroTipo
      if (filtroEstado) filtros.estado = filtroEstado
      try {
        const lista = await listarSesiones(token, filtros)
        if (ref.cancelado) return
        setSesiones(lista)
        setEstado(lista.length === 0 ? 'vacio' : 'listo')
      } catch (e) {
        if (ref.cancelado) return
        setMensajeError(e instanceof Error ? e.message : 'No se pudieron cargar las sesiones.')
        setEstado('error')
      }
    }
    cargar()
    return () => { ref.cancelado = true }
  }, [token, filtroTipo, filtroEstado])

  // Cuando cambian los filtros, volver a página 1.
  useEffect(() => { setPagina(1) }, [filtroTipo, filtroEstado])

  const totalElementos = sesiones.length
  const totalPaginas = Math.max(1, Math.ceil(totalElementos / TAMANIO_PAGINA))
  const paginaSegura = Math.min(pagina, totalPaginas)
  const inicioNumeracion = (paginaSegura - 1) * TAMANIO_PAGINA + 1

  const sesionesPagina = useMemo(() => {
    const desde = (paginaSegura - 1) * TAMANIO_PAGINA
    return sesiones.slice(desde, desde + TAMANIO_PAGINA)
  }, [sesiones, paginaSegura])

  const columnas: ColumnaTabla<SesionListadoDto>[] = [
    { clave: 'nombre', encabezado: 'Nombre', render: (s) => s.nombre },
    {
      clave: 'tipoJuego',
      encabezado: 'Tipo de juego',
      render: (s) => nombreTipoJuego(s.tipoJuego)
    },
    { clave: 'modo', encabezado: 'Modo', render: (s) => s.modo },
    {
      clave: 'estado',
      encabezado: 'Estado',
      render: (s) => <BadgeEstadoSesion estado={s.estado} />
    },
    {
      clave: 'fechaProgramada',
      encabezado: 'Fecha de programación',
      render: (s) => formatearFechaSesion(s.fechaProgramada)
    }
  ]

  return (
    <LayoutPanel
      titulo="Sesiones"
      descripcion="Sesiones programadas y en ejecución."
    >
      <section className="seccion">
        <div className="seccion-cabecera">
          <div>
            <h2>Listado de sesiones</h2>
            <p>Puede consultar y gestionar sesiones programadas o en ejecución.</p>
          </div>
          <div className="cabecera-pagina-acciones">
            <Boton variante="primario" onClick={() => navegar(`${rutaBase}/crear`)}>
              + Crear sesión
            </Boton>
          </div>
        </div>

        <div className="barra-filtros">
          <div className="campo">
            <label htmlFor="filtro-tipo">Tipo de juego</label>
            <select
              id="filtro-tipo"
              value={filtroTipo}
              onChange={(e) => setFiltroTipo(e.target.value as TipoJuegoSesion | '')}
            >
              <option value="">Todos</option>
              {TIPOS_JUEGO.map(t => (
                <option key={t.valor} value={t.valor}>{t.etiqueta}</option>
              ))}
            </select>
          </div>
          <div className="campo">
            <label htmlFor="filtro-estado">Estado</label>
            <select
              id="filtro-estado"
              value={filtroEstado}
              onChange={(e) => setFiltroEstado(e.target.value as EstadoSesionApi | '')}
            >
              <option value="">Todos</option>
              {ESTADOS.map(e => (
                <option key={e.valor} value={e.valor}>{e.etiqueta}</option>
              ))}
            </select>
          </div>
        </div>

        {estado === 'error' && mensajeError && <Alerta tono="error">{mensajeError}</Alerta>}

        <TablaUsuarios<SesionListadoDto>
          columnas={columnas}
          filas={sesionesPagina}
          obtenerId={(s) => s.id}
          estadoCarga={estado}
          mensajeError={mensajeError ?? undefined}
          mensajeVacio="No hay sesiones disponibles con los filtros actuales."
          inicioNumeracion={inicioNumeracion}
          mostrarColumnaNumero
          alVerPerfil={(s) => navegar(`${rutaBase}/${s.id}`)}
          etiquetaAccion="Ver detalle"
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
