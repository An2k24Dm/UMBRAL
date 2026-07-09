import { useState } from 'react'
import { actualizarSesion } from '../servicios/sesionesApi'
import type {
  ModificarSesionSolicitud,
  ModoSesion,
  SesionDetalleDto
} from '../tipos/sesiones'

// Reutiliza los mismos límites de misiones que la creación.
export const MIN_MISIONES = 1
export const MAX_MISIONES = 5

// Mínimos de capacidad reflejados del backend (PoliticaCapacidadSesion).
export const MIN_PARTICIPANTES_INDIVIDUAL = 1
export const MIN_EQUIPOS = 1
export const MIN_PARTICIPANTES_POR_EQUIPO = 2

export interface FormularioEditarSesion {
  nombre: string
  descripcion: string
  modo: ModoSesion
  fechaProgramada: string
  misionesIds: string[]
  // Capacidad como texto porque proviene de inputs; se convierte al enviar.
  maximoParticipantes: string
  maximoEquipos: string
  maximoParticipantesPorEquipo: string
}

export type ErroresFormularioEditarSesion = Partial<
  Record<keyof FormularioEditarSesion, string>
>

const ESTADO_INICIAL: FormularioEditarSesion = {
  nombre: '',
  descripcion: '',
  modo: 'Individual',
  fechaProgramada: '',
  misionesIds: [],
  maximoParticipantes: '',
  maximoEquipos: '',
  maximoParticipantesPorEquipo: '',
}

// Convierte una fecha ISO (UTC) al formato que espera <input datetime-local>
// en hora local.
function aDateTimeLocal(iso: string): string {
  const fecha = new Date(iso)
  if (Number.isNaN(fecha.getTime())) return ''
  const desplazamiento = fecha.getTimezoneOffset() * 60_000
  return new Date(fecha.getTime() - desplazamiento).toISOString().slice(0, 16)
}

// Prellena el formulario a partir del detalle cargado del backend.
export function formularioDesdeDetalle(detalle: SesionDetalleDto): FormularioEditarSesion {
  const misiones = detalle.misiones
    .slice()
    .sort((a, b) => a.orden - b.orden)
    .map(m => m.misionId)

  return {
    nombre: detalle.nombre,
    descripcion: detalle.descripcion,
    modo: (detalle.modo as ModoSesion) ?? 'Individual',
    fechaProgramada: aDateTimeLocal(detalle.fechaProgramada),
    misionesIds: misiones,
    maximoParticipantes: detalle.maximoParticipantes?.toString() ?? '',
    maximoEquipos: detalle.maximoEquipos?.toString() ?? '',
    maximoParticipantesPorEquipo: detalle.maximoParticipantesPorEquipo?.toString() ?? '',
  }
}

function validarEntero(valor: string): number | null {
  const n = Number(valor)
  if (valor.trim() === '' || !Number.isInteger(n)) return null
  return n
}

// Validador puro: recibe "ahora" para que la regla de fecha-futura sea
// determinística y testeable.
export function validarFormularioEditarSesion(
  datos: FormularioEditarSesion,
  ahora: Date,
): ErroresFormularioEditarSesion {
  const errores: ErroresFormularioEditarSesion = {}

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
      errores.maximoParticipantes = 'Debe indicar el máximo de participantes.'
    else if (maximo < MIN_PARTICIPANTES_INDIVIDUAL)
      errores.maximoParticipantes =
        `El máximo de participantes debe ser al menos ${MIN_PARTICIPANTES_INDIVIDUAL}.`
  } else if (datos.modo === 'Grupal') {
    const equipos = validarEntero(datos.maximoEquipos)
    if (equipos === null)
      errores.maximoEquipos = 'Debe indicar el máximo de equipos.'
    else if (equipos < MIN_EQUIPOS)
      errores.maximoEquipos = `El máximo de equipos debe ser al menos ${MIN_EQUIPOS}.`

    const porEquipo = validarEntero(datos.maximoParticipantesPorEquipo)
    if (porEquipo === null)
      errores.maximoParticipantesPorEquipo =
        'Debe indicar el máximo de participantes por equipo.'
    else if (porEquipo < MIN_PARTICIPANTES_POR_EQUIPO)
      errores.maximoParticipantesPorEquipo =
        `El máximo de participantes por equipo debe ser al menos ${MIN_PARTICIPANTES_POR_EQUIPO}.`
  }

  return errores
}

// Construye el payload del PUT enviando solo la capacidad que aplica al modo.
export function construirPayloadEditar(
  datos: FormularioEditarSesion,
): ModificarSesionSolicitud {
  const esIndividual = datos.modo === 'Individual'
  return {
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
  }
}

interface OpcionesUseEditarSesion {
  token: string | null
  id: string | undefined
}

export function useEditarSesion({ token, id }: OpcionesUseEditarSesion) {
  const [datos, setDatos] = useState<FormularioEditarSesion>(ESTADO_INICIAL)
  const [errores, setErrores] = useState<ErroresFormularioEditarSesion>({})
  const [errorGeneral, setErrorGeneral] = useState<string | null>(null)
  const [enviando, setEnviando] = useState(false)
  const [exito, setExito] = useState<SesionDetalleDto | null>(null)

  function prefijar(detalle: SesionDetalleDto) {
    setDatos(formularioDesdeDetalle(detalle))
  }

  function actualizarCampo<K extends keyof FormularioEditarSesion>(
    campo: K,
    valor: FormularioEditarSesion[K],
  ) {
    setDatos(prev => ({ ...prev, [campo]: valor }))
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
      misionesIds: prev.misionesIds.filter(mId => mId !== misionId),
    }))
  }

  async function enviar(): Promise<SesionDetalleDto | null> {
    const erroresValidacion = validarFormularioEditarSesion(datos, new Date())
    if (Object.keys(erroresValidacion).length > 0) {
      setErrores(erroresValidacion)
      return null
    }
    if (!token || !id) {
      setErrorGeneral('Debe iniciar sesión.')
      return null
    }

    setEnviando(true)
    setErrorGeneral(null)
    try {
      const respuesta = await actualizarSesion(id, construirPayloadEditar(datos), token)
      setExito(respuesta)
      return respuesta
    } catch (err) {
      setErrorGeneral(
        err instanceof Error ? err.message : 'No fue posible actualizar la sesión.',
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
    prefijar,
    actualizarCampo,
    agregarMision,
    quitarMision,
    enviar,
  }
}
