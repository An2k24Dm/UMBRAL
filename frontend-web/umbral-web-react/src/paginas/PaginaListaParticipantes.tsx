import { useEffect, useMemo, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { LayoutPanel } from '../componentes/LayoutPanel'
import { TablaUsuarios, type ColumnaTabla } from '../componentes/TablaUsuarios'
import { Paginacion } from '../componentes/Paginacion'
import { BadgeEstado } from '../componentes/BadgeEstado'
import { CampoFormulario } from '../componentes/CampoFormulario'
import { Alerta } from '../componentes/Alerta'
import { Boton } from '../componentes/Boton'
import { obtenerParticipantes } from '../autenticacion/clienteApi'
import { usarAutenticacion } from '../autenticacion/ProveedorAutenticacion'
import type {
  OrdenEstado,
  ResultadoPaginado,
  UsuarioListadoParticipante
} from '../autenticacion/tipos'

// HU07 — listar Participantes. Visible para Administrador y Operador.
// La columna "Número" es la posición en la página (no es el id).

const TAMANIO_PAGINA = 10

interface Props {
  // Cada rol navega a su propia ruta de detalle.
  rutaBaseDetalle: string
}

function noDisponible(valor: string | null | undefined): string {
  const v = (valor ?? '').toString().trim()
  return v.length > 0 ? v : '—'
}

export function PaginaListaParticipantes({ rutaBaseDetalle }: Props) {
  const { token } = usarAutenticacion()
  const navegar = useNavigate()

  const [pagina, setPagina] = useState(1)
  const [busqueda, setBusqueda] = useState('')
  const [busquedaAplicada, setBusquedaAplicada] = useState('')
  const [ordenEstado, setOrdenEstado] = useState<OrdenEstado>(null)

  const [estado, setEstado] = useState<'cargando' | 'error' | 'vacio' | 'listo'>('cargando')
  const [mensajeError, setMensajeError] = useState<string | null>(null)
  const [resultado, setResultado] = useState<ResultadoPaginado<UsuarioListadoParticipante> | null>(null)

  useEffect(() => {
    let cancelado = false
    async function cargar() {
      if (!token) {
        setEstado('error')
        setMensajeError('Debe iniciar sesión.')
        return
      }
      setEstado('cargando')
      setMensajeError(null)
      try {
        const respuesta = await obtenerParticipantes(
          {
            pagina,
            tamanioPagina: TAMANIO_PAGINA,
            busqueda: busquedaAplicada || undefined,
            ordenEstado
          },
          token
        )
        if (cancelado) return
        setResultado(respuesta)
        setEstado(respuesta.elementos.length === 0 ? 'vacio' : 'listo')
      } catch (e) {
        if (cancelado) return
        const mensaje = e instanceof Error ? e.message : 'No fue posible consultar los participantes.'
        setMensajeError(mensaje)
        setEstado('error')
      }
    }
    cargar()
    return () => { cancelado = true }
  }, [token, pagina, busquedaAplicada, ordenEstado])

  const inicioNumeracion = useMemo(
    () => (pagina - 1) * TAMANIO_PAGINA + 1,
    [pagina]
  )

  const columnas: ColumnaTabla<UsuarioListadoParticipante>[] = [
    { clave: 'alias', encabezado: 'Alias', render: (f) => noDisponible(f.alias) },
    { clave: 'nombreUsuario', encabezado: 'Username', render: (f) => f.nombreUsuario },
    { clave: 'nombre', encabezado: 'Nombre', render: (f) => f.nombre },
    { clave: 'apellido', encabezado: 'Apellido', render: (f) => f.apellido },
    {
      clave: 'estado',
      encabezado: 'Estado',
      ordenable: true,
      render: (f) => <BadgeEstado estado={f.estado} />
    },
    { clave: 'sexo', encabezado: 'Sexo', render: (f) => noDisponible(f.sexo) }
  ]

  const alOrdenar = (clave: string) => {
    if (clave !== 'estado') return
    setOrdenEstado((actual) => (actual === 'asc' ? 'desc' : actual === 'desc' ? null : 'asc'))
    setPagina(1)
  }

  const aplicarBusqueda = () => {
    setPagina(1)
    setBusquedaAplicada(busqueda.trim())
  }

  const limpiarBusqueda = () => {
    setBusqueda('')
    setBusquedaAplicada('')
    setPagina(1)
  }

  return (
    <LayoutPanel titulo="Participantes" descripcion="Listado de cuentas con rol Participante.">
      <section className="seccion">
        <div className="seccion-cabecera">
          <div>
            <h2>Participantes registrados</h2>
            <p>Use la búsqueda y el ordenamiento para localizar un participante específico.</p>
          </div>
        </div>

        <div className="barra-filtros">
          <CampoFormulario etiqueta="Buscar" htmlFor="busqueda" opcional="username, alias, nombre o apellido">
            <input
              id="busqueda"
              value={busqueda}
              onChange={(e) => setBusqueda(e.target.value)}
              onKeyDown={(e) => { if (e.key === 'Enter') aplicarBusqueda() }}
              placeholder="Escriba para filtrar…"
            />
          </CampoFormulario>
          <Boton variante="primario" onClick={aplicarBusqueda}>Buscar</Boton>
          <Boton variante="secundario" onClick={limpiarBusqueda} disabled={!busquedaAplicada && !busqueda}>
            Limpiar
          </Boton>
        </div>

        {estado === 'error' && mensajeError && <Alerta tono="error">{mensajeError}</Alerta>}

        <TablaUsuarios
          columnas={columnas}
          filas={resultado?.elementos ?? []}
          obtenerId={(f) => f.id}
          estadoCarga={estado}
          mensajeError={mensajeError ?? undefined}
          mensajeVacio="No se encontraron participantes con los criterios seleccionados."
          inicioNumeracion={inicioNumeracion}
          mostrarColumnaNumero
          alVerPerfil={(f) => navegar(`${rutaBaseDetalle}/${f.id}`)}
          columnaOrdenada="estado"
          direccionOrden={ordenEstado}
          alOrdenar={alOrdenar}
        />

        <Paginacion
          pagina={pagina}
          tamanioPagina={TAMANIO_PAGINA}
          total={resultado?.total ?? 0}
          alCambiarPagina={setPagina}
        />
      </section>
    </LayoutPanel>
  )
}
