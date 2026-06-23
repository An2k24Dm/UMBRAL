import { StyleSheet, Text, TouchableOpacity, View } from "react-native";
import { useLocalSearchParams, useRouter } from "expo-router";
import RutaProtegidaMovil from "../../../autenticacion/RutaProtegidaMovil";
import { PantallaBase } from "../../../componentes/PantallaBase";
import { tema } from "../../../estilos/tema";

// HU40 — Placeholder de "Ver equipo". El detalle real del equipo se
// implementará en la HU43 (consultar equipos de una sesión).
export default function PantallaDetalleEquipoPlaceholder() {
  return (
    <RutaProtegidaMovil>
      <Contenido />
    </RutaProtegidaMovil>
  );
}

function Contenido() {
  const enrutador = useRouter();
  const parametros = useLocalSearchParams<{ equipoNombre?: string }>();
  const equipoNombre = parametros.equipoNombre ?? "";

  return (
    <PantallaBase>
      <View style={estilos.encabezado}>
        <Text style={estilos.titulo}>Detalle de equipo</Text>
        {equipoNombre ? <Text style={estilos.nombre}>{equipoNombre}</Text> : null}
      </View>

      <View style={estilos.tarjeta}>
        <Text style={estilos.texto}>
          El detalle del equipo se implementará en la HU43.
        </Text>
      </View>

      <TouchableOpacity
        style={estilos.botonSecundario}
        onPress={() => enrutador.back()}
        accessibilityRole="button"
      >
        <Text style={estilos.botonSecundarioTexto}>Volver</Text>
      </TouchableOpacity>
    </PantallaBase>
  );
}

const estilos = StyleSheet.create({
  encabezado: {
    marginBottom: tema.espacios.lg,
    paddingTop: tema.espacios.md,
  },
  titulo: {
    fontSize: tema.tipografia.tamanos.h2,
    fontWeight: tema.tipografia.pesos.extrabold,
    color: tema.colores.texto,
  },
  nombre: {
    fontSize: tema.tipografia.tamanos.md,
    color: tema.colores.primario,
    marginTop: tema.espacios.xs,
    fontWeight: tema.tipografia.pesos.semibold,
  },
  tarjeta: {
    backgroundColor: tema.colores.fondoTarjeta,
    borderRadius: tema.radios.tarjeta,
    borderWidth: 1,
    borderColor: tema.colores.bordeTarjeta,
    padding: tema.espacios.lg,
  },
  texto: {
    color: tema.colores.textoTenue,
    fontSize: tema.tipografia.tamanos.md,
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
});
