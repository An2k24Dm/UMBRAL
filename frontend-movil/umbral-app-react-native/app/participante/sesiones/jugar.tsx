import { useCallback, useEffect, useRef, useState } from "react";
import {
  ActivityIndicator,
  Alert,
  BackHandler,
  StyleSheet,
  Text,
  TouchableOpacity,
  View,
} from "react-native";
import { useFocusEffect, useLocalSearchParams, useNavigation, useRouter } from "expo-router";
import * as signalR from "@microsoft/signalr";
import { useAutenticacion } from "../../../autenticacion/ContextoAutenticacion";
import RutaProtegidaMovil from "../../../autenticacion/RutaProtegidaMovil";
import { PantallaBase } from "../../../componentes/PantallaBase";
import { tema } from "../../../estilos/tema";
import {
  enviarRespuestaTrivia,
  obtenerPreguntasRespondidas,
  obtenerTriviaParticipante,
  ErrorRespuestaTrivia,
  type PreguntaTrivia,
  type TriviaParticipante,
} from "../../../servicios/juegoApi";
import {
  crearConexionSesionesTiempoReal,
  esErrorNoAutenticadoTiempoReal,
  registrarEventoConexionSesionesTiempoReal,
  registrarAccesoGrupoRechazadoDev,
} from "../../../servicios/sesionesTiempoReal";
import { obtenerDetalleSesionDisponibleApi } from "../../../servicios/sesionesApi";
import { obtenerProgresoSecuencialSesionApi } from "../../../servicios/sesionesApi";
import { CronometroActivo } from "../../../servicios/cronometroActivo";
import {
  mapearEstadoSesionJuego,
  type EstadoSesionJuego,
} from "../../../servicios/estadoSesionJuego";

// Feedback final visible (ms) antes de volver al detalle al completar la etapa.
// Coincide con la ventana de feedback autoritativa del backend (CierrePendiente).
const MS_FEEDBACK_FINAL = 5000;

export default function PantallaJugar() {
  return (
    <RutaProtegidaMovil>
      <ContenidoJuego />
    </RutaProtegidaMovil>
  );
}

type EstadoPregunta =
  | "esperando"
  | "respondiendo"
  | "correcta"
  | "incorrecta"
  | "tiempo_agotado"
  | "ya_respondida"
  // El jugador ya respondió la pregunta actual y aún no llega la ventana de la
  // siguiente pregunta según el backend: se espera y se resincroniza.
  | "esperando_siguiente";

