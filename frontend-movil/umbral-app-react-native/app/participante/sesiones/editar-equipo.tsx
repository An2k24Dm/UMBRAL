import { useState } from "react";
import {
  ActivityIndicator,
  StyleSheet,
  Text,
  TextInput,
  TouchableOpacity,
  View,
} from "react-native";
import { useLocalSearchParams, useRouter } from "expo-router";
import RutaProtegidaMovil from "../../../autenticacion/RutaProtegidaMovil";
import { PantallaBase } from "../../../componentes/PantallaBase";
import { tema } from "../../../estilos/tema";
import { useModificarEquipo } from "../../../hooks/useModificarEquipo";
import type { TipoEquipo } from "../../../tipos/equipos";

const LONGITUD_MINIMA_CONTRASENA = 6;
const LONGITUD_MAXIMA_NOMBRE = 80;

// HU41 — Edición de un equipo por su líder. El tipo actual llega por params
// para decidir si la contraseña es obligatoria al pasar de Público a Privado.
export default function PantallaEditarEquipo() {
  return (
    <RutaProtegidaMovil>
      <Contenido />
    </RutaProtegidaMovil>
  );
}

function Contenido() {
  const enrutador = useRouter();
  const parametros = useLocalSearchParams<{
    sesionId?: string;
    equipoId?: string;
    nombre?: string;
    tipo?: string;
  }>();
  const sesionId = parametros.sesionId ?? "";
  const equipoId = parametros.equipoId ?? "";
  const tipoActual: TipoEquipo = parametros.tipo === "Privado" ? "Privado" : "Publico";

  const [nombre, setNombre] = useState(parametros.nombre ?? "");
  const [tipo, setTipo] = useState<TipoEquipo>(tipoActual);
  const [contrasena, setContrasena] = useState("");
  const [errorLocal, setErrorLocal] = useState<string | null>(null);

  const { guardando, error, modificar } = useModificarEquipo(sesionId, equipoId);

  // La contraseña es obligatoria solo si el equipo pasa de Público a Privado.
  const contrasenaObligatoria = tipo === "Privado" && tipoActual === "Publico";

  const volverAlDetalle = () =>
    enrutador.replace(
      `/participante/sesiones/equipo?sesionId=${sesionId}&equipoId=${equipoId}`,
    );

  const validarLocal = (): string | null => {
    const limpio = nombre.trim();
    if (limpio.length === 0) return "El nombre del equipo es obligatorio.";
    if (limpio.length > LONGITUD_MAXIMA_NOMBRE)
      return `El nombre no puede superar ${LONGITUD_MAXIMA_NOMBRE} caracteres.`;
    if (contrasenaObligatoria && contrasena.trim().length === 0)
      return "Debes indicar una contraseña para un equipo privado.";
    if (
      tipo === "Privado" &&
      contrasena.trim().length > 0 &&
      contrasena.trim().length < LONGITUD_MINIMA_CONTRASENA
    )
      return `La contraseña debe tener al menos ${LONGITUD_MINIMA_CONTRASENA} caracteres.`;
    return null;
  };

  const guardar = async () => {
    if (guardando) return; // evita doble submit
    setErrorLocal(null);
    const errorValidacion = validarLocal();
    if (errorValidacion) {
      setErrorLocal(errorValidacion);
      return;
    }
    const ok = await modificar({
      nombre: nombre.trim(),
      tipo,
      // En privado: contraseña solo si se escribió (si no, se conserva).
      contrasena:
        tipo === "Privado" && contrasena.trim().length > 0
          ? contrasena.trim()
          : null,
    });
    if (ok) volverAlDetalle();
  };

  return (
    <PantallaBase>
      <View style={estilos.encabezado}>
        <Text style={estilos.titulo}>Editar equipo</Text>
        <Text style={estilos.subtitulo}>
          Actualiza el nombre, el tipo o la contraseña del equipo.
        </Text>
      </View>

      <View style={estilos.tarjeta}>
        <Text style={estilos.etiqueta}>Nombre del equipo</Text>
        <TextInput
          style={estilos.entrada}
          value={nombre}
          onChangeText={setNombre}
          placeholder="Ej. Los exploradores"
          placeholderTextColor={tema.colores.textoTenue}
          maxLength={LONGITUD_MAXIMA_NOMBRE}
          editable={!guardando}
          accessibilityLabel="Nombre del equipo"
        />

        <Text style={estilos.etiqueta}>Tipo de equipo</Text>
        <View style={estilos.filaTipos}>
          <BotonTipo
            activo={tipo === "Publico"}
            texto="Público"
            onPress={() => setTipo("Publico")}
            deshabilitado={guardando}
          />
          <BotonTipo
            activo={tipo === "Privado"}
            texto="Privado"
            onPress={() => setTipo("Privado")}
            deshabilitado={guardando}
          />
        </View>

        {tipo === "Privado" ? (
          <>
            <Text style={estilos.etiqueta}>Contraseña</Text>
            <TextInput
              style={estilos.entrada}
              value={contrasena}
              onChangeText={setContrasena}
              placeholder={
                contrasenaObligatoria ? "Mínimo 6 caracteres" : "Mínimo 6 caracteres"
              }
              placeholderTextColor={tema.colores.textoTenue}
              secureTextEntry
              editable={!guardando}
              accessibilityLabel="Contraseña del equipo"
            />
            <Text style={estilos.ayuda}>
              {contrasenaObligatoria
                ? "Debes definir una contraseña para hacer el equipo privado."
                : "Deja la contraseña vacía si no deseas cambiarla."}
            </Text>
          </>
        ) : (
          <Text style={estilos.ayuda}>
            El equipo público no requiere contraseña.
          </Text>
        )}

        {(errorLocal || error) && (
          <View style={estilos.cuadroError}>
            <Text style={estilos.cuadroErrorTexto}>{errorLocal ?? error}</Text>
          </View>
        )}

        <TouchableOpacity
          style={[estilos.botonPrimario, guardando && estilos.botonDeshabilitado]}
          onPress={guardar}
          disabled={guardando}
          accessibilityRole="button"
        >
          {guardando ? (
            <ActivityIndicator color={tema.colores.textoBlanco} />
          ) : (
            <Text style={estilos.botonPrimarioTexto}>Guardar cambios</Text>
          )}
        </TouchableOpacity>

        <TouchableOpacity
          style={estilos.botonSecundario}
          onPress={() => enrutador.back()}
          disabled={guardando}
          accessibilityRole="button"
        >
          <Text style={estilos.botonSecundarioTexto}>Cancelar</Text>
        </TouchableOpacity>
      </View>
    </PantallaBase>
  );
}

