import { useCallback, useEffect, useRef, useState } from "react";
import {
  ActivityIndicator,
  ScrollView,
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
import { usePartidasTiempoReal } from "../../../hooks/usePartidasTiempoReal";
import {
  enviarRespuestaTriviaApi,
  obtenerPreguntasRespondidasApi,
  obtenerRankingApi,
  obtenerTriviaParticipanteApi,
} from "../../../servicios/partidasApi";
import { obtenerDetalleSesionDisponibleApi } from "../../../servicios/sesionesApi";
import type { RankingEntradaDto, TriviaParticipanteDto } from "../../../tipos/partidas";
import type {
  EtapaSesionMovilDto,
  MisionSesionMovilDto,
} from "../../../tipos/sesiones";

export default function PantallaJugar() {
  return (
    <RutaProtegidaMovil>
      <ContenidoJugar />
    </RutaProtegidaMovil>
  );
}

// ---------------------------------------------------------------------------
// Tipos internos
// ---------------------------------------------------------------------------
type FaseJuego =
  | "cargando"
  | "esperando_partida"
  | "entre_misiones"
  | "cargando_etapa"
  | "trivia"
  | "resultado_pregunta"
  | "entre_etapas"
  | "ranking"
  | "error";

interface ResultadoPregunta {
  esCorrecta: boolean;
  puntosGanados: number;
  yaRespondida: boolean;
  mensaje: string;
}

// Returns first stage not in etapasJugadas, starting from (desdeMision, desdeEtapa).
function encontrarSiguienteEtapaNoJugada(
  misiones: MisionSesionMovilDto[],
  etapasJugadas: Set<string>,
  desdeMision: number,
  desdeEtapa: number,
): { misionIdx: number; etapaIdx: number } | null {
  for (let mi = desdeMision; mi < misiones.length; mi++) {
    const mision = misiones[mi];
    const etapas = [...mision.etapas].sort((a, b) => a.orden - b.orden);
    const startEi = mi === desdeMision ? desdeEtapa : 0;
    for (let ei = startEi; ei < etapas.length; ei++) {
      const etapa = etapas[ei];
      if (!etapasJugadas.has(`${mision.id}:${etapa.id}`)) {
        return { misionIdx: mi, etapaIdx: ei };
      }
    }
  }
  return null;
}

// ---------------------------------------------------------------------------
// Componente principal
// ---------------------------------------------------------------------------
function ContenidoJugar() {
  const enrutador = useRouter();
  const { sesionId } = useLocalSearchParams<{ sesionId?: string }>();
  const { sesion: sesionAuth } = useAutenticacion();
  const token = sesionAuth?.tokenAcceso ?? "";

  // Estado de navegación por el juego
  const [fase, setFase] = useState<FaseJuego>("cargando");
  const [error, setError] = useState<string | null>(null);

  // Datos de la sesión (misiones ordenadas)
  const [misiones, setMisiones] = useState<MisionSesionMovilDto[]>([]);
  const [misionIdx, setMisionIdx] = useState(0);
  const [etapaIdx, setEtapaIdx] = useState(0);

  // Etapas completadas en esta sesión de juego (no volver a mostrarlas)
  const [etapasJugadas, setEtapasJugadas] = useState<Set<string>>(new Set());

  // Contenido de la etapa actual
  const [triviaActual, setTriviaActual] = useState<TriviaParticipanteDto | null>(null);
  const [preguntaIdx, setPreguntaIdx] = useState(0);

  // Resultado de la última respuesta
  const [ultimoResultado, setUltimoResultado] = useState<ResultadoPregunta | null>(null);

  // Tiempo por pregunta
  const [tiempoRestante, setTiempoRestante] = useState(0);
  const tiempoInicioPregunta = useRef<number>(0);
  const intervalTimer = useRef<ReturnType<typeof setInterval> | null>(null);

  // Ranking final
  const [ranking, setRanking] = useState<RankingEntradaDto[]>([]);

  // Enviando respuesta
  const [enviando, setEnviando] = useState(false);

  // ---------------------------------------------------------------------------
  // SignalR: escuchar estado de partida
  // ---------------------------------------------------------------------------
  const alEstadoCambiado = useCallback((estado: string) => {
    if (estado === "Pausada" || estado === "Cancelada" || estado === "Finalizada") {
      detenerTimer();
      if (estado === "Finalizada") {
        void cargarRanking();
      } else {
        setFase("esperando_partida");
      }
    } else if (estado === "Iniciada") {
      if (fase === "esperando_partida") {
        setFase("entre_misiones");
      }
    }
  }, [fase]);

  const alPuntajeActualizado = useCallback((nuevoRanking: RankingEntradaDto[]) => {
    setRanking(nuevoRanking);
  }, []);

  usePartidasTiempoReal({
    sesionId,
    onEstadoCambiado: alEstadoCambiado,
    onPuntajeActualizado: alPuntajeActualizado,
  });

  // ---------------------------------------------------------------------------
  // Carga inicial: misiones de la sesión
  // ---------------------------------------------------------------------------
  useEffect(() => {
    if (!sesionId || !token) return;
    void cargarSesion();
  }, [sesionId, token]);

  async function cargarSesion() {
    try {
      const detalle = await obtenerDetalleSesionDisponibleApi(token, sesionId!);
      const misionesOrdenadas = [...detalle.misiones].sort((a, b) => a.orden - b.orden);
      setMisiones(misionesOrdenadas);

      if (detalle.estado !== "Activa") {
        setFase("esperando_partida");
      } else {
        setFase("entre_misiones");
      }
    } catch (e) {
      setError(e instanceof Error ? e.message : "No se pudo cargar la sesión.");
      setFase("error");
    }
  }

  // ---------------------------------------------------------------------------
  // Timer de pregunta
  // ---------------------------------------------------------------------------
  function iniciarTimer(segundos: number) {
    detenerTimer();
    tiempoInicioPregunta.current = Date.now();
    setTiempoRestante(segundos);
    intervalTimer.current = setInterval(() => {
      setTiempoRestante(prev => {
        if (prev <= 1) {
          detenerTimer();
          return 0;
        }
        return prev - 1;
      });
    }, 1000);
  }

  function detenerTimer() {
    if (intervalTimer.current) {
      clearInterval(intervalTimer.current);
      intervalTimer.current = null;
    }
  }

  useEffect(() => () => detenerTimer(), []);

  // ---------------------------------------------------------------------------
  // Cargar etapa actual
  // Fetches trivia + preguntas ya respondidas, then skips to first unanswered.
  // If all answered, marks stage as jugada and goes to entre_etapas.
  // ---------------------------------------------------------------------------
  const etapaActual: EtapaSesionMovilDto | null =
    misiones[misionIdx]?.etapas.sort((a, b) => a.orden - b.orden)[etapaIdx] ?? null;

  async function cargarEtapa(mision: MisionSesionMovilDto, etapa: EtapaSesionMovilDto) {
    setFase("cargando_etapa");
    try {
      if (etapa.tipoModoDeJuego === "Trivia") {
        const [trivia, respondidas] = await Promise.all([
          obtenerTriviaParticipanteApi(etapa.modoDeJuegoId, token),
          obtenerPreguntasRespondidasApi(sesionId!, mision.id, etapa.id, token),
        ]);

        setTriviaActual(trivia);

        const respondidasSet = new Set(respondidas);
        const primeraNoRespondida = trivia.preguntas.findIndex(p => !respondidasSet.has(p.id));

        if (primeraNoRespondida === -1) {
          // All questions answered → mark stage done and skip to entre_etapas
          marcarEtapaJugada(mision, etapa);
          setFase("entre_etapas");
        } else {
          setPreguntaIdx(primeraNoRespondida);
          setFase("trivia");
          const tiempo = trivia.preguntas[primeraNoRespondida].tiempoEstimado
            ?? trivia.tiempoLimitePorPregunta;
          iniciarTimer(tiempo);
        }
      } else {
        // BusquedaDelTesoro u otros: no implementado aún
        setFase("entre_etapas");
      }
    } catch (e) {
      setError(e instanceof Error ? e.message : "No se pudo cargar la etapa.");
      setFase("error");
    }
  }

  function marcarEtapaJugada(mision: MisionSesionMovilDto, etapa: EtapaSesionMovilDto) {
    setEtapasJugadas(prev => new Set([...prev, `${mision.id}:${etapa.id}`]));
  }

  // Cuando se confirma iniciar una etapa
  function iniciarEtapa() {
    const mision = misiones[misionIdx];
    if (!etapaActual || !mision) return;
    void cargarEtapa(mision, etapaActual);
  }

  // ---------------------------------------------------------------------------
  // Enviar respuesta
  // ---------------------------------------------------------------------------
  async function responder(opcionId: string) {
    if (enviando || !triviaActual || !sesionId) return;
    detenerTimer();

    const mision = misiones[misionIdx];
    const etapa = mision.etapas.sort((a, b) => a.orden - b.orden)[etapaIdx];
    const pregunta = triviaActual.preguntas[preguntaIdx];
    const tiempoTardadoMs = Date.now() - tiempoInicioPregunta.current;

    setEnviando(true);
    try {
      const resultado = await enviarRespuestaTriviaApi(
        token,
        sesionId,
        mision.id,
        etapa.id,
        etapa.modoDeJuegoId,
        pregunta.id,
        opcionId,
        tiempoTardadoMs,
      );
      setUltimoResultado(resultado);
      setFase("resultado_pregunta");
    } catch (e) {
      setUltimoResultado({
        esCorrecta: false,
        puntosGanados: 0,
        yaRespondida: false,
        mensaje: e instanceof Error ? e.message : "Error al enviar la respuesta.",
      });
      setFase("resultado_pregunta");
    } finally {
      setEnviando(false);
    }
  }

  // ---------------------------------------------------------------------------
  // Avanzar después de ver el resultado de una pregunta
  // ---------------------------------------------------------------------------
  function avanzar() {
    if (!triviaActual) return;
    const siguientePreguntaIdx = preguntaIdx + 1;

    if (siguientePreguntaIdx < triviaActual.preguntas.length) {
      setPreguntaIdx(siguientePreguntaIdx);
      setFase("trivia");
      const pregunta = triviaActual.preguntas[siguientePreguntaIdx];
      iniciarTimer(pregunta.tiempoEstimado ?? triviaActual.tiempoLimitePorPregunta);
    } else {
      // Last question done → mark stage as jugada
      const mision = misiones[misionIdx];
      const etapa = [...(mision?.etapas ?? [])].sort((a, b) => a.orden - b.orden)[etapaIdx];
      if (mision && etapa) {
        marcarEtapaJugada(mision, etapa);
      }
      setFase("entre_etapas");
    }
  }

  // ---------------------------------------------------------------------------
  // Avanzar a la siguiente etapa / misión (skips already-played stages)
  // ---------------------------------------------------------------------------
  function avanzarEtapa() {
    const siguiente = encontrarSiguienteEtapaNoJugada(misiones, etapasJugadas, misionIdx, etapaIdx + 1);

    if (siguiente === null) {
      void cargarRanking();
      return;
    }

    setMisionIdx(siguiente.misionIdx);
    setEtapaIdx(siguiente.etapaIdx);
    setFase("entre_misiones");
  }

  async function cargarRanking() {
    try {
      const r = await obtenerRankingApi(sesionId!, token);
      setRanking(r);
    } catch {
      // Si falla, mostramos ranking vacío
    }
    setFase("ranking");
  }

  // ---------------------------------------------------------------------------
  // Render por fase
  // ---------------------------------------------------------------------------
  if (fase === "cargando") {
    return (
      <PantallaBase>
        <View style={estilos.centrado}>
          <ActivityIndicator color={tema.colores.primario} size="large" />
          <Text style={estilos.textoEstado}>Cargando sesión…</Text>
        </View>
      </PantallaBase>
    );
  }

  if (fase === "error") {
    return (
      <PantallaBase>
        <View style={estilos.cuadroError}>
          <Text style={estilos.textoError}>{error ?? "Ocurrió un error."}</Text>
        </View>
        <TouchableOpacity
          style={estilos.botonPrimario}
          onPress={() => enrutador.back()}
        >
          <Text style={estilos.botonTexto}>Volver</Text>
        </TouchableOpacity>
      </PantallaBase>
    );
  }

  if (fase === "esperando_partida") {
    return (
      <PantallaBase>
        <View style={estilos.centrado}>
          <Text style={estilos.titulo}>Esperando al operador…</Text>
          <Text style={estilos.subtitulo}>
            La partida comenzará en breve. Esta pantalla se actualizará automáticamente.
          </Text>
          <ActivityIndicator
            color={tema.colores.primario}
            style={{ marginTop: tema.espacios.xl }}
          />
        </View>
        <TouchableOpacity
          style={[estilos.botonSecundario, { marginTop: tema.espacios.xxl }]}
          onPress={() => enrutador.back()}
        >
          <Text style={estilos.botonSecundarioTexto}>Volver al detalle</Text>
        </TouchableOpacity>
      </PantallaBase>
    );
  }

  if (fase === "entre_misiones" || fase === "cargando_etapa") {
    const mision = misiones[misionIdx];
    const etapasOrdenadas = mision
      ? [...mision.etapas].sort((a, b) => a.orden - b.orden)
      : [];
    const etapa = etapasOrdenadas[etapaIdx];

    return (
      <PantallaBase>
        <View style={estilos.encabezado}>
          <Text style={estilos.etiquetaSeccion}>
            MISIÓN {misionIdx + 1} DE {misiones.length}
          </Text>
          <Text style={estilos.titulo}>{mision?.nombre ?? "Misión"}</Text>
          {mision?.descripcion ? (
            <Text style={estilos.subtitulo}>{mision.descripcion}</Text>
          ) : null}
        </View>

        {etapa && (
          <View style={estilos.tarjeta}>
            <Text style={estilos.etiquetaSeccion}>
              ETAPA {etapaIdx + 1} DE {etapasOrdenadas.length}
            </Text>
            <Text style={estilos.nombreEtapa}>{etapa.nombreModoDeJuego}</Text>
            <Text style={estilos.tipoEtapa}>
              {etapa.tipoModoDeJuego === "Trivia" ? "Trivia" : "Búsqueda del Tesoro"}
              {etapa.tiempoEstimadoSegundos
                ? ` · ~${Math.round(etapa.tiempoEstimadoSegundos / 60)} min`
                : ""}
            </Text>

            {etapa.tipoModoDeJuego !== "Trivia" && (
              <View style={estilos.cuadroAviso}>
                <Text style={estilos.textoAviso}>
                  Este tipo de etapa no está disponible aún en la app móvil.
                </Text>
              </View>
            )}
          </View>
        )}

        {fase === "cargando_etapa" ? (
          <ActivityIndicator
            color={tema.colores.primario}
            style={{ marginTop: tema.espacios.xl }}
          />
        ) : etapa?.tipoModoDeJuego === "Trivia" ? (
          <TouchableOpacity style={estilos.botonPrimario} onPress={iniciarEtapa}>
            <Text style={estilos.botonTexto}>Iniciar etapa</Text>
          </TouchableOpacity>
        ) : (
          <TouchableOpacity style={estilos.botonPrimario} onPress={avanzarEtapa}>
            <Text style={estilos.botonTexto}>Continuar</Text>
          </TouchableOpacity>
        )}
      </PantallaBase>
    );
  }

  if (fase === "trivia" && triviaActual) {
    const pregunta = triviaActual.preguntas[preguntaIdx];
    const tiempoLimit = pregunta.tiempoEstimado ?? triviaActual.tiempoLimitePorPregunta;

    return (
      <PantallaBase>
        {/* Barra de progreso */}
        <View style={estilos.barraProgreso}>
          <View
            style={[
              estilos.barraProgresoRelleno,
              {
                width: `${tiempoLimit > 0
                  ? Math.round((tiempoRestante / tiempoLimit) * 100)
                  : 100}%`,
                backgroundColor:
                  tiempoRestante <= 5
                    ? tema.colores.error
                    : tiempoRestante <= 10
                    ? tema.colores.aviso
                    : tema.colores.primario,
              },
            ]}
          />
        </View>

        <View style={estilos.filaTimer}>
          <Text style={estilos.etiquetaSeccion}>
            {preguntaIdx + 1} / {triviaActual.preguntas.length}
          </Text>
          <Text
            style={[
              estilos.timer,
              tiempoRestante <= 5 && { color: tema.colores.error },
            ]}
          >
            {tiempoRestante}s
          </Text>
        </View>

        <ScrollView style={{ flex: 1 }} contentContainerStyle={{ paddingBottom: tema.espacios.xl }}>
          <View style={estilos.tarjeta}>
            <Text style={estilos.enunciado}>{pregunta.enunciado}</Text>
            <Text style={estilos.puntajeTexto}>{pregunta.puntajeAsignado} pts</Text>
          </View>

          {pregunta.opciones.map(opcion => (
            <TouchableOpacity
              key={opcion.id}
              style={[estilos.botonOpcion, enviando && estilos.opcionDeshabilitada]}
              onPress={() => void responder(opcion.id)}
              disabled={enviando || tiempoRestante === 0}
              activeOpacity={0.7}
            >
              {enviando ? (
                <ActivityIndicator color={tema.colores.textoBlanco} size="small" />
              ) : (
                <Text style={estilos.textoOpcion}>{opcion.texto}</Text>
              )}
            </TouchableOpacity>
          ))}

          {tiempoRestante === 0 && (
            <View style={estilos.cuadroAviso}>
              <Text style={estilos.textoAviso}>¡Se acabó el tiempo!</Text>
              <TouchableOpacity
                style={[estilos.botonPrimario, { marginTop: tema.espacios.md }]}
                onPress={avanzar}
              >
                <Text style={estilos.botonTexto}>Siguiente</Text>
              </TouchableOpacity>
            </View>
          )}
        </ScrollView>
      </PantallaBase>
    );
  }

  if (fase === "resultado_pregunta" && ultimoResultado) {
    return (
      <PantallaBase>
        <View style={estilos.centrado}>
          <View
            style={[
              estilos.iconoResultado,
              {
                backgroundColor: ultimoResultado.esCorrecta
                  ? tema.colores.exitoSuave
                  : tema.colores.errorSuave,
              },
            ]}
          >
            <Text style={estilos.iconoResultadoTexto}>
              {ultimoResultado.esCorrecta ? "✓" : "✗"}
            </Text>
          </View>

          <Text
            style={[
              estilos.titulo,
              {
                color: ultimoResultado.esCorrecta
                  ? tema.colores.exito
                  : tema.colores.error,
                marginTop: tema.espacios.lg,
              },
            ]}
          >
            {ultimoResultado.esCorrecta ? "¡Correcto!" : "Incorrecto"}
          </Text>

          <Text style={estilos.subtitulo}>{ultimoResultado.mensaje}</Text>

          {ultimoResultado.puntosGanados > 0 && (
            <Text style={estilos.puntosGanados}>
              +{ultimoResultado.puntosGanados} pts
            </Text>
          )}
        </View>

        <TouchableOpacity
          style={estilos.botonPrimario}
          onPress={avanzar}
        >
          <Text style={estilos.botonTexto}>
            {triviaActual && preguntaIdx + 1 < triviaActual.preguntas.length
              ? "Siguiente pregunta"
              : "Ver resultado de etapa"}
          </Text>
        </TouchableOpacity>
      </PantallaBase>
    );
  }

  if (fase === "entre_etapas") {
    const mision = misiones[misionIdx];
    const etapasOrdenadas = [...(mision?.etapas ?? [])].sort((a, b) => a.orden - b.orden);
    const etapaActualCompletada = etapasOrdenadas[etapaIdx];

    // Next unplayed stage considering current etapasJugadas
    const siguiente = encontrarSiguienteEtapaNoJugada(misiones, etapasJugadas, misionIdx, etapaIdx + 1);
    const esUltima = siguiente === null;
    const siguienteEtapa = siguiente && siguiente.misionIdx === misionIdx
      ? misiones[siguiente.misionIdx]?.etapas.sort((a, b) => a.orden - b.orden)[siguiente.etapaIdx]
      : null;
    const siguienteMision = siguiente && siguiente.misionIdx !== misionIdx
      ? misiones[siguiente.misionIdx]
      : null;

    return (
      <PantallaBase>
        <View style={estilos.centrado}>
          <Text style={estilos.etiquetaSeccion}>ETAPA COMPLETADA</Text>
          <Text style={estilos.titulo}>
            {etapaActualCompletada?.nombreModoDeJuego ?? "Etapa"}
          </Text>

          {!esUltima && (
            <Text style={estilos.subtitulo}>
              {siguienteEtapa
                ? `Siguiente etapa: ${siguienteEtapa.nombreModoDeJuego}`
                : siguienteMision
                ? `Siguiente misión: ${siguienteMision.nombre}`
                : ""}
            </Text>
          )}
        </View>

        {ranking.length > 0 && <MiniRanking ranking={ranking} />}

        <TouchableOpacity
          style={estilos.botonPrimario}
          onPress={avanzarEtapa}
        >
          <Text style={estilos.botonTexto}>
            {esUltima ? "Ver ranking final" : "Continuar"}
          </Text>
        </TouchableOpacity>
      </PantallaBase>
    );
  }

  if (fase === "ranking") {
    return (
      <PantallaBase>
        <View style={estilos.encabezado}>
          <Text style={estilos.titulo}>Ranking final</Text>
        </View>

        <ScrollView style={{ flex: 1 }}>
          {ranking.length === 0 ? (
            <Text style={[estilos.subtitulo, { textAlign: "center" }]}>
              Sin datos de ranking aún.
            </Text>
          ) : (
            ranking.map(entrada => (
              <View key={`${entrada.equipoId ?? entrada.participanteId}`} style={estilos.filaRanking}>
                <Text style={estilos.posicion}>#{entrada.posicion}</Text>
                <View style={{ flex: 1 }}>
                  <Text style={estilos.nombreRanking}>
                    {entrada.nombre || (entrada.equipoId ? `Equipo #${entrada.posicion}` : `Jugador #${entrada.posicion}`)}
                  </Text>
                  <Text style={estilos.textoTenue}>
                    {entrada.respuestasCorrectas} correctas
                  </Text>
                </View>
                <Text style={estilos.puntajeRanking}>{entrada.puntajeTotal} pts</Text>
              </View>
            ))
          )}
        </ScrollView>

        <TouchableOpacity
          style={[estilos.botonSecundario, { marginTop: tema.espacios.xl }]}
          onPress={() => enrutador.replace("/participante/sesiones")}
        >
          <Text style={estilos.botonSecundarioTexto}>Volver al inicio</Text>
        </TouchableOpacity>
      </PantallaBase>
    );
  }

  return null;
}

// ---------------------------------------------------------------------------
// Mini-ranking durante el juego
// ---------------------------------------------------------------------------
function MiniRanking({ ranking }: { ranking: RankingEntradaDto[] }) {
  const top5 = ranking.slice(0, 5);
  return (
    <View style={estilos.miniRanking}>
      <Text style={estilos.etiquetaSeccion}>RANKING ACTUAL</Text>
      {top5.map(entrada => (
        <View key={`${entrada.equipoId ?? entrada.participanteId}`} style={estilos.filaRankingMini}>
          <Text style={estilos.textoTenue}>#{entrada.posicion}</Text>
          <Text style={[estilos.nombreRanking, { flex: 1, marginHorizontal: tema.espacios.sm }]}>
            {entrada.nombre || (entrada.equipoId ? `Equipo #${entrada.posicion}` : `Jugador #${entrada.posicion}`)}
          </Text>
          <Text style={estilos.textoTenue}>{entrada.puntajeTotal} pts</Text>
        </View>
      ))}
    </View>
  );
}

// ---------------------------------------------------------------------------
// Estilos
// ---------------------------------------------------------------------------
const estilos = StyleSheet.create({
  centrado: {
    flex: 1,
    alignItems: "center",
    justifyContent: "center",
    paddingHorizontal: tema.espacios.xl,
  },
  encabezado: {
    paddingVertical: tema.espacios.lg,
  },
  etiquetaSeccion: {
    fontSize: tema.tipografia.tamanos.xs,
    fontWeight: tema.tipografia.pesos.bold,
    letterSpacing: tema.tipografia.espaciadoLetra.md,
    color: tema.colores.textoTenue,
    textTransform: "uppercase",
    marginBottom: tema.espacios.xs,
  },
  titulo: {
    fontSize: tema.tipografia.tamanos.h3,
    fontWeight: tema.tipografia.pesos.bold,
    color: tema.colores.texto,
    marginBottom: tema.espacios.sm,
  },
  subtitulo: {
    fontSize: tema.tipografia.tamanos.base,
    color: tema.colores.textoTenue,
    marginBottom: tema.espacios.lg,
    lineHeight: 20,
  },
  textoEstado: {
    fontSize: tema.tipografia.tamanos.base,
    color: tema.colores.textoTenue,
    marginTop: tema.espacios.md,
  },
  tarjeta: {
    backgroundColor: tema.colores.fondoTarjeta,
    borderRadius: tema.radios.tarjeta,
    borderWidth: 1,
    borderColor: tema.colores.bordeTarjeta,
    padding: tema.espacios.lg,
    marginBottom: tema.espacios.lg,
  },
  nombreEtapa: {
    fontSize: tema.tipografia.tamanos.xl,
    fontWeight: tema.tipografia.pesos.semibold,
    color: tema.colores.texto,
    marginBottom: tema.espacios.xs,
  },
  tipoEtapa: {
    fontSize: tema.tipografia.tamanos.sm,
    color: tema.colores.textoTenue,
  },
  // Timer y barra
  barraProgreso: {
    height: 6,
    backgroundColor: tema.colores.bordeTarjeta,
    borderRadius: 3,
    marginBottom: tema.espacios.sm,
    overflow: "hidden",
  },
  barraProgresoRelleno: {
    height: "100%",
    borderRadius: 3,
  },
  filaTimer: {
    flexDirection: "row",
    justifyContent: "space-between",
    alignItems: "center",
    marginBottom: tema.espacios.md,
  },
  timer: {
    fontSize: tema.tipografia.tamanos.h4,
    fontWeight: tema.tipografia.pesos.bold,
    color: tema.colores.primario,
  },
  // Pregunta
  enunciado: {
    fontSize: tema.tipografia.tamanos.lg,
    fontWeight: tema.tipografia.pesos.semibold,
    color: tema.colores.texto,
    lineHeight: 24,
    marginBottom: tema.espacios.sm,
  },
  puntajeTexto: {
    fontSize: tema.tipografia.tamanos.sm,
    color: tema.colores.textoTenue,
  },
  // Opciones
  botonOpcion: {
    backgroundColor: tema.colores.fondoTarjeta,
    borderWidth: 1,
    borderColor: tema.colores.bordeTarjeta,
    borderRadius: tema.radios.boton,
    padding: tema.espacios.lg,
    marginBottom: tema.espacios.sm,
    alignItems: "center",
    minHeight: 52,
    justifyContent: "center",
  },
  opcionDeshabilitada: {
    opacity: 0.5,
  },
  textoOpcion: {
    fontSize: tema.tipografia.tamanos.md,
    color: tema.colores.texto,
    textAlign: "center",
  },
  // Resultado
  iconoResultado: {
    width: 80,
    height: 80,
    borderRadius: 40,
    alignItems: "center",
    justifyContent: "center",
  },
  iconoResultadoTexto: {
    fontSize: 40,
    fontWeight: tema.tipografia.pesos.bold,
    color: tema.colores.texto,
  },
  puntosGanados: {
    fontSize: tema.tipografia.tamanos.h2,
    fontWeight: tema.tipografia.pesos.extrabold,
    color: tema.colores.exito,
    marginTop: tema.espacios.sm,
  },
  // Ranking
  filaRanking: {
    flexDirection: "row",
    alignItems: "center",
    paddingVertical: tema.espacios.md,
    paddingHorizontal: tema.espacios.lg,
    borderBottomWidth: 1,
    borderBottomColor: tema.colores.bordeTarjeta,
    gap: tema.espacios.md,
  },
  filaRankingMini: {
    flexDirection: "row",
    alignItems: "center",
    paddingVertical: tema.espacios.xs,
  },
  posicion: {
    fontSize: tema.tipografia.tamanos.lg,
    fontWeight: tema.tipografia.pesos.bold,
    color: tema.colores.primario,
    width: 36,
  },
  nombreRanking: {
    fontSize: tema.tipografia.tamanos.md,
    color: tema.colores.texto,
    fontWeight: tema.tipografia.pesos.semibold,
  },
  puntajeRanking: {
    fontSize: tema.tipografia.tamanos.md,
    color: tema.colores.exito,
    fontWeight: tema.tipografia.pesos.bold,
  },
  textoTenue: {
    fontSize: tema.tipografia.tamanos.sm,
    color: tema.colores.textoTenue,
  },
  miniRanking: {
    backgroundColor: tema.colores.fondoTarjeta,
    borderRadius: tema.radios.tarjeta,
    borderWidth: 1,
    borderColor: tema.colores.bordeTarjeta,
    padding: tema.espacios.lg,
    marginBottom: tema.espacios.lg,
    gap: tema.espacios.sm,
  },
  // Botones
  botonPrimario: {
    backgroundColor: tema.colores.primario,
    borderRadius: tema.radios.boton,
    paddingVertical: tema.espacios.md,
    paddingHorizontal: tema.espacios.xl,
    alignItems: "center",
    marginTop: tema.espacios.lg,
  },
  botonTexto: {
    color: tema.colores.textoBlanco,
    fontSize: tema.tipografia.tamanos.md,
    fontWeight: tema.tipografia.pesos.semibold,
  },
  botonSecundario: {
    borderWidth: 1,
    borderColor: tema.colores.bordeTarjeta,
    borderRadius: tema.radios.boton,
    paddingVertical: tema.espacios.md,
    paddingHorizontal: tema.espacios.xl,
    alignItems: "center",
  },
  botonSecundarioTexto: {
    color: tema.colores.textoTenue,
    fontSize: tema.tipografia.tamanos.md,
  },
  // Alertas
  cuadroError: {
    backgroundColor: tema.colores.errorSuave,
    borderRadius: tema.radios.tarjeta,
    padding: tema.espacios.lg,
    marginBottom: tema.espacios.lg,
  },
  textoError: {
    color: tema.colores.error,
    fontSize: tema.tipografia.tamanos.base,
    textAlign: "center",
  },
  cuadroAviso: {
    backgroundColor: tema.colores.avisoSuave,
    borderRadius: tema.radios.tarjeta,
    padding: tema.espacios.lg,
    marginTop: tema.espacios.md,
  },
  textoAviso: {
    color: tema.colores.aviso,
    fontSize: tema.tipografia.tamanos.base,
    textAlign: "center",
  },
});
