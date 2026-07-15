import { useEffect, useState } from "react";
import { StyleSheet, Text, View } from "react-native";
import { useSafeAreaInsets } from "react-native-safe-area-context";
import { useAvisosSesionTiempoReal } from "../hooks/useAvisosSesionTiempoReal";
import { tema } from "../estilos/tema";

// Banner global superior de preparación entre etapas/misiones. Reutiliza la
// conexión SignalR global (useAvisosSesionTiempoReal); NO abre otra conexión.
export default function AvisosTiempoRealParticipante() {
  const { bannerEtapaPorComenzar } = useAvisosSesionTiempoReal();
  const insets = useSafeAreaInsets();

  // Reloj visual: se recalcula contra fechaInicioProgramadaUtc (fuente de verdad
  // del backend), no un contador local. Tick cada 250 ms para una barra fluida.
  const [ahoraMs, setAhoraMs] = useState(() => Date.now());
  useEffect(() => {
    if (!bannerEtapaPorComenzar) return;
    setAhoraMs(Date.now());
    const id = setInterval(() => setAhoraMs(Date.now()), 250);
    return () => clearInterval(id);
  }, [bannerEtapaPorComenzar]);

  if (!bannerEtapaPorComenzar) return null;

  const objetivoMs = Date.parse(bannerEtapaPorComenzar.fechaInicioProgramadaUtc);
  const totalMs = Math.max(1, bannerEtapaPorComenzar.duracionPreparacionSegundos * 1000);
  const restanteMs = Number.isNaN(objetivoMs) ? 0 : Math.max(0, objetivoMs - ahoraMs);
  const porcentaje = Math.min(1, Math.max(0, restanteMs / totalMs));
  const segundos = Math.ceil(restanteMs / 1000);

  return (
    <View style={[estilos.contenedor, { paddingTop: insets.top + 8 }]} pointerEvents="none">
      <View style={estilos.tarjeta}>
        <Text style={estilos.titulo}>{bannerEtapaPorComenzar.mensaje}</Text>
        <Text style={estilos.subtexto}>
          {restanteMs > 0
            ? `Prepárate para continuar · ${segundos} s`
            : "Iniciando etapa…"}
        </Text>
        <View style={estilos.barraFondo}>
          <View style={[estilos.barraProgreso, { width: `${porcentaje * 100}%` }]} />
        </View>
      </View>
    </View>
  );
}

const estilos = StyleSheet.create({
  contenedor: {
    position: "absolute",
    top: 0,
    left: 0,
    right: 0,
    paddingHorizontal: 12,
    zIndex: 1000,
    elevation: 1000,
  },
  tarjeta: {
    backgroundColor: tema.colores.primario,
    borderRadius: tema.radios.tarjeta,
    paddingVertical: 12,
    paddingHorizontal: 16,
    shadowColor: "#000",
    shadowOpacity: 0.2,
    shadowRadius: 8,
    shadowOffset: { width: 0, height: 2 },
  },
  titulo: {
    color: "#ffffff",
    fontSize: 15,
    fontWeight: "800",
    textAlign: "center",
  },
  subtexto: {
    color: "#ffffff",
    fontSize: 13,
    opacity: 0.9,
    textAlign: "center",
    marginTop: 2,
    marginBottom: 8,
  },
  barraFondo: {
    height: 6,
    borderRadius: 3,
    backgroundColor: "rgba(255,255,255,0.3)",
    overflow: "hidden",
  },
  barraProgreso: {
    height: "100%",
    borderRadius: 3,
    backgroundColor: "#ffffff",
  },
});
