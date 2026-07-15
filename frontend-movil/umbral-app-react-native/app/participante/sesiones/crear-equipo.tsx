import { useMemo, useState } from "react";
import {
  ActivityIndicator,
  Alert,
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
import { useCrearEquipo } from "../../../hooks/useCrearEquipo";
import type { TipoEquipo } from "../../../tipos/equipos";

const LONGITUD_MINIMA_CONTRASENA = 6;
const LONGITUD_MAXIMA_NOMBRE = 80;

// HU40 — Pantalla independiente para crear un equipo. Sustituye al formulario
// que antes se mostraba dentro del detalle de la sesión.
export default function PantallaCrearEquipo() {
  return (
    <RutaProtegidaMovil>
      <Contenido />
    </RutaProtegidaMovil>
  );
}

// Traduce el mensaje del backend a un texto claro y detecta si el conflicto
// se debe a que el participante ya pertenece a un equipo de la sesión.
function interpretarError(mensaje: string): { texto: string; yaPertenece: boolean } {
  const m = mensaje.toLowerCase();
  // Regla de participación única: ya está en otra sesión activa.
  if (m.includes("participando en otra"))
    return {
      texto:
        "Ya estás participando en otra sesión. Debes esperar a que finalice " +
        "o sea cancelada para ingresar a una nueva.",
      yaPertenece: true,
    };
  if (m.includes("ya forma parte") || m.includes("pertenec"))
    return { texto: "Ya perteneces a un equipo en esta sesión.", yaPertenece: true };
  if (m.includes("preparaci"))
    return {
      texto: "Solo puedes crear equipos mientras la sesión está en preparación.",
      yaPertenece: false,
    };
  if (m.includes("grupal"))
    return { texto: "Solo puedes crear equipos en sesiones grupales.", yaPertenece: false };
  if (m.includes("ya existe un equipo"))
    return { texto: "Ya existe un equipo con ese nombre en esta sesión.", yaPertenece: false };
  return { texto: mensaje, yaPertenece: false };
}

function Contenido() {
  const enrutador = useRouter();
  const parametros = useLocalSearchParams<{ sesionId?: string; nombre?: string }>();
  const sesionId = parametros.sesionId ?? "";
  const nombreSesion = parametros.nombre ?? "";

  const [nombre, setNombre] = useState("");
  const [tipo, setTipo] = useState<TipoEquipo>("Publico");
  const [contrasena, setContrasena] = useState("");
  const [errorLocal, setErrorLocal] = useState<string | null>(null);

  const { creando, error, crear } = useCrearEquipo(sesionId);

  const errorBackend = useMemo(
    () => (error ? interpretarError(error) : null),
    [error],
  );

  const volverADetalle = () =>
    enrutador.replace(`/participante/sesiones/${sesionId}`);

  const validarLocal = (): string | null => {
    const nombreLimpio = nombre.trim();
    if (nombreLimpio.length === 0) return "El nombre del equipo es obligatorio.";
    if (nombreLimpio.length > LONGITUD_MAXIMA_NOMBRE)
      return `El nombre no puede superar ${LONGITUD_MAXIMA_NOMBRE} caracteres.`;
    if (tipo === "Privado" && contrasena.trim().length < LONGITUD_MINIMA_CONTRASENA)
      return `La contraseña debe tener al menos ${LONGITUD_MINIMA_CONTRASENA} caracteres.`;
    return null;
  };

  const enviar = async () => {
    if (creando) return; // evita doble submit
    setErrorLocal(null);
    const errorValidacion = validarLocal();
    if (errorValidacion) {
      setErrorLocal(errorValidacion);
      return;
    }
    const creado = await crear({
      nombre: nombre.trim(),
      tipo,
      contrasena: tipo === "Privado" ? contrasena.trim() : null,
    });
    if (creado) {
      // Mutación exitosa: reemplazamos el formulario por el detalle del equipo
      // creado (no queda en el historial, no se vuelve al detalle viejo).
      Alert.alert("Equipo creado", "Equipo creado correctamente.");
      enrutador.replace(
        `/participante/sesiones/equipo?sesionId=${sesionId}` +
          `&equipoId=${creado.id}`,
      );
    }
  };

  // Conflicto: ya pertenece a un equipo. No reabrimos el formulario.
  if (errorBackend?.yaPertenece) {
    return (
      <PantallaBase>
        <View style={estilos.tarjeta}>
          <View style={estilos.cuadroError}>
            <Text style={estilos.cuadroErrorTexto}>{errorBackend.texto}</Text>
          </View>
          <TouchableOpacity
            style={estilos.botonPrimario}
            onPress={volverADetalle}
            accessibilityRole="button"
          >
            <Text style={estilos.botonPrimarioTexto}>Volver a la sesión</Text>
          </TouchableOpacity>
        </View>
      </PantallaBase>
    );
  }

  return (
    <PantallaBase>
      <View style={estilos.encabezado}>
        <Text style={estilos.titulo}>Crear equipo</Text>
        <Text style={estilos.subtitulo}>
          Crea un equipo para participar en esta sesión grupal.
        </Text>
        {nombreSesion ? <Text style={estilos.nombreSesion}>{nombreSesion}</Text> : null}
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
          editable={!creando}
          accessibilityLabel="Nombre del equipo"
        />

        <Text style={estilos.etiqueta}>Tipo de equipo</Text>
        <View style={estilos.filaTipos}>
          <BotonTipo
            activo={tipo === "Publico"}
            texto="Público"
            onPress={() => setTipo("Publico")}
            deshabilitado={creando}
          />
          <BotonTipo
            activo={tipo === "Privado"}
            texto="Privado"
            onPress={() => setTipo("Privado")}
            deshabilitado={creando}
          />
        </View>

        {tipo === "Privado" && (
          <>
            <Text style={estilos.etiqueta}>Contraseña</Text>
            <TextInput
              style={estilos.entrada}
              value={contrasena}
              onChangeText={setContrasena}
              placeholder="Mínimo 6 caracteres"
              placeholderTextColor={tema.colores.textoTenue}
              secureTextEntry
              editable={!creando}
              accessibilityLabel="Contraseña del equipo"
            />
          </>
        )}

        {(errorLocal || errorBackend) && (
          <View style={estilos.cuadroError}>
            <Text style={estilos.cuadroErrorTexto}>
              {errorLocal ?? errorBackend?.texto}
            </Text>
          </View>
        )}

        <TouchableOpacity
          style={[estilos.botonPrimario, creando && estilos.botonDeshabilitado]}
          onPress={enviar}
          disabled={creando}
          accessibilityRole="button"
        >
          {creando ? (
            <ActivityIndicator color={tema.colores.textoBlanco} />
          ) : (
            <Text style={estilos.botonPrimarioTexto}>Crear equipo</Text>
          )}
        </TouchableOpacity>

        <TouchableOpacity
          style={estilos.botonSecundario}
          onPress={() => enrutador.back()}
          disabled={creando}
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
  encabezado: {
    marginBottom: tema.espacios.md,
    paddingTop: tema.espacios.md,
  },
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
  nombreSesion: {
    fontSize: tema.tipografia.tamanos.sm,
    color: tema.colores.primario,
    marginTop: tema.espacios.sm,
    fontWeight: tema.tipografia.pesos.semibold,
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
  filaTipos: {
    flexDirection: "row",
    gap: tema.espacios.sm,
  },
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
  botonTipoTextoActivo: {
    color: tema.colores.textoBlanco,
  },
  botonPrimario: {
    backgroundColor: tema.colores.primario,
    paddingVertical: tema.espacios.md,
    borderRadius: tema.radios.boton,
    alignItems: "center",
    marginTop: tema.espacios.md,
  },
  botonDeshabilitado: {
    opacity: 0.7,
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
  cuadroExito: {
    backgroundColor: tema.colores.fondo,
    borderColor: tema.colores.primario,
    borderWidth: 1,
    borderRadius: tema.radios.entrada,
    padding: tema.espacios.md,
    marginBottom: tema.espacios.md,
  },
  cuadroExitoTexto: {
    color: tema.colores.primario,
    fontSize: tema.tipografia.tamanos.md,
    fontWeight: tema.tipografia.pesos.bold,
    textAlign: "center",
  },
  bloqueMeta: {
    marginTop: tema.espacios.sm,
  },
  metaEtiqueta: {
    color: tema.colores.textoTenue,
    fontSize: tema.tipografia.tamanos.xs,
    letterSpacing: tema.tipografia.espaciadoLetra.sm,
  },
  metaValor: {
    color: tema.colores.texto,
    fontSize: tema.tipografia.tamanos.md,
    fontWeight: tema.tipografia.pesos.semibold,
    marginTop: 2,
  },
});
