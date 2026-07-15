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
import { useAutenticacion } from "../../../autenticacion/ContextoAutenticacion";
import RutaProtegidaMovil from "../../../autenticacion/RutaProtegidaMovil";
import { PantallaBase } from "../../../componentes/PantallaBase";
import { tema } from "../../../estilos/tema";
import { useRefrescarAlEnfocar } from "../../../hooks/useRefrescarAlEnfocar";
import { obtenerMisParticipacionesApi } from "../../../servicios/sesionesApi";
import type { MiParticipacionDto } from "../../../tipos/sesiones";
import { formatearFechaHora } from "../../../utilidades/formatoFechas";

export default function PantallaSesionesFinalizadas() {
  return (
    <RutaProtegidaMovil>
      <ContenidoFinalizadas />
    </RutaProtegidaMovil>
  );
}

function ContenidoFinalizadas() {
  const enrutador = useRouter();
  const { sesion: sesionAuth, cerrarSesion } = useAutenticacion();
  const token = sesionAuth?.tokenAcceso ?? null;

  const [participaciones, setParticipaciones] = useState<MiParticipacionDto[]>([]);
  const [cargando, setCargando] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [sesionExpirada, setSesionExpirada] = useState(false);

  const cargar = useCallback(async () => {
    if (!token) return;
    setCargando(true);
    setError(null);
    try {
      const datos = await obtenerMisParticipacionesApi(token);
      setParticipaciones(datos);
    } catch (e: unknown) {
      if (e && typeof e === "object" && "estadoHttp" in e && (e as { estadoHttp: number }).estadoHttp === 401) {
        setSesionExpirada(true);
        return;
      }
      setError(e instanceof Error ? e.message : "No se pudieron cargar tus sesiones.");
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
        <Text style={estilos.subtitulo}>Mis sesiones finalizadas</Text>
      </View>

      {cargando && (
        <View style={estilos.contenedorEstado}>
          <ActivityIndicator color={tema.colores.primario} size="large" />
          <Text style={estilos.textoEstado}>Cargando historial…</Text>
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

      {!cargando && !error && participaciones.length === 0 && (
        <View style={estilos.cuadroVacio}>
          <Text style={estilos.cuadroVacioTitulo}>Sin historial</Text>
          <Text style={estilos.cuadroVacioTexto}>
            Aún no participaste en ninguna sesión finalizada.
          </Text>
        </View>
      )}

      {!cargando && !error && participaciones.length > 0 && (
        <View style={estilos.lista}>
          {participaciones.map((p) => (
            <TouchableOpacity
              key={p.sesionId}
              style={estilos.tarjeta}
              onPress={() =>
                enrutador.push({
                  pathname: "/participante/historial/[id]",
                  params: {
                    id: p.sesionId,
                    nombre: p.nombreSesion,
                    modo: p.modo,
                    fechaFin: p.fechaFinalizacionUtc ?? "",
                  },
                })
              }
              accessibilityRole="button"
            >
              <View style={estilos.tarjetaCabecera}>
                <Text style={estilos.tarjetaNombre} numberOfLines={2}>{p.nombreSesion}</Text>
                <View style={estilos.badgeModo}>
                  <Text style={estilos.badgeModoTexto}>{p.modo}</Text>
                </View>
              </View>

              {p.fechaInicioUtc && (
                <Text style={estilos.tarjetaFecha}>
                  Inicio: {formatearFechaHora(p.fechaInicioUtc)}
                </Text>
              )}
              {p.fechaFinalizacionUtc && (
                <Text style={estilos.tarjetaFecha}>
                  Finalizada: {formatearFechaHora(p.fechaFinalizacionUtc)}
                </Text>
              )}

              <View style={estilos.filaDetalle}>
                <Text style={estilos.textoDetalle}>Ver detalle y ranking</Text>
                <Text style={estilos.flecha}>›</Text>
              </View>
            </TouchableOpacity>
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
  lista: { marginTop: tema.espacios.sm, gap: tema.espacios.sm },
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
  tarjeta: {
    backgroundColor: tema.colores.fondoTarjeta,
    borderRadius: tema.radios.tarjeta,
    borderWidth: 1,
    borderColor: tema.colores.bordeTarjeta,
    padding: tema.espacios.md,
  },
  tarjetaCabecera: {
    flexDirection: "row",
    justifyContent: "space-between",
    alignItems: "flex-start",
    marginBottom: tema.espacios.xs,
  },
  tarjetaNombre: {
    color: tema.colores.texto,
    fontSize: tema.tipografia.tamanos.md,
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
  tarjetaFecha: {
    color: tema.colores.textoTenue,
    fontSize: tema.tipografia.tamanos.sm,
    marginBottom: tema.espacios.sm,
  },
  filaPuntaje: {
    flexDirection: "row",
    justifyContent: "space-between",
    alignItems: "center",
    borderTopWidth: 1,
    borderTopColor: tema.colores.bordeTarjeta,
    paddingTop: tema.espacios.sm,
    marginTop: tema.espacios.xs,
  },
  puntajeEtiqueta: {
    color: tema.colores.textoTenue,
    fontSize: tema.tipografia.tamanos.sm,
    textTransform: "uppercase",
    letterSpacing: tema.tipografia.espaciadoLetra.sm,
  },
  puntajeValor: {
    color: tema.colores.exito,
    fontSize: tema.tipografia.tamanos.lg,
    fontWeight: tema.tipografia.pesos.extrabold,
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
  filaDetalle: {
    flexDirection: "row",
    justifyContent: "space-between",
    alignItems: "center",
    marginTop: tema.espacios.sm,
    borderTopWidth: 1,
    borderTopColor: tema.colores.bordeTarjeta,
    paddingTop: tema.espacios.xs,
  },
  textoDetalle: {
    color: tema.colores.primario,
    fontSize: tema.tipografia.tamanos.sm,
    fontWeight: tema.tipografia.pesos.bold,
  },
  flecha: {
    color: tema.colores.primario,
    fontSize: 20,
    lineHeight: 22,
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
