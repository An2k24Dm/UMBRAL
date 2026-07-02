import { useEffect, useState } from "react";
import {
  ActivityIndicator,
  Modal,
  StyleSheet,
  Text,
  TextInput,
  TouchableOpacity,
  View,
} from "react-native";
import { tema } from "../../estilos/tema";

interface Props {
  visible: boolean;
  nombreEquipo: string;
  procesando: boolean;
  error: string | null;
  onIngresar: (contrasena: string) => void;
  onCancelar: () => void;
}

// HU47 — Modal para ingresar la contraseña de un equipo privado. Reutilizado
// por el listado de equipos y el detalle de equipo. El llamador controla la
// visibilidad, el estado de carga y el error (que se muestra sin cerrar).
export function ModalContrasenaEquipo({
  visible,
  nombreEquipo,
  procesando,
  error,
  onIngresar,
  onCancelar,
}: Props) {
  const [contrasena, setContrasena] = useState("");
  const [errorLocal, setErrorLocal] = useState<string | null>(null);

  // Limpia el campo cada vez que se abre para otro intento/equipo.
  useEffect(() => {
    if (visible) {
      setContrasena("");
      setErrorLocal(null);
    }
  }, [visible]);

  const confirmar = () => {
    const limpia = contrasena.trim();
    if (!limpia) {
      setErrorLocal("Debes ingresar la contraseña del equipo.");
      return;
    }
    setErrorLocal(null);
    onIngresar(limpia);
  };

  const mensajeError = errorLocal ?? error;

  return (
    <Modal
      visible={visible}
      transparent
      animationType="fade"
      onRequestClose={() => !procesando && onCancelar()}
    >
      <View style={estilos.modalFondo}>
        <View style={estilos.modalTarjeta}>
          <Text style={estilos.modalTitulo}>Equipo privado</Text>
          <Text style={estilos.modalMensaje}>
            Ingresa la contraseña del equipo “{nombreEquipo}” para unirte.
          </Text>

          <TextInput
            style={estilos.entrada}
            placeholder="Contraseña del equipo"
            placeholderTextColor={tema.colores.textoTenue}
            secureTextEntry
            autoCapitalize="none"
            autoCorrect={false}
            value={contrasena}
            onChangeText={setContrasena}
            editable={!procesando}
          />

          {mensajeError ? (
            <View style={estilos.cuadroError}>
              <Text style={estilos.cuadroErrorTexto}>{mensajeError}</Text>
            </View>
          ) : null}

          <TouchableOpacity
            style={[estilos.botonPrimario, procesando && estilos.botonDeshabilitado]}
            onPress={confirmar}
            disabled={procesando}
            accessibilityRole="button"
          >
            {procesando ? (
              <ActivityIndicator color={tema.colores.textoBlanco} />
            ) : (
              <Text style={estilos.botonPrimarioTexto}>Ingresar</Text>
            )}
          </TouchableOpacity>
          <TouchableOpacity
            style={estilos.botonSecundario}
            onPress={onCancelar}
            disabled={procesando}
            accessibilityRole="button"
          >
            <Text style={estilos.botonSecundarioTexto}>Cancelar</Text>
          </TouchableOpacity>
        </View>
      </View>
    </Modal>
  );
}

const estilos = StyleSheet.create({
  modalFondo: {
    flex: 1,
    backgroundColor: "rgba(0,0,0,0.6)",
    justifyContent: "center",
    padding: tema.espacios.lg,
  },
  modalTarjeta: {
    backgroundColor: tema.colores.fondoTarjeta,
    borderRadius: tema.radios.tarjeta,
    borderWidth: 1,
    borderColor: tema.colores.bordeTarjeta,
    padding: tema.espacios.lg,
  },
  modalTitulo: {
    color: tema.colores.texto,
    fontSize: tema.tipografia.tamanos.h3,
    fontWeight: tema.tipografia.pesos.extrabold,
    marginBottom: tema.espacios.sm,
  },
  modalMensaje: {
    color: tema.colores.textoTenue,
    fontSize: tema.tipografia.tamanos.md,
    lineHeight: 20,
    marginBottom: tema.espacios.md,
  },
  entrada: {
    borderWidth: 1,
    borderColor: tema.colores.bordeTarjeta,
    borderRadius: tema.radios.entrada,
    color: tema.colores.texto,
    paddingHorizontal: tema.espacios.md,
    paddingVertical: tema.espacios.sm,
    fontSize: tema.tipografia.tamanos.md,
    marginBottom: tema.espacios.sm,
  },
  cuadroError: {
    backgroundColor: tema.colores.errorSuave,
    borderColor: tema.colores.error,
    borderWidth: 1,
    borderRadius: tema.radios.entrada,
    padding: tema.espacios.md,
    marginBottom: tema.espacios.sm,
  },
  cuadroErrorTexto: {
    color: tema.colores.error,
    fontSize: tema.tipografia.tamanos.sm,
    textAlign: "center",
  },
  botonPrimario: {
    backgroundColor: tema.colores.primario,
    paddingVertical: tema.espacios.md,
    borderRadius: tema.radios.boton,
    alignItems: "center",
    marginTop: tema.espacios.xs,
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
    marginTop: tema.espacios.sm,
    borderWidth: 1,
    borderColor: tema.colores.bordeTarjeta,
  },
  botonSecundarioTexto: {
    color: tema.colores.texto,
    fontWeight: tema.tipografia.pesos.bold,
    fontSize: tema.tipografia.tamanos.md,
  },
  botonDeshabilitado: { opacity: 0.7 },
});
