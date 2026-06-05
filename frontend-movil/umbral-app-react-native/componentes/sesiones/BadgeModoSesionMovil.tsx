import { StyleSheet, Text, View } from "react-native";
import { tema } from "../../estilos/tema";
import type { ModoSesion } from "../../tipos/sesiones";

interface Props {
  modo: ModoSesion;
}

const ETIQUETAS: Record<ModoSesion, string> = {
  Individual: "INDIVIDUAL",
  Grupal: "GRUPAL",
};

// Individual y Grupal tienen los mismos colores: lo distintivo es el
// texto, no el color, para evitar sobrecargar la tarjeta con paletas.
export function BadgeModoSesionMovil({ modo }: Props) {
  return (
    <View style={estilos.badge}>
      <Text style={estilos.texto}>{ETIQUETAS[modo]}</Text>
    </View>
  );
}

const estilos = StyleSheet.create({
  badge: {
    alignSelf: "flex-start",
    paddingHorizontal: tema.espacios.sm,
    paddingVertical: 2,
    borderRadius: tema.radios.pastilla,
    borderWidth: 1,
    borderColor: tema.colores.primario,
    backgroundColor: "rgba(124,92,255,0.18)",
  },
  texto: {
    color: tema.colores.enlace,
    fontSize: tema.tipografia.tamanos.xs,
    fontWeight: tema.tipografia.pesos.bold,
    letterSpacing: tema.tipografia.espaciadoLetra.sm,
  },
});
