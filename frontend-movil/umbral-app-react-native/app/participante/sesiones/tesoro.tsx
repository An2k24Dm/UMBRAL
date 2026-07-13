import { useCallback, useEffect, useRef, useState } from "react";
import {
  ActivityIndicator,
  Alert,
  BackHandler,
  Modal,
  ScrollView,
  StyleSheet,
  Text,
  TouchableOpacity,
  View,
} from "react-native";
import { useFocusEffect, useLocalSearchParams, useNavigation, useRouter } from "expo-router";
import * as signalR from "@microsoft/signalr";
import { CameraView, useCameraPermissions } from "expo-camera";
import { useAutenticacion } from "../../../autenticacion/ContextoAutenticacion";
import RutaProtegidaMovil from "../../../autenticacion/RutaProtegidaMovil";
import { PantallaBase } from "../../../componentes/PantallaBase";
import { tema } from "../../../estilos/tema";
import {
  enviarEvidenciaTesoro,
  obtenerBusquedaConPistasCompleto,
  type BusquedaConPistas,
  type PistaLiberadaSesion,
} from "../../../servicios/tesoroApi";
import {
  crearConexionSesionesTiempoReal,
  esErrorNoAutenticadoTiempoReal,
  registrarEventoConexionSesionesTiempoReal,
  registrarAccesoGrupoRechazadoDev,
} from "../../../servicios/sesionesTiempoReal";
import { obtenerDetalleSesionDisponibleApi } from "../../../servicios/sesionesApi";
import { obtenerProgresoSecuencialSesionApi } from "../../../servicios/sesionesApi";
import { CronometroActivo } from "../../../servicios/cronometroActivo";
import { useRankingTiempoReal } from "../../../hooks/useRankingTiempoReal";
import { useCorrelacionPuntaje } from "../../../hooks/useCorrelacionPuntaje";
import {
  mapearEstadoSesionJuego,
  type EstadoSesionJuego,
} from "../../../servicios/estadoSesionJuego";

// Ventana visual LOCAL de la pantalla de éxito del tesoro. El resultado se
// muestra al menos MS_FEEDBACK_MIN_TESORO (feedback autoritativo del backend) y
// se concede una gracia hasta MS_VENTANA_MAXIMA_TESORO para recibir el +X pts
// real por SignalR. NO retrasa el backend (cierre/transición/siguiente etapa):
// si llega la navegación autoritativa (EtapaIniciada) la pantalla se desmonta y
// estos timers se cancelan.
const MS_FEEDBACK_MIN_TESORO = 5000;
const MS_VENTANA_MAXIMA_TESORO = 8000;

export default function PantallaTesoro() {
  return (
    <RutaProtegidaMovil>
      <ContenidoTesoro />
    </RutaProtegidaMovil>
  );
}

type EstadoEnvio = "esperando" | "enviando" | "valido" | "invalido" | "ya_completado";

