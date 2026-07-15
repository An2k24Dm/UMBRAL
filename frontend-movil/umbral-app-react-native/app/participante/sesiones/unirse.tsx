import { StyleSheet, Text, TouchableOpacity, View } from "react-native";
import { useLocalSearchParams, useRouter } from "expo-router";
import RutaProtegidaMovil from "../../../autenticacion/RutaProtegidaMovil";
import { PantallaBase } from "../../../componentes/PantallaBase";
import { tema } from "../../../estilos/tema";
import { useNavegacionSegura } from "../../../hooks/useNavegacionSegura";

// HU40/HU43 — Pantalla de opciones al pulsar "Unirse" en una sesión grupal.
// Ofrece "Unirse a un equipo" (lista equipos, HU43) y "Crear equipo" (HU40).
export default function PantallaOpcionesUnirse() {
  return (
    <RutaProtegidaMovil>
      <Contenido />
    </RutaProtegidaMovil>
  );
}

function Contenido() {
  const enrutador = useRouter();
  const parametros = useLocalSearchParams<{ sesionId?: string; nombre?: string }>();
  const sesionId = parametros.sesionId ?? "";
  const nombre = parametros.nombre ?? "";
  const navegarSeguro = useNavegacionSegura();

  const irACrearEquipo = () =>
    navegarSeguro(() =>
      enrutador.push(
        `/participante/sesiones/crear-equipo?sesionId=${sesionId}` +
          `&nombre=${encodeURIComponent(nombre)}`,
      ),
    );

  const unirseAEquipoExistente = () =>
    // HU43: abre el listado de equipos. El ingreso real es HU47.
    navegarSeguro(() =>
      enrutador.push(
        `/participante/sesiones/equipos?sesionId=${sesionId}` +
          `&nombre=${encodeURIComponent(nombre)}`,
      ),
    );

  return (
    <PantallaBase>
      <View style={estilos.encabezado}>
        <Text style={estilos.titulo}>Unirse a sesión grupal</Text>
        <Text style={estilos.subtitulo}>
          Elige cómo quieres participar en esta sesión.
        </Text>
        {nombre ? <Text style={estilos.nombreSesion}>{nombre}</Text> : null}
      </View>

      <TouchableOpacity
        style={estilos.opcion}
        onPress={unirseAEquipoExistente}
        accessibilityRole="button"
      >
        <Text style={estilos.opcionTitulo}>Unirse a un equipo</Text>
        <Text style={estilos.opcionTexto}>
          Ingresa a un equipo creado por otro participante.
        </Text>
      </TouchableOpacity>

      <TouchableOpacity
        style={[estilos.opcion, estilos.opcionDestacada]}
        onPress={irACrearEquipo}
        accessibilityRole="button"
      >
        <Text style={[estilos.opcionTitulo, estilos.opcionTituloDestacado]}>
          Crear equipo
        </Text>
        <Text style={[estilos.opcionTexto, estilos.opcionTextoDestacado]}>
          Crea tu propio equipo y conviértete en su líder.
        </Text>
      </TouchableOpacity>

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
  opcion: {
    backgroundColor: tema.colores.fondoTarjeta,
    borderRadius: tema.radios.tarjeta,
    borderWidth: 1,
    borderColor: tema.colores.bordeTarjeta,
    padding: tema.espacios.lg,
    marginBottom: tema.espacios.md,
  },
  opcionDestacada: {
    borderColor: tema.colores.primario,
  },
  opcionTitulo: {
    color: tema.colores.texto,
    fontSize: tema.tipografia.tamanos.lg,
    fontWeight: tema.tipografia.pesos.bold,
  },
  opcionTituloDestacado: {
    color: tema.colores.primario,
  },
  opcionTexto: {
    color: tema.colores.textoTenue,
    fontSize: tema.tipografia.tamanos.sm,
    marginTop: tema.espacios.xs,
  },
  opcionTextoDestacado: {
    color: tema.colores.textoTenue,
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