function BotonTipo({
  activo,
  texto,
  onPress,
  deshabilitado,
}: {
  activo: boolean;
  texto: string;
  onPress: () => void;
  deshabilitado?: boolean;
}) {
  return (
    <TouchableOpacity
      style={[estilos.botonTipo, activo && estilos.botonTipoActivo]}
      onPress={onPress}
      disabled={deshabilitado}
      accessibilityRole="button"
      accessibilityState={{ selected: activo }}
    >
      <Text style={[estilos.botonTipoTexto, activo && estilos.botonTipoTextoActivo]}>
        {texto}
      </Text>
    </TouchableOpacity>
  );
}

const estilos = StyleSheet.create({
  encabezado: { marginBottom: tema.espacios.md, paddingTop: tema.espacios.md },
  titulo: {
    fontSize: tema.tipografia.tamanos.h2,
    fontWeight: tema.tipografia.pesos.extrabold,
    color: tema.colores.texto,
  },
  subtitulo: {
    fontSize: tema.tipografia.tamanos.md,
    color: tema.colores.textoTenue,
    marginTop: tema.espacios.xs,
  },
  tarjeta: {
    backgroundColor: tema.colores.fondoTarjeta,
    borderRadius: tema.radios.tarjeta,
    borderWidth: 1,
    borderColor: tema.colores.bordeTarjeta,
    padding: tema.espacios.lg,
  },
  etiqueta: {
    color: tema.colores.textoTenue,
    fontSize: tema.tipografia.tamanos.xs,
    letterSpacing: tema.tipografia.espaciadoLetra.sm,
    textTransform: "uppercase",
    marginBottom: tema.espacios.xs,
    marginTop: tema.espacios.sm,
  },
  ayuda: {
    color: tema.colores.textoTenue,
    fontSize: tema.tipografia.tamanos.xs,
    marginTop: tema.espacios.xs,
  },
  entrada: {
    backgroundColor: tema.colores.fondo,
    borderWidth: 1,
    borderColor: tema.colores.bordeTarjeta,
    borderRadius: tema.radios.entrada,
    paddingHorizontal: tema.espacios.md,
    paddingVertical: tema.espacios.sm,
    color: tema.colores.texto,
    fontSize: tema.tipografia.tamanos.md,
  },
  filaTipos: { flexDirection: "row", gap: tema.espacios.sm },
  botonTipo: {
    flex: 1,
    paddingVertical: tema.espacios.sm,
    borderRadius: tema.radios.boton,
    borderWidth: 1,
    borderColor: tema.colores.bordeTarjeta,
    alignItems: "center",
    backgroundColor: tema.colores.fondo,
  },
  botonTipoActivo: {
    borderColor: tema.colores.primario,
    backgroundColor: tema.colores.primario,
  },
  botonTipoTexto: {
    color: tema.colores.texto,
    fontWeight: tema.tipografia.pesos.semibold,
    fontSize: tema.tipografia.tamanos.md,
  },
  botonTipoTextoActivo: { color: tema.colores.textoBlanco },
  botonPrimario: {
    backgroundColor: tema.colores.primario,
    paddingVertical: tema.espacios.md,
    borderRadius: tema.radios.boton,
    alignItems: "center",
    marginTop: tema.espacios.md,
  },
  botonDeshabilitado: { opacity: 0.7 },
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
    backgroundColor: tema.colores.fondoTarjeta,
  },
  botonSecundarioTexto: {
    color: tema.colores.texto,
    fontWeight: tema.tipografia.pesos.bold,
    fontSize: tema.tipografia.tamanos.md,
  },
  cuadroError: {
    backgroundColor: tema.colores.errorSuave,
    borderColor: tema.colores.error,
    borderWidth: 1,
    borderRadius: tema.radios.entrada,
    padding: tema.espacios.md,
    marginTop: tema.espacios.md,
  },
  cuadroErrorTexto: {
    color: tema.colores.error,
    fontSize: tema.tipografia.tamanos.sm,
    textAlign: "center",
  },
});
