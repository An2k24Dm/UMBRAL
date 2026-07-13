import { useCallback, useEffect, useState } from "react";
import {
  ActivityIndicator,
  RefreshControl,
  StyleSheet,
  Text,
  TouchableOpacity,
  View,
} from "react-native";
import { useRouter } from "expo-router";
import { useAutenticacion } from "../../autenticacion/ContextoAutenticacion";
import RutaProtegidaMovil from "../../autenticacion/RutaProtegidaMovil";
import { PantallaBase } from "../../componentes/PantallaBase";
import { tema } from "../../estilos/tema";
import { useRefrescarAlEnfocar } from "../../hooks/useRefrescarAlEnfocar";
import {
  obtenerRankingGlobalApi,
  type EntradaRankingGlobalDto,
} from "../../servicios/rankingApi";

export default function PantallaRankingGlobal() {
  return (
    <RutaProtegidaMovil>
      <ContenidoRanking />
    </RutaProtegidaMovil>
  );
}

function medalla(idx: number): string {
  if (idx === 0) return "🥇";
  if (idx === 1) return "🥈";
  if (idx === 2) return "🥉";
  return `#${idx + 1}`;
}

function ContenidoRanking() {
  const enrutador = useRouter();
  const { sesion: sesionAuth, cerrarSesion } = useAutenticacion();
  const token = sesionAuth?.tokenAcceso ?? null;

  const [entradas, setEntradas] = useState<EntradaRankingGlobalDto[]>([]);
  const [cargando, setCargando] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [sesionExpirada, setSesionExpirada] = useState(false);

  const cargar = useCallback(async () => {
    if (!token) return;
    setCargando(true);
    setError(null);
    try {
      const datos = await obtenerRankingGlobalApi(token, 50);
      setEntradas(datos);
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
        e instanceof Error ? e.message : "No se pudo cargar el ranking global.",
      );
    } finally {
      setCargando(false);
    }
  }, [token]);

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

      {!cargando && !error && entradas.length === 0 && (
        <View style={estilos.cuadroVacio}>
          <Text style={estilos.cuadroVacioTitulo}>Sin datos</Text>
          <Text style={estilos.cuadroVacioTexto}>
            Aún no hay puntajes en el ranking global. Los puntajes se acumulan
            cuando finalizan las sesiones.
          </Text>
        </View>
      )}

      {!cargando && !error && entradas.length > 0 && (
        <View style={estilos.lista}>
          {entradas.map((e, idx) => (
            <View
              key={e.participanteIdentidadId}
              style={[estilos.fila, idx === 0 && estilos.filaOro]}
            >
              <View style={estilos.filaPuesto}>
                <Text
                  style={[estilos.puesto, idx < 3 && estilos.puestoDestacado]}
                >
                  {medalla(idx)}
                </Text>
              </View>
              <View style={estilos.filaDetalle}>
                <Text style={estilos.nombre}>{e.nombreParticipante}</Text>
                <Text style={estilos.desglose}>
                  {e.sesionesJugadas} sesión{e.sesionesJugadas !== 1 ? "es" : ""} · {e.etapasCompletadasTotal} etapas
                </Text>
              </View>
              <View style={estilos.filaPuntaje}>
                <Text style={estilos.puntaje}>{e.puntajeAcumulado}</Text>
                <Text style={estilos.puntajeEtiqueta}>pts</Text>
              </View>
            </View>
          ))}
        </View>
      )}

      <TouchableOpacity
        style={estilos.botonSecundario}
        onPress={() => enrutador.replace("/participante/menu")}
        accessibilityRole="button"
      >
        <Text style={estilos.botonSecundarioTexto}>Volver al menú</Text>
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
    marginVertical: tema.espacios.md,
  },
  cuadroVacioTitulo: {
    color: tema.colores.texto,
    fontSize: tema.tipografia.tamanos.lg,
    fontWeight: tema.tipografia.pesos.bold,
    marginBottom: tema.espacios.sm,
  },
  cuadroVacioTexto: {
    color: tema.colores.textoTenue,
    fontSize: tema.tipografia.tamanos.sm,
    textAlign: "center",
    lineHeight: 20,
  },
  lista: { marginTop: tema.espacios.sm, gap: tema.espacios.xs },
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
  nombre: {
    color: tema.colores.texto,
    fontWeight: tema.tipografia.pesos.bold,
    fontSize: tema.tipografia.tamanos.md,
  },
  desglose: {
    color: tema.colores.textoTenue,
    fontSize: tema.tipografia.tamanos.xs,
    marginTop: 2,
  },
  filaPuntaje: {
    alignItems: "flex-end",
  },
  puntaje: {
    color: tema.colores.primario,
    fontWeight: tema.tipografia.pesos.extrabold,
    fontSize: tema.tipografia.tamanos.lg,
  },
  puntajeEtiqueta: {
    color: tema.colores.textoTenue,
    fontSize: tema.tipografia.tamanos.xs,
  },
  botonPrimario: {
    backgroundColor: tema.colores.primario,
    paddingVertical: tema.espacios.md,
    borderRadius: tema.radios.boton,
    alignItems: "center",
    marginTop: tema.espacios.sm,
  },
  botonPrimarioTexto: {
    color: tema.colores.textoBlanco,
    fontWeight: tema.tipografia.pesos.bold,
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
