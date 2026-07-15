import { Fragment, useCallback, useEffect, useState } from 'react'
import { useLocation, useNavigate, useParams } from 'react-router-dom'
import { LayoutPanel } from '../componentes/LayoutPanel'
import { Alerta } from '../componentes/Alerta'
import { Boton } from '../componentes/Boton'
import { BadgeEstadoSesion } from '../componentes/BadgeEstadoSesion'
import { ModalConfirmacion } from '../componentes/ModalConfirmacion'
import {
  obtenerSesion,
  eliminarSesion,
  listarEquiposSesion,
  expulsarParticipanteSesion,
  expulsarEquipoSesion,
  iniciarSesionOperacion,
  pausarSesionOperacion,
  reanudarSesionOperacion,
  cancelarSesionOperacion,
  obtenerProgresoSesion,
  type EquipoSesionListadoDto,
  type OperacionSesionRespuestaDto,
  type ParticipanteSesionDto,
  type ProgresoSesionDto,
  type ProgresoSesionParticipanteDto,
  type SesionDetalleDto
} from '../autenticacion/clienteApiSesiones'
import {
  obtenerDetalleMision,
  obtenerDetalleTrivia,
  obtenerDetalleBusqueda,
  type MisionDetalleDto,
  type TriviaDetalleDto,
  type BusquedaTesoroDetalleDto
} from '../autenticacion/clienteApiJuegos'
import { liberarPista } from '../autenticacion/clienteApiSesiones'
import { MapaLeaflet, type MarcadorMapa } from '../componentes/MapaLeaflet'
import {
  obtenerRankingParticipantes,
  obtenerRankingEquipos,
  type EntradaRankingParticipanteDto,
  type EntradaRankingEquipoDto
} from '../autenticacion/clienteApiRanking'
import { usarAutenticacion } from '../autenticacion/ProveedorAutenticacion'
import { useSesionesTiempoReal, type UbicacionActualizadaTR } from '../hooks/useSesionesTiempoReal'
import { useRankingTiempoReal } from '../hooks/useRankingTiempoReal'
import {
  formatearFechaSesion,
  nombreModoSesion,
  nombreTipoContenidoEtapa
} from '../utilidades/formatoSesiones'

interface EtapaEnriquecida {
  id: string
  orden: number
  tipoModoDeJuego: string
  modoDeJuegoId: string
  nombreModoDeJuego: string
  tiempoEstimado: number
  trivia?: TriviaDetalleDto
  busqueda?: BusquedaTesoroDetalleDto
  errorContenido?: string
}

interface MisionEnriquecida {
  id: string
  orden: number
  mision?: MisionDetalleDto
  etapas: EtapaEnriquecida[]
  cargando: boolean
  error?: string
}

function formatearDuracionSegundos(segundos: number | null | undefined): string {
  if (segundos === null || segundos === undefined || Number.isNaN(segundos)) {
    return '—'
  }

  if (segundos < 60) {
    return `${segundos} s`
  }

  const minutos = Math.floor(segundos / 60)
  const segundosRestantes = segundos % 60

  if (segundosRestantes === 0) {
    return `${minutos} min`
  }

  return `${minutos} min ${segundosRestantes} s`
}

// HU52 — Operaciones del ciclo de vida de la sesión (patrón Facade en backend).
type AccionOperacionSesion = 'iniciar' | 'pausar' | 'reanudar' | 'cancelar'

// Configuración declarativa de cada operación: qué función de API ejecutar y
// los textos del modal de confirmación y del mensaje de éxito.
const OPERACIONES_SESION: Record<AccionOperacionSesion, {
  titulo: string
  textoConfirmar: string
  confirmacion: string
  ejecutar: (id: string, token: string) => Promise<OperacionSesionRespuestaDto>
  exito: string
}> = {
  iniciar: {
    titulo: 'Iniciar sesión',
    textoConfirmar: 'Iniciar',
    confirmacion:
      '¿Deseas iniciar esta sesión? Los participantes inscritos podrán comenzar a jugar.',
    ejecutar: iniciarSesionOperacion,
    exito: 'Sesión iniciada correctamente.'
  },
  pausar: {
    titulo: 'Pausar sesión',
    textoConfirmar: 'Pausar',
    confirmacion: '¿Deseas pausar esta sesión? Podrás reanudarla más tarde.',
    ejecutar: pausarSesionOperacion,
    exito: 'Sesión pausada correctamente.'
  },
  reanudar: {
    titulo: 'Reanudar sesión',
    textoConfirmar: 'Reanudar',
    confirmacion: '¿Deseas reanudar esta sesión?',
    ejecutar: reanudarSesionOperacion,
    exito: 'Sesión reanudada correctamente.'
  },
  cancelar: {
    titulo: 'Cancelar sesión',
    textoConfirmar: 'Cancelar sesión',
    confirmacion:
      '¿Seguro que deseas cancelar esta sesión? Esta acción no se puede deshacer.',
    ejecutar: cancelarSesionOperacion,
    exito: 'Sesión cancelada correctamente.'
  }
}

