import { useState } from 'react'
import { crearSesion } from '../servicios/sesionesApi'
import type {
  CrearSesionRespuestaDto,
  ModoSesion
} from '../tipos/sesiones'

// Política de capacidad reflejada del backend para mostrar al usuario.
export const MIN_MISIONES = 1
export const MAX_MISIONES = 5

// Mínimos de capacidad reflejados del backend (PoliticaCapacidadSesion).
export const MIN_PARTICIPANTES_INDIVIDUAL = 1
export const MIN_EQUIPOS = 1
export const MIN_PARTICIPANTES_POR_EQUIPO = 2

export interface FormularioCrearSesion {
  nombre: string
  descripcion: string
  modo: ModoSesion
  fechaProgramada: string
  misionesIds: string[]
  // Capacidad como texto porque proviene de inputs; se convierte al enviar.
  maximoParticipantes: string
  maximoEquipos: string
  maximoParticipantesPorEquipo: string
  // Duración opcional en minutos para auto-finalización. Vacío = sin límite.
  duracionMinutosLimite: string
}

export type ErroresFormularioCrearSesion = Partial<
  Record<keyof FormularioCrearSesion, string>
>

const ESTADO_INICIAL: FormularioCrearSesion = {
  nombre: '',
  descripcion: '',
  modo: 'Individual',
  fechaProgramada: '',
  misionesIds: [],
  maximoParticipantes: '',
  maximoEquipos: '',
  maximoParticipantesPorEquipo: '',
  duracionMinutosLimite: ''
}

function validarEntero(valor: string): number | null {
  const n = Number(valor)
  if (valor.trim() === '' || !Number.isInteger(n)) return null
  return n
}

// Validador puro: la página le pasa el "ahora" para que el test pueda
// fijar una fecha estable y la validación de fecha-futura sea
// determinística.
export function validarFormularioCrearSesion(
  datos: FormularioCrearSesion,
  ahora: Date,
): ErroresFormularioCrearSesion {
  const errores: ErroresFormularioCrearSesion = {}

  const nombre = datos.nombre.trim()
  if (!nombre) errores.nombre = 'El nombre es obligatorio.'
  else if (nombre.length < 3 || nombre.length > 150)
    errores.nombre = 'El nombre debe tener entre 3 y 150 caracteres.'

  const descripcion = datos.descripcion.trim()
  if (!descripcion) errores.descripcion = 'La descripción es obligatoria.'
  else if (descripcion.length > 1000)
    errores.descripcion = 'La descripción no puede superar los 1000 caracteres.'

  if (datos.modo !== 'Individual' && datos.modo !== 'Grupal')
    errores.modo = 'Seleccione un tipo de sesión válido.'

  if (!datos.fechaProgramada) {
    errores.fechaProgramada = 'Debe indicar la fecha programada.'
  } else {
    const fechaSeleccionada = new Date(datos.fechaProgramada)
    if (Number.isNaN(fechaSeleccionada.getTime())) {
      errores.fechaProgramada = 'La fecha programada no es válida.'
    } else if (fechaSeleccionada.getTime() <= ahora.getTime()) {
      errores.fechaProgramada =
        'La sesión no puede programarse para una fecha y hora que ya pasó.'
    }
  }

  if (datos.misionesIds.length < MIN_MISIONES) {
    errores.misionesIds = `Debe seleccionar al menos ${MIN_MISIONES} misión.`
  } else if (datos.misionesIds.length > MAX_MISIONES) {
    errores.misionesIds = `No puede seleccionar más de ${MAX_MISIONES} misiones.`
  }

  if (datos.modo === 'Individual') {
    const maximo = validarEntero(datos.maximoParticipantes)
    if (maximo === null)
      errores.maximoParticipantes = 'El máximo de participantes es obligatorio.'
    else if (maximo < MIN_PARTICIPANTES_INDIVIDUAL)
      errores.maximoParticipantes =
        `El máximo de participantes debe ser al menos ${MIN_PARTICIPANTES_INDIVIDUAL}.`
  } else if (datos.modo === 'Grupal') {
    const equipos = validarEntero(datos.maximoEquipos)
    if (equipos === null)
      errores.maximoEquipos = 'El máximo de equipos es obligatorio.'
    else if (equipos < MIN_EQUIPOS)
      errores.maximoEquipos = `El máximo de equipos debe ser al menos ${MIN_EQUIPOS}.`

    const porEquipo = validarEntero(datos.maximoParticipantesPorEquipo)
    if (porEquipo === null)
      errores.maximoParticipantesPorEquipo =
        'Los participantes por equipo son obligatorios.'
    else if (porEquipo < MIN_PARTICIPANTES_POR_EQUIPO)
      errores.maximoParticipantesPorEquipo =
        `Los participantes por equipo deben ser al menos ${MIN_PARTICIPANTES_POR_EQUIPO}.`
  }

  return errores
}

