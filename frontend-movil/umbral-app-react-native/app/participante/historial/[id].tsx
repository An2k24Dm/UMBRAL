import { useCallback, useEffect, useState } from "react";
import {
  ActivityIndicator,
  RefreshControl,
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
import { useRefrescarAlEnfocar } from "../../../hooks/useRefrescarAlEnfocar";
import {
  obtenerRankingParticipantesSesionApi,
  obtenerRankingEquiposSesionApi,
  type EntradaRankingParticipanteDto,
  type EntradaRankingEquipoDto,
} from "../../../servicios/rankingApi";
import {
  obtenerProgresoSesionParticipanteApi,
  type ProgresoSesionParticipanteDto,
} from "../../../servicios/sesionesApi";
import { formatearFechaHora } from "../../../utilidades/formatoFechas";

function subDesdeToken(token: string): string | null {
  try {
    const payload = token.split(".")[1];
    const json = JSON.parse(atob(payload.replace(/-/g, "+").replace(/_/g, "/"))) as Record<string, unknown>;
    return typeof json["sub"] === "string" ? json["sub"] : null;
  } catch {
    return null;
  }
}

export default function PantallaResultadoSesion() {
  return (
    <RutaProtegidaMovil>
      <ContenidoResultado />
    </RutaProtegidaMovil>
  );
}

function medalla(idx: number): string {
  if (idx === 0) return "🥇";
  if (idx === 1) return "🥈";
  if (idx === 2) return "🥉";
  return `#${idx + 1}`;
}

function ContenidoResultado() {
  const enrutador = useRouter();
  const { sesion: sesionAuth, cerrarSesion } = useAutenticacion();
  const token = sesionAuth?.tokenAcceso ?? null;

  const params = useLocalSearchParams<{
    id?: string | string[];
    nombre?: string;
    modo?: string;
    puntaje?: string;
    fechaFin?: string;
  }>();

  const sesionId = Array.isArray(params.id) ? params.id[0] : params.id;
  const nombreSesion = Array.isArray(params.nombre) ? params.nombre[0] : (params.nombre ?? "Sesión");
  const modo = Array.isArray(params.modo) ? params.modo[0] : (params.modo ?? "Individual");
  const puntajePropio = parseInt(Array.isArray(params.puntaje) ? params.puntaje[0] : (params.puntaje ?? "0"), 10);
  const fechaFin = Array.isArray(params.fechaFin) ? params.fechaFin[0] : params.fechaFin;

  const esGrupal = modo === "Grupal";

  const [rankingParticipantes, setRankingParticipantes] = useState<EntradaRankingParticipanteDto[] | null>(null);
  const [rankingEquipos, setRankingEquipos] = useState<EntradaRankingEquipoDto[] | null>(null);
  const [miProgreso, setMiProgreso] = useState<ProgresoSesionParticipanteDto | null>(null);
  const [cargando, setCargando] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [sesionExpirada, setSesionExpirada] = useState(false);

  const cargar = useCallback(async () => {
    if (!token || !sesionId) return;
    setCargando(true);
    setError(null);
    try {
      const miSub = subDesdeToken(token);
      const [progreso, ...rankingResult] = await Promise.all([
        obtenerProgresoSesionParticipanteApi(token, sesionId).catch(() => [] as ProgresoSesionParticipanteDto[]),
        esGrupal
          ? obtenerRankingEquiposSesionApi(token, sesionId).then((d) => { setRankingEquipos(d); return d; })
          : obtenerRankingParticipantesSesionApi(token, sesionId).then((d) => { setRankingParticipantes(d); return d; }),
      ]);
      void rankingResult;
      if (miSub) {
        setMiProgreso(progreso.find((p) => p.participanteIdentidadId === miSub) ?? null);
      }
    } catch (e: unknown) {
      if (
        e &&
        typeof e === "object" &&
        "estadoHttp" in e &&
        (e as { estadoHttp: number }).estadoHttp === 401
      ) {
        setSesionExpirada(true);
        return;
      }
      setError(
        e instanceof Error
          ? e.message
          : "No se pudo cargar el detalle de la sesión.",
      );
    } finally {
      setCargando(false);
    }
  }, [token, sesionId, esGrupal]);

  useEffect(() => {
    void cargar();
  }, [cargar]);

  useEffect(() => {
    if (sesionExpirada) {
      cerrarSesion().finally(() => enrutador.replace("/"));
    }
  }, [sesionExpirada, cerrarSesion, enrutador]);

  useRefrescarAlEnfocar(cargar);

  const [refrescando, setRefrescando] = useState(false);
  const alRefrescar = useCallback(async () => {
    setRefrescando(true);
    try {
      await cargar();
    } finally {
      setRefrescando(false);
    }
  }, [cargar]);

  const volver = () => enrutador.replace("/participante/sesiones/finalizadas");

  const entradas = esGrupal ? rankingEquipos : rankingParticipantes;

  return (
    <PantallaBase
      refreshControl={
        <RefreshControl
          refreshing={refrescando}
          onRefresh={alRefrescar}
          tintColor={tema.colores.primario}
          colors={[tema.colores.primario]}
        />
      }
    >
      <View style={estilos.encabezado}>
        <Text style={estilos.titulo}>UMBRAL</Text>
        <Text style={estilos.subtitulo}>Resultado de sesión</Text>
      </View>

      {/* Tarjeta con info de la sesión */}
      <View style={estilos.tarjetaSesion}>
        <View style={estilos.tarjetaFila}>
          <Text style={estilos.nombreSesion}>{nombreSesion}</Text>
          <View style={estilos.badgeModo}>
            <Text style={estilos.badgeModoTexto}>{modo}</Text>
          </View>
        </View>
        {fechaFin ? (
          <Text style={estilos.fechaFin}>
            Finalizada: {formatearFechaHora(fechaFin)}
          </Text>
        ) : null}
        <View style={estilos.filaPuntajePropio}>
          <Text style={estilos.puntajePropioEtiqueta}>Mi puntaje</Text>
          <Text style={estilos.puntajePropioValor}>{puntajePropio} pts</Text>
        </View>
      </View>

      {/* Mis estadísticas detalladas */}
      {miProgreso && (
        <>
          <Text style={estilos.tituloSeccion}>MIS ESTADÍSTICAS</Text>
          <View style={estilos.tarjetaStats}>
            {miProgreso.triviaRespondidas > 0 && (
              <View style={estilos.filaStat}>
                <View style={estilos.filaStatIzq}>
                  <Text style={estilos.statEtiqueta}>Trivia respondidas</Text>
                  <Text style={estilos.statValor}>
                    {miProgreso.triviaCorrectas} correctas · {miProgreso.triviaIncorrectas} incorrectas
                    {" "}({miProgreso.triviaRespondidas > 0
                      ? Math.round((miProgreso.triviaCorrectas / miProgreso.triviaRespondidas) * 100)
                      : 0}% acierto)
                  </Text>
                </View>
                <Text style={estilos.statPts}>{miProgreso.triviaPuntosGanados} pts</Text>
              </View>
            )}
            {miProgreso.tesoroIntentosEnviados > 0 && (
              <View style={[estilos.filaStat, miProgreso.triviaRespondidas > 0 && estilos.filaStatBorde]}>
                <View style={estilos.filaStatIzq}>
                  <Text style={estilos.statEtiqueta}>Búsqueda del tesoro</Text>
                  <Text style={estilos.statValor}>
                    {miProgreso.tesoroEtapasCompletadas} etapa{miProgreso.tesoroEtapasCompletadas !== 1 ? "s" : ""} completada{miProgreso.tesoroEtapasCompletadas !== 1 ? "s" : ""} · {miProgreso.tesoroIntentosEnviados} intento{miProgreso.tesoroIntentosEnviados !== 1 ? "s" : ""}
                  </Text>
                </View>
                <Text style={estilos.statPts}>{miProgreso.tesoroPuntosGanados} pts</Text>
              </View>
            )}
            <View style={[estilos.filaStat, estilos.filaStatBorde, estilos.filaStatTotal]}>
              <Text style={estilos.statTotalEtiqueta}>TOTAL</Text>
              <Text style={estilos.statTotalValor}>{miProgreso.totalPuntosGanados} pts</Text>
            </View>
          </View>
        </>
      )}

      {/* Ranking */}
      <Text style={estilos.tituloSeccion}>
        {esGrupal ? "RANKING DE EQUIPOS" : "RANKING DE PARTICIPANTES"}
      </Text>

      {cargando && (
        <View style={estilos.contenedorEstado}>
          <ActivityIndicator color={tema.colores.primario} size="large" />
          <Text style={estilos.textoEstado}>Cargando ranking…</Text>
        </View>
      )}

      {!cargando && error && (
        <View>
          <View style={estilos.cuadroError}>
            <Text style={estilos.cuadroErrorTexto}>{error}</Text>
          </View>
          <TouchableOpacity style={estilos.botonPrimario} onPress={cargar}>
            <Text style={estilos.botonPrimarioTexto}>Reintentar</Text>
          </TouchableOpacity>
        </View>
      )}

      {!cargando && !error && entradas !== null && entradas.length === 0 && (
        <View style={estilos.cuadroVacio}>
          <Text style={estilos.cuadroVacioTexto}>
            Aún no hay datos de ranking para esta sesión.
          </Text>
        </View>
      )}

      {!cargando && !error && !esGrupal && rankingParticipantes !== null && rankingParticipantes.length > 0 && (
        <View style={estilos.lista}>
          {rankingParticipantes.map((p, idx) => (
            <View
              key={p.participanteIdentidadId}
              style={[estilos.fila, idx === 0 && estilos.filaOro]}
            >
              <View style={estilos.filaPuesto}>
                <Text style={[estilos.puesto, idx < 3 && estilos.puestoDestacado]}>
                  {medalla(idx)}
                </Text>
              </View>
              <View style={estilos.filaDetalle}>
                <Text style={estilos.nombreEntrada}>{p.nombreParticipante}</Text>
                <Text style={estilos.desglose}>
                  {p.etapasCompletadas} etapa{p.etapasCompletadas !== 1 ? "s" : ""} · {p.respuestasCorrectas}/{p.respuestasTotales} ✓
                </Text>
              </View>
              <View style={estilos.filaPuntajeEntrada}>
                <Text style={estilos.puntajeEntrada}>{p.puntajeTotal}</Text>
                <Text style={estilos.puntajeEntradaEtiqueta}>pts</Text>
              </View>
            </View>
          ))}
        </View>
      )}

      {!cargando && !error && esGrupal && rankingEquipos !== null && rankingEquipos.length > 0 && (
        <View style={estilos.lista}>
          {rankingEquipos.map((e, idx) => (
            <View
              key={e.equipoId}
              style={[estilos.fila, idx === 0 && estilos.filaOro]}
            >
              <View style={estilos.filaPuesto}>
                <Text style={[estilos.puesto, idx < 3 && estilos.puestoDestacado]}>
                  {medalla(idx)}
                </Text>
              </View>
              <View style={estilos.filaDetalle}>
                <Text style={estilos.nombreEntrada}>{e.nombreEquipo}</Text>
                <Text style={estilos.desglose}>
                  {e.etapasCompletadas} etapa{e.etapasCompletadas !== 1 ? "s" : ""}
                </Text>
              </View>
              <View style={estilos.filaPuntajeEntrada}>
                <Text style={estilos.puntajeEntrada}>{e.puntajeTotal}</Text>
                <Text style={estilos.puntajeEntradaEtiqueta}>pts</Text>
              </View>
            </View>
          ))}
        </View>
      )}

      <TouchableOpacity
        style={estilos.botonSecundario}
        onPress={volver}
        accessibilityRole="button"
      >
        <Text style={estilos.botonSecundarioTexto}>Volver a mis sesiones</Text>
      </TouchableOpacity>
    </PantallaBase>
  );
}

const estilos = StyleSheet.create({
  encabezado: {
    alignItems: "center",
    marginBottom: tema.espacios.lg,
    paddingTop: tema.espacios.md,
  },
  titulo: {
    fontSize: tema.tipografia.tamanos.h2,
    fontWeight: tema.tipografia.pesos.extrabold,
    color: tema.colores.texto,
    letterSpacing: tema.tipografia.espaciadoLetra.md,
  },
  subtitulo: {
    fontSize: tema.tipografia.tamanos.xs,
    color: tema.colores.textoTenue,
    marginTop: tema.espacios.xs,
    letterSpacing: tema.tipografia.espaciadoLetra.sm,
    textTransform: "uppercase",
  },
  tarjetaSesion: {
    backgroundColor: tema.colores.fondoTarjeta,
    borderRadius: tema.radios.tarjeta,
    borderWidth: 1,
    borderColor: tema.colores.bordeTarjeta,
    padding: tema.espacios.md,
    marginBottom: tema.espacios.md,
  },
  tarjetaFila: {
    flexDirection: "row",
    justifyContent: "space-between",
    alignItems: "flex-start",
    marginBottom: tema.espacios.xs,
  },
  nombreSesion: {
    color: tema.colores.texto,
    fontSize: tema.tipografia.tamanos.lg,
    fontWeight: tema.tipografia.pesos.bold,
    flex: 1,
    marginRight: tema.espacios.sm,
  },
  badgeModo: {
    backgroundColor: "#ede9fe",
    borderRadius: 6,
    paddingHorizontal: tema.espacios.sm,
    paddingVertical: 2,
  },
  badgeModoTexto: {
    color: tema.colores.primario,
    fontSize: tema.tipografia.tamanos.xs,
    fontWeight: tema.tipografia.pesos.bold,
  },
  fechaFin: {
    color: tema.colores.textoTenue,
    fontSize: tema.tipografia.tamanos.sm,
    marginBottom: tema.espacios.sm,
  },
  filaPuntajePropio: {
    flexDirection: "row",
    justifyContent: "space-between",
    alignItems: "center",
    borderTopWidth: 1,
    borderTopColor: tema.colores.bordeTarjeta,
    paddingTop: tema.espacios.sm,
    marginTop: tema.espacios.xs,
  },
  puntajePropioEtiqueta: {
    color: tema.colores.textoTenue,
    fontSize: tema.tipografia.tamanos.sm,
    textTransform: "uppercase",
    letterSpacing: tema.tipografia.espaciadoLetra.sm,
  },
  puntajePropioValor: {
    color: tema.colores.exito,
    fontSize: tema.tipografia.tamanos.lg,
    fontWeight: tema.tipografia.pesos.extrabold,
  },
  tituloSeccion: {
    color: tema.colores.textoTenue,
    fontSize: tema.tipografia.tamanos.xs,
    letterSpacing: tema.tipografia.espaciadoLetra.sm,
    textTransform: "uppercase",
    marginBottom: tema.espacios.sm,
    marginLeft: tema.espacios.xs,
    fontWeight: tema.tipografia.pesos.bold,
  },
  contenedorEstado: {
    alignItems: "center",
    justifyContent: "center",
    padding: tema.espacios.xl,
  },
  textoEstado: {
    color: tema.colores.textoTenue,
    marginTop: tema.espacios.md,
    fontSize: tema.tipografia.tamanos.md,
  },
  cuadroError: {
    backgroundColor: tema.colores.errorSuave,
    borderColor: tema.colores.error,
    borderWidth: 1,
    borderRadius: tema.radios.entrada,
    padding: tema.espacios.md,
    marginBottom: tema.espacios.md,
  },
  cuadroErrorTexto: {
    color: tema.colores.error,
    fontSize: tema.tipografia.tamanos.sm,
    textAlign: "center",
  },
  cuadroVacio: {
    backgroundColor: tema.colores.fondoTarjeta,
    borderRadius: tema.radios.tarjeta,
    borderWidth: 1,
    borderColor: tema.colores.bordeTarjeta,
    padding: tema.espacios.lg,
    alignItems: "center",
    marginBottom: tema.espacios.md,
  },
  cuadroVacioTexto: {
    color: tema.colores.textoTenue,
    fontSize: tema.tipografia.tamanos.sm,
    textAlign: "center",
    lineHeight: 20,
  },
  lista: { gap: tema.espacios.xs, marginBottom: tema.espacios.md },
  fila: {
    flexDirection: "row",
    alignItems: "center",
    backgroundColor: tema.colores.fondoTarjeta,
    borderRadius: tema.radios.entrada,
    borderWidth: 1,
    borderColor: tema.colores.bordeTarjeta,
    padding: tema.espacios.sm,
    gap: tema.espacios.sm,
  },
  filaOro: {
    borderColor: "#f59e0b",
    backgroundColor: "#fffbeb",
  },
  filaPuesto: {
    width: 40,
    alignItems: "center",
  },
  puesto: {
    color: tema.colores.textoTenue,
    fontWeight: tema.tipografia.pesos.bold,
    fontSize: tema.tipografia.tamanos.md,
  },
  puestoDestacado: {
    fontSize: tema.tipografia.tamanos.lg,
  },
  filaDetalle: {
    flex: 1,
  },
  nombreEntrada: {
    color: tema.colores.texto,
    fontWeight: tema.tipografia.pesos.bold,
    fontSize: tema.tipografia.tamanos.md,
  },
  desglose: {
    color: tema.colores.textoTenue,
    fontSize: tema.tipografia.tamanos.xs,
    marginTop: 2,
  },
  filaPuntajeEntrada: {
    alignItems: "flex-end",
  },
  puntajeEntrada: {
    color: tema.colores.primario,
    fontWeight: tema.tipografia.pesos.extrabold,
    fontSize: tema.tipografia.tamanos.lg,
  },
  puntajeEntradaEtiqueta: {
    color: tema.colores.textoTenue,
    fontSize: tema.tipografia.tamanos.xs,
  },
  botonPrimario: {
    backgroundColor: tema.colores.primario,
    paddingVertical: tema.espacios.md,
    borderRadius: tema.radios.boton,
    alignItems: "center",
    marginTop: tema.espacios.sm,
    marginBottom: tema.espacios.md,
  },
  botonPrimarioTexto: {
    color: tema.colores.textoBlanco,
    fontWeight: tema.tipografia.pesos.bold,
    fontSize: tema.tipografia.tamanos.lg,
  },
  tarjetaStats: {
    backgroundColor: tema.colores.fondoTarjeta,
    borderRadius: tema.radios.tarjeta,
    borderWidth: 1,
    borderColor: tema.colores.bordeTarjeta,
    padding: tema.espacios.md,
    marginBottom: tema.espacios.md,
  },
  filaStat: {
    flexDirection: "row",
    justifyContent: "space-between",
    alignItems: "center",
    paddingVertical: tema.espacios.sm,
  },
  filaStatBorde: {
    borderTopWidth: 1,
    borderTopColor: tema.colores.bordeTarjeta,
  },
  filaStatIzq: {
    flex: 1,
    marginRight: tema.espacios.sm,
  },
  statEtiqueta: {
    color: tema.colores.texto,
    fontWeight: tema.tipografia.pesos.bold,
    fontSize: tema.tipografia.tamanos.sm,
  },
  statValor: {
    color: tema.colores.textoTenue,
    fontSize: tema.tipografia.tamanos.xs,
    marginTop: 2,
  },
  statPts: {
    color: tema.colores.primario,
    fontWeight: tema.tipografia.pesos.extrabold,
    fontSize: tema.tipografia.tamanos.md,
  },
  filaStatTotal: {
    marginTop: 2,
  },
  statTotalEtiqueta: {
    color: tema.colores.textoTenue,
    fontWeight: tema.tipografia.pesos.bold,
    fontSize: tema.tipografia.tamanos.xs,
    letterSpacing: tema.tipografia.espaciadoLetra.sm,
    textTransform: "uppercase",
  },
  statTotalValor: {
    color: tema.colores.exito,
    fontWeight: tema.tipografia.pesos.extrabold,
    fontSize: tema.tipografia.tamanos.lg,
  },
  botonSecundario: {
    paddingVertical: tema.espacios.md,
    borderRadius: tema.radios.boton,
    alignItems: "center",
    marginTop: tema.espacios.md,
    borderWidth: 1,
    borderColor: tema.colores.bordeTarjeta,
    backgroundColor: tema.colores.fondoTarjeta,
  },
  botonSecundarioTexto: {
    color: tema.colores.texto,
    fontWeight: tema.tipografia.pesos.bold,
    fontSize: tema.tipografia.tamanos.md,
  },
});
