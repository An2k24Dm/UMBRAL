import { StyleSheet, Text, View } from "react-native";
import { tema } from "../../estilos/tema";
import type { MisionSesionMovilDto } from "../../tipos/sesiones";
import { TarjetaMisionSesionMovil } from "./TarjetaMisionSesionMovil";

interface Props {
  misiones: MisionSesionMovilDto[];
}

// Lista vertical de misiones del detalle. Mantiene el orden recibido
// del backend (que ya viene ordenado por `orden` ascendente desde el
// manejador).
export function ListaMisionesSesionMovil({ misiones }: Props) {
  if (misiones.length === 0) {
    return (
      <View style={estilos.vacio}>
        <Text style={estilos.vacioTexto}>
          Esta sesión todavía no tiene misiones asociadas.
        </Text>
      </View>
    );
  }

  return (
    <View>
      {misiones.map((mision) => (
        <TarjetaMisionSesionMovil key={mision.id} mision={mision} />
      ))}
    </View>
  );
}

const estilos = StyleSheet.create({
  vacio: {
    backgroundColor: tema.colores.fondoTarjeta,
    borderRadius: tema.radios.tarjeta,
    borderWidth: 1,
    borderColor: tema.colores.bordeTarjeta,
    padding: tema.espacios.lg,
    alignItems: "center",
  },
  vacioTexto: {
    color: tema.colores.textoTenue,
    fontSize: tema.tipografia.tamanos.sm,
    textAlign: "center",
  },
});
