import { useEffect, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { LayoutPanel } from '../componentes/LayoutPanel'
import { CampoFormulario } from '../componentes/CampoFormulario'
import { Boton } from '../componentes/Boton'
import { Alerta } from '../componentes/Alerta'
import {
  crearSesion,
  listarContenidoActivo,
  type ContenidoActivoResumen,
  type ModoSesionApi,
  type TipoJuegoSesion
} from '../autenticacion/clienteApiSesiones'
import { usarAutenticacion } from '../autenticacion/ProveedorAutenticacion'

// HU33 — Formulario de creación de sesión en vivo.
//
// Carga dinámicamente el catálogo de contenido activo según el tipo de
// juego elegido. Si no existe contenido activo se informa al usuario en
// vez de bloquearlo silenciosamente. Al confirmar, envía el comando al
// backend y redirige al listado de sesiones para ver la nueva entrada
// en estado "Programada".

interface Datos {
  nombre: string
  tipoJuego: TipoJuegoSesion
  contenidoJuegoId: string
  modo: ModoSesionApi
  fechaProgramada: string
}

interface Errores {
  nombre?: string
  tipoJuego?: string
  contenidoJuegoId?: string
  modo?: string
  fechaProgramada?: string
}

const ESTADO_INICIAL: Datos = {
  nombre: '',
  tipoJuego: 'Trivia',
  contenidoJuegoId: '',
  modo: 'Individual',
  fechaProgramada: ''
}

// Devuelve "YYYY-MM-DDTHH:mm" en hora local del usuario, formato que
// espera el input datetime-local para `min` y para `value`.
function aDateTimeLocalString(fecha: Date): string {
  const desplazamiento = fecha.getTimezoneOffset() * 60_000
  return new Date(fecha.getTime() - desplazamiento).toISOString().slice(0, 16)
}

function validar(datos: Datos, ahora: Date): Errores {
  const errores: Errores = {}
  const nombre = datos.nombre.trim()
  if (!nombre) errores.nombre = 'El nombre es obligatorio.'
  else if (nombre.length < 3 || nombre.length > 150)
    errores.nombre = 'El nombre debe tener entre 3 y 150 caracteres.'

  if (datos.tipoJuego !== 'Trivia' && datos.tipoJuego !== 'BusquedaTesoro')
    errores.tipoJuego = 'Seleccione un tipo de juego válido.'

  if (!datos.contenidoJuegoId) errores.contenidoJuegoId = 'Seleccione el contenido del juego.'

  if (datos.modo !== 'Individual' && datos.modo !== 'Grupo')
    errores.modo = 'Seleccione un modo válido.'

  if (!datos.fechaProgramada) {
    errores.fechaProgramada = 'Debe indicar la fecha programada.'
  } else {
    // El input datetime-local emite "YYYY-MM-DDTHH:mm" sin zona; al
    // pasarlo a Date se interpreta en la zona local del usuario, que
    // es exactamente lo que él seleccionó visualmente. Comparamos esa
    // instancia con la hora actual también en local.
    const fechaSeleccionada = new Date(datos.fechaProgramada)
    if (Number.isNaN(fechaSeleccionada.getTime())) {
      errores.fechaProgramada = 'La fecha programada no es válida.'
    } else if (fechaSeleccionada.getTime() <= ahora.getTime()) {
      errores.fechaProgramada =
        'La sesión no puede programarse para una fecha y hora que ya pasó.'
    }
  }

  return errores
}

export function PaginaCrearSesion() {
  const { token, usuario } = usarAutenticacion()
  const navegar = useNavigate()
  const rutaBase = usuario?.rol === 'Administrador'
    ? '/administrador/sesiones'
    : '/operador/sesiones'

  const [datos, setDatos] = useState<Datos>(ESTADO_INICIAL)
  const [errores, setErrores] = useState<Errores>({})
  const [errorGeneral, setErrorGeneral] = useState<string | null>(null)
  const [enviando, setEnviando] = useState(false)

  const [contenidos, setContenidos] = useState<ContenidoActivoResumen[]>([])
  const [cargandoContenidos, setCargandoContenidos] = useState(false)
  const [mensajeContenidos, setMensajeContenidos] = useState<string | null>(null)

  // Cuando cambia el tipo de juego, recarga la lista de contenido activo
  // del tipo seleccionado y limpia la selección actual.
  useEffect(() => {
    if (!token) return
    const ref = { cancelado: false }
    async function cargar() {
      setCargandoContenidos(true)
      setMensajeContenidos(null)
      try {
        const lista = await listarContenidoActivo(datos.tipoJuego, token!)
        if (ref.cancelado) return
        setContenidos(lista)
        if (lista.length === 0)
          setMensajeContenidos('No hay contenido activo de este tipo. Active uno primero.')
        setDatos(prev => ({ ...prev, contenidoJuegoId: '' }))
      } catch (e) {
        if (ref.cancelado) return
        setMensajeContenidos(
          e instanceof Error ? e.message : 'No fue posible cargar el contenido activo.')
        setContenidos([])
      } finally {
        if (!ref.cancelado) setCargandoContenidos(false)
      }
    }
    cargar()
    return () => { ref.cancelado = true }
  }, [datos.tipoJuego, token])

  function manejarCambio<K extends keyof Datos>(campo: K, valor: Datos[K]) {
    setDatos(prev => ({ ...prev, [campo]: valor }))
    if (errores[campo]) setErrores(prev => ({ ...prev, [campo]: undefined }))
  }

  async function manejarEnvio(e: React.FormEvent) {
    e.preventDefault()
    // "ahora" se recalcula en cada envío para que la validación de
    // fecha futura no use un valor cacheado del primer render.
    const erroresValidacion = validar(datos, new Date())
    if (Object.keys(erroresValidacion).length > 0) {
      setErrores(erroresValidacion)
      return
    }
    if (!token) { setErrorGeneral('Debe iniciar sesión.'); return }

    setEnviando(true)
    setErrorGeneral(null)
    try {
      await crearSesion({
        nombre: datos.nombre.trim(),
        tipoJuego: datos.tipoJuego,
        contenidoJuegoId: datos.contenidoJuegoId,
        modo: datos.modo,
        fechaProgramada: new Date(datos.fechaProgramada).toISOString()
      }, token)
      navegar(rutaBase)
    } catch (err) {
      setErrorGeneral(err instanceof Error ? err.message : 'No fue posible crear la sesión.')
      setEnviando(false)
    }
  }

  return (
    <LayoutPanel
      titulo="Crear sesión"
      descripcion="Inicie una sesión en vivo a partir de una Trivia o Búsqueda del Tesoro activa."
    >
      <section className="seccion">
        <div className="seccion-cabecera">
          <div>
            <h2>Nueva sesión</h2>
            <p>La sesión quedará en estado Programada y visible en el listado.</p>
          </div>
        </div>

        {errorGeneral && <Alerta tono="error">{errorGeneral}</Alerta>}

        <form onSubmit={manejarEnvio} noValidate className="formulario-trivia">
          <CampoFormulario etiqueta="Nombre" htmlFor="nombre" error={errores.nombre}>
            <input
              id="nombre"
              type="text"
              maxLength={150}
              value={datos.nombre}
              onChange={(e) => manejarCambio('nombre', e.target.value)}
              disabled={enviando}
              placeholder="Ej. Sesión piloto de historia"
            />
          </CampoFormulario>

          <CampoFormulario etiqueta="Tipo de juego" htmlFor="tipoJuego" error={errores.tipoJuego}>
            <select
              id="tipoJuego"
              value={datos.tipoJuego}
              onChange={(e) => manejarCambio('tipoJuego', e.target.value as TipoJuegoSesion)}
              disabled={enviando}
            >
              <option value="Trivia">Trivia</option>
              <option value="BusquedaTesoro">Búsqueda del Tesoro</option>
            </select>
          </CampoFormulario>

          <CampoFormulario
            etiqueta="Contenido activo"
            htmlFor="contenidoJuegoId"
            error={errores.contenidoJuegoId}
            ayuda={mensajeContenidos ?? undefined}
          >
            <select
              id="contenidoJuegoId"
              value={datos.contenidoJuegoId}
              onChange={(e) => manejarCambio('contenidoJuegoId', e.target.value)}
              disabled={enviando || cargandoContenidos || contenidos.length === 0}
            >
              <option value="">
                {cargandoContenidos ? 'Cargando…' : 'Seleccione un contenido'}
              </option>
              {contenidos.map((c) => (
                <option key={c.id} value={c.id}>{c.nombre}</option>
              ))}
            </select>
          </CampoFormulario>

          <CampoFormulario etiqueta="Modo" htmlFor="modo" error={errores.modo}>
            <select
              id="modo"
              value={datos.modo}
              onChange={(e) => manejarCambio('modo', e.target.value as ModoSesionApi)}
              disabled={enviando}
            >
              <option value="Individual">Individual</option>
              <option value="Grupo">Grupo</option>
            </select>
          </CampoFormulario>

          <CampoFormulario
            etiqueta="Fecha y hora programada"
            htmlFor="fechaProgramada"
            error={errores.fechaProgramada}
          >
            <input
              id="fechaProgramada"
              type="datetime-local"
              value={datos.fechaProgramada}
              min={aDateTimeLocalString(new Date())}
              onChange={(e) => manejarCambio('fechaProgramada', e.target.value)}
              disabled={enviando}
            />
          </CampoFormulario>

          <div className="acciones-formulario-trivia">
            <Boton variante="volver" type="button" onClick={() => navegar(rutaBase)} disabled={enviando}>
              Cancelar
            </Boton>
            <Boton variante="primario" type="submit" disabled={enviando}>
              {enviando ? 'Creando…' : 'Crear sesión'}
            </Boton>
          </div>
        </form>
      </section>
    </LayoutPanel>
  )
}