export function PaginaDetalleSesion() {
  const { id } = useParams<{ id: string }>()
  const { token, usuario } = usarAutenticacion()
  const navegar = useNavigate()
  const ubicacion = useLocation()
  const rutaListado = usuario?.rol === 'Administrador'
    ? '/administrador/sesiones'
    : '/operador/sesiones'
  const rutaBaseEquipos = `${rutaListado}/${id}/equipos`

  // Solo el Operador puede editar sesiones (el Administrador es de solo
  // lectura). El mensaje de éxito llega como state al volver de la edición.
  const esOperador = usuario?.rol === 'Operador'
  // HU44 — Operador y Administrador consultan el listado enriquecido de
  // equipos (listarEquiposSesion); el Administrador solo en modo lectura.
  const puedeConsultarEquipos =
    usuario?.rol === 'Operador' || usuario?.rol === 'Administrador'
  const mensajeExito = (ubicacion.state as { mensajeExito?: string } | null)?.mensajeExito

  const [estado, setEstado] = useState<'cargando' | 'error' | 'listo'>('cargando')
  const [mensajeError, setMensajeError] = useState<string | null>(null)
  const [sesion, setSesion] = useState<SesionDetalleDto | null>(null)
  const [equiposListado, setEquiposListado] = useState<EquipoSesionListadoDto[] | null>(null)
  const [misiones, setMisiones] = useState<MisionEnriquecida[]>([])
  const [misionAbierta, setMisionAbierta] = useState<string | null>(null)
  const [eliminando, setEliminando] = useState(false)
  const [errorEliminar, setErrorEliminar] = useState<string | null>(null)
  const [modalEliminarAbierto, setModalEliminarAbierto] = useState(false)
  const [versionTiempoReal, setVersionTiempoReal] = useState(0)
  const [versionProgreso, setVersionProgreso] = useState(0)

  // HU44 — Expulsión de participante (individual) o equipo (grupal) por el
  // Operador. Guardamos el objetivo seleccionado para el modal de confirmación.
  const [participanteAExpulsar, setParticipanteAExpulsar] =
    useState<ParticipanteSesionDto | null>(null)
  const [equipoAExpulsar, setEquipoAExpulsar] =
    useState<EquipoSesionListadoDto | null>(null)
  const [expulsando, setExpulsando] = useState(false)
  const [errorExpulsar, setErrorExpulsar] = useState<string | null>(null)

  const [progresoSesion, setProgresoSesion] = useState<ProgresoSesionDto | null>(null)
  const [cargandoProgreso, setCargandoProgreso] = useState(false)
  const [errorProgreso, setErrorProgreso] = useState<string | null>(null)

  const [rankingParticipantes, setRankingParticipantes] = useState<EntradaRankingParticipanteDto[] | null>(null)
  const [rankingEquipos, setRankingEquipos] = useState<EntradaRankingEquipoDto[] | null>(null)
  const [cargandoRanking, setCargandoRanking] = useState(false)
  const [errorRanking, setErrorRanking] = useState<string | null>(null)
  const [versionRanking, setVersionRanking] = useState(0)
  const [equiposExpandidos, setEquiposExpandidos] = useState<Set<string>>(new Set())
  const [ubicacionesParticipantes, setUbicacionesParticipantes] = useState<Map<string, { nombre: string, latitud: number, longitud: number }>>(new Map())

  const alternarEquipoExpandido = (equipoId: string) => {
    setEquiposExpandidos(previo => {
      const siguiente = new Set(previo)
      if (siguiente.has(equipoId)) siguiente.delete(equipoId)
      else siguiente.add(equipoId)
      return siguiente
    })
  }

  // HU52 — Operación del ciclo de vida. accionOperacion no nula ⇒ modal abierto.
  const [accionOperacion, setAccionOperacion] = useState<AccionOperacionSesion | null>(null)
  const [operandoSesion, setOperandoSesion] = useState(false)
  const [errorOperacion, setErrorOperacion] = useState<string | null>(null)
  const [mensajeOperacion, setMensajeOperacion] = useState<string | null>(null)

  const refrescarDetalleTiempoReal = useCallback(() => {
    if (import.meta.env.DEV) {
      console.debug('[DetalleSesion Web] refresco solicitado por SignalR')
    }
    setVersionTiempoReal(version => version + 1)
  }, [])

  const refrescarProgresoTiempoReal = useCallback(() => {
    if (import.meta.env.DEV) {
      console.debug('[DetalleSesion Web] refresco progreso solicitado por SignalR')
    }
    setVersionProgreso(v => v + 1)
  }, [])

  const refrescarRankingTiempoReal = useCallback(() => {
    if (import.meta.env.DEV) {
      console.debug('[DetalleSesion Web] refresco ranking solicitado por SignalR')
    }
    setVersionRanking(v => v + 1)
  }, [])

  const refrescarDetalleYProgresoTiempoReal = useCallback(() => {
    refrescarDetalleTiempoReal()
    refrescarProgresoTiempoReal()
  }, [refrescarDetalleTiempoReal, refrescarProgresoTiempoReal])

  const refrescarTodoTiempoReal = useCallback(() => {
    refrescarDetalleTiempoReal()
    refrescarProgresoTiempoReal()
    refrescarRankingTiempoReal()
  }, [refrescarDetalleTiempoReal, refrescarProgresoTiempoReal, refrescarRankingTiempoReal])

  const manejarSesionActualizadaTiempoReal = useCallback((estado?: string) => {
    if (estado === 'Finalizada') {
      refrescarTodoTiempoReal()
      return
    }

    refrescarDetalleTiempoReal()
  }, [refrescarDetalleTiempoReal, refrescarTodoTiempoReal])

  const manejarUbicacionActualizada = useCallback((dto: UbicacionActualizadaTR) => {
    const pid = dto.participanteIdentidadId ?? dto.ParticipanteIdentidadId ?? ''
    const nombre = dto.nombre ?? dto.Nombre ?? ''
    const lat = dto.latitud ?? dto.Latitud
    const lng = dto.longitud ?? dto.Longitud
    if (!pid || lat == null || lng == null) return
    setUbicacionesParticipantes(prev => {
      const siguiente = new Map(prev)
      siguiente.set(pid, { nombre, latitud: lat, longitud: lng })
      return siguiente
    })
  }, [])

  useSesionesTiempoReal({
    token,
    sesionId: id,
    onParticipantesSesionActualizados: refrescarDetalleTiempoReal,
    onEquiposSesionActualizados: refrescarDetalleTiempoReal,
    onEquipoActualizado: refrescarDetalleTiempoReal,
    onSesionActualizada: manejarSesionActualizadaTiempoReal,
    onParticipanteExpulsado: refrescarDetalleTiempoReal,
    onEquipoExpulsado: refrescarDetalleTiempoReal,
    onRespuestaRegistrada: refrescarProgresoTiempoReal,
    onEtapaCompletada: refrescarProgresoTiempoReal,
    onEtapaPorComenzar: refrescarDetalleYProgresoTiempoReal,
    onEtapaIniciada: refrescarDetalleYProgresoTiempoReal,
    onProgresoSecuencialActualizado: refrescarProgresoTiempoReal,
    onReconectado: refrescarDetalleYProgresoTiempoReal,
    onUbicacionActualizada: manejarUbicacionActualizada
  })

  useRankingTiempoReal({
    token,
    sesionId: id,
    onPuntajeCalculado: refrescarRankingTiempoReal,
    onRankingParticipantesActualizado: refrescarRankingTiempoReal,
    onRankingEquiposActualizado: refrescarRankingTiempoReal,
    onReconectado: refrescarRankingTiempoReal
  })

  function abrirModalEliminar() {
    setErrorEliminar(null)
    setModalEliminarAbierto(true)
  }

  function cerrarModalEliminar() {
    if (eliminando) return
    setModalEliminarAbierto(false)
    setErrorEliminar(null)
  }

  async function confirmarEliminar() {
    if (!token || !sesion) return

    setEliminando(true)
    setErrorEliminar(null)
    try {
      await eliminarSesion(sesion.id, token)
      navegar(rutaListado, { state: { mensajeExito: 'Sesión eliminada correctamente.' } })
    } catch (e) {
      // El error se muestra dentro del modal sin cerrarlo, para no perder
      // el contexto de la acción.
      setErrorEliminar(
        e instanceof Error ? e.message : 'No se pudo eliminar la sesión. Intenta nuevamente.')
      setEliminando(false)
    }
  }

  // --- HU44: expulsar participante individual ---
  function cerrarModalExpulsarParticipante() {
    if (expulsando) return
    setParticipanteAExpulsar(null)
    setErrorExpulsar(null)
  }

  async function confirmarExpulsarParticipante() {
    if (!token || !sesion || !participanteAExpulsar) return
    setExpulsando(true)
    setErrorExpulsar(null)
    try {
      await expulsarParticipanteSesion(
        sesion.id, participanteAExpulsar.participanteSesionId, token)
      setParticipanteAExpulsar(null)
      // Refetch inmediato para respuesta instantánea; SignalR también refresca.
      refrescarDetalleTiempoReal()
    } catch (e) {
      setErrorExpulsar(
        e instanceof Error ? e.message : 'No se pudo expulsar al participante. Intenta nuevamente.')
    } finally {
      setExpulsando(false)
    }
  }

  // --- HU44: expulsar equipo grupal ---
  function cerrarModalExpulsarEquipo() {
    if (expulsando) return
    setEquipoAExpulsar(null)
    setErrorExpulsar(null)
  }

  async function confirmarExpulsarEquipo() {
    if (!token || !sesion || !equipoAExpulsar) return
    setExpulsando(true)
    setErrorExpulsar(null)
    try {
      await expulsarEquipoSesion(sesion.id, equipoAExpulsar.id, token)
      setEquipoAExpulsar(null)
      refrescarDetalleTiempoReal()
    } catch (e) {
      setErrorExpulsar(
        e instanceof Error ? e.message : 'No se pudo expulsar al equipo. Intenta nuevamente.')
    } finally {
      setExpulsando(false)
    }
  }

  // --- HU52: operación de ciclo de vida (la coordina la fachada en backend) ---
  function abrirOperacion(accion: AccionOperacionSesion) {
    setErrorOperacion(null)
    setMensajeOperacion(null)
    setAccionOperacion(accion)
  }

  function cerrarModalOperacion() {
    if (operandoSesion) return
    setAccionOperacion(null)
    setErrorOperacion(null)
  }

  async function confirmarOperacion() {
    if (!token || !sesion || !accionOperacion) return
    const config = OPERACIONES_SESION[accionOperacion]
    setOperandoSesion(true)
    setErrorOperacion(null)
    try {
      const resultado = await config.ejecutar(sesion.id, token)
      // Actualizamos el estado local con el estado retornado por el backend.
      setSesion(prev => prev
        ? {
            ...prev,
            estado: resultado.estado,
            fechaInicioUtc: resultado.fechaInicioUtc,
            fechaFinalizacionUtc: resultado.fechaFinalizacionUtc
          }
        : prev)
      setAccionOperacion(null)
      setMensajeOperacion(config.exito)
      // SignalR también avisa; refrescamos para dejar el detalle consistente.
      refrescarDetalleTiempoReal()
    } catch (e) {
      setErrorOperacion(
        e instanceof Error ? e.message : 'No se pudo completar la operación. Intenta nuevamente.')
    } finally {
      setOperandoSesion(false)
    }
  }

  useEffect(() => {
    const ref = { cancelado: false }
    async function cargar() {
      if (!token || !id) {
        setEstado('error')
        setMensajeError('Identificador de sesión inválido.')
        return
      }
      try {
        if (import.meta.env.DEV) {
          console.debug('[DetalleSesion Web] cargando detalle', id)
        }
        const detalle = await obtenerSesion(id, token)
        const equipos = puedeConsultarEquipos && detalle.modo === 'Grupal'
          ? await listarEquiposSesion(id, token)
          : null
        if (ref.cancelado) return
        setSesion(detalle)
        setEquiposListado(equipos)
        setEstado('listo')
        if (import.meta.env.DEV) {
          console.debug('[DetalleSesion Web] detalle recargado', detalle.estado)
          console.debug(
            '[DetalleSesion Web] participantes individuales:',
            detalle.participantesIndividuales?.length ?? 0
          )
          if (equipos) {
            console.debug('[DetalleSesion Web] equipos recargados:', equipos.length)
          }
        }

        const inicial: MisionEnriquecida[] = detalle.misiones
          .slice()
          .sort((a, b) => a.orden - b.orden)
          .map(m => ({ id: m.misionId, orden: m.orden, etapas: [], cargando: true }))
        setMisiones(inicial)
        if (inicial.length > 0) setMisionAbierta(inicial[0].id)

        await Promise.all(inicial.map(slot => cargarMisionConEtapas(slot.id, ref)))
      } catch (e) {
        if (ref.cancelado) return
        setMensajeError(e instanceof Error ? e.message : 'No se pudo cargar el detalle de la sesión.')
        setEstado('error')
      }
    }

    async function cargarMisionConEtapas(misionId: string, ref: { cancelado: boolean }) {
      try {
        const detalleMision = await obtenerDetalleMision(misionId, token!)
        if (ref.cancelado) return

        const etapas: EtapaEnriquecida[] = detalleMision.etapas
          .slice()
          .sort((a, b) => a.orden - b.orden)
          .map(e => ({
            id: e.id,
            orden: e.orden,
            tipoModoDeJuego: e.tipoModoDeJuego,
            modoDeJuegoId: e.modoDeJuegoId,
            nombreModoDeJuego: e.nombreModoDeJuego,
            tiempoEstimado: e.tiempoEstimado
          }))

        // Resolvemos en paralelo el contenido de cada etapa.
        const etapasEnriquecidas = await Promise.all(etapas.map(async (etapa) => {
          try {
            if (etapa.tipoModoDeJuego === 'Trivia') {
              const trivia = await obtenerDetalleTrivia(etapa.modoDeJuegoId, token!)
              return { ...etapa, trivia }
            }
            if (etapa.tipoModoDeJuego === 'BusquedaTesoro') {
              const busqueda = await obtenerDetalleBusqueda(etapa.modoDeJuegoId, token!)
              return { ...etapa, busqueda }
            }
            return etapa
          } catch (e) {
            return {
              ...etapa,
              errorContenido: e instanceof Error
                ? e.message
                : 'No fue posible cargar el contenido de la etapa.'
            }
          }
        }))

        if (ref.cancelado) return
        setMisiones(prev => prev.map(m => m.id === misionId
          ? { ...m, mision: detalleMision, etapas: etapasEnriquecidas, cargando: false }
          : m))
      } catch (e) {
        if (ref.cancelado) return
        setMisiones(prev => prev.map(m => m.id === misionId
          ? {
              ...m,
              cargando: false,
              error: e instanceof Error ? e.message : 'No se pudo cargar la misión.'
            }
          : m))
      }
    }

    cargar()
    return () => { ref.cancelado = true }
  }, [token, id, puedeConsultarEquipos, versionTiempoReal])

  // Progreso completo (trivia + tesoro): se carga cuando la sesión está Activa, Pausada o Finalizada.
  useEffect(() => {
    if (!token || !id || !sesion) return
    const estadosConProgreso = ['Activa', 'Pausada', 'Finalizada']
    if (!estadosConProgreso.includes(sesion.estado)) return

    let cancelado = false
    setCargandoProgreso(true)
    setErrorProgreso(null)
    obtenerProgresoSesion(id, token)
      .then(data => { if (!cancelado) { setProgresoSesion(data); setErrorProgreso(null) } })
      .catch(e => { if (!cancelado) { setProgresoSesion(null); setErrorProgreso(e instanceof Error ? e.message : 'Error al cargar el progreso') } })
      .finally(() => { if (!cancelado) setCargandoProgreso(false) })

    return () => { cancelado = true }
  }, [token, id, sesion?.estado, versionProgreso])

  // Ranking en tiempo real: se carga cuando la sesión tiene actividad.
  useEffect(() => {
    if (!token || !id || !sesion) return
    const estadosConRanking = ['Activa', 'Pausada', 'Finalizada']
    if (!estadosConRanking.includes(sesion.estado)) return

    const esGrupalLocal = sesion.modo === 'Grupal'
    let cancelado = false
    setCargandoRanking(true)
    setErrorRanking(null)

    const promesas: Promise<void>[] = esGrupalLocal
      ? [
          obtenerRankingEquipos(id, token)
            .then(data => {
              if (!cancelado) {
                setRankingEquipos(data)
                setRankingParticipantes(null)
              }
            })
            .catch(e => { if (!cancelado) setErrorRanking(e instanceof Error ? e.message : 'Error al cargar ranking') })
        ]
      : [
          obtenerRankingParticipantes(id, token)
            .then(data => {
              if (!cancelado) {
                setRankingParticipantes(data)
                setRankingEquipos(null)
              }
            })
            .catch(e => { if (!cancelado) setErrorRanking(e instanceof Error ? e.message : 'Error al cargar ranking') })
        ]

    void Promise.all(promesas).finally(() => { if (!cancelado) setCargandoRanking(false) })

    return () => { cancelado = true }
  }, [token, id, sesion?.estado, sesion?.modo, versionRanking])

  if (estado === 'cargando') {
    return (
      <LayoutPanel titulo="Detalle de sesión" descripcion="Cargando…">
        <section className="seccion">
          <p className="detalle-mensaje-vacio">Cargando detalle de la sesión…</p>
        </section>
      </LayoutPanel>
    )
  }

  if (estado === 'error' || !sesion) {
    return (
      <LayoutPanel titulo="Detalle de sesión" descripcion="">
        <div style={{ marginBottom: 'var(--espacio-4)' }}>
          <Boton variante="volver" onClick={() => navegar(rutaListado)}>
            ← Volver a sesiones
          </Boton>
        </div>
        <section className="seccion">
          <Alerta tono="error">
            {mensajeError ?? 'No se pudo cargar el detalle de la sesión.'}
          </Alerta>
        </section>
      </LayoutPanel>
    )
  }

  const esGrupal = sesion.modo === 'Grupal'
  // HU44 — Solo el Operador puede expulsar, y solo con la sesión En
  // Preparación o Pausada. En Activa mostramos una pista para pausar primero.
  const puedeExpulsar =
    esOperador && (sesion.estado === 'EnPreparacion' || sesion.estado === 'Pausada')
  const expulsarBloqueadoPorActiva = esOperador && sesion.estado === 'Activa'
  const equiposMostrados: EquipoSesionListadoDto[] = equiposListado ?? sesion.equipos.map(eq => ({
    id: eq.id,
    sesionId: sesion.id,
    nombre: eq.nombre,
    tipo: eq.tipo,
    puntaje: eq.puntajeActual,
    cantidadParticipantes: eq.participantes.length,
    capacidadMaxima: eq.capacidadMaxima,
    estaLleno: eq.participantes.length >= eq.capacidadMaxima,
    fechaCreacion: eq.fechaCreacion,
    esMiEquipo: false,
    soyLider: false
  }))
  const filasProgreso = progresoSesion?.filas ?? []
  const tituloProgreso = esGrupal ? 'Progreso por equipo' : 'Progreso por participante'
  const descripcionProgreso = esGrupal
    ? 'Trivia y b\u00fasqueda del tesoro \u2014 avance de ejecuci\u00f3n por equipo.'
    : 'Trivia y b\u00fasqueda del tesoro \u2014 avance de ejecuci\u00f3n por participante.'
  const etiquetaUbicacionProgreso =
    progresoSesion?.ordenMisionActual && progresoSesion?.ordenEtapaActual
      ? `Misi\u00f3n ${progresoSesion.ordenMisionActual} \u00b7 Etapa ${progresoSesion.ordenEtapaActual}`
      : sesion.estado === 'Finalizada'
        ? 'Ejecuci\u00f3n finalizada'
        : null
  const resolverNombreProgreso = (p: ProgresoSesionParticipanteDto) => {
    if (esGrupal) {
      const equipo = p.equipoId
        ? equiposMostrados.find(e => e.id === p.equipoId)
        : null
      return equipo?.nombre ?? 'Equipo'
    }

    const participante = sesion.participantesIndividuales.find(
      pi => pi.participanteIdentidadId === p.participanteIdentidadId
    )
    return participante
      ? (participante.alias || `${participante.nombre} ${participante.apellido}`.trim() || 'Participante')
      : 'Participante'
  }

  return (
    <LayoutPanel
      titulo="Detalle de sesión"
      descripcion="Información completa de la sesión y de sus misiones asociadas."
    >
      <div
        style={{
          marginBottom: 'var(--espacio-4)',
          display: 'flex',
          justifyContent: 'space-between',
          alignItems: 'center',
          gap: 'var(--espacio-3)',
          flexWrap: 'wrap'
        }}
      >
        <Boton variante="volver" onClick={() => navegar(rutaListado)}>
          ← Volver a sesiones
        </Boton>

        {/* Acciones de escritura: solo Operador. */}
        {esOperador && (
          <div style={{ display: 'flex', gap: 'var(--espacio-3)', flexWrap: 'wrap' }}>
            {/* Editar: activo solo si la sesión está Programada. */}
            {sesion.estado === 'Programada' ? (
              <Boton
                variante="primario"
                onClick={() => navegar(`${rutaListado}/${sesion.id}/editar`)}
                disabled={eliminando}
              >
                Editar
              </Boton>
            ) : (
              <Boton
                variante="primario"
                disabled
                title="Solo se pueden editar sesiones en estado Programada."
              >
                Editar
              </Boton>
            )}

            {/* Eliminar: solo visible si la sesión está Programada (HU39). */}
            {sesion.estado === 'Programada' && (
              <Boton
                variante="peligro"
                onClick={abrirModalEliminar}
                disabled={eliminando}
              >
                Eliminar
              </Boton>
            )}

            {/* HU52 — Operación del ciclo de vida. El backend valida cada
                transición con la fachada y el patrón State. */}
            {(sesion.estado === 'EnPreparacion' || sesion.estado === 'Programada') && (
              <Boton
                variante="primario"
                onClick={() => abrirOperacion('iniciar')}
                disabled={operandoSesion}
              >
                Iniciar
              </Boton>
            )}
            {sesion.estado === 'Activa' && (
              <Boton
                variante="secundario"
                onClick={() => abrirOperacion('pausar')}
                disabled={operandoSesion}
              >
                Pausar
              </Boton>
            )}
            {sesion.estado === 'Pausada' && (
              <Boton
                variante="primario"
                onClick={() => abrirOperacion('reanudar')}
                disabled={operandoSesion}
              >
                Reanudar
              </Boton>
            )}
            {(sesion.estado === 'EnPreparacion' ||
              sesion.estado === 'Activa' ||
              sesion.estado === 'Pausada') && (
              <Boton
                variante="peligro"
                onClick={() => abrirOperacion('cancelar')}
                disabled={operandoSesion}
              >
                Cancelar
              </Boton>
            )}
          </div>
        )}
      </div>

      {mensajeExito && <Alerta tono="exito">{mensajeExito}</Alerta>}
      {mensajeOperacion && <Alerta tono="exito">{mensajeOperacion}</Alerta>}

      {/* Cabecera con nombre + badge de estado + código de acceso */}
      <section className="seccion">
        <div className="detalle-sesion-cabecera">
          <div>
            <h2>{sesion.nombre}</h2>
            <p>Identificador: {sesion.id}</p>
          </div>
          <BadgeEstadoSesion estado={sesion.estado} />
        </div>

        <div className="detalle-grilla">
          <div className="detalle-campo">
            <span className="detalle-campo-etiqueta">Descripción</span>
            <span className="detalle-campo-valor">{sesion.descripcion || '—'}</span>
          </div>
          <div className="detalle-campo">
            <span className="detalle-campo-etiqueta">Modo</span>
            <span className="detalle-campo-valor">{nombreModoSesion(sesion.modo)}</span>
          </div>
          <div className="detalle-campo">
            <span className="detalle-campo-etiqueta">Código de acceso</span>
            <span className="detalle-campo-valor" style={{ fontFamily: 'monospace', fontSize: '1.05rem' }}>
              {sesion.codigoAcceso || '—'}
            </span>
          </div>
          <div className="detalle-campo">
            <span className="detalle-campo-etiqueta">Fecha programada</span>
            <span className="detalle-campo-valor">{formatearFechaSesion(sesion.fechaProgramada)}</span>
          </div>
          <div className="detalle-campo">
            <span className="detalle-campo-etiqueta">Fecha de creación</span>
            <span className="detalle-campo-valor">{formatearFechaSesion(sesion.fechaCreacion)}</span>
          </div>
          {sesion.fechaInicioUtc && (
            <div className="detalle-campo">
              <span className="detalle-campo-etiqueta">Inicio</span>
              <span className="detalle-campo-valor">{formatearFechaSesion(sesion.fechaInicioUtc)}</span>
            </div>
          )}
          {sesion.fechaFinalizacionUtc && (
            <div className="detalle-campo">
              <span className="detalle-campo-etiqueta">Finalización</span>
              <span className="detalle-campo-valor">{formatearFechaSesion(sesion.fechaFinalizacionUtc)}</span>
            </div>
          )}
        </div>
      </section>

      {/* Misiones asociadas con acordeón por misión */}
      <section className="seccion">
        <div className="detalle-subtitulo">
          <div>
            <h3>Misiones asociadas</h3>
            <p>{misiones.length} misión(es) ordenadas según su orden de ejecución.</p>
          </div>
        </div>

        {misiones.length === 0 && (
          <p className="detalle-mensaje-vacio">Esta sesión no tiene misiones asociadas.</p>
        )}

        <div className="lista-misiones-detalle">
          {misiones.map((m) => {
            const abierta = misionAbierta === m.id
            return (
              <article key={m.id} className="mision-card">
                <button
                  type="button"
                  className="mision-card-cabecera"
                  onClick={() => setMisionAbierta(abierta ? null : m.id)}
                  aria-expanded={abierta}
                >
                  <div>
                    <strong>
                      {m.orden}. {m.mision?.nombre ?? (m.cargando ? 'Cargando…' : `Misión ${m.id}`)}
                    </strong>
                    {m.mision?.descripcion && (
                      <p style={{ margin: '4px 0 0', fontSize: '0.85rem', opacity: 0.8 }}>
                        {m.mision.descripcion}
                      </p>
                    )}
                    {m.mision && (
                      <small>
                        Dificultad: {m.mision.dificultad} ·
                        Tiempo total: {formatearDuracionSegundos(m.mision.tiempoTotal)} ·
                        Etapas: {m.mision.etapas.length} ·
                        Estado: {m.mision.estado}
                      </small>
                    )}
                  </div>
                  <span aria-hidden="true">{abierta ? '▾' : '▸'}</span>
                </button>

                {abierta && (
                  <div className="mision-card-cuerpo">
                    {m.cargando && <p className="detalle-mensaje-vacio">Cargando misión…</p>}
                    {m.error && <Alerta tono="error">{m.error}</Alerta>}
                    {!m.cargando && !m.error && m.etapas.length === 0 && (
                      <p className="detalle-mensaje-vacio">Esta misión no tiene etapas.</p>
                    )}
                    {m.etapas.map(etapa => (
                      <div key={etapa.id} className="etapa-card">
                        <div className="detalle-subtitulo">
                          <div>
                            <h4>Etapa {etapa.orden}: {etapa.nombreModoDeJuego}</h4>
                            <small>
                              Tipo de contenido: {nombreTipoContenidoEtapa(etapa.tipoModoDeJuego)} ·
                              Tiempo estimado: {formatearDuracionSegundos(etapa.tiempoEstimado)}
                            </small>
                          </div>
                        </div>

                        {etapa.errorContenido && (
                          <Alerta tono="error">{etapa.errorContenido}</Alerta>
                        )}

                        {etapa.trivia && <BloqueTrivia trivia={etapa.trivia} />}
                        {etapa.busqueda && (
                          <BloqueBusqueda
                            busqueda={etapa.busqueda}
                            sesionId={sesion.id}
                            etapaId={etapa.id}
                            sesionActiva={sesion.estado === 'Activa'}
                            esOperador={esOperador}
                            ubicacionesParticipantes={ubicacionesParticipantes}
                          />
                        )}
                        {!etapa.trivia && !etapa.busqueda && !etapa.errorContenido && (
                          <p className="detalle-mensaje-vacio">Cargando contenido de la etapa…</p>
                        )}
                      </div>
                    ))}
                  </div>
                )}
              </article>
            )
          })}
        </div>
      </section>

      {/* Participantes individuales (modo Individual) */}
      {!esGrupal && (
        <section className="seccion">
          <div className="detalle-subtitulo">
            <div>
              <h3>Participantes individuales</h3>
              <p>Participantes: {sesion.participantesIndividuales.length} / {sesion.maximoParticipantes ?? '—'}</p>
            </div>
          </div>
          {expulsarBloqueadoPorActiva && (
            <Alerta tono="aviso">
              Pausa la sesión para poder expulsar participantes.
            </Alerta>
          )}
          {sesion.participantesIndividuales.length === 0 ? (
            <p className="detalle-mensaje-vacio">
              Aún no hay participantes unidos a esta sesión.
            </p>
          ) : (
            <table className="tabla-usuarios">
              <thead>
                <tr>
                  <th>#</th>
                  <th>Alias</th>
                  <th>Nombre</th>
                  <th>Apellido</th>
                  <th>Fecha de unión</th>
                  {puedeExpulsar && <th>Acciones</th>}
                </tr>
              </thead>
              <tbody>
                {sesion.participantesIndividuales.map((p, idx) => (
                  <tr key={p.participanteSesionId}>
                    <td>{idx + 1}</td>
                    <td>{p.alias || '—'}</td>
                    <td>{p.nombre || '—'}</td>
                    <td>{p.apellido || '—'}</td>
                    <td>{formatearFechaSesion(p.fechaUnion)}</td>
                    {puedeExpulsar && (
                      <td>
                        <Boton
                          variante="peligro"
                          tamaño="sm"
                          onClick={() => { setErrorExpulsar(null); setParticipanteAExpulsar(p) }}
                        >
                          Expulsar
                        </Boton>
                      </td>
                    )}
                  </tr>
                ))}
              </tbody>
            </table>
          )}
        </section>
      )}

      {/* Equipos (modo Grupal) */}
      {esGrupal && (
        <section className="seccion">
          <div className="detalle-subtitulo">
            <div>
              <h3>Equipos</h3>
              <p>Equipos: {equiposMostrados.length} / {sesion.maximoEquipos ?? '—'}</p>
            </div>
          </div>
          {expulsarBloqueadoPorActiva && (
            <Alerta tono="aviso">
              Pausa la sesión para poder expulsar equipos.
            </Alerta>
          )}
          {equiposMostrados.length === 0 ? (
            <p className="detalle-mensaje-vacio">
              Aún no hay equipos unidos a esta sesión.
            </p>
          ) : (
            <table className="tabla-usuarios">
              <thead>
                <tr>
                  <th>#</th>
                  <th>Nombre del equipo</th>
                  <th>Tipo</th>
                  <th>Integrantes</th>
                  <th>Fecha de creación</th>
                  <th>Acciones</th>
                </tr>
              </thead>
              <tbody>
                {equiposMostrados.map((eq, idx) => (
                  <tr key={eq.id}>
                    <td>{idx + 1}</td>
                    <td><strong className="equipo-nombre-listado">{eq.nombre}</strong></td>
                    <td>
                      <span className={`badge ${eq.tipo === 'Publico' ? 'badge-equipo-publico' : 'badge-equipo-privado'}`}>
                        {eq.tipo === 'Publico' ? 'Público' : 'Privado'}
                      </span>
                    </td>
                    <td>{eq.cantidadParticipantes} / {eq.capacidadMaxima}</td>
                    <td>{formatearFechaSesion(eq.fechaCreacion)}</td>
                    <td>
                      <div style={{ display: 'flex', gap: 'var(--espacio-2)', flexWrap: 'wrap' }}>
                        <Boton
                          variante="primario"
                          tamaño="sm"
                          onClick={() => navegar(`${rutaBaseEquipos}/${eq.id}`)}
                        >
                          Ver
                        </Boton>
                        {puedeExpulsar && (
                          <Boton
                            variante="peligro"
                            tamaño="sm"
                            onClick={() => { setErrorExpulsar(null); setEquipoAExpulsar(eq) }}
                          >
                            Expulsar
                          </Boton>
                        )}
                      </div>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          )}
        </section>
      )}

      {/* Ranking en tiempo real — visible cuando la sesión tiene actividad */}
      {(sesion.estado === 'Activa' || sesion.estado === 'Pausada' || sesion.estado === 'Finalizada') && (
        <section className="seccion">
          <div className="detalle-subtitulo">
            <div>
              <h3>Ranking de la sesión</h3>
              <p>Clasificación en tiempo real por puntaje acumulado.</p>
            </div>
          </div>

          {cargandoRanking && <p className="detalle-mensaje-vacio">Cargando ranking…</p>}
          {!cargandoRanking && errorRanking && <Alerta tono="aviso">{errorRanking}</Alerta>}

          {/* Ranking de equipos (solo modo grupal) */}
          {!cargandoRanking && !errorRanking && esGrupal && rankingEquipos !== null && rankingEquipos.length > 0 && (
            <div style={{ marginBottom: 'var(--espacio-4)' }}>
              <h4 style={{ marginBottom: 'var(--espacio-2)' }}>Equipos</h4>
              <div style={{ overflowX: 'auto' }}>
                <table className="tabla-usuarios">
                  <thead>
                    <tr>
                      <th>#</th>
                      <th>Equipo</th>
                      <th>Puntaje</th>
                      <th aria-label="Detalle" />
                    </tr>
                  </thead>
                  <tbody>
                    {rankingEquipos
                      .slice()
                      .sort((a, b) => a.posicion - b.posicion)
                      .map(e => {
                        const expandido = equiposExpandidos.has(e.equipoId)
                        return (
                          <Fragment key={e.equipoId}>
                            <tr>
                              <td>
                                <strong style={{ fontSize: '1.1rem' }}>
                                  {e.posicion === 1 ? '🥇' : e.posicion === 2 ? '🥈' : e.posicion === 3 ? '🥉' : `#${e.posicion}`}
                                </strong>
                              </td>
                              <td><strong>{e.nombreEquipo}</strong></td>
                              <td><strong style={{ color: 'var(--color-primario, #6366f1)' }}>{e.puntaje} pts</strong></td>
                              <td>
                                {e.participantes.length > 0 && (
                                  <Boton
                                    variante="secundario"
                                    onClick={() => alternarEquipoExpandido(e.equipoId)}
                                  >
                                    {expandido ? '▲ Ocultar detalle' : '▼ Ver detalle'}
                                  </Boton>
                                )}
                              </td>
                            </tr>
                            {expandido && e.participantes.map(p => (
                              <tr key={`${e.equipoId}-${p.participanteSesionId}`}>
                                <td>#{p.posicion}</td>
                                <td style={{ paddingLeft: 'var(--espacio-4)', opacity: 0.85 }}>
                                  {p.alias}
                                </td>
                                <td>{p.puntaje} pts</td>
                                <td />
                              </tr>
                            ))}
                          </Fragment>
                        )
                      })}
                  </tbody>
                </table>
              </div>
            </div>
          )}

          {/* Ranking de participantes */}
          {!cargandoRanking && !errorRanking && !esGrupal && rankingParticipantes !== null && rankingParticipantes.length === 0 && (
            <p className="detalle-mensaje-vacio">Aún no hay actividad registrada en el ranking.</p>
          )}
          {!cargandoRanking && !errorRanking && !esGrupal && rankingParticipantes !== null && rankingParticipantes.length > 0 && (
            <div>
              <div style={{ overflowX: 'auto' }}>
                <table className="tabla-usuarios">
                  <thead>
                    <tr>
                      <th>#</th>
                      <th>Participante</th>
                      <th>Puntaje</th>
                    </tr>
                  </thead>
                  <tbody>
                    {rankingParticipantes
                      .slice()
                      .sort((a, b) => a.posicion - b.posicion)
                      .map(p => (
                        <tr key={p.participanteSesionId}>
                          <td>
                            <strong style={{ fontSize: '1.1rem' }}>
                              {p.posicion === 1 ? '🥇' : p.posicion === 2 ? '🥈' : p.posicion === 3 ? '🥉' : `#${p.posicion}`}
                            </strong>
                          </td>
                          <td>{p.alias}</td>
                          <td><strong style={{ color: 'var(--color-primario, #6366f1)' }}>{p.puntaje} pts</strong></td>
                        </tr>
                      ))}
                  </tbody>
                </table>
              </div>
            </div>
          )}
        </section>
      )}

      {/* Panel de progreso completo — visible cuando la sesión tiene actividad */}
      {(sesion.estado === 'Activa' || sesion.estado === 'Pausada' || sesion.estado === 'Finalizada') && (
        <section className="seccion">
          <div className="detalle-subtitulo">
            <div>
              <h3>{tituloProgreso}</h3>
              <p>{descripcionProgreso}</p>
              {etiquetaUbicacionProgreso && (
                <span
                  className="badge"
                  style={{
                    display: 'inline-block',
                    marginTop: 'var(--espacio-2)',
                    background: 'var(--color-fondo-tarjeta)',
                    border: '1px solid var(--color-borde-tarjeta)'
                  }}
                >
                  {etiquetaUbicacionProgreso}
                </span>
              )}
            </div>
          </div>
          {cargandoProgreso && (
            <p className="detalle-mensaje-vacio">Cargando progreso…</p>
          )}
          {!cargandoProgreso && errorProgreso && (
            <Alerta tono="aviso">{errorProgreso}</Alerta>
          )}
          {!cargandoProgreso && !errorProgreso && progresoSesion !== null && filasProgreso.length === 0 && (
            <p className="detalle-mensaje-vacio">Aún no hay actividad registrada.</p>
          )}
          {!cargandoProgreso && !errorProgreso && progresoSesion !== null && filasProgreso.length > 0 && (
            <div style={{ overflowX: 'auto' }}>
              <table className="tabla-usuarios">
                <thead>
                  <tr>
                    <th>#</th>
                    <th>{esGrupal ? 'Equipo' : 'Participante'}</th>
                    <th title="Etapas de trivia completadas">Trivia completadas</th>
                    <th title="Respuestas correctas" style={{ color: 'var(--color-exito, #22c55e)' }}>✓ Correctas</th>
                    <th title="Respuestas incorrectas" style={{ color: 'var(--color-error, #ef4444)' }}>✗ Incorrectas</th>
                    <th title="Etapas de tesoro completadas">Tesoro completadas</th>
                  </tr>
                </thead>
                <tbody>
                  {filasProgreso
                    .slice()
                    .sort((a, b) =>
                      b.triviaEtapasCompletadas + b.tesoroEtapasCompletadas
                      - (a.triviaEtapasCompletadas + a.tesoroEtapasCompletadas))
                    .map((p, idx) => {
                      const nombre = resolverNombreProgreso(p)
                      return (
                        <tr key={p.equipoId ?? p.participanteIdentidadId}>
                          <td>{idx + 1}</td>
                          <td>{nombre}</td>
                          <td>{p.triviaEtapasCompletadas}</td>
                          <td style={{ color: 'var(--color-exito, #22c55e)', fontWeight: 600 }}>
                            {p.triviaCorrectas}
                          </td>
                          <td style={{ color: 'var(--color-error, #ef4444)', fontWeight: 600 }}>
                            {p.triviaIncorrectas}
                          </td>
                          <td style={{ color: 'var(--color-exito, #22c55e)', fontWeight: 600 }}>
                            {p.tesoroEtapasCompletadas}
                          </td>
                        </tr>
                      )
                    })}
                </tbody>
              </table>
            </div>
          )}
        </section>
      )}

      <ModalConfirmacion
        abierto={modalEliminarAbierto}
        titulo="Eliminar sesión"
        textoConfirmar="Eliminar sesión"
        textoCancelar="Cancelar"
        procesando={eliminando}
        mensajeError={errorEliminar}
        onConfirmar={confirmarEliminar}
        onCancelar={cerrarModalEliminar}
      >
        <p>
          ¿Estás seguro de que deseas eliminar esta sesión? Esta acción no se
          puede deshacer.
        </p>
        <p style={{ fontSize: '0.85rem', opacity: 0.8 }}>
          Se eliminará la sesión y sus registros asociados.
        </p>
      </ModalConfirmacion>

      {/* HU44 — Confirmar expulsión de participante individual */}
      <ModalConfirmacion
        abierto={participanteAExpulsar !== null}
        titulo="Expulsar participante"
        textoConfirmar="Expulsar"
        textoCancelar="Cancelar"
        procesando={expulsando}
        mensajeError={errorExpulsar}
        onConfirmar={confirmarExpulsarParticipante}
        onCancelar={cerrarModalExpulsarParticipante}
      >
        <p>¿Seguro que deseas expulsar a este participante de la sesión?</p>
        {participanteAExpulsar && (
          <p style={{ fontSize: '0.85rem', opacity: 0.8 }}>
            {participanteAExpulsar.alias || participanteAExpulsar.nombre || 'Participante'}
          </p>
        )}
      </ModalConfirmacion>

      {/* HU44 — Confirmar expulsión de equipo grupal */}
      <ModalConfirmacion
        abierto={equipoAExpulsar !== null}
        titulo="Expulsar equipo"
        textoConfirmar="Expulsar equipo"
        textoCancelar="Cancelar"
        procesando={expulsando}
        mensajeError={errorExpulsar}
        onConfirmar={confirmarExpulsarEquipo}
        onCancelar={cerrarModalExpulsarEquipo}
      >
        <p>
          ¿Seguro que deseas expulsar este equipo? Todos sus integrantes saldrán
          de la sesión.
        </p>
        {equipoAExpulsar && (
          <p style={{ fontSize: '0.85rem', opacity: 0.8 }}>{equipoAExpulsar.nombre}</p>
        )}
      </ModalConfirmacion>

      {/* HU52 — Confirmar operación de ciclo de vida (iniciar/pausar/reanudar/
          cancelar). El backend aplica la regla y devuelve el estado resultante. */}
      <ModalConfirmacion
        abierto={accionOperacion !== null}
        titulo={accionOperacion ? OPERACIONES_SESION[accionOperacion].titulo : ''}
        textoConfirmar={accionOperacion ? OPERACIONES_SESION[accionOperacion].textoConfirmar : ''}
        textoCancelar="Volver"
        procesando={operandoSesion}
        mensajeError={errorOperacion}
        onConfirmar={confirmarOperacion}
        onCancelar={cerrarModalOperacion}
      >
        <p>{accionOperacion ? OPERACIONES_SESION[accionOperacion].confirmacion : ''}</p>
      </ModalConfirmacion>
    </LayoutPanel>
  )
}

function BloqueTrivia({ trivia }: { trivia: TriviaDetalleDto }) {
  return (
    <div>
      <p style={{ marginTop: 12 }}>
        <strong>Trivia:</strong> {trivia.nombre}
        {trivia.descripcion && <span> — {trivia.descripcion}</span>}
      </p>
      {trivia.preguntas.length === 0 ? (
        <p className="detalle-mensaje-vacio">Esta trivia no tiene preguntas cargadas.</p>
      ) : (
        <div className="lista-preguntas">
          {trivia.preguntas.map((p, idx) => (
            <article key={p.id} className="pregunta-card">
              <div className="pregunta-card-cabecera">
                <div className="pregunta-card-info">
                  <span className="pregunta-numero">Pregunta {idx + 1}</span>
                </div>
                <span className="pregunta-puntaje">{p.puntajeAsignado} pts</span>
              </div>
              <p className="pregunta-enunciado">{p.enunciado}</p>
              <ul className="pregunta-opciones">
                {p.opciones.map(o => (
                  <li
                    key={o.id}
                    className={`pregunta-opcion${o.esCorrecta ? ' pregunta-opcion-correcta' : ''}`}
                  >
                    {o.esCorrecta && (
                      <span className="opcion-check-icono" aria-hidden="true">✓</span>
                    )}
                    <span style={{ flex: 1 }}>{o.texto}</span>
                    {o.esCorrecta && (
                      <span className="badge badge-sesion-activa">Correcta</span>
                    )}
                  </li>
                ))}
              </ul>
            </article>
          ))}
        </div>
      )}
    </div>
  )
}

function BloqueBusqueda({
  busqueda,
  sesionId,
  etapaId,
  sesionActiva,
  esOperador,
  ubicacionesParticipantes
}: {
  busqueda: BusquedaTesoroDetalleDto
  sesionId: string
  etapaId: string
  sesionActiva: boolean
  esOperador: boolean
  ubicacionesParticipantes?: Map<string, { nombre: string, latitud: number, longitud: number }>
}) {
  const { token } = usarAutenticacion()
  const [liberando, setLiberando] = useState<string | null>(null)
  const [pistasLiberadasIds, setPistasLiberadasIds] = useState<Set<string>>(new Set())
  const [pistaPersonalizada, setPistaPersonalizada] = useState('')
  const [enviandoPersonalizada, setEnviandoPersonalizada] = useState(false)
  const [errorPista, setErrorPista] = useState<string | null>(null)
  const [exitoPista, setExitoPista] = useState<string | null>(null)

  const urlQr = busqueda.codigoQr
    ? `https://api.qrserver.com/v1/create-qr-code/?size=200x200&data=${encodeURIComponent(busqueda.codigoQr)}`
    : null

  const pistaGps = esOperador && sesionActiva
    ? busqueda.pistas.find(p => p.tipo === 'CoordenadaGps' && p.latitud != null && p.longitud != null)
    : undefined

  const marcadoresParticipantes: MarcadorMapa[] = pistaGps
    ? [...(ubicacionesParticipantes?.entries() ?? [])].map(([id, u]) => ({
        id,
        latitud: u.latitud,
        longitud: u.longitud,
        etiqueta: u.nombre,
        color: '#f97316'
      }))
    : []

  async function handleLiberarPista(
    pistaId: string,
    contenido: string,
    tipo?: 'Texto' | 'CoordenadaGps',
    latitud?: number,
    longitud?: number
  ) {
    if (!token || liberando) return
    setLiberando(pistaId)
    setErrorPista(null)
    setExitoPista(null)
    try {
      await liberarPista(sesionId, etapaId, pistaId, contenido, token, tipo, latitud, longitud)
      setPistasLiberadasIds(prev => new Set([...prev, pistaId]))
      const desc = tipo === 'CoordenadaGps'
        ? `GPS: ${latitud?.toFixed(4)}, ${longitud?.toFixed(4)}`
        : `"${contenido.slice(0, 40)}${contenido.length > 40 ? '…' : ''}"`
      setExitoPista(`Pista liberada: ${desc}`)
    } catch (e) {
      setErrorPista(e instanceof Error ? e.message : 'No se pudo liberar la pista.')
    } finally {
      setLiberando(null)
    }
  }

  async function handleLiberarPersonalizada() {
    if (!token || !pistaPersonalizada.trim() || enviandoPersonalizada) return
    setEnviandoPersonalizada(true)
    setErrorPista(null)
    setExitoPista(null)
    try {
      await liberarPista(sesionId, etapaId, null, pistaPersonalizada.trim(), token)
      setExitoPista(`Pista personalizada enviada: "${pistaPersonalizada.trim().slice(0, 40)}${pistaPersonalizada.length > 40 ? '…' : ''}"`)
      setPistaPersonalizada('')
    } catch (e) {
      setErrorPista(e instanceof Error ? e.message : 'No se pudo enviar la pista.')
    } finally {
      setEnviandoPersonalizada(false)
    }
  }

  return (
    <div>
      <p style={{ marginTop: 12 }}>
        <strong>Búsqueda del Tesoro:</strong> {busqueda.nombre}
        {busqueda.descripcion && <span> — {busqueda.descripcion}</span>}
      </p>
      <p style={{ fontSize: '0.85rem', color: 'var(--color-texto-tenue)' }}>
        Tiempo: {busqueda.tiempo} min · Puntaje base: {busqueda.puntaje} pts
      </p>

      {/* QR del tesoro — solo operadores */}
      {esOperador && busqueda.codigoQr && (
        <div style={{
          marginTop: 16,
          padding: 16,
          background: 'var(--color-fondo-tarjeta)',
          border: '1px solid var(--color-borde-tarjeta)',
          borderRadius: 8,
          display: 'inline-flex',
          flexDirection: 'column',
          alignItems: 'center',
          gap: 12
        }}>
          <p style={{ fontSize: '0.75rem', textTransform: 'uppercase', letterSpacing: 1, opacity: 0.6, margin: 0 }}>
            Código QR del tesoro
          </p>
          {urlQr && (
            <img
              src={urlQr}
              alt="Código QR del tesoro"
              width={200}
              height={200}
              style={{ display: 'block', borderRadius: 4 }}
            />
          )}
          <code style={{
            fontSize: '0.75rem',
            padding: '4px 8px',
            background: 'var(--color-fondo)',
            borderRadius: 4,
            letterSpacing: 2,
            userSelect: 'all'
          }}>
            {busqueda.codigoQr}
          </code>
        </div>
      )}

      {/* Mapa de ubicaciones en tiempo real — solo operadores con sesión activa */}
      {pistaGps && pistaGps.latitud != null && pistaGps.longitud != null && (
        <div style={{ marginTop: 16 }}>
          <p style={{ fontSize: '0.85rem', fontWeight: 600, margin: '0 0 8px' }}>
            Ubicaciones en tiempo real
            {marcadoresParticipantes.length > 0 && (
              <span style={{ fontWeight: 400, color: 'var(--color-texto-tenue)', marginLeft: 8 }}>
                ({marcadoresParticipantes.length} participante{marcadoresParticipantes.length !== 1 ? 's' : ''} activo{marcadoresParticipantes.length !== 1 ? 's' : ''})
              </span>
            )}
          </p>
          <MapaLeaflet
            latitud={pistaGps.latitud}
            longitud={pistaGps.longitud}
            marcadores={marcadoresParticipantes}
            alto={320}
          />
        </div>
      )}

      {/* Pistas predefinidas */}
      {busqueda.pistas.length === 0 ? (
        <p className="detalle-mensaje-vacio">Esta búsqueda no tiene pistas registradas.</p>
      ) : (
        <ul className="lista-pistas" style={{ marginTop: 16 }}>
          {busqueda.pistas.map((p, idx) => {
            const yaLiberada = pistasLiberadasIds.has(p.id)
            const esGps = p.tipo === 'CoordenadaGps'
            return (
              <li key={p.id} className="pista-item" style={{ flexDirection: 'column', alignItems: 'stretch', gap: 8 }}>
                <div style={{ display: 'flex', alignItems: 'center', gap: 12 }}>
                <span className="pista-orden">{idx + 1}</span>
                <span style={{ flex: 1 }}>
                  {esGps ? (
                    <>
                      <span style={{ display: 'inline-block', fontSize: '0.7rem', fontWeight: 700, padding: '1px 6px', borderRadius: 4, background: '#dbeafe', color: '#1d4ed8', marginRight: 6 }}>GPS</span>
                      {p.latitud != null && p.longitud != null ? `${p.latitud.toFixed(6)}, ${p.longitud.toFixed(6)}` : 'Coordenadas no disponibles'}
                    </>
                  ) : p.contenido}
                </span>
                {esOperador && sesionActiva && (
                  <button
                    type="button"
                    disabled={yaLiberada || liberando === p.id}
                    onClick={() => void handleLiberarPista(p.id, p.contenido, p.tipo, p.latitud, p.longitud)}
                    style={{
                      fontSize: '0.8rem',
                      padding: '4px 10px',
                      borderRadius: 4,
                      border: 'none',
                      cursor: yaLiberada ? 'default' : 'pointer',
                      background: yaLiberada ? '#6b7280' : 'var(--color-exito, #22c55e)',
                      color: '#fff',
                      opacity: liberando && liberando !== p.id ? 0.5 : 1,
                      whiteSpace: 'nowrap'
                    }}
                  >
                    {yaLiberada ? 'Liberada' : liberando === p.id ? '…' : 'Liberar'}
                  </button>
                )}
                </div>
                {esGps && p.latitud != null && p.longitud != null && (
                  <div style={{ marginLeft: 36 }}>
                    <MapaLeaflet latitud={p.latitud} longitud={p.longitud} alto={200} />
                  </div>
                )}
              </li>
            )
          })}
        </ul>
      )}

      {/* Panel de pista personalizada — solo operadores con sesión activa */}
      {esOperador && sesionActiva && (
        <div style={{
          marginTop: 20,
          padding: 16,
          background: 'var(--color-fondo-tarjeta)',
          border: '1px solid var(--color-borde-tarjeta)',
          borderRadius: 8
        }}>
          <p style={{ margin: '0 0 10px', fontWeight: 600, fontSize: '0.9rem' }}>
            Enviar pista personalizada
          </p>
          <div style={{ display: 'flex', gap: 8, flexWrap: 'wrap' }}>
            <input
              type="text"
              placeholder="Escribe una pista para los participantes…"
              value={pistaPersonalizada}
              onChange={e => setPistaPersonalizada(e.target.value)}
              maxLength={1000}
              style={{
                flex: 1,
                minWidth: 200,
                padding: '8px 12px',
                borderRadius: 6,
                border: '1px solid var(--color-borde-tarjeta)',
                background: 'var(--color-fondo)',
                color: 'inherit',
                fontSize: '0.9rem'
              }}
            />
            <button
              type="button"
              disabled={enviandoPersonalizada || !pistaPersonalizada.trim()}
              onClick={() => void handleLiberarPersonalizada()}
              style={{
                padding: '8px 18px',
                borderRadius: 6,
                border: 'none',
                background: 'var(--color-primario, #6366f1)',
                color: '#fff',
                cursor: enviandoPersonalizada || !pistaPersonalizada.trim() ? 'not-allowed' : 'pointer',
                opacity: enviandoPersonalizada || !pistaPersonalizada.trim() ? 0.6 : 1,
                fontWeight: 600,
                fontSize: '0.9rem'
              }}
            >
              {enviandoPersonalizada ? 'Enviando…' : 'Enviar pista'}
            </button>
          </div>
        </div>
      )}

      {/* Mensajes de feedback */}
      {exitoPista && (
        <div style={{ marginTop: 12 }}>
          <Alerta tono="exito">{exitoPista}</Alerta>
        </div>
      )}
      {errorPista && (
        <div style={{ marginTop: 12 }}>
          <Alerta tono="error">{errorPista}</Alerta>
        </div>
      )}
    </div>
  )
}
