import { useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { LayoutPanel } from '../componentes/LayoutPanel'
import { CampoFormulario } from '../componentes/CampoFormulario'
import { Boton } from '../componentes/Boton'
import { Alerta } from '../componentes/Alerta'
import { crearBusquedaTesoro } from '../autenticacion/clienteApiJuegos'
import { usarAutenticacion } from '../autenticacion/ProveedorAutenticacion'

interface Errores {
  nombre?: string
  descripcion?: string
  tiempo?: string
  puntaje?: string
}

interface Datos {
  nombre: string
  descripcion: string
  tiempo: string
  puntaje: string
}

const PUNTAJE_OPCIONES = Array.from({ length: 20 }, (_, i) => (i + 1) * 5)

const ESTADO_INICIAL: Datos = { nombre: '', descripcion: '', tiempo: '5', puntaje: '5' }

function validar(datos: Datos): Errores {
  const errores: Errores = {}
  if (!datos.nombre.trim()) {
    errores.nombre = 'El nombre es obligatorio.'
  } else if (datos.nombre.trim().length > 200) {
    errores.nombre = 'El nombre no puede superar 200 caracteres.'
  }
  if (!datos.descripcion.trim()) {
    errores.descripcion = 'La descripción es obligatoria.'
  } else if (datos.descripcion.trim().length > 1000) {
    errores.descripcion = 'La descripción no puede superar 1000 caracteres.'
  }
  const tiempo = Number(datos.tiempo)
  if (isNaN(tiempo) || tiempo < 5) {
    errores.tiempo = 'El tiempo debe ser al menos 5 segundos.'
  }
  const puntaje = Number(datos.puntaje)
  if (isNaN(puntaje) || puntaje < 5) {
    errores.puntaje = 'El puntaje debe ser al menos 5 puntos.'
  }
  return errores
}

export function PaginaCrearBusqueda() {
  const { token, usuario } = usarAutenticacion()
  const navegar = useNavigate()
  const rutaBase = usuario?.rol === 'Administrador' ? '/administrador/busquedas' : '/operador/busquedas'

  const [datos, setDatos] = useState<Datos>(ESTADO_INICIAL)
  const [errores, setErrores] = useState<Errores>({})
  const [errorGeneral, setErrorGeneral] = useState<string | null>(null)
  const [enviando, setEnviando] = useState(false)

  function manejarCambio(campo: keyof Datos, valor: string) {
    setDatos((prev) => ({ ...prev, [campo]: valor }))
    if (errores[campo]) setErrores((prev) => ({ ...prev, [campo]: undefined }))
  }

  async function manejarEnvio(e: React.FormEvent) {
    e.preventDefault()
    const erroresValidacion = validar(datos)
    if (Object.keys(erroresValidacion).length > 0) {
      setErrores(erroresValidacion)
      return
    }
    if (!token) { setErrorGeneral('Debe iniciar sesión.'); return }

    setEnviando(true)
    setErrorGeneral(null)
    try {
      const id = await crearBusquedaTesoro(
        {
          nombre: datos.nombre.trim(),
          descripcion: datos.descripcion.trim(),
          tiempo: Number(datos.tiempo),
          puntaje: Number(datos.puntaje)
        },
        token
      )
      navegar(`${rutaBase}/${id}/mision`)
    } catch (e) {
      setErrorGeneral(e instanceof Error ? e.message : 'No fue posible crear la búsqueda del tesoro.')
      setEnviando(false)
    }
  }

  return (
    <LayoutPanel titulo="Crear búsqueda del tesoro" descripcion="Complete los datos para crear una nueva búsqueda en borrador.">
      <section className="seccion">
        <div className="seccion-cabecera">
          <div>
            <h2>Nueva búsqueda del tesoro</h2>
            <p>La búsqueda se crea en estado Inactiva. Podrá agregar etapas y activarla después.</p>
          </div>
        </div>

        {errorGeneral && <Alerta tono="error">{errorGeneral}</Alerta>}

        <form onSubmit={manejarEnvio} noValidate className="formulario-trivia">
          <CampoFormulario etiqueta="Nombre" htmlFor="nombre" error={errores.nombre}>
            <input
              id="nombre"
              type="text"
              maxLength={200}
              value={datos.nombre}
              onChange={(e) => manejarCambio('nombre', e.target.value)}
              disabled={enviando}
              placeholder="Ej. Búsqueda del tesoro del centro histórico"
            />
          </CampoFormulario>

          <CampoFormulario etiqueta="Descripción" htmlFor="descripcion" error={errores.descripcion}>
            <textarea
              id="descripcion"
              rows={4}
              maxLength={1000}
              value={datos.descripcion}
              onChange={(e) => manejarCambio('descripcion', e.target.value)}
              disabled={enviando}
              placeholder="Describe brevemente de qué trata esta búsqueda del tesoro"
            />
          </CampoFormulario>

          <CampoFormulario etiqueta="Tiempo estimado (segundos)" htmlFor="tiempo" error={errores.tiempo}>
            <input
              id="tiempo"
              type="number"
              min={5}
              step={5}
              max={3600}
              value={datos.tiempo}
              onChange={(e) => manejarCambio('tiempo', e.target.value)}
              disabled={enviando}
              placeholder="Ej. 60"
            />
          </CampoFormulario>

          <CampoFormulario etiqueta="Puntaje" htmlFor="puntaje" error={errores.puntaje}>
            <select
              id="puntaje"
              value={datos.puntaje}
              onChange={(e) => manejarCambio('puntaje', e.target.value)}
              disabled={enviando}
            >
              {PUNTAJE_OPCIONES.map((pts) => (
                <option key={pts} value={String(pts)}>{pts} pts</option>
              ))}
            </select>
          </CampoFormulario>

          <div className="acciones-formulario-trivia">
            <Boton variante="volver" type="button" onClick={() => navegar(rutaBase)} disabled={enviando}>
              Cancelar
            </Boton>
            <Boton variante="primario" type="submit" disabled={enviando}>
              {enviando ? 'Creando…' : 'Crear búsqueda'}
            </Boton>
          </div>
        </form>
      </section>
    </LayoutPanel>
  )
}
