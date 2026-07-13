import { useCallback, useEffect, useState } from "react";
import {
  ActivityIndicator,
  RefreshControl,
  StyleSheet,
  Text,
  View,
} from "react-native";
import { useRouter } from "expo-router";
import { useAutenticacion } from "../../autenticacion/ContextoAutenticacion";
import RutaProtegidaMovil from "../../autenticacion/RutaProtegidaMovil";
import { BotonMovil } from "../../componentes/BotonMovil";
import { PantallaBase } from "../../componentes/PantallaBase";
import { tema } from "../../estilos/tema";
import {
  obtenerRankingGlobalApi,
  type RankingGlobalDto,
} from "../../servicios/rankingApi";

export default function PantallaRankingGlobalParticipante() {
  return (
    <RutaProtegidaMovil>
      <ContenidoRankingGlobal />
    </RutaProtegidaMovil>
  );
}

function ContenidoRankingGlobal() {
  const enrutador = useRouter();
  const { sesion, cerrarSesion } = useAutenticacion();
  const token = sesion?.tokenAcceso ?? null;
  const [ranking, setRanking] = useState<RankingGlobalDto[]>([]);
  const [cargando, setCargando] = useState(true);
  const [refrescando, setRefrescando] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const cargar = useCallback(async () => {
    if (!token) return;
    setError(null);
    try {
      const datos = await obtenerRankingGlobalApi(token, 50);
      setRanking(datos);
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
  }, [token, cerrarSesion, enrutador]);

  useEffect(() => {
    void cargar();
  }, [cargar]);

  const alRefrescar = useCallback(async () => {
    setRefrescando(true);
    try {
      await cargar();
    } finally {
      setRefrescando(false);
    }
  }, [cargar]);

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
        <Text style={estilos.subtitulo}>Ranking global</Text>
      </View>

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

      {!cargando && !error && ranking.length === 0 && (
        <Text style={estilos.textoTenue}>Aun no hay puntajes registrados.</Text>
      )}

      {!cargando && !error && ranking.map((item) => (
        <View key={item.participanteIdentidadId} style={estilos.fila}>
          <Text style={estilos.posicion}>{medalla(item.posicion)}</Text>
          <Text style={estilos.alias}>{item.alias}</Text>
          <Text style={estilos.puntaje}>{item.puntaje} pts</Text>
        </View>
      ))}

      <BotonMovil
        titulo="Volver"
        variante="secundario"
        onPress={() => enrutador.replace("/participante/menu")}
      />
    </PantallaBase>
  );
}

function medalla(posicion: number): string {
  if (posicion === 1) return "🥇 #1";
  if (posicion === 2) return "🥈 #2";
  if (posicion === 3) return "🥉 #3";
  return `#${posicion}`;
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
  estado: {
    alignItems: "center",
    gap: tema.espacios.sm,
    paddingVertical: tema.espacios.lg,
  },
  textoTenue: {
    color: tema.colores.textoTenue,
    textAlign: "center",
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
  posicion: {
    width: 56,
    color: tema.colores.texto,
    fontWeight: tema.tipografia.pesos.bold,
  },
  alias: {
    flex: 1,
    color: tema.colores.texto,
    fontWeight: tema.tipografia.pesos.bold,
  },
  puntaje: {
    color: tema.colores.primario,
    fontWeight: tema.tipografia.pesos.extrabold,
  },
});
