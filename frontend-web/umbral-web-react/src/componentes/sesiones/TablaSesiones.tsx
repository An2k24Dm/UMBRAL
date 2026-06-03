import { TablaUsuarios, type ColumnaTabla } from '../TablaUsuarios'
import { BadgeEstadoSesion } from '../BadgeEstadoSesion'
import { BadgeModoSesion } from './BadgeModoSesion'
import { formatearFechaSesion } from '../../utilidades/formatoSesiones'
import type { SesionListadoDto } from '../../tipos/sesiones'

interface Props {
  sesiones: SesionListadoDto[]
  inicioNumeracion: number
  estado: 'cargando' | 'error' | 'vacio' | 'listo'
  mensajeError?: string
  mensajeVacio?: string
  alAbrirDetalle: (sesion: SesionListadoDto) => void
}

// Tabla reusable del listado de sesiones. Columnas reducidas según el
// ERS actual: #, Nombre, Tipo de sesión, Estado, Fecha y Acciones.
export function TablaSesiones({
  sesiones,
  inicioNumeracion,
  estado,
  mensajeError,
  mensajeVacio,
  alAbrirDetalle,
}: Props) {
  const columnas: ColumnaTabla<SesionListadoDto>[] = [
    { clave: 'nombre', encabezado: 'Nombre', render: (s) => s.nombre },
    {
      clave: 'modo',
      encabezado: 'Tipo de sesión',
      render: (s) => <BadgeModoSesion modo={s.modo} />,
    },
    {
      clave: 'estado',
      encabezado: 'Estado',
      render: (s) => <BadgeEstadoSesion estado={s.estado} />,
    },
    {
      clave: 'fechaProgramada',
      encabezado: 'Fecha de programación',
      render: (s) => formatearFechaSesion(s.fechaProgramada),
    },
  ]

  return (
    <TablaUsuarios<SesionListadoDto>
      columnas={columnas}
      filas={sesiones}
      obtenerId={(s) => s.id}
      estadoCarga={estado}
      mensajeError={mensajeError}
      mensajeVacio={mensajeVacio ?? 'No hay sesiones disponibles con los filtros actuales.'}
      inicioNumeracion={inicioNumeracion}
      mostrarColumnaNumero
      alVerPerfil={alAbrirDetalle}
      etiquetaAccion="Ver detalle"
    />
  )
}
