import { useCallback, useEffect, useRef, useState } from "react";
import {
  ActivityIndicator,
  Alert,
  StyleSheet,
  Text,
  TouchableOpacity,
  View,
} from "react-native";
import { useLocalSearchParams, useRouter } from "expo-router";
import { useAutenticacion } from "../../../autenticacion/ContextoAutenticacion";
import RutaProtegidaMovil from "../../../autenticacion/RutaProtegidaMovil";
import { PantallaBase } from "../../../componentes/PantallaBase";
import { tema } from "../../../estilos/tema";
import {
  enviarRespuestaTrivia,
  obtenerPreguntasRespondidas,
  obtenerTriviaParticipante,
  type PreguntaTrivia,
  type TriviaParticipante,
} from "../../../servicios/juegoApi";
import {
  crearConexionSesionesTiempoReal,
  esErrorNoAutenticadoTiempoReal,
} from "../../../servicios/sesionesTiempoReal";

export default function PantallaJugar() {
  return (
    <RutaProtegidaMovil>
      <ContenidoJuego />
    </RutaProtegidaMovil>
  );
}

type EstadoPregunta = "esperando" | "respondiendo" | "correcta" | "incorrecta" | "tiempo_agotado";

function ContenidoJuego() {
  const enrutador = useRouter();
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
  const [tiempoInicio, setTiempoInicio] = useState<number>(0);
  const [etapaTerminada, setEtapaTerminada] = useState(false);
  const [enviando, setEnviando] = useState(false);
  const [puntosGanadosUltima, setPuntosGanadosUltima] = useState<number | null>(null);
  const [puntosAcumulados, setPuntosAcumulados] = useState(0);
  const intervalRef = useRef<ReturnType<typeof setInterval> | null>(null);

  const preguntaActual: PreguntaTrivia | undefined = trivia?.preguntas[indicePregunta];
  const tiempoLimite = preguntaActual?.tiempoEstimado ?? trivia?.tiempoLimitePorPregunta ?? 10;

  // Carga inicial
  useEffect(() => {
    if (!token || !sesionId || !triviaId) return;

    void (async () => {
      try {
        setCargando(true);
        const [triviaData, respondidas] = await Promise.all([
          obtenerTriviaParticipante(sesionId, triviaId, token),
          obtenerPreguntasRespondidas(sesionId, misionId, etapaId, token),
        ]);
        setTrivia(triviaData);

        // Reanudar desde la primera pregunta no respondida
        if (respondidas.length > 0) {
          const primeroSinResponder = triviaData.preguntas.findIndex(
            (p) => !respondidas.includes(p.id),
          );
          if (primeroSinResponder === -1) {
            setEtapaTerminada(true);
          } else {
            setIndicePregunta(primeroSinResponder);
          }
        }
      } catch (e) {
        setError(e instanceof Error ? e.message : "Error al cargar la trivia.");
      } finally {
        setCargando(false);
      }
    })();
  }, [sesionId, misionId, etapaId, triviaId, token]);

  // Iniciar temporizador cuando cambia la pregunta
  useEffect(() => {
    if (!trivia || etapaTerminada || estadoPregunta !== "esperando") return;

    const limite = preguntaActual?.tiempoEstimado ?? trivia.tiempoLimitePorPregunta;
    setTiempoRestante(limite);
    setTiempoInicio(Date.now());

    intervalRef.current = setInterval(() => {
      setTiempoRestante((prev) => {
        if (prev <= 1) {
          clearInterval(intervalRef.current!);
          void manejarTiempoAgotado();
          return 0;
        }
        return prev - 1;
      });
    }, 1000);

    return () => {
      if (intervalRef.current) clearInterval(intervalRef.current);
    };
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [indicePregunta, trivia, etapaTerminada]);

  const manejarTiempoAgotado = useCallback(async () => {
    if (!token || !preguntaActual || !trivia || enviando) return;
    setEstadoPregunta("tiempo_agotado");
    setEnviando(true);

    try {
      await enviarRespuestaTrivia(
        sesionId, misionId, etapaId, triviaId,
        preguntaActual.id,
        preguntaActual.opciones[0]?.id ?? "", // Opción inválida → 0 pts
        tiempoLimite * 1000 + 1, // Supera el límite → 0 pts
        trivia.preguntas.length,
        token,
      );
    } catch {
      // Silenciar: si falla el registro, el servidor lo manejará por timeout
    } finally {
      setEnviando(false);
    }
  }, [token, preguntaActual, trivia, sesionId, misionId, etapaId, triviaId, tiempoLimite, enviando]);

  const seleccionarOpcion = async (opcionId: string) => {
    if (estadoPregunta !== "esperando" || enviando || !preguntaActual || !trivia || !token) return;

    if (intervalRef.current) clearInterval(intervalRef.current);

    const tiempoTranscurrido = Date.now() - tiempoInicio;
    setOpcionSeleccionada(opcionId);
    setEnviando(true);

    try {
      const resultado = await enviarRespuestaTrivia(
        sesionId, misionId, etapaId, triviaId,
        preguntaActual.id,
        opcionId,
        tiempoTranscurrido,
        trivia.preguntas.length,
        token,
      );

      setEstadoPregunta(resultado.esCorrecta ? "correcta" : "incorrecta");
      setPuntosGanadosUltima(resultado.puntosGanados);
      setPuntosAcumulados((prev) => prev + resultado.puntosGanados);

      // Avanzar automáticamente después de mostrar el resultado
      setTimeout(() => {
        if (resultado.etapaCompletada) {
          setEtapaTerminada(true);
        } else if (indicePregunta < trivia.preguntas.length - 1) {
          setIndicePregunta((i) => i + 1);
          setOpcionSeleccionada(null);
          setEstadoPregunta("esperando");
          setPuntosGanadosUltima(null);
        } else {
          // Última pregunta respondida, esperando que los demás terminen
          setEtapaTerminada(true);
        }
      }, 1500);
    } catch (e) {
      Alert.alert("Error", e instanceof Error ? e.message : "Error al enviar respuesta.");
      setEstadoPregunta("esperando");
      setOpcionSeleccionada(null);
    } finally {
      setEnviando(false);
    }
  };

  // SignalR: EtapaCompletada y SesionActualizada
  useEffect(() => {
    if (!token || !sesionId) return;

    let desmontado = false;
    const conexion = crearConexionSesionesTiempoReal(token);

    const manejarEtapaCompletada = (evento: { sesionId?: string; SesionId?: string; etapaId?: string; EtapaId?: string }) => {
      const sid = (evento.sesionId ?? evento.SesionId ?? "").toLowerCase();
      const eid = (evento.etapaId ?? evento.EtapaId ?? "").toLowerCase();
      if (sid === sesionId.toLowerCase() && eid === etapaId.toLowerCase()) {
        if (!desmontado) setEtapaTerminada(true);
      }
    };

    const manejarSesionActualizada = (evento: { estado?: string; Estado?: string }) => {
      const estado = evento.estado ?? evento.Estado ?? "";
      if (estado === "Cancelada" || estado === "Finalizada") {
        Alert.alert(
          "Sesión terminada",
          `La sesión ha sido ${estado.toLowerCase()}.`,
          [{ text: "Aceptar", onPress: () => enrutador.replace("/participante/sesiones") }],
        );
      }
    };

    conexion.on("EtapaCompletada", manejarEtapaCompletada);
    conexion.on("SesionActualizada", manejarSesionActualizada);

    conexion
      .start()
      .then(async () => {
        if (desmontado) { await conexion.stop().catch(() => undefined); return; }
        await conexion.invoke("UnirseASesion", sesionId).catch(() => undefined);
      })
      .catch((e: unknown) => {
        if (esErrorNoAutenticadoTiempoReal(e)) void cerrarSesion();
      });

    return () => {
      desmontado = true;
      conexion.off("EtapaCompletada", manejarEtapaCompletada);
      conexion.off("SesionActualizada", manejarSesionActualizada);
      if (conexion.state !== "Disconnected") {
        conexion.invoke("SalirDeSesion", sesionId).catch(() => undefined).finally(() => {
          conexion.stop().catch(() => undefined);
        });
      }
    };
  }, [token, sesionId, etapaId, cerrarSesion, enrutador]);

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
          <Text style={estilos.textoExito}>¡Etapa completada!</Text>
          <View style={estilos.cajaResumen}>
            <Text style={estilos.resumenEtiqueta}>TUS PUNTOS EN ESTA ETAPA</Text>
            <Text style={estilos.resumenPuntos}>{puntosAcumulados} pts</Text>
          </View>
          <Text style={estilos.textoInfo}>
            Todos los participantes han terminado. Espera la siguiente etapa.
          </Text>
          <TouchableOpacity
            style={estilos.boton}
            onPress={() => enrutador.back()}
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
                disabled={estadoPregunta !== "esperando" || enviando}
              >
                <Text style={estilos.textoOpcion}>{opcion.texto}</Text>
              </TouchableOpacity>
            );
          })}
        </View>

        {/* Feedback */}
        {estadoPregunta === "correcta" && (
          <View style={estilos.feedbackContenedor}>
            <Text style={estilos.feedbackCorrecto}>¡Correcto!</Text>
            <Text style={estilos.feedbackPuntos}>
              +{puntosGanadosUltima ?? 0} pts
              {puntosGanadosUltima === 0 ? " (tiempo agotado)" : ""}
            </Text>
          </View>
        )}
        {estadoPregunta === "incorrecta" && (
          <View style={estilos.feedbackContenedor}>
            <Text style={estilos.feedbackIncorrecto}>Incorrecto</Text>
            <Text style={estilos.feedbackPuntos}>+0 pts</Text>
          </View>
        )}
        {estadoPregunta === "tiempo_agotado" && (
          <View style={estilos.feedbackContenedor}>
            <Text style={estilos.feedbackIncorrecto}>¡Tiempo agotado!</Text>
            <Text style={estilos.feedbackPuntos}>+0 pts</Text>
          </View>
        )}
      </View>
    </PantallaBase>
  );
}

const estilos = StyleSheet.create({
  contenedor: {
    flex: 1,
    padding: 16,
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
  feedbackCorrecto: {
    fontSize: 20,
    fontWeight: "700",
    color: "#22c55e",
    textAlign: "center",
    marginTop: 20,
  },
  feedbackIncorrecto: {
    fontSize: 20,
    fontWeight: "700",
    color: "#ef4444",
    textAlign: "center",
    marginTop: 20,
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
