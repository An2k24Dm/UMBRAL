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
  obtenerMiDesgloseSesionApi,
  obtenerRankingEquiposSesionApi,
  obtenerRankingParticipantesSesionApi,
  type MiDesgloseSesionDto,
  type RankingEquipoDto,
  type RankingParticipanteDto,
} from "../../../servicios/rankingApi";
import { formatearFechaHora } from "../../../utilidades/formatoFechas";

function subDesdeToken(token: string): string | null {
  try {
    const payload = token.split(".")[1];
    const json = JSON.parse(atob(payload.replace(/-/g, "+").replace(/_/g, "/"))) as Record<string, unknown>;
    return typeof json.sub === "string" ? json.sub : null;
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

function ContenidoResultado() {
  const enrutador = useRouter();
  const { sesion: sesionAuth, cerrarSesion } = useAutenticacion();
  const token = sesionAuth?.tokenAcceso ?? null;
  const params = useLocalSearchParams<{
    id?: string | string[];
    nombre?: string | string[];
    modo?: string | string[];
    fechaFin?: string | string[];
  }>();

  const sesionId = Array.isArray(params.id) ? params.id[0] : params.id;
  const nombreSesion = Array.isArray(params.nombre) ? params.nombre[0] : (params.nombre ?? "Sesion");
  const modo = Array.isArray(params.modo) ? params.modo[0] : (params.modo ?? "Individual");
  const fechaFin = Array.isArray(params.fechaFin) ? params.fechaFin[0] : params.fechaFin;
  const esGrupal = modo === "Grupal";

  const [rankingParticipantes, setRankingParticipantes] = useState<RankingParticipanteDto[] | null>(null);
  const [rankingEquipos, setRankingEquipos] = useState<RankingEquipoDto[] | null>(null);
  const [desglose, setDesglose] = useState<MiDesgloseSesionDto | null>(null);
  const [equiposExpandidos, setEquiposExpandidos] = useState<Set<string>>(new Set());
  const [cargando, setCargando] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [refrescando, setRefrescando] = useState(false);

  const cargar = useCallback(async () => {
    if (!token || !sesionId) return;
    setCargando(true);
    setError(null);
    try {
      // El desglose por etapa no debe romper el ranking si falla: se pide en
      // paralelo y su error se ignora (queda null).
      const desglosePromesa = obtenerMiDesgloseSesionApi(token, sesionId).catch(() => null);
      if (esGrupal) {
        const [equipos, d] = await Promise.all([
          obtenerRankingEquiposSesionApi(token, sesionId),
          desglosePromesa,
        ]);
        setRankingEquipos(equipos);
        setRankingParticipantes(null);
        setDesglose(d);
      } else {
        const [participantes, d] = await Promise.all([
          obtenerRankingParticipantesSesionApi(token, sesionId),
          desglosePromesa,
        ]);
        setRankingParticipantes(participantes);
        setRankingEquipos(null);
        setDesglose(d);
      }
    } catch (e) {
      const estado = (e as { estadoHttp?: number }).estadoHttp;
      if (estado === 401) {
        await cerrarSesion();
        enrutador.replace("/");
        return;
      }
      setError(e instanceof Error ? e.message : "No se pudo cargar el ranking.");
    } finally {
      setCargando(false);
    }
  }, [token, sesionId, esGrupal, cerrarSesion, enrutador]);

  useEffect(() => {
    void cargar();
  }, [cargar]);

  useRefrescarAlEnfocar(cargar);

  const alRefrescar = useCallback(async () => {
    setRefrescando(true);
    try {
      await cargar();
    } finally {
      setRefrescando(false);
    }
  }, [cargar]);

  const participanteActualId = token ? subDesdeToken(token) : null;
  const miRanking = rankingParticipantes?.find(
    (p) => p.participanteIdentidadId === participanteActualId,
  ) ?? rankingEquipos
    ?.flatMap((e) => e.participantes)
    .find((p) => p.participanteIdentidadId === participanteActualId);
  const miEquipo = rankingEquipos?.find((e) =>
    e.participantes.some((p) => p.participanteIdentidadId === participanteActualId),
  );

  const alternarEquipo = (equipoId: string) => {
    setEquiposExpandidos((previo) => {
      const siguiente = new Set(previo);
      if (siguiente.has(equipoId)) siguiente.delete(equipoId);
      else siguiente.add(equipoId);
      return siguiente;
    });
  };

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
        <Text style={estilos.subtitulo}>Resultado de sesion</Text>
      </View>

      <View style={estilos.tarjeta}>
        <Text style={estilos.nombreSesion}>{nombreSesion}</Text>
        <Text style={estilos.textoTenue}>{modo}</Text>
        {fechaFin ? (
          <Text style={estilos.textoTenue}>Finalizada: {formatearFechaHora(fechaFin)}</Text>
        ) : null}
        <View style={estilos.filaResumen}>
          <Text style={estilos.textoTenue}>Mi puntaje</Text>
          <Text style={estilos.puntaje}>{miRanking?.puntaje ?? 0} pts</Text>
        </View>
        {miEquipo && (
          <Text style={estilos.textoTenue}>
            Mi equipo: {miEquipo.nombreEquipo} · {miEquipo.puntaje} pts
          </Text>
        )}
      </View>

      {desglose && desglose.misiones.length > 0 && (
        <View style={estilos.desglose}>
          <Text style={estilos.tituloSeccion}>MISIONES Y ETAPAS</Text>
          {desglose.misiones.map((m) => (
            <View key={m.misionId} style={estilos.desgloseMision}>
              <Text style={estilos.desgloseMisionTitulo}>{m.nombre || "Misión"}</Text>
              {m.etapas.map((e) => (
                <View key={e.etapaId} style={estilos.desgloseEtapaFila}>
                  <Text style={estilos.desgloseEtapaNombre}>
                    {e.nombre || "Etapa"}
                    {e.tipo ? ` — ${etiquetaTipoEtapa(e.tipo)}` : ""}
                  </Text>
                  <Text style={estilos.desgloseEtapaPuntaje}>+{e.puntaje} pts</Text>
                </View>
              ))}
              <View style={estilos.desgloseSubtotalFila}>
                <Text style={estilos.desgloseSubtotalTexto}>Subtotal misión</Text>
                <Text style={estilos.desgloseSubtotalPuntaje}>{m.puntajeTotal} pts</Text>
              </View>
            </View>
          ))}
          <View style={estilos.desgloseTotalFila}>
            <Text style={estilos.desgloseTotalTexto}>PUNTAJE TOTAL</Text>
            <Text style={estilos.desgloseTotalPuntaje}>{desglose.puntajeTotal} pts</Text>
          </View>
        </View>
      )}

      <Text style={estilos.tituloSeccion}>
        {esGrupal ? "RANKING DE EQUIPOS" : "RANKING DE PARTICIPANTES"}
      </Text>

      {cargando && (
        <View style={estilos.estado}>
          <ActivityIndicator color={tema.colores.primario} />
          <Text style={estilos.textoTenue}>Cargando ranking...</Text>
        </View>
      )}

      {!cargando && error && (
        <View style={estilos.error}>
          <Text style={estilos.errorTexto}>{error}</Text>
        </View>
      )}

      {!cargando && !error && !esGrupal && (rankingParticipantes?.length ?? 0) === 0 && (
        <Text style={estilos.textoTenue}>Aun no hay datos de ranking.</Text>
      )}

      {!cargando && !error && !esGrupal && rankingParticipantes
        ?.slice()
        .sort((a, b) => a.posicion - b.posicion)
        .map((p) => (
          <View key={p.participanteSesionId} style={estilos.fila}>
            <Text style={estilos.posicion}>{medalla(p.posicion)}</Text>
            <Text style={estilos.nombreEntrada}>{p.alias}</Text>
            <Text style={estilos.puntaje}>{p.puntaje} pts</Text>
          </View>
        ))}

      {!cargando && !error && esGrupal && (rankingEquipos?.length ?? 0) === 0 && (
        <Text style={estilos.textoTenue}>Aun no hay datos de ranking.</Text>
      )}

      {!cargando && !error && esGrupal && rankingEquipos
        ?.slice()
        .sort((a, b) => a.posicion - b.posicion)
        .map((equipo) => {
          const expandido = equiposExpandidos.has(equipo.equipoId);
          return (
            <View key={equipo.equipoId}>
              <TouchableOpacity
                style={estilos.fila}
                onPress={() => alternarEquipo(equipo.equipoId)}
              >
                <Text style={estilos.posicion}>{medalla(equipo.posicion)}</Text>
                <Text style={estilos.nombreEntrada}>{equipo.nombreEquipo}</Text>
                <Text style={estilos.puntaje}>{equipo.puntaje} pts {expandido ? "▼" : "▶"}</Text>
              </TouchableOpacity>
              {expandido && equipo.participantes.map((p) => (
                <View key={p.participanteSesionId} style={estilos.filaDetalle}>
                  <Text style={estilos.posicion}>#{p.posicion}</Text>
                  <Text style={estilos.nombreEntrada}>{p.alias}</Text>
                  <Text style={estilos.puntaje}>{p.puntaje} pts</Text>
                </View>
              ))}
            </View>
          );
        })}

      <TouchableOpacity
        style={estilos.botonSecundario}
        onPress={() => enrutador.replace("/participante/sesiones/finalizadas")}
      >
        <Text style={estilos.botonSecundarioTexto}>Volver a mis sesiones</Text>
      </TouchableOpacity>
    </PantallaBase>
  );
}

function medalla(posicion: number): string {
  if (posicion === 1) return "🥇 #1";
  if (posicion === 2) return "🥈 #2";
  if (posicion === 3) return "🥉 #3";
  return `#${posicion}`;
}

function etiquetaTipoEtapa(tipo: string): string {
  const t = tipo.toLowerCase();
  if (t.includes("trivia")) return "Trivia";
  if (t.includes("tesoro")) return "Búsqueda del Tesoro";
  return tipo;
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
  tarjeta: {
    backgroundColor: tema.colores.fondoTarjeta,
    borderColor: tema.colores.bordeTarjeta,
    borderWidth: 1,
    borderRadius: tema.radios.tarjeta,
    padding: tema.espacios.md,
    marginBottom: tema.espacios.md,
  },
  nombreSesion: {
    color: tema.colores.texto,
    fontWeight: tema.tipografia.pesos.bold,
    fontSize: tema.tipografia.tamanos.lg,
  },
  filaResumen: {
    flexDirection: "row",
    justifyContent: "space-between",
    alignItems: "center",
    marginTop: tema.espacios.sm,
  },
  tituloSeccion: {
    color: tema.colores.textoTenue,
    fontSize: tema.tipografia.tamanos.xs,
    letterSpacing: tema.tipografia.espaciadoLetra.sm,
    textTransform: "uppercase",
    marginBottom: tema.espacios.sm,
    fontWeight: tema.tipografia.pesos.bold,
  },
  estado: {
    alignItems: "center",
    gap: tema.espacios.sm,
    paddingVertical: tema.espacios.lg,
  },
  textoTenue: {
    color: tema.colores.textoTenue,
    marginTop: tema.espacios.xs,
  },
  fila: {
    flexDirection: "row",
    alignItems: "center",
    backgroundColor: tema.colores.fondoTarjeta,
    borderColor: tema.colores.bordeTarjeta,
    borderWidth: 1,
    borderRadius: tema.radios.entrada,
    padding: tema.espacios.md,
    marginBottom: tema.espacios.sm,
    gap: tema.espacios.sm,
  },
  filaDetalle: {
    flexDirection: "row",
    alignItems: "center",
    backgroundColor: "#f8fafc",
    borderColor: tema.colores.bordeTarjeta,
    borderWidth: 1,
    borderRadius: tema.radios.entrada,
    padding: tema.espacios.sm,
    marginLeft: tema.espacios.lg,
    marginBottom: tema.espacios.sm,
    gap: tema.espacios.sm,
  },
  posicion: {
    width: 58,
    color: tema.colores.texto,
    fontWeight: tema.tipografia.pesos.bold,
  },
  nombreEntrada: {
    flex: 1,
    color: tema.colores.texto,
    fontWeight: tema.tipografia.pesos.bold,
  },
  puntaje: {
    color: tema.colores.primario,
    fontWeight: tema.tipografia.pesos.extrabold,
  },
  error: {
    backgroundColor: tema.colores.errorSuave,
    borderColor: tema.colores.error,
    borderWidth: 1,
    borderRadius: tema.radios.entrada,
    padding: tema.espacios.md,
    marginBottom: tema.espacios.md,
  },
  errorTexto: {
    color: tema.colores.error,
    textAlign: "center",
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
  desglose: {
    marginBottom: tema.espacios.md,
  },
  desgloseMision: {
    backgroundColor: tema.colores.fondoTarjeta,
    borderColor: tema.colores.bordeTarjeta,
    borderWidth: 1,
    borderRadius: tema.radios.entrada,
    padding: tema.espacios.md,
    marginBottom: tema.espacios.sm,
  },
  desgloseMisionTitulo: {
    color: tema.colores.texto,
    fontWeight: tema.tipografia.pesos.bold,
    fontSize: tema.tipografia.tamanos.md,
    marginBottom: tema.espacios.xs,
  },
  desgloseEtapaFila: {
    flexDirection: "row",
    justifyContent: "space-between",
    alignItems: "center",
    paddingVertical: 4,
    paddingLeft: tema.espacios.sm,
  },
  desgloseEtapaNombre: {
    color: tema.colores.textoTenue,
    flex: 1,
  },
  desgloseEtapaPuntaje: {
    color: tema.colores.primario,
    fontWeight: tema.tipografia.pesos.bold,
    marginLeft: tema.espacios.sm,
  },
  desgloseSubtotalFila: {
    flexDirection: "row",
    justifyContent: "space-between",
    marginTop: tema.espacios.xs,
    paddingTop: tema.espacios.xs,
    borderTopWidth: 1,
    borderTopColor: tema.colores.bordeTarjeta,
  },
  desgloseSubtotalTexto: {
    color: tema.colores.texto,
    fontWeight: tema.tipografia.pesos.semibold,
  },
  desgloseSubtotalPuntaje: {
    color: tema.colores.texto,
    fontWeight: tema.tipografia.pesos.bold,
  },
  desgloseTotalFila: {
    flexDirection: "row",
    justifyContent: "space-between",
    alignItems: "center",
    backgroundColor: tema.colores.fondoTarjeta,
    borderColor: tema.colores.primario,
    borderWidth: 1,
    borderRadius: tema.radios.entrada,
    padding: tema.espacios.md,
    marginTop: tema.espacios.xs,
  },
  desgloseTotalTexto: {
    color: tema.colores.texto,
    fontWeight: tema.tipografia.pesos.extrabold,
  },
  desgloseTotalPuntaje: {
    color: tema.colores.primario,
    fontWeight: tema.tipografia.pesos.extrabold,
    fontSize: tema.tipografia.tamanos.lg,
  },
});
