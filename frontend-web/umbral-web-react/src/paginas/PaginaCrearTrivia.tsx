import { useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { LayoutPanel } from '../componentes/LayoutPanel'
import { CampoFormulario } from '../componentes/CampoFormulario'
import { Boton } from '../componentes/Boton'
import { Alerta } from '../componentes/Alerta'
import { crearTrivia } from '../autenticacion/clienteApiJuegos'
import { usarAutenticacion } from '../autenticacion/ProveedorAutenticacion'

interface Errores {
  nombre?: string
  descripcion?: string
  tiempoLimitePorPregunta?: string
}

interface Datos {
  nombre: string
  descripcion: string
  tiempoLimitePorPregunta: string
}

const ESTADO_INICIAL: Datos = { nombre: '', descripcion: '', tiempoLimitePorPregunta: '30' }

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
  const tiempo = Number(datos.tiempoLimitePorPregunta)
  if (!datos.tiempoLimitePorPregunta || isNaN(tiempo) || tiempo <= 0) {
    errores.tiempoLimitePorPregunta = 'El tiempo límite debe ser mayor a 0.'
  }
  return errores
}

export function PaginaCrearTrivia() {
  const { token } = usarAutenticacion()
  const navegar = useNavigate()

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
      const id = await crearTrivia(
        {
          nombre: datos.nombre.trim(),
          descripcion: datos.descripcion.trim(),
          tiempoLimitePorPregunta: Number(datos.tiempoLimitePorPregunta)
        },
        token
      )
      navegar(`/operador/trivias/${id}/preguntas`)
    } catch (e) {
      setErrorGeneral(e instanceof Error ? e.message : 'No fue posible crear la trivia.')
      setEnviando(false)
    }
  }

  return (
    <LayoutPanel titulo="Crear trivia" descripcion="Complete los datos para crear una nueva trivia en borrador.">
      <section className="seccion">
        <div className="seccion-cabecera">
          <div>
            <h2>Nueva trivia</h2>
            <p>La trivia se crea en estado Borrador. Podrá agregar preguntas después.</p>
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
              placeholder="Ej. Trivia de historia venezolana"
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
              placeholder="Describe brevemente de qué trata esta trivia"
            />
          </CampoFormulario>

          <CampoFormulario
            etiqueta="Tiempo límite por pregunta (segundos)"
            htmlFor="tiempo"
            error={errores.tiempoLimitePorPregunta}
            ayuda="Tiempo en segundos que tendrá el participante para responder cada pregunta."
          >
            <input
              id="tiempo"
              type="number"
              min={1}
              max={300}
              value={datos.tiempoLimitePorPregunta}
              onChange={(e) => manejarCambio('tiempoLimitePorPregunta', e.target.value)}
              disabled={enviando}
            />
          </CampoFormulario>

          <div className="acciones-formulario-trivia">
            <Boton variante="volver" type="button" onClick={() => navegar('/operador/trivias')} disabled={enviando}>
              Cancelar
            </Boton>
            <Boton variante="primario" type="submit" disabled={enviando}>
              {enviando ? 'Creando…' : 'Crear trivia'}
            </Boton>
          </div>
        </form>
      </section>
    </LayoutPanel>
  )
}
