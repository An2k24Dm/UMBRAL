import { StyleSheet, Text, TextInput, TouchableOpacity, View } from "react-native";
import { tema } from "../../estilos/tema";
import type { FiltroModoSesion } from "../../tipos/sesiones";

interface Props {
  busqueda: string;
  alCambiarBusqueda: (valor: string) => void;
  modo: FiltroModoSesion;
  alCambiarModo: (modo: FiltroModoSesion) => void;
}

// Cabecera de filtros del listado de sesiones. El cambio de búsqueda
// es controlado por el padre para que el hook de listado pueda
// refrescar cuando el filtro cambia.
const OPCIONES_MODO: FiltroModoSesion[] = ["Todas", "Individual", "Grupal"];

export function FiltrosSesionesMovil({
  busqueda,
  alCambiarBusqueda,
  modo,
  alCambiarModo,
}: Props) {
  return (
    <View style={estilos.contenedor}>
      <Text style={estilos.etiqueta}>BUSCAR POR NOMBRE</Text>
      <TextInput
        value={busqueda}
        onChangeText={alCambiarBusqueda}
        placeholder="Ej: Misión piloto"
        placeholderTextColor={tema.colores.textoTenue}
        style={estilos.entrada}
        autoCapitalize="none"
        autoCorrect={false}
        accessibilityLabel="Buscar sesiones por nombre"
      />

      <Text style={[estilos.etiqueta, estilos.etiquetaModo]}>MODO</Text>
      <View style={estilos.pildoras}>
        {OPCIONES_MODO.map((opcion) => {
          const seleccionada = opcion === modo;
          return (
            <TouchableOpacity
              key={opcion}
              onPress={() => alCambiarModo(opcion)}
              activeOpacity={0.75}
              accessibilityRole="button"
              accessibilityState={{ selected: seleccionada }}
              style={[
                estilos.pildora,
                seleccionada && estilos.pildoraActiva,
              ]}
            >
              <Text
                style={[
                  estilos.pildoraTexto,
                  seleccionada && estilos.pildoraTextoActivo,
                ]}
              >
                {opcion}
              </Text>
            </TouchableOpacity>
          );
        })}
      </View>
    </View>
  );
}

const estilos = StyleSheet.create({
  contenedor: { marginBottom: tema.espacios.md },
  etiqueta: {
    color: tema.colores.textoTenue,
    fontSize: tema.tipografia.tamanos.xs,
    fontWeight: tema.tipografia.pesos.semibold,
    letterSpacing: tema.tipografia.espaciadoLetra.xs,
    textTransform: "uppercase",
    marginBottom: tema.espacios.xs,
  },
  etiquetaModo: { marginTop: tema.espacios.md },
  entrada: {
    backgroundColor: tema.colores.entradaFondo,
    borderWidth: 1,
    borderColor: tema.colores.entradaBorde,
    borderRadius: tema.radios.entrada,
    paddingHorizontal: tema.espacios.md,
    paddingVertical: tema.espacios.md,
    color: tema.colores.texto,
    fontSize: tema.tipografia.tamanos.md,
  },
  pildoras: {
    flexDirection: "row",
    gap: tema.espacios.sm,
  },
  pildora: {
    paddingHorizontal: tema.espacios.md,
    paddingVertical: tema.espacios.sm,
    borderRadius: tema.radios.pastilla,
    borderWidth: 1,
    borderColor: tema.colores.bordeTarjeta,
    backgroundColor: tema.colores.fondoTarjeta,
  },
  pildoraActiva: {
    borderColor: tema.colores.primario,
    backgroundColor: "rgba(124,92,255,0.18)",
  },
  pildoraTexto: {
    color: tema.colores.textoTenue,
    fontSize: tema.tipografia.tamanos.sm,
    fontWeight: tema.tipografia.pesos.semibold,
  },
  pildoraTextoActivo: {
    color: tema.colores.texto,
    fontWeight: tema.tipografia.pesos.bold,
  },
});
