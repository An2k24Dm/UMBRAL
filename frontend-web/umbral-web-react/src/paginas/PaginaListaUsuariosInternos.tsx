import { useEffect, useMemo, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { LayoutPanel } from '../componentes/LayoutPanel'
import { TablaUsuarios, type ColumnaTabla } from '../componentes/TablaUsuarios'
import { Paginacion } from '../componentes/Paginacion'
import { BadgeEstado } from '../componentes/BadgeEstado'
import { CampoFormulario } from '../componentes/CampoFormulario'
import { Alerta } from '../componentes/Alerta'
import { obtenerUsuariosInternos } from '../autenticacion/clienteApi'
import { usarAutenticacion } from '../autenticacion/ProveedorAutenticacion'
import type {
  FiltroRolInterno,
  OrdenEstado,
  ResultadoPaginado,
  UsuarioListadoInterno
} from '../autenticacion/tipos'

// HU08 — listar Operadores y Administradores. Solo Administrador.
// La columna "Código" resuelve entre codigoOperador / codigoAdministrador.

const TAMANIO_PAGINA = 10

function codigoColumna(fila: UsuarioListadoInterno): string {
  if (fila.rol === 'Operador') return (fila.codigoOperador ?? '').trim() || '—'
  if (fila.rol === 'Administrador') return (fila.codigoAdministrador ?? '').trim() || '—'
  return '—'
}

function noDisponible(valor: string | null | undefined): string {
  const v = (valor ?? '').toString().trim()
  return v.length > 0 ? v : '—'
}

export function PaginaListaUsuariosInternos() {
  const { token } = usarAutenticacion()
  const navegar = useNavigate()

  const [pagina, setPagina] = useState(1)
  const [filtroRol, setFiltroRol] = useState<FiltroRolInterno>('Todos')
  const [ordenEstado, setOrdenEstado] = useState<OrdenEstado>(null)

  const [estado, setEstado] = useState<'cargando' | 'error' | 'vacio' | 'listo'>('cargando')
  const [mensajeError, setMensajeError] = useState<string | null>(null)
  const [resultado, setResultado] = useState<ResultadoPaginado<UsuarioListadoInterno> | null>(null)

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
        const respuesta = await obtenerUsuariosInternos(
          {
            pagina,
            tamanioPagina: TAMANIO_PAGINA,
            rol: filtroRol,
            ordenEstado
          },
          token
        )
        if (cancelado) return
        setResultado(respuesta)
        setEstado(respuesta.elementos.length === 0 ? 'vacio' : 'listo')
      } catch (e) {
        if (cancelado) return
        const mensaje = e instanceof Error ? e.message : 'No fue posible consultar los usuarios internos.'
        setMensajeError(mensaje)
        setEstado('error')
      }
    }
    cargar()
    return () => { cancelado = true }
  }, [token, pagina, filtroRol, ordenEstado])

  const columnas: ColumnaTabla<UsuarioListadoInterno>[] = useMemo(() => [
    { clave: 'codigo', encabezado: 'Código', render: (f) => codigoColumna(f) },
    { clave: 'nombreUsuario', encabezado: 'Username', render: (f) => f.nombreUsuario },
    { clave: 'nombre', encabezado: 'Nombre', render: (f) => f.nombre },
    { clave: 'apellido', encabezado: 'Apellido', render: (f) => f.apellido },
    { clave: 'rol', encabezado: 'Rol', render: (f) => f.rol },
    {
      clave: 'estado',
      encabezado: 'Estado',
      ordenable: true,
      render: (f) => <BadgeEstado estado={f.estado} />
    },
    { clave: 'sexo', encabezado: 'Sexo', render: (f) => noDisponible(f.sexo) }
  ], [])

  const alOrdenar = (clave: string) => {
    if (clave !== 'estado') return
    setOrdenEstado((actual) => (actual === 'asc' ? 'desc' : actual === 'desc' ? null : 'asc'))
    setPagina(1)
  }

  return (
    <LayoutPanel
      titulo="Operadores y Administradores"
      descripcion="Listado de cuentas internas del sistema."
    >
      <section className="seccion">
        <div className="seccion-cabecera">
          <div>
            <h2>Usuarios internos</h2>
            <p>Filtre por rol y ordene por estado según necesidad.</p>
          </div>
        </div>

        <div className="barra-filtros">
          <CampoFormulario etiqueta="Filtrar por rol" htmlFor="filtroRol">
            <select
              id="filtroRol"
              value={filtroRol}
              onChange={(e) => {
                setFiltroRol(e.target.value as FiltroRolInterno)
                setPagina(1)
              }}
            >
              <option value="Todos">Todos</option>
              <option value="Operador">Operadores</option>
              <option value="Administrador">Administradores</option>
            </select>
          </CampoFormulario>
        </div>

        {estado === 'error' && mensajeError && <Alerta tono="error">{mensajeError}</Alerta>}

        <TablaUsuarios
          columnas={columnas}
          filas={resultado?.elementos ?? []}
          obtenerId={(f) => f.id}
          estadoCarga={estado}
          mensajeError={mensajeError ?? undefined}
          mensajeVacio="No se encontraron usuarios con los criterios seleccionados."
          alVerPerfil={(f) => navegar(`/administrador/usuarios/internos/${f.id}`)}
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