interface OpcionesUseCrearSesion {
  token: string | null
}

interface EstadoUseCrearSesion {
  datos: FormularioCrearSesion
  errores: ErroresFormularioCrearSesion
  errorGeneral: string | null
  enviando: boolean
  exito: CrearSesionRespuestaDto | null
  actualizarCampo: <K extends keyof FormularioCrearSesion>(
    campo: K,
    valor: FormularioCrearSesion[K],
  ) => void
  agregarMision: (misionId: string) => void
  quitarMision: (misionId: string) => void
  reiniciar: () => void
  enviar: () => Promise<CrearSesionRespuestaDto | null>
}

// Hook que encapsula el estado del formulario, su validación local y
// el envío al backend. La página de crear sesión solo decide cómo
// renderizar; la lógica vive acá.
export function useCrearSesion({ token }: OpcionesUseCrearSesion): EstadoUseCrearSesion {
  const [datos, setDatos] = useState<FormularioCrearSesion>(ESTADO_INICIAL)
  const [errores, setErrores] = useState<ErroresFormularioCrearSesion>({})
  const [errorGeneral, setErrorGeneral] = useState<string | null>(null)
  const [enviando, setEnviando] = useState(false)
  const [exito, setExito] = useState<CrearSesionRespuestaDto | null>(null)

  function actualizarCampo<K extends keyof FormularioCrearSesion>(
    campo: K,
    valor: FormularioCrearSesion[K],
  ) {
    setDatos(prev => {
      const siguiente = { ...prev, [campo]: valor }
      // Al cambiar de modo, se limpia la capacidad del modo que ya no aplica
      // para no enviar datos incompatibles en el payload.
      if (campo === 'modo') {
        if (valor === 'Individual') {
          siguiente.maximoEquipos = ''
          siguiente.maximoParticipantesPorEquipo = ''
        } else {
          siguiente.maximoParticipantes = ''
        }
      }
      return siguiente
    })
    if (campo === 'modo') {
      setErrores(prev => ({
        ...prev,
        maximoParticipantes: undefined,
        maximoEquipos: undefined,
        maximoParticipantesPorEquipo: undefined,
      }))
    }
    if (errores[campo]) setErrores(prev => ({ ...prev, [campo]: undefined }))
  }

  function agregarMision(misionId: string) {
    if (!misionId) return
    setDatos(prev => {
      if (prev.misionesIds.includes(misionId)) return prev
      if (prev.misionesIds.length >= MAX_MISIONES) return prev
      return { ...prev, misionesIds: [...prev.misionesIds, misionId] }
    })
    if (errores.misionesIds) setErrores(prev => ({ ...prev, misionesIds: undefined }))
  }

  function quitarMision(misionId: string) {
    setDatos(prev => ({
      ...prev,
      misionesIds: prev.misionesIds.filter(id => id !== misionId),
    }))
  }

  function reiniciar() {
    setDatos(ESTADO_INICIAL)
    setErrores({})
    setErrorGeneral(null)
    setEnviando(false)
    setExito(null)
  }

  async function enviar(): Promise<CrearSesionRespuestaDto | null> {
    const erroresValidacion = validarFormularioCrearSesion(datos, new Date())
    if (Object.keys(erroresValidacion).length > 0) {
      setErrores(erroresValidacion)
      return null
    }
    if (!token) {
      setErrorGeneral('Debe iniciar sesión.')
      return null
    }

    setEnviando(true)
    setErrorGeneral(null)
    try {
      const esIndividual = datos.modo === 'Individual'
      const respuesta = await crearSesion(
        {
          nombre: datos.nombre.trim(),
          descripcion: datos.descripcion.trim(),
          modo: datos.modo,
          fechaProgramada: new Date(datos.fechaProgramada).toISOString(),
          misionesIds: datos.misionesIds,
          maximoParticipantes: esIndividual ? Number(datos.maximoParticipantes) : null,
          maximoEquipos: esIndividual ? null : Number(datos.maximoEquipos),
          maximoParticipantesPorEquipo: esIndividual
            ? null
            : Number(datos.maximoParticipantesPorEquipo),
          duracionMinutosLimite: datos.duracionMinutosLimite.trim()
            ? Number(datos.duracionMinutosLimite)
            : null,
        },
        token,
      )
      setExito(respuesta)
      return respuesta
    } catch (err) {
      setErrorGeneral(
        err instanceof Error ? err.message : 'No fue posible crear la sesión.',
      )
      return null
    } finally {
      setEnviando(false)
    }
  }

  return {
    datos,
    errores,
    errorGeneral,
    enviando,
    exito,
    actualizarCampo,
    agregarMision,
    quitarMision,
    reiniciar,
    enviar,
  }
}
