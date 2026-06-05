import { StyleSheet, Text, View } from "react-native";
import { tema } from "../../estilos/tema";
import type { EstadoSesion } from "../../tipos/sesiones";

interface Props {
  estado: EstadoSesion;
}

// Mapa de color por estado. Mantiene la paleta del tema y reusa
// alpha-backgrounds existentes para que el badge se lea sobre la
// tarjeta sin necesidad de definir colores nuevos.
const COLORES_POR_ESTADO: Record<
  EstadoSesion,
  { texto: string; fondo: string; borde: string }
> = {
  Programada: {
    texto: tema.colores.info,
    fondo: tema.colores.infoSuave,
    borde: tema.colores.info,
  },
  EnPreparacion: {
    texto: tema.colores.aviso,
    fondo: tema.colores.avisoSuave,
    borde: tema.colores.aviso,
  },
  Activa: {
    texto: tema.colores.exito,
    fondo: tema.colores.exitoSuave,
    borde: tema.colores.exito,
  },
  // Estados que el listado no devuelve, pero que el detalle podría
  // mostrar si la sesión cambia mientras se consulta:
  Pausada: {
    texto: tema.colores.textoTenue,
    fondo: "rgba(154,160,192,0.12)",
    borde: tema.colores.textoTenue,
  },
  Finalizada: {
    texto: tema.colores.textoTenue,
    fondo: "rgba(154,160,192,0.12)",
    borde: tema.colores.textoTenue,
  },
  Cancelada: {
    texto: tema.colores.error,
    fondo: tema.colores.errorSuave,
    borde: tema.colores.error,
  },
};

const ETIQUETAS: Record<EstadoSesion, string> = {
  Programada: "PROGRAMADA",
  EnPreparacion: "EN PREPARACIÓN",
  Activa: "ACTIVA",
  Pausada: "PAUSADA",
  Finalizada: "FINALIZADA",
  Cancelada: "CANCELADA",
};

export function BadgeEstadoSesionMovil({ estado }: Props) {
  const colores = COLORES_POR_ESTADO[estado];
  return (
    <View
      style={[
        estilos.badge,
        { backgroundColor: colores.fondo, borderColor: colores.borde },
      ]}
    >
      <Text style={[estilos.texto, { color: colores.texto }]}>
        {ETIQUETAS[estado]}
      </Text>
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
  },
  texto: {
    fontSize: tema.tipografia.tamanos.xs,
    fontWeight: tema.tipografia.pesos.bold,
    letterSpacing: tema.tipografia.espaciadoLetra.sm,
  },
});