function ContenidoJuego() {
  const enrutador = useRouter();
  const navegacion = useNavigation();
  const { sesion: auth, cerrarSesion } = useAutenticacion();
  const token = auth?.tokenAcceso ?? null;

  const params = useLocalSearchParams<{
    sesionId?: string;
    misionId?: string;
    etapaId?: string;
    triviaId?: string;
  }>();

  const sesionId = params.sesionId ?? "";
  const misionId = params.misionId ?? "";
  const etapaId = params.etapaId ?? "";
  const triviaId = params.triviaId ?? "";

  const [trivia, setTrivia] = useState<TriviaParticipante | null>(null);
  const [cargando, setCargando] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [indicePregunta, setIndicePregunta] = useState(0);
  const [opcionSeleccionada, setOpcionSeleccionada] = useState<string | null>(null);
  const [estadoPregunta, setEstadoPregunta] = useState<EstadoPregunta>("esperando");
  const [tiempoRestante, setTiempoRestante] = useState(0);
  const [segundosRestantesEtapa, setSegundosRestantesEtapa] = useState<number | null>(null);
  const [segundosRestantesPreguntaServidor, setSegundosRestantesPreguntaServidor] = useState<number | null>(null);
  const [etapaTerminada, setEtapaTerminada] = useState(false);
  // Solo es true cuando el BACKEND confirma que TODOS los jugadores completaron
  // la etapa (respuesta HTTP etapaCompletada o evento SignalR EtapaCompletada).
  // No se infiere localmente por haber terminado el participante/equipo actual.
  const [todosCompletaron, setTodosCompletaron] = useState(false);
  const [esGrupal, setEsGrupal] = useState(false);
  const [enviando, setEnviando] = useState(false);
  const [puntosGanadosUltima, setPuntosGanadosUltima] = useState<number | null>(null);
  const [puntosAcumulados, setPuntosAcumulados] = useState(0);
  const [conflictoTipo, setConflictoTipo] = useState<"equipo" | "individual" | null>(null);
  // Countdown autoritativo de la ventana de feedback entre preguntas (segundos).
  // null = no estamos en transición. El valor viene del backend, por lo que
  // sobrevive a reconexiones (no se reinicia a 5).
  const [transicionRestanteSeg, setTransicionRestanteSeg] = useState<number | null>(null);
  // Estado local autoritativo de la sesión. Empieza "Desconocida" hasta que se
  // consulta el estado real por HTTP (no se asume "Activa").
  const [estadoSesion, setEstadoSesion] = useState<EstadoSesionJuego>("Desconocida");
  const intervalRef = useRef<ReturnType<typeof setInterval> | null>(null);
  // Mide solo el tiempo ACTIVO de la pregunta (excluye el tiempo en pausa).
  const cronometroRef = useRef(new CronometroActivo());
  // Índice mostrado (en ref, para leerlo sin cierres obsoletos en la resync).
  const indicePreguntaRef = useRef(0);
  // Previene que SignalR corte la ventana de 1500ms donde se muestra el puntaje de la última respuesta.
  const mostrandoResultadoRef = useRef(false);

  const preguntaActual: PreguntaTrivia | undefined = trivia?.preguntas[indicePregunta];
  const tiempoLimite = preguntaActual?.tiempoEstimado ?? trivia?.tiempoLimitePorPregunta ?? 10;
  const bloquearSalida = estadoSesion === "Activa" && !etapaTerminada;

  useEffect(() => {
    if (!bloquearSalida) return;

    const subscripcion = BackHandler.addEventListener("hardwareBackPress", () => true);
    const remover = (navegacion as unknown as {
      addListener?: (evento: string, callback: (event: { preventDefault: () => void }) => void) => () => void;
      setOptions?: (opciones: { gestureEnabled?: boolean }) => void;
    }).addListener?.("beforeRemove", (event) => {
      event.preventDefault();
    });

    (navegacion as unknown as { setOptions?: (opciones: { gestureEnabled?: boolean }) => void })
      .setOptions?.({ gestureEnabled: false });

    return () => {
      subscripcion.remove();
      remover?.();
      (navegacion as unknown as { setOptions?: (opciones: { gestureEnabled?: boolean }) => void })
        .setOptions?.({ gestureEnabled: true });
    };
  }, [bloquearSalida, navegacion]);

  // Consulta por HTTP el estado real de la sesión (autoritativo). Si falla, se
  // asume "Activa" para no bloquear el juego: el backend sigue siendo la
  // autoridad final y rechaza cualquier acción si la sesión no está Activa.
  const refrescarEstadoSesion = useCallback(async () => {
    if (!token || !sesionId) return;
    try {
      const detalle = await obtenerDetalleSesionDisponibleApi(token, sesionId);
      setEstadoSesion(mapearEstadoSesionJuego(detalle.estado));
      setEsGrupal(detalle.modo === "Grupal");
    } catch {
      setEstadoSesion("Desconocida");
    }
  }, [token, sesionId]);

  // Carga inicial
  useEffect(() => {
    if (!token || !sesionId || !triviaId) return;

    void (async () => {
      try {
        setCargando(true);
        const [triviaData, respondidas, progresoSecuencial] = await Promise.all([
          obtenerTriviaParticipante(sesionId, misionId, etapaId, triviaId, token),
          obtenerPreguntasRespondidas(sesionId, misionId, etapaId, token),
          obtenerProgresoSecuencialSesionApi(token, sesionId),
        ]);
        setTrivia(triviaData);
        setSegundosRestantesEtapa(progresoSecuencial.segundosRestantesEtapa ?? null);
        setSegundosRestantesPreguntaServidor(
          progresoSecuencial.triviaTiempoRestantePreguntaMs == null
            ? null
            : Math.ceil(progresoSecuencial.triviaTiempoRestantePreguntaMs / 1000),
        );
        await refrescarEstadoSesion();

        if (progresoSecuencial.triviaAgotada) {
          setEtapaTerminada(true);
          return;
        }

        // Reconexión durante el feedback entre preguntas (#5): reconstruir el
        // countdown con el restante REAL del backend (no reiniciar a 5 s).
        if (progresoSecuencial.triviaEnTransicionEntrePreguntas) {
          setTransicionRestanteSeg(
            Math.max(1, Math.ceil(
              (progresoSecuencial.triviaTiempoRestanteTransicionMs ?? 0) / 1000)),
          );
          setEstadoPregunta("esperando_siguiente");
          const idxSiguiente = progresoSecuencial.triviaSiguientePreguntaId
            ? triviaData.preguntas.findIndex(
                (p) => p.id === progresoSecuencial.triviaSiguientePreguntaId)
            : -1;
          if (idxSiguiente >= 0) setIndicePregunta(idxSiguiente);
          return;
        }

        const indicePreguntaGlobal = progresoSecuencial.triviaPreguntaActualId
          ? triviaData.preguntas.findIndex(
              (p) => p.id === progresoSecuencial.triviaPreguntaActualId,
            )
          : -1;

        if (indicePreguntaGlobal >= 0 && !respondidas.includes(triviaData.preguntas[indicePreguntaGlobal].id)) {
          setIndicePregunta(indicePreguntaGlobal);
          return;
        }

        const primeroSinResponder = triviaData.preguntas.findIndex(
          (p) => !respondidas.includes(p.id),
        );
        if (primeroSinResponder === -1) {
          setEtapaTerminada(true);
        } else {
          setIndicePregunta(primeroSinResponder);
        }
      } catch (e) {
        setError(e instanceof Error ? e.message : "Error al cargar la trivia.");
      } finally {
        setCargando(false);
      }
    })();
  }, [sesionId, misionId, etapaId, triviaId, token, refrescarEstadoSesion]);

  // Preparar una pregunta nueva: fija el tiempo restante al límite y reinicia
  // el cronómetro activo. El conteo activo real lo arranca el efecto siguiente
  // cuando la sesión está Activa.
  useEffect(() => {
    if (!trivia || etapaTerminada || estadoPregunta !== "esperando") return;
    const limite = preguntaActual?.tiempoEstimado ?? trivia.tiempoLimitePorPregunta;
    const segundosAutoritativosPregunta = segundosRestantesPreguntaServidor;
    setTiempoRestante(
      segundosAutoritativosPregunta !== null
        ? Math.min(limite, segundosAutoritativosPregunta)
        : segundosRestantesEtapa === null
          ? limite
          : Math.min(limite, segundosRestantesEtapa),
    );
    cronometroRef.current.reiniciar();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [indicePregunta, trivia, etapaTerminada, segundosRestantesEtapa, segundosRestantesPreguntaServidor]);

  // Control del intervalo + cronómetro según el estado de la sesión. El
  // temporizador SOLO corre cuando la sesión está Activa, la pregunta está en
  // espera y la etapa no terminó. Al pausar se congela (clearInterval + el
  // cronómetro deja de acumular); al reanudar continúa desde el tiempoRestante
  // actual, sin reiniciar la pregunta.
  useEffect(() => {
    const activo =
      estadoSesion === "Activa" &&
      estadoPregunta === "esperando" &&
      !etapaTerminada &&
      !!trivia;

    if (!activo) {
      cronometroRef.current.pausar();
      if (intervalRef.current) {
        clearInterval(intervalRef.current);
        intervalRef.current = null;
      }
      return;
    }

    cronometroRef.current.reanudar();
    intervalRef.current = setInterval(() => {
      setTiempoRestante((prev) => {
        if (prev <= 1) {
          if (intervalRef.current) {
            clearInterval(intervalRef.current);
            intervalRef.current = null;
          }
          void manejarTiempoAgotado();
          return 0;
        }
        return prev - 1;
      });
    }, 1000);

    return () => {
      if (intervalRef.current) {
        clearInterval(intervalRef.current);
        intervalRef.current = null;
      }
    };
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [estadoSesion, estadoPregunta, etapaTerminada, indicePregunta, trivia]);

  // Mantiene el índice mostrado en un ref (para leerlo sin cierres obsoletos).
  useEffect(() => {
    indicePreguntaRef.current = indicePregunta;
  }, [indicePregunta]);

  // Resincroniza la pregunta visual con la pregunta AUTORITATIVA del backend. No
  // se avanza por índice local: se consulta el progreso secuencial y solo se
  // avanza cuando el backend habilita una pregunta posterior a la ya respondida.
  const resincronizarPregunta = useCallback(async () => {
    if (!token || !sesionId || !trivia) return;
    let progreso;
    try {
      progreso = await obtenerProgresoSecuencialSesionApi(token, sesionId);
    } catch {
      return; // Silencioso: se reintenta al agotar la ventana o por SignalR.
    }

    if (
      progreso.triviaAgotada ||
      progreso.jugadorActualCompletoEtapaActual ||
      progreso.todoCompletado
    ) {
      setTransicionRestanteSeg(null);
      setEtapaTerminada(true);
      return;
    }

    // Feedback autoritativo entre preguntas: mantener el resultado visible y
    // mostrar el countdown REAL del backend (no reinicia al reconectar). La
    // siguiente pregunta todavía no consume su tiempo.
    if (progreso.triviaEnTransicionEntrePreguntas) {
      const restanteTransicion = Math.max(
        1, Math.ceil((progreso.triviaTiempoRestanteTransicionMs ?? 0) / 1000));
      setTransicionRestanteSeg(restanteTransicion);
      return;
    }

    const restanteSeg =
      progreso.triviaTiempoRestantePreguntaMs == null
        ? null
        : Math.max(0, Math.ceil(progreso.triviaTiempoRestantePreguntaMs / 1000));

    const idxActual = progreso.triviaPreguntaActualId
      ? trivia.preguntas.findIndex((p) => p.id === progreso.triviaPreguntaActualId)
      : -1;

    // Sin pregunta actual ni transición aún expuesta: reintentar en breve.
    if (idxActual < 0) {
      setEstadoPregunta("esperando_siguiente");
      return;
    }

    // Avanzar a la pregunta autoritativa actual del backend (ya pasó el feedback).
    setTransicionRestanteSeg(null);
    setIndicePregunta(idxActual);
    setSegundosRestantesPreguntaServidor(restanteSeg);
    setOpcionSeleccionada(null);
    setPuntosGanadosUltima(null);
    setConflictoTipo(null);
    setEstadoPregunta("esperando");
  }, [token, sesionId, trivia]);

  // Countdown de la ventana de feedback autoritativa. Cada segundo decrementa; al
  // llegar a 0 se re-consulta el progreso y, si el backend ya habilitó la
  // siguiente pregunta, se avanza. El valor inicial siempre proviene del backend.
  useEffect(() => {
    if (transicionRestanteSeg === null || estadoSesion !== "Activa") return;
    if (transicionRestanteSeg <= 0) {
      const id = setTimeout(() => void resincronizarPregunta(), 300);
      return () => clearTimeout(id);
    }
    const id = setTimeout(
      () => setTransicionRestanteSeg(transicionRestanteSeg - 1), 1000);
    return () => clearTimeout(id);
  }, [transicionRestanteSeg, estadoSesion, resincronizarPregunta]);

  // Fallback poco frecuente: no hay pregunta actual ni transición todavía
  // expuesta (p. ej. carrera de persistencia). Se re-consulta a los ~1 s.
  useEffect(() => {
    if (estadoPregunta !== "esperando_siguiente" || estadoSesion !== "Activa") return;
    if (transicionRestanteSeg !== null) return;
    const id = setTimeout(() => void resincronizarPregunta(), 1000);
    return () => clearTimeout(id);
  }, [estadoPregunta, estadoSesion, transicionRestanteSeg, resincronizarPregunta]);

  // Ref a la última versión de resincronizarPregunta, para invocarla desde el
  // manejador SignalR sin capturar una versión obsoleta (p. ej. trivia recién
  // cargada tras montar la suscripción).
  const resincronizarRef = useRef(resincronizarPregunta);
  useEffect(() => {
    resincronizarRef.current = resincronizarPregunta;
  }, [resincronizarPregunta]);

  // #10/#17/#27: al completar su Trivia, el resultado final (con los puntos)
  // permanece visible al menos 5 s antes de volver automáticamente al detalle.
  // Los 10 s de preparación NO empiezan aquí: los inicia el backend al cerrar la
  // etapa (CierrePendiente), no este redirect. Si EtapaIniciada navega antes, el
  // cleanup cancela este timer.
  useEffect(() => {
    if (!etapaTerminada || !sesionId) return;
    const id = setTimeout(() => {
      enrutador.replace(`/participante/sesiones/${sesionId}`);
    }, MS_FEEDBACK_FINAL);
    return () => clearTimeout(id);
  }, [etapaTerminada, sesionId, enrutador]);

  const manejarTiempoAgotado = useCallback(async () => {
    if (!token || !preguntaActual || !trivia || enviando) return;
    // No disparar el timeout si la sesión no está Activa (p. ej. pausada).
    if (estadoSesion !== "Activa") return;
    setEstadoPregunta("tiempo_agotado");
    setPuntosGanadosUltima(0);
    setEnviando(true);

    try {
      const resultado = await enviarRespuestaTrivia(
        sesionId, misionId, etapaId, triviaId,
        preguntaActual.id,
        null,
        tiempoLimite * 1000 + 1,
        token,
      );

      if (resultado.conflicto) {
        setConflictoTipo(resultado.conflicto);
        setEstadoPregunta("ya_respondida");
      } else {
        setConflictoTipo(null);
      }

      // Se muestra el resultado ~1 s y luego se consulta la transición
      // autoritativa (feedback de 5 s); el resultado permanece visible durante
      // el countdown que devuelve el backend.
      mostrandoResultadoRef.current = true;
      setTimeout(() => {
        mostrandoResultadoRef.current = false;
        if (resultado.etapaCompletada) {
          setTodosCompletaron(true);
          setEtapaTerminada(true);
          return;
        }
        void resincronizarPregunta();
      }, 1000);
    } catch (e) {
      if (e instanceof ErrorRespuestaTrivia && e.codigo === "OPERACION_SESION_INVALIDA") {
        // La ventana temporal cambió: resincronizar con la pregunta autoritativa.
        setEstadoPregunta("esperando_siguiente");
        void resincronizarPregunta();
      } else {
        Alert.alert("Error", e instanceof Error ? e.message : "Error al registrar el timeout.");
        setEstadoPregunta("esperando");
      }
    } finally {
      setEnviando(false);
    }
  }, [token, preguntaActual, trivia, sesionId, misionId, etapaId, triviaId, tiempoLimite, enviando, estadoSesion, resincronizarPregunta]);

  const seleccionarOpcion = async (opcionId: string) => {
    if (estadoPregunta !== "esperando" || enviando || !preguntaActual || !trivia || !token) return;

    // Protección de UX: no permitir responder si la sesión no está Activa.
    if (estadoSesion !== "Activa") {
      if (estadoSesion === "Pausada") {
        Alert.alert(
          "Sesión pausada",
          "La sesión está pausada. Espera a que el operador la reanude.",
        );
      }
      return;
    }

    if (intervalRef.current) {
      clearInterval(intervalRef.current);
      intervalRef.current = null;
    }

    // Tiempo ACTIVO real (excluye cualquier intervalo en pausa).
    const tiempoTardadoMs = cronometroRef.current.transcurridoMs();
    setOpcionSeleccionada(opcionId);
    setEnviando(true);

    try {
      const resultado = await enviarRespuestaTrivia(
        sesionId, misionId, etapaId, triviaId,
        preguntaActual.id,
        opcionId,
        tiempoTardadoMs,
        token,
      );

      // La pregunta ya estaba respondida (por el equipo en grupal, o por el
      // propio participante). No se marca como incorrecta ni suma puntos: se
      // informa y se avanza a la siguiente pregunta.
      if (resultado.conflicto) {
        setConflictoTipo(resultado.conflicto);
        setEstadoPregunta("ya_respondida");
        setPuntosGanadosUltima(0);
      } else {
        setConflictoTipo(null);
        setEstadoPregunta(resultado.esCorrecta ? "correcta" : "incorrecta");
        setPuntosGanadosUltima(resultado.puntosGanados);
        setPuntosAcumulados((prev) => prev + resultado.puntosGanados);
      }

      // Tras mostrar el resultado, NO se avanza por índice local: se resincroniza
      // con la pregunta autoritativa del backend (que respeta las ventanas
      // temporales por pregunta).
      // Se muestra el resultado ~1 s y luego se consulta la transición
      // autoritativa (feedback de 5 s); el resultado permanece visible durante
      // el countdown que devuelve el backend.
      mostrandoResultadoRef.current = true;
      setTimeout(() => {
        mostrandoResultadoRef.current = false;
        if (resultado.etapaCompletada) {
          setTodosCompletaron(true);
          setEtapaTerminada(true);
          return;
        }
        void resincronizarPregunta();
      }, 1000);
    } catch (e) {
      if (e instanceof ErrorRespuestaTrivia && e.codigo === "OPERACION_SESION_INVALIDA") {
        // La pregunta aún no está en su ventana temporal: no es error del jugador
        // ni un duplicado. Se resincroniza con la pregunta autoritativa actual.
        setEstadoPregunta("esperando_siguiente");
        setOpcionSeleccionada(null);
        void resincronizarPregunta();
      } else {
        Alert.alert("Error", e instanceof Error ? e.message : "Error al enviar respuesta.");
        setEstadoPregunta("esperando");
        setOpcionSeleccionada(null);
      }
    } finally {
      setEnviando(false);
    }
  };

  // SignalR: EtapaCompletada y SesionActualizada
  useFocusEffect(useCallback(() => {
    if (!token || !sesionId) return;

    let desmontado = false;
    let cerrando = false;
    let cierreRegistrado = false;
    let inicioPromise: Promise<void> | null = null;
    const conexion = crearConexionSesionesTiempoReal(token, "Trivia");
    const logDev = (mensaje: string) => {
      registrarEventoConexionSesionesTiempoReal(conexion, mensaje);
    };
    const registrarCerrado = () => {
      if (cierreRegistrado) return;
      cierreRegistrado = true;
      logDev("cerrado");
    };
    const manejarErrorConexion = async (error: unknown) => {
      if (desmontado || !error) return;
      if (esErrorNoAutenticadoTiempoReal(error)) {
        await cerrarSesion();
        return;
      }
      registrarAccesoGrupoRechazadoDev(error);
    };

    const manejarEtapaCompletada = (evento: { sesionId?: string; SesionId?: string; etapaId?: string; EtapaId?: string }) => {
      const sid = (evento.sesionId ?? evento.SesionId ?? "").toLowerCase();
      const eid = (evento.etapaId ?? evento.EtapaId ?? "").toLowerCase();
      if (sid === sesionId.toLowerCase() && eid === etapaId.toLowerCase()) {
        if (desmontado) return;
        // EtapaCompletada global: el backend confirma que TODOS terminaron.
        if (mostrandoResultadoRef.current) {
          setTimeout(() => {
            if (!desmontado) { setTodosCompletaron(true); setEtapaTerminada(true); }
          }, 1600);
        } else {
          setTodosCompletaron(true);
          setEtapaTerminada(true);
        }
      }
    };

    const manejarEtapaIniciada = async (evento: { sesionId?: string; SesionId?: string }) => {
      const sid = (evento.sesionId ?? evento.SesionId ?? "").toLowerCase();
      if (sid !== sesionId.toLowerCase() || desmontado || !token) return;

      const progreso = await obtenerProgresoSecuencialSesionApi(token, sesionId);
      if (
        progreso.jugadorActualCompletoEtapaActual ||
        !progreso.misionActualId ||
        !progreso.etapaActualId ||
        !progreso.modoDeJuegoId ||
        !progreso.tipoEtapaActual ||
        progreso.etapaActualId.toLowerCase() === etapaId.toLowerCase()
      ) {
        return;
      }

      const destino =
        progreso.tipoEtapaActual === "Trivia"
          ? `/participante/sesiones/jugar?sesionId=${sesionId}` +
            `&misionId=${progreso.misionActualId}` +
            `&etapaId=${progreso.etapaActualId}` +
            `&triviaId=${progreso.modoDeJuegoId}`
          : `/participante/sesiones/tesoro?sesionId=${sesionId}` +
            `&misionId=${progreso.misionActualId}` +
            `&etapaId=${progreso.etapaActualId}` +
            `&busquedaId=${progreso.modoDeJuegoId}`;

      enrutador.replace(destino);
    };

    const manejarSesionActualizada = (evento: { estado?: string; Estado?: string }) => {
      if (desmontado) return;
      const estado = evento.estado ?? evento.Estado ?? "";
      const nuevo = mapearEstadoSesionJuego(estado);

      // Pausa/reanudación: solo se actualiza el estado local; el efecto del
      // temporizador congela o reanuda el conteo desde el tiempo restante.
      if (nuevo === "Activa" || nuevo === "Pausada" || nuevo === "EnPreparacion") {
        setEstadoSesion(nuevo);
        return;
      }

      if (estado === "Cancelada" || estado === "Finalizada") {
        setEstadoSesion(nuevo);
        Alert.alert(
          "Sesión terminada",
          `La sesión ha sido ${estado.toLowerCase()}.`,
          [{ text: "Aceptar", onPress: () => enrutador.replace("/participante/sesiones") }],
        );
      }
    };

    // #9 grupal: cuando un integrante completa la última pregunta oficial del
    // equipo, el backend notifica al GrupoEquipo. Los demás integrantes
    // resincronizan por HTTP: si su equipo ya completó, la resync marca la etapa
    // terminada (y vuelve al detalle); si no, avanza a la pregunta actual.
    const manejarProgresoActualizado = () => {
      if (desmontado) return;
      // No cortar la ventana breve donde se muestra el puntaje de la respuesta.
      if (mostrandoResultadoRef.current) return;
      void resincronizarRef.current();
    };

    conexion.on("EtapaCompletada", manejarEtapaCompletada);
    conexion.on("EtapaIniciada", manejarEtapaIniciada);
    conexion.on("SesionActualizada", manejarSesionActualizada);
    conexion.on("ProgresoSecuencialActualizado", manejarProgresoActualizado);

    // Al reconectar, re-unirse al grupo (pasa de nuevo por el Proxy) y
    // resincronizar el estado real por HTTP: no se asume que no hubo cambios
    // mientras la conexión estuvo caída.
    conexion.onreconnected(async () => {
      if (desmontado) return;
      logDev("reconectado");
      await conexion.invoke("UnirseASesion", sesionId)
        .then(() => logDev("unido a sesion"))
        .catch((error: unknown) => manejarErrorConexion(error));
      await refrescarEstadoSesion();
    });
    conexion.onreconnecting((error) => {
      logDev("reconectando");
      void manejarErrorConexion(error);
    });
    conexion.onclose((error) => {
      registrarCerrado();
      void manejarErrorConexion(error);
    });

    const cerrarConexion = async () => {
      if (cerrando) return;
      cerrando = true;
      logDev("cerrando");

      await inicioPromise?.catch(() => undefined);

      if (conexion.state === signalR.HubConnectionState.Connected) {
        await conexion.invoke("SalirDeSesion", sesionId).catch(() => undefined);
      }
      if (conexion.state !== signalR.HubConnectionState.Disconnected) {
        await conexion.stop().catch(() => undefined);
      }
      if (conexion.state === signalR.HubConnectionState.Disconnected) {
        registrarCerrado();
      }
    };

    inicioPromise = conexion.start();
    void inicioPromise
      .then(async () => {
        logDev("conectado");
        if (desmontado) {
          await cerrarConexion();
          return;
        }
        await conexion.invoke("UnirseASesion", sesionId)
          .then(() => logDev("unido a sesion"))
          .catch((error: unknown) => manejarErrorConexion(error));
      })
      .catch((e: unknown) => {
        if (desmontado) return;
        void manejarErrorConexion(e);
      });

    return () => {
      desmontado = true;
      conexion.off("EtapaCompletada", manejarEtapaCompletada);
      conexion.off("EtapaIniciada", manejarEtapaIniciada);
      conexion.off("SesionActualizada", manejarSesionActualizada);
      conexion.off("ProgresoSecuencialActualizado", manejarProgresoActualizado);
      void cerrarConexion();
    };
  }, [token, sesionId, etapaId, cerrarSesion, enrutador, refrescarEstadoSesion]));

  if (cargando) {
    return (
      <PantallaBase>
        <View style={estilos.centrado}>
          <ActivityIndicator size="large" color={tema.colores.primario} />
        </View>
      </PantallaBase>
    );
  }

  if (error) {
    return (
      <PantallaBase>
        <View style={estilos.centrado}>
          <Text style={estilos.textoError}>{error}</Text>
          <TouchableOpacity
            style={estilos.boton}
            onPress={() => enrutador.back()}
          >
            <Text style={estilos.textoBoton}>Volver</Text>
          </TouchableOpacity>
        </View>
      </PantallaBase>
    );
  }

  if (etapaTerminada) {
    return (
      <PantallaBase>
        <View style={estilos.centrado}>
          <Text style={estilos.textoExito}>
            {esGrupal ? "¡Tu equipo completó esta etapa!" : "¡Has completado esta etapa!"}
          </Text>
          <View style={estilos.cajaResumen}>
            <Text style={estilos.resumenEtiqueta}>TUS PUNTOS EN ESTA ETAPA</Text>
            <Text style={estilos.resumenPuntos}>{puntosAcumulados} pts</Text>
          </View>
          <Text style={estilos.textoInfo}>
            {todosCompletaron
              ? "Todos los participantes han completado esta etapa."
              : "Espera a que los demás terminen para avanzar a la siguiente etapa."}
          </Text>
          <TouchableOpacity
            style={estilos.boton}
            onPress={() => enrutador.replace(`/participante/sesiones/${sesionId}`)}
          >
            <Text style={estilos.textoBoton}>Ver sesión</Text>
          </TouchableOpacity>
        </View>
      </PantallaBase>
    );
  }

  if (!trivia || !preguntaActual) {
    return (
      <PantallaBase>
        <View style={estilos.centrado}>
          <Text style={estilos.textoInfo}>No hay preguntas disponibles.</Text>
        </View>
      </PantallaBase>
    );
  }

  const porcentajeTiempo = tiempoRestante / tiempoLimite;
  const colorTiempo = porcentajeTiempo > 0.5
    ? tema.colores.exito
    : porcentajeTiempo > 0.25
      ? tema.colores.aviso
      : tema.colores.error;

  return (
    <PantallaBase>
      <View style={estilos.contenedor}>
        {/* Banner de pausa: no oculta el contenido; solo informa y bloquea. */}
        {estadoSesion === "Pausada" && (
          <View style={estilos.bannerPausa}>
            <Text style={estilos.bannerPausaTitulo}>SESIÓN PAUSADA</Text>
            <Text style={estilos.bannerPausaTexto}>
              El operador ha pausado la partida. Espera a que sea reanudada.
            </Text>
          </View>
        )}

        {estadoSesion === "Desconocida" && (
          <View style={estilos.bannerPausa}>
            <Text style={estilos.bannerPausaTitulo}>VERIFICANDO ESTADO</Text>
            <Text style={estilos.bannerPausaTexto}>
              Verificando estado de la sesion...
            </Text>
          </View>
        )}

        {/* Progreso y puntos acumulados */}
        <View style={estilos.filaProgreso}>
          <Text style={estilos.progreso}>
            Pregunta {indicePregunta + 1} / {trivia.preguntas.length}
          </Text>
          <Text style={estilos.puntosTotal}>
            Total: {puntosAcumulados} pts
          </Text>
        </View>

        {/* Temporizador */}
        <View style={estilos.temporizadorContenedor}>
          <View
            style={[
              estilos.temporizadorBarra,
              { width: `${porcentajeTiempo * 100}%`, backgroundColor: colorTiempo },
            ]}
          />
        </View>
        <Text style={[estilos.temporizadorTexto, { color: colorTiempo }]}>
          {tiempoRestante}s
        </Text>

        {/* Puntaje */}
        <Text style={estilos.puntaje}>
          Puntaje base: {preguntaActual.puntajeAsignado} pts
        </Text>

        {/* Enunciado */}
        <Text style={estilos.enunciado}>{preguntaActual.enunciado}</Text>

        {/* Opciones */}
        <View style={estilos.opcionesContenedor}>
          {preguntaActual.opciones.map((opcion) => {
            const seleccionada = opcionSeleccionada === opcion.id;
            let estiloOpcion = estilos.opcion;

            if (seleccionada && estadoPregunta === "correcta") {
              estiloOpcion = { ...estilos.opcion, ...estilos.opcionCorrecta };
            } else if (seleccionada && (estadoPregunta === "incorrecta" || estadoPregunta === "tiempo_agotado")) {
              estiloOpcion = { ...estilos.opcion, ...estilos.opcionIncorrecta };
            } else if (seleccionada) {
              estiloOpcion = { ...estilos.opcion, ...estilos.opcionSeleccionada };
            }

            return (
              <TouchableOpacity
                key={opcion.id}
                style={estiloOpcion}
                onPress={() => void seleccionarOpcion(opcion.id)}
                disabled={estadoPregunta !== "esperando" || enviando || estadoSesion !== "Activa"}
              >
                <Text style={estilos.textoOpcion}>{opcion.texto}</Text>
              </TouchableOpacity>
            );
          })}
        </View>

        {estadoPregunta === "esperando_siguiente" && transicionRestanteSeg === null && (
          <View style={estilos.feedbackContenedor}>
            <Text style={estilos.feedbackInfo}>¡Respuesta registrada!</Text>
            <Text style={estilos.feedbackPuntos}>Preparando la siguiente pregunta…</Text>
          </View>
        )}
      </View>

      {/* Resultado de la respuesta: overlay centrado SIEMPRE visible (no exige
          desplazarse). Permanece ≥5 s (feedback autoritativo del backend) y, entre
          preguntas, muestra la cuenta regresiva real de la transición. */}
      {(estadoPregunta === "correcta" ||
        estadoPregunta === "incorrecta" ||
        estadoPregunta === "tiempo_agotado" ||
        estadoPregunta === "ya_respondida") && (
        <View style={estilos.feedbackOverlay} pointerEvents="none">
          <View style={estilos.feedbackTarjeta}>
            {estadoPregunta === "correcta" && (
              <>
                <Text style={[estilos.feedbackIcono, { color: tema.colores.exito }]}>✓</Text>
                <Text style={estilos.feedbackCorrecto}>¡Correcto!</Text>
                <Text style={estilos.feedbackPuntos}>+{puntosGanadosUltima ?? 0} pts</Text>
              </>
            )}
            {estadoPregunta === "incorrecta" && (
              <>
                <Text style={[estilos.feedbackIcono, { color: tema.colores.error }]}>✕</Text>
                <Text style={estilos.feedbackIncorrecto}>Incorrecta</Text>
                <Text style={estilos.feedbackPuntos}>+0 pts</Text>
              </>
            )}
            {estadoPregunta === "tiempo_agotado" && (
              <>
                <Text style={[estilos.feedbackIcono, { color: tema.colores.aviso }]}>⌛</Text>
                <Text style={estilos.feedbackIncorrecto}>Tiempo agotado</Text>
                <Text style={estilos.feedbackPuntos}>+0 pts</Text>
              </>
            )}
            {estadoPregunta === "ya_respondida" && (
              <Text style={estilos.feedbackInfo}>
                {conflictoTipo === "equipo"
                  ? "Otro integrante de tu equipo ya respondió esta pregunta."
                  : "Ya habías respondido esta pregunta."}
              </Text>
            )}
            {transicionRestanteSeg !== null && (
              <View style={estilos.transicionContenedor}>
                <Text style={estilos.transicionTitulo}>Siguiente pregunta en</Text>
                <Text style={estilos.transicionCuenta}>{transicionRestanteSeg}s</Text>
              </View>
            )}
          </View>
        </View>
      )}
    </PantallaBase>
  );
}

const estilos = StyleSheet.create({
  contenedor: {
    flex: 1,
    padding: 16,
  },
  bannerPausa: {
    backgroundColor: "#fef3c7",
    borderWidth: 1,
    borderColor: "#d97706",
    borderRadius: tema.radios.tarjeta,
    padding: 12,
    marginBottom: 12,
    alignItems: "center",
  },
  bannerPausaTitulo: {
    color: "#92400e",
    fontSize: 14,
    fontWeight: "800",
    letterSpacing: 1,
    marginBottom: 2,
  },
  bannerPausaTexto: {
    color: "#92400e",
    fontSize: 13,
    textAlign: "center",
  },
  filaProgreso: {
    flexDirection: "row",
    justifyContent: "space-between",
    alignItems: "center",
    marginBottom: 8,
  },
  puntosTotal: {
    fontSize: 14,
    fontWeight: "700",
    color: tema.colores.primario,
  },
  feedbackContenedor: {
    alignItems: "center",
    marginTop: 20,
  },
  feedbackOverlay: {
    position: "absolute",
    top: 0,
    left: 0,
    right: 0,
    bottom: 0,
    alignItems: "center",
    justifyContent: "center",
    padding: 24,
    backgroundColor: "rgba(0,0,0,0.35)",
  },
  feedbackTarjeta: {
    backgroundColor: tema.colores.fondoTarjeta,
    borderRadius: tema.radios.tarjeta,
    paddingVertical: 28,
    paddingHorizontal: 36,
    alignItems: "center",
    minWidth: 220,
    shadowColor: "#000",
    shadowOpacity: 0.25,
    shadowRadius: 12,
    shadowOffset: { width: 0, height: 4 },
    elevation: 8,
  },
  feedbackPuntos: {
    fontSize: 16,
    fontWeight: "600",
    color: tema.colores.textoTenue,
    marginTop: 4,
  },
  cajaResumen: {
    backgroundColor: tema.colores.fondoTarjeta,
    borderWidth: 1,
    borderColor: tema.colores.bordeTarjeta,
    borderRadius: tema.radios.tarjeta,
    padding: 20,
    alignItems: "center",
    marginVertical: 16,
    minWidth: 180,
  },
  resumenEtiqueta: {
    fontSize: 11,
    color: tema.colores.textoTenue,
    letterSpacing: 1,
    textTransform: "uppercase",
  },
  resumenPuntos: {
    fontSize: 40,
    fontWeight: "700",
    color: tema.colores.primario,
    marginTop: 4,
  },
  centrado: {
    flex: 1,
    justifyContent: "center",
    alignItems: "center",
    padding: 24,
    gap: 16,
  },
  progreso: {
    fontSize: 14,
    color: tema.colores.textoTenue,
  },
  temporizadorContenedor: {
    height: 8,
    backgroundColor: tema.colores.bordeTarjeta,
    borderRadius: 4,
    overflow: "hidden",
    marginBottom: 4,
  },
  temporizadorBarra: {
    height: "100%",
    borderRadius: 4,
  },
  temporizadorTexto: {
    fontSize: 20,
    fontWeight: "700",
    textAlign: "center",
    marginBottom: 4,
  },
  puntaje: {
    fontSize: 13,
    color: tema.colores.textoTenue,
    textAlign: "center",
    marginBottom: 12,
  },
  enunciado: {
    fontSize: 18,
    fontWeight: "600",
    color: tema.colores.texto,
    textAlign: "center",
    marginBottom: 24,
    lineHeight: 26,
  },
  opcionesContenedor: {
    gap: 10,
  },
  opcion: {
    backgroundColor: tema.colores.fondoTarjeta,
    borderWidth: 2,
    borderColor: tema.colores.bordeTarjeta,
    borderRadius: tema.radios.entrada,
    padding: 14,
    alignItems: "center",
  },
  opcionSeleccionada: {
    borderColor: tema.colores.primario,
    backgroundColor: tema.colores.primarioDeshabilitado,
  },
  opcionCorrecta: {
    borderColor: "#22c55e",
    backgroundColor: "#f0fdf4",
  },
  opcionIncorrecta: {
    borderColor: "#ef4444",
    backgroundColor: "#fef2f2",
  },
  textoOpcion: {
    fontSize: 15,
    color: tema.colores.texto,
    textAlign: "center",
  },
  feedbackIcono: {
    fontSize: 44,
    fontWeight: "800",
    textAlign: "center",
    marginTop: 12,
    lineHeight: 48,
  },
  transicionContenedor: {
    alignItems: "center",
    marginTop: 16,
    paddingTop: 12,
    borderTopWidth: 1,
    borderTopColor: tema.colores.bordeTarjeta,
  },
  transicionTitulo: {
    fontSize: 13,
    color: tema.colores.textoTenue,
    textTransform: "uppercase",
    letterSpacing: 1,
  },
  transicionCuenta: {
    fontSize: 28,
    fontWeight: "800",
    color: tema.colores.primario,
    marginTop: 2,
  },
  feedbackCorrecto: {
    fontSize: 20,
    fontWeight: "700",
    color: "#22c55e",
    textAlign: "center",
    marginTop: 8,
  },
  feedbackIncorrecto: {
    fontSize: 20,
    fontWeight: "700",
    color: "#ef4444",
    textAlign: "center",
    marginTop: 20,
  },
  feedbackInfo: {
    fontSize: 16,
    fontWeight: "600",
    color: tema.colores.primario,
    textAlign: "center",
    marginTop: 20,
    lineHeight: 22,
  },
  textoError: {
    color: tema.colores.error,
    fontSize: 16,
    textAlign: "center",
  },
  textoExito: {
    color: "#22c55e",
    fontSize: 24,
    fontWeight: "700",
    textAlign: "center",
  },
  textoInfo: {
    color: tema.colores.textoTenue,
    fontSize: 15,
    textAlign: "center",
    lineHeight: 22,
  },
  boton: {
    backgroundColor: tema.colores.primario,
    borderRadius: tema.radios.boton,
    padding: 14,
    minWidth: 140,
    alignItems: "center",
    marginTop: 8,
  },
  textoBoton: {
    color: "#ffffff",
    fontSize: 15,
    fontWeight: "600",
  },
});