function ContenidoTesoro() {
  const enrutador = useRouter();
  const navegacion = useNavigation();
  const { sesion: auth, cerrarSesion } = useAutenticacion();
  const token = auth?.tokenAcceso ?? null;

  const params = useLocalSearchParams<{
    sesionId?: string;
    misionId?: string;
    etapaId?: string;
    busquedaId?: string;
  }>();

  const sesionId = params.sesionId ?? "";
  const misionId = params.misionId ?? "";
  const etapaId = params.etapaId ?? "";
  const busquedaId = params.busquedaId ?? "";

  const [busqueda, setBusqueda] = useState<BusquedaConPistas | null>(null);
  const [cargando, setCargando] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [estadoEnvio, setEstadoEnvio] = useState<EstadoEnvio>("esperando");
  const [conflictoTipo, setConflictoTipo] = useState<"equipo" | "individual" | null>(null);
  const [etapaCompletada, setEtapaCompletada] = useState(false);
  const [enviando, setEnviando] = useState(false);
  const { feedbackPuntaje, resueltoRef, esperarPuntaje, alRecibirPuntajeCalculado } =
    useCorrelacionPuntaje({ esperaMaximaMs: MS_VENTANA_MAXIMA_TESORO });
  // Estado local autoritativo de la sesión (no se asume "Activa" de entrada).
  const [estadoSesion, setEstadoSesion] = useState<EstadoSesionJuego>("Desconocida");
  // Temporizador informativo de la etapa (regresivo). Se congela en pausa.
  const [tiempoRestante, setTiempoRestante] = useState<number | null>(null);
  const cronometroRef = useRef(new CronometroActivo());
  const intervalRef = useRef<ReturnType<typeof setInterval> | null>(null);

  // QR scanner
  const [mostrandoCamara, setMostrandoCamara] = useState(false);
  // useRef para bloqueo sincrónico: onBarcodeScanned puede disparar varias veces
  // antes de que React aplique el setState, lo que causaría múltiples POSTs simultáneos.
  const yaEscaneadoRef = useRef(false);
  const [codigoEscaneado, setCodigoEscaneado] = useState("");
  const [permisosCamara, solicitarPermisosCamara] = useCameraPermissions();

  useRankingTiempoReal({
    sesionId,
    onPuntajeCalculado: alRecibirPuntajeCalculado,
  });

  const jugadorCompleto = estadoEnvio === "valido" || estadoEnvio === "ya_completado";
  const bloquearSalida = estadoSesion === "Activa" && !jugadorCompleto && !etapaCompletada;

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
  // autoridad final y rechaza la evidencia si la sesión no está Activa.
  const refrescarEstadoSesion = useCallback(async () => {
    if (!token || !sesionId) return;
    try {
      const detalle = await obtenerDetalleSesionDisponibleApi(token, sesionId);
      setEstadoSesion(mapearEstadoSesionJuego(detalle.estado));
    } catch {
      setEstadoSesion("Desconocida");
    }
  }, [token, sesionId]);

  const cargarBusqueda = useCallback(async () => {
    if (!token || !sesionId || !busquedaId) return;
    try {
      const datos = await obtenerBusquedaConPistasCompleto(
        sesionId, misionId, etapaId, busquedaId, token,
      );
      const detalleSesion = await obtenerDetalleSesionDisponibleApi(token, sesionId);
      setBusqueda(datos);
      // Temporizador informativo de la etapa (regresivo). No afecta al puntaje
      // (la evidencia del tesoro no puntúa por tiempo); solo se muestra y se
      // congela durante la pausa.
      if (datos.tiempoSegundos > 0 && !datos.yaEnvioEvidencia) {
        setTiempoRestante(
          detalleSesion.ejecucionActual?.segundosRestantes ?? datos.tiempoSegundos,
        );
        cronometroRef.current.reiniciar();
      }
      if (datos.yaEnvioEvidencia) {
        // "Ya completada" es autoridad del backend (evidencia válida del
        // jugador). No se persiste localmente: al reabrir/refrescar/cambiar de
        // dispositivo, el estado se determina desde el servidor.
        setEstadoEnvio("valido");
      }
      setEstadoSesion(mapearEstadoSesionJuego(detalleSesion.estado));
    } catch (e) {
      setError(e instanceof Error ? e.message : "Error al cargar la búsqueda del tesoro.");
    } finally {
      setCargando(false);
    }
  }, [token, sesionId, misionId, etapaId, busquedaId, refrescarEstadoSesion]);

  useEffect(() => {
    void cargarBusqueda();
  }, [cargarBusqueda]);

  // Temporizador regresivo informativo: solo corre cuando la sesión está Activa
  // y aún no se completó la etapa. En pausa se congela (clearInterval) y el
  // tiempo restante se conserva; al reanudar continúa desde el mismo punto.
  useEffect(() => {
    const activo =
      estadoSesion === "Activa" &&
      !etapaCompletada &&
      estadoEnvio !== "valido" &&
      estadoEnvio !== "ya_completado" &&
      tiempoRestante !== null &&
      tiempoRestante > 0;

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
        if (prev === null || prev <= 1) {
          if (intervalRef.current) {
            clearInterval(intervalRef.current);
            intervalRef.current = null;
          }
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
  }, [estadoSesion, etapaCompletada, estadoEnvio, tiempoRestante]);

  // Si llega una pausa con la cámara abierta, cerrarla de inmediato.
  useEffect(() => {
    if (estadoSesion !== "Activa" && mostrandoCamara) {
      setMostrandoCamara(false);
    }
  }, [estadoSesion, mostrandoCamara]);

  // #18/#19: al encontrar el tesoro (o completar el equipo) se muestra el
  // resultado final AL MENOS 5 s y luego se vuelve automáticamente al detalle.
  // Los 10 s de preparación NO empiezan aquí: los inicia el backend al cerrar la
  // etapa (CierrePendiente). Si EtapaIniciada navega antes, el cleanup lo cancela.
  useEffect(() => {
    if ((!jugadorCompleto && !etapaCompletada) || !sesionId) return;
    const inicio = Date.now();
    let cancelado = false;
    let timer: ReturnType<typeof setTimeout>;
    const revisar = () => {
      if (cancelado) return;
      const transcurrido = Date.now() - inicio;
      // Navega al detalle cuando se alcanza el máximo, o cuando ya se mostró el
      // puntaje real (o su respaldo) y pasó el mínimo visible. Nunca bloquea la
      // navegación autoritativa: EtapaIniciada desmonta la pantalla y cancela
      // este timer.
      if (
        transcurrido >= MS_VENTANA_MAXIMA_TESORO ||
        (transcurrido >= MS_FEEDBACK_MIN_TESORO && resueltoRef.current)
      ) {
        enrutador.replace(`/participante/sesiones/${sesionId}`);
        return;
      }
      timer = setTimeout(revisar, 250);
    };
    timer = setTimeout(revisar, MS_FEEDBACK_MIN_TESORO);
    return () => {
      cancelado = true;
      clearTimeout(timer);
    };
  }, [jugadorCompleto, etapaCompletada, sesionId, enrutador, resueltoRef]);

  // SignalR: pistas en tiempo real + EtapaCompletada + SesionActualizada
  useFocusEffect(useCallback(() => {
    if (!token || !sesionId) return;

    let desmontado = false;
    let cerrando = false;
    let cierreRegistrado = false;
    let inicioPromise: Promise<void> | null = null;
    const conexion = crearConexionSesionesTiempoReal(token, "Tesoro");
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

    const manejarPistaLiberada = (evento: {
      sesionId?: string; SesionId?: string;
      etapaId?: string; EtapaId?: string;
      contenido?: string; Contenido?: string;
      pistaId?: string | null; PistaId?: string | null;
      fechaEventoUtc?: string; FechaEventoUtc?: string;
    }) => {
      const sid = (evento.sesionId ?? evento.SesionId ?? "").toLowerCase();
      const eid = (evento.etapaId ?? evento.EtapaId ?? "").toLowerCase();
      if (sid !== sesionId.toLowerCase() || eid !== etapaId.toLowerCase()) return;

      const contenido = evento.contenido ?? evento.Contenido ?? "";
      const pistaId = evento.pistaId ?? evento.PistaId ?? null;
      const fecha = evento.fechaEventoUtc ?? evento.FechaEventoUtc ?? new Date().toISOString();

      if (!desmontado) {
        setBusqueda((prev) => {
          if (!prev) return prev;
          const yaExiste = pistaId
            ? prev.pistasLiberadas.some((p) => p.pistaId === pistaId)
            : false;
          if (yaExiste) return prev;
          const nueva: PistaLiberadaSesion = { pistaId, contenido, fechaLiberacionUtc: fecha };
          return { ...prev, pistasLiberadas: [...prev.pistasLiberadas, nueva] };
        });
      }
    };

    const manejarEtapaCompletada = (evento: {
      sesionId?: string; SesionId?: string;
      etapaId?: string; EtapaId?: string;
    }) => {
      const sid = (evento.sesionId ?? evento.SesionId ?? "").toLowerCase();
      const eid = (evento.etapaId ?? evento.EtapaId ?? "").toLowerCase();
      if (sid === sesionId.toLowerCase() && eid === etapaId.toLowerCase()) {
        if (!desmontado) setEtapaCompletada(true);
      }
    };

    const manejarEtapaIniciada = async (evento: {
      sesionId?: string; SesionId?: string;
    }) => {
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
      // temporizador congela o reanuda, y la cámara se cierra al pausar.
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

    // #15 grupal: cuando un integrante registra la evidencia válida del equipo,
    // el backend notifica al GrupoEquipo. Los demás integrantes consultan el
    // progreso: si su equipo ya completó, quedan completos y vuelven al detalle.
    const manejarProgresoActualizado = async () => {
      if (desmontado || !token) return;
      try {
        const progreso = await obtenerProgresoSecuencialSesionApi(token, sesionId);
        if (!desmontado && progreso.jugadorActualCompletoEtapaActual) {
          setEstadoEnvio((prev) =>
            prev === "valido" || prev === "ya_completado" ? prev : "ya_completado");
          setConflictoTipo((prev) => prev ?? "equipo");
        }
      } catch {
        // Silencioso: se reintenta con el siguiente evento.
      }
    };

    conexion.on("PistaLiberada", manejarPistaLiberada);
    conexion.on("EtapaCompletada", manejarEtapaCompletada);
    conexion.on("EtapaIniciada", manejarEtapaIniciada);
    conexion.on("SesionActualizada", manejarSesionActualizada);
    conexion.on("ProgresoSecuencialActualizado", manejarProgresoActualizado);

    // Al reconectar, re-unirse al grupo (pasa de nuevo por el Proxy) y
    // resincronizar el estado real por HTTP.
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
      conexion.off("PistaLiberada", manejarPistaLiberada);
      conexion.off("EtapaCompletada", manejarEtapaCompletada);
      conexion.off("EtapaIniciada", manejarEtapaIniciada);
      conexion.off("SesionActualizada", manejarSesionActualizada);
      conexion.off("ProgresoSecuencialActualizado", manejarProgresoActualizado);
      void cerrarConexion();
    };
  }, [token, sesionId, etapaId, cerrarSesion, enrutador, refrescarEstadoSesion]));

  const abrirCamara = async () => {
    // Protección de UX: no abrir la cámara si la sesión no está Activa.
    if (estadoSesion !== "Activa") {
      if (estadoSesion === "Pausada") {
        Alert.alert(
          "Sesión pausada",
          "La sesión está pausada. Espera a que el operador la reanude.",
        );
      }
      return;
    }
    if (!permisosCamara?.granted) {
      const resultado = await solicitarPermisosCamara();
      if (!resultado.granted) {
        Alert.alert(
          "Permiso necesario",
          "UMBRAL necesita acceso a la cámara para escanear el QR del tesoro.",
        );
        return;
      }
    }
    yaEscaneadoRef.current = false;
    setMostrandoCamara(true);
  };

  const alEscanearQr = ({ data }: { data: string }) => {
    if (yaEscaneadoRef.current) return;
    // No procesar escaneos si la sesión no está Activa.
    if (estadoSesion !== "Activa") {
      setMostrandoCamara(false);
      return;
    }
    yaEscaneadoRef.current = true; // bloqueo sincrónico — evita múltiples POSTs
    setCodigoEscaneado(data);
    setMostrandoCamara(false);
    void enviarConCodigo(data);
  };

  const enviarConCodigo = async (codigo: string) => {
    if (!token || !codigo.trim() || enviando) return;
    // Protección de UX: no enviar evidencia si la sesión no está Activa.
    if (estadoSesion !== "Activa") return;

    setEnviando(true);
    try {
      const resultado = await enviarEvidenciaTesoro(
        sesionId, misionId, etapaId, busquedaId,
        codigo.trim(),
        token,
      );
      esperarPuntaje(resultado.eventoId);

      // La etapa ya estaba completada por el equipo (o por el propio
      // participante). No es un QR incorrecto: se informa y se marca completada
      // para este jugador, sin sumar puntos nuevos.
      if (resultado.conflicto) {
        setConflictoTipo(resultado.conflicto);
        setEstadoEnvio("ya_completado");
        return;
      }

      setEstadoEnvio(resultado.esValida ? "valido" : "invalido");

      if (resultado.etapaCompletada) {
        setEtapaCompletada(true);
      }
    } catch (e) {
      Alert.alert("Error", e instanceof Error ? e.message : "Error al enviar evidencia.");
      yaEscaneadoRef.current = false;
      setCodigoEscaneado("");
    } finally {
      setEnviando(false);
    }
  };

  // --- Estados de pantalla ---

  if (cargando) {
    return (
      <PantallaBase>
        <View style={estilos.centrado}>
          <ActivityIndicator size="large" color={tema.colores.primario} />
          <Text style={estilos.textoCargando}>Cargando búsqueda del tesoro…</Text>
        </View>
      </PantallaBase>
    );
  }

  if (error) {
    return (
      <PantallaBase>
        <View style={estilos.centrado}>
          <Text style={estilos.textoError}>{error}</Text>
          <TouchableOpacity style={estilos.boton} onPress={() => enrutador.back()}>
            <Text style={estilos.textoBoton}>Volver</Text>
          </TouchableOpacity>
        </View>
      </PantallaBase>
    );
  }

  // #18/#19: resultado final del tesoro, legible y prominente, durante ≥5 s
  // antes de volver automáticamente al detalle.
  if (jugadorCompleto || etapaCompletada) {
    return (
      <PantallaBase>
        <View style={estilos.centrado}>
          <Text style={estilos.trofeo}>🏆</Text>
          <Text style={estilos.textoExito}>
            {conflictoTipo === "equipo"
              ? "¡Tu equipo encontró el tesoro!"
              : "¡Encontraste el tesoro!"}
          </Text>
          <Text style={estilos.textoInfo}>
            Espera a que los demás terminen para avanzar a la siguiente etapa.
          </Text>
          <Text style={estilos.textoInfo}>{feedbackPuntaje}</Text>
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

  return (
    <PantallaBase>
      {/* Modal con cámara para escanear QR */}
      <Modal visible={mostrandoCamara} animationType="slide" onRequestClose={() => setMostrandoCamara(false)}>
        <View style={estilos.modalCamara}>
          <CameraView
            style={estilos.camara}
            facing="back"
            onBarcodeScanned={alEscanearQr}
            barcodeScannerSettings={{ barcodeTypes: ["qr"] }}
          />
          <View style={estilos.overlayQr}>
            <View style={estilos.marcadorQr} />
            <Text style={estilos.textoOverlay}>
              Apunta la cámara al código QR del tesoro
            </Text>
          </View>
          <TouchableOpacity
            style={estilos.botonCerrarCamara}
            onPress={() => setMostrandoCamara(false)}
          >
            <Text style={estilos.textoBotonCerrarCamara}>Cancelar</Text>
          </TouchableOpacity>
        </View>
      </Modal>

      <ScrollView contentContainerStyle={estilos.contenedor} showsVerticalScrollIndicator={false}>
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

        {/* Encabezado */}
        <View style={estilos.encabezado}>
          <Text style={estilos.titulo}>{busqueda?.nombre ?? "Búsqueda del Tesoro"}</Text>
          {busqueda?.descripcion ? (
            <Text style={estilos.descripcion}>{busqueda.descripcion}</Text>
          ) : null}
          <View style={estilos.fila}>
            <Text style={estilos.metaDato}>Puntaje: {busqueda?.puntajeBase ?? 0} pts</Text>
            <Text style={estilos.metaDato}>
              Tiempo: {Math.round((busqueda?.tiempoSegundos ?? 0) / 60)} min
            </Text>
          </View>
          {tiempoRestante !== null && (
            <Text style={estilos.tiempoRestante}>
              Tiempo restante: {Math.floor(tiempoRestante / 60)}:
              {String(tiempoRestante % 60).padStart(2, "0")}
              {estadoSesion === "Pausada" ? " (en pausa)" : ""}
            </Text>
          )}
        </View>

        {/* Pistas liberadas */}
        <View style={estilos.seccion}>
          <Text style={estilos.tituloSeccion}>
            PISTAS LIBERADAS ({busqueda?.pistasLiberadas.length ?? 0})
          </Text>
          {(!busqueda || busqueda.pistasLiberadas.length === 0) ? (
            <View style={estilos.tarjetaVacia}>
              <Text style={estilos.textoVacio}>
                El operador aún no ha liberado ninguna pista. Mantente atento.
              </Text>
            </View>
          ) : (
            busqueda.pistasLiberadas.map((pista, idx) => (
              <View key={pista.pistaId ?? `custom-${idx}`} style={estilos.tarjetaPista}>
                <Text style={estilos.numeroPista}>Pista {idx + 1}</Text>
                <Text style={estilos.contenidoPista}>{pista.contenido}</Text>
              </View>
            ))
          )}
        </View>

        {/* Panel de envío */}
        <View style={estilos.seccion}>
          <Text style={estilos.tituloSeccion}>CÓDIGO QR DEL TESORO</Text>

          {/* Los estados "valido"/"ya_completado" se muestran en la pantalla
              final del tesoro (🏆), que redirige al detalle; aquí solo queda el
              flujo de escaneo/verificación. */}
          {enviando ? (
            <View style={estilos.centradoInline}>
              <ActivityIndicator color={tema.colores.primario} />
              <Text style={estilos.textoEnviando}>Verificando código…</Text>
            </View>
          ) : (
            <>
              {estadoEnvio === "invalido" && (
                <View style={estilos.tarjetaError}>
                  <Text style={estilos.textoErrorPanel}>Código incorrecto.</Text>
                  <Text style={estilos.textoErrorDetalle}>{feedbackPuntaje}</Text>
                  <Text style={estilos.textoErrorDetalle}>
                    El código escaneado no coincide. Sigue buscando el tesoro.
                  </Text>
                </View>
              )}
              <TouchableOpacity
                style={[
                  estilos.botonEscanear,
                  estadoSesion !== "Activa" && estilos.botonEscanearDeshabilitado,
                ]}
                onPress={() => void abrirCamara()}
                disabled={estadoSesion !== "Activa"}
              >
                <Text style={estilos.textoBotonEscanear}>
                  {estadoSesion === "Pausada"
                    ? "Escaneo pausado"
                    : estadoEnvio === "invalido"
                      ? "Escanear de nuevo"
                      : "Escanear código QR"}
                </Text>
              </TouchableOpacity>
              {codigoEscaneado ? (
                <Text style={estilos.codigoLeido}>
                  Último código leído: {codigoEscaneado}
                </Text>
              ) : null}
            </>
          )}
        </View>
      </ScrollView>
    </PantallaBase>
  );
}

const estilos = StyleSheet.create({
  contenedor: {
    padding: 16,
    paddingBottom: 32,
  },
  centrado: {
    flex: 1,
    justifyContent: "center",
    alignItems: "center",
    padding: 24,
    gap: 16,
  },
  centradoInline: {
    alignItems: "center",
    paddingVertical: 24,
    gap: 12,
  },
  encabezado: {
    backgroundColor: tema.colores.fondoTarjeta,
    borderRadius: tema.radios.tarjeta,
    borderWidth: 1,
    borderColor: tema.colores.bordeTarjeta,
    padding: tema.espacios.lg,
    marginBottom: tema.espacios.md,
  },
  titulo: {
    fontSize: tema.tipografia.tamanos.h3,
    fontWeight: tema.tipografia.pesos.extrabold,
    color: tema.colores.texto,
    marginBottom: tema.espacios.xs,
  },
  descripcion: {
    color: tema.colores.textoTenue,
    fontSize: tema.tipografia.tamanos.md,
    marginBottom: tema.espacios.sm,
    lineHeight: 20,
  },
  fila: {
    flexDirection: "row",
    gap: tema.espacios.md,
    marginTop: tema.espacios.xs,
  },
  metaDato: {
    color: tema.colores.primario,
    fontSize: tema.tipografia.tamanos.sm,
    fontWeight: tema.tipografia.pesos.semibold,
  },
  seccion: {
    marginBottom: tema.espacios.md,
  },
  tituloSeccion: {
    color: tema.colores.textoTenue,
    fontSize: tema.tipografia.tamanos.xs,
    letterSpacing: 1,
    textTransform: "uppercase",
    fontWeight: tema.tipografia.pesos.bold,
    marginBottom: tema.espacios.sm,
  },
  tarjetaPista: {
    backgroundColor: tema.colores.fondoTarjeta,
    borderRadius: tema.radios.entrada,
    borderWidth: 1,
    borderColor: tema.colores.bordeTarjeta,
    padding: tema.espacios.md,
    marginBottom: tema.espacios.sm,
  },
  numeroPista: {
    color: tema.colores.primario,
    fontSize: tema.tipografia.tamanos.xs,
    fontWeight: tema.tipografia.pesos.bold,
    textTransform: "uppercase",
    letterSpacing: 0.5,
    marginBottom: 4,
  },
  contenidoPista: {
    color: tema.colores.texto,
    fontSize: tema.tipografia.tamanos.md,
    lineHeight: 22,
  },
  tarjetaVacia: {
    backgroundColor: tema.colores.fondoTarjeta,
    borderRadius: tema.radios.entrada,
    borderWidth: 1,
    borderColor: tema.colores.bordeTarjeta,
    padding: tema.espacios.lg,
    alignItems: "center",
  },
  textoVacio: {
    color: tema.colores.textoTenue,
    fontSize: tema.tipografia.tamanos.sm,
    textAlign: "center",
    lineHeight: 20,
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
  tiempoRestante: {
    marginTop: tema.espacios.sm,
    color: tema.colores.texto,
    fontSize: tema.tipografia.tamanos.md,
    fontWeight: tema.tipografia.pesos.bold,
  },
  botonEscanear: {
    backgroundColor: "#d97706",
    paddingVertical: tema.espacios.md + 4,
    borderRadius: tema.radios.boton,
    alignItems: "center",
    marginTop: tema.espacios.sm,
  },
  botonEscanearDeshabilitado: {
    backgroundColor: tema.colores.bordeTarjeta,
  },
  textoBotonEscanear: {
    color: tema.colores.textoBlanco,
    fontWeight: tema.tipografia.pesos.bold,
    fontSize: tema.tipografia.tamanos.lg,
  },
  textoEnviando: {
    color: tema.colores.textoTenue,
    fontSize: tema.tipografia.tamanos.md,
  },
  codigoLeido: {
    color: tema.colores.textoTenue,
    fontSize: tema.tipografia.tamanos.xs,
    textAlign: "center",
    marginTop: tema.espacios.xs,
  },
  tarjetaExito: {
    backgroundColor: "#f0fdf4",
    borderRadius: tema.radios.entrada,
    borderWidth: 1,
    borderColor: "#22c55e",
    padding: tema.espacios.lg,
  },
  textoExitoPanel: {
    color: "#16a34a",
    fontSize: tema.tipografia.tamanos.lg,
    fontWeight: tema.tipografia.pesos.bold,
    marginBottom: 4,
  },
  textoExitoDetalle: {
    color: "#15803d",
    fontSize: tema.tipografia.tamanos.sm,
    lineHeight: 20,
  },
  tarjetaInfo: {
    backgroundColor: tema.colores.fondoTarjeta,
    borderRadius: tema.radios.entrada,
    borderWidth: 1,
    borderColor: tema.colores.primario,
    padding: tema.espacios.lg,
  },
  textoInfoPanel: {
    color: tema.colores.primario,
    fontSize: tema.tipografia.tamanos.lg,
    fontWeight: tema.tipografia.pesos.bold,
    marginBottom: 4,
  },
  textoInfoDetalle: {
    color: tema.colores.textoTenue,
    fontSize: tema.tipografia.tamanos.sm,
    lineHeight: 20,
  },
  tarjetaError: {
    backgroundColor: tema.colores.errorSuave,
    borderRadius: tema.radios.entrada,
    borderWidth: 1,
    borderColor: tema.colores.error,
    padding: tema.espacios.md,
    marginBottom: tema.espacios.sm,
  },
  textoErrorPanel: {
    color: tema.colores.error,
    fontSize: tema.tipografia.tamanos.md,
    fontWeight: tema.tipografia.pesos.bold,
    marginBottom: 2,
  },
  textoErrorDetalle: {
    color: tema.colores.error,
    fontSize: tema.tipografia.tamanos.sm,
  },
  // Cámara / Modal
  modalCamara: {
    flex: 1,
    backgroundColor: "#000",
  },
  camara: {
    flex: 1,
  },
  overlayQr: {
    ...StyleSheet.absoluteFillObject,
    alignItems: "center",
    justifyContent: "center",
    pointerEvents: "none",
  },
  marcadorQr: {
    width: 220,
    height: 220,
    borderWidth: 3,
    borderColor: "#d97706",
    borderRadius: 12,
    backgroundColor: "transparent",
  },
  textoOverlay: {
    color: "#fff",
    fontSize: 14,
    marginTop: 16,
    textAlign: "center",
    paddingHorizontal: 32,
  },
  botonCerrarCamara: {
    position: "absolute",
    bottom: 48,
    alignSelf: "center",
    backgroundColor: "rgba(0,0,0,0.7)",
    paddingHorizontal: 32,
    paddingVertical: 14,
    borderRadius: 30,
    borderWidth: 1,
    borderColor: "#fff",
  },
  textoBotonCerrarCamara: {
    color: "#fff",
    fontSize: 16,
    fontWeight: "600",
  },
  // Estados de resultado
  textoCargando: {
    color: tema.colores.textoTenue,
    marginTop: tema.espacios.md,
    fontSize: tema.tipografia.tamanos.md,
  },
  textoError: {
    color: tema.colores.error,
    fontSize: 16,
    textAlign: "center",
  },
  trofeo: {
    fontSize: 56,
    textAlign: "center",
    marginBottom: 4,
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
