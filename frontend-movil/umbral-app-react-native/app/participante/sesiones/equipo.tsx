import { useEffect } from "react";
import {
  ActivityIndicator,
  Alert,
  ScrollView,
  StyleSheet,
  Text,
  TouchableOpacity,
  View,
} from "react-native";
import { useLocalSearchParams, useRouter } from "expo-router";
import { useAutenticacion } from "../../../autenticacion/ContextoAutenticacion";
import RutaProtegidaMovil from "../../../autenticacion/RutaProtegidaMovil";
import { PantallaBase } from "../../../componentes/PantallaBase";
import { tema } from "../../../estilos/tema";
import { useDetalleEquipoSesion } from "../../../hooks/useDetalleEquipoSesion";
import type { IntegranteEquipo } from "../../../tipos/equipos";
import { formatearFechaHora } from "../../../utilidades/formatoFechas";

// HU43 — Detalle real de un equipo de la sesión. El ingreso a un equipo es HU47.
export default function PantallaDetalleEquipo() {
  return (
    <RutaProtegidaMovil>
      <Contenido />
    </RutaProtegidaMovil>
  );
}

function Contenido() {
  const enrutador = useRouter();
  const { cerrarSesion } = useAutenticacion();
  const parametros = useLocalSearchParams<{ sesionId?: string; equipoId?: string }>();
  const sesionId = parametros.sesionId ?? "";
  const equipoId = parametros.equipoId ?? "";

  const { equipo, cargando, error, sesionExpirada, refrescar } =
    useDetalleEquipoSesion(sesionId, equipoId);

  useEffect(() => {
    if (sesionExpirada) cerrarSesion().finally(() => enrutador.replace("/"));
  }, [sesionExpirada, cerrarSesion, enrutador]);

  const ingresarAlEquipo = () => {
    // HU47: ingreso real (público o con contraseña si es privado).
    Alert.alert(
      "Ingresar al equipo",
      "Ingresar a un equipo se implementará en la HU47.",
    );
  };

  // HU41: solo el líder puede editar el equipo.
  const editarEquipo = () => {
    if (!equipo) return;
    enrutador.push(
      `/participante/sesiones/editar-equipo?sesionId=${sesionId}` +
        `&equipoId=${equipoId}` +
        `&nombre=${encodeURIComponent(equipo.nombre)}` +
        `&tipo=${equipo.tipo}`,
    );
  };

  return (
    <PantallaBase>
      <View style={estilos.encabezado}>
        <Text style={estilos.titulo}>Detalle de equipo</Text>
      </View>

      {cargando && (
        <View style={estilos.contenedorEstado}>
          <ActivityIndicator color={tema.colores.primario} size="large" />
          <Text style={estilos.textoEstado}>Cargando equipo…</Text>
        </View>
      )}

      {!cargando && error && !sesionExpirada && (
        <View>
          <View style={estilos.cuadroError}>
            <Text style={estilos.cuadroErrorTexto}>{error}</Text>
          </View>
          <TouchableOpacity style={estilos.botonPrimario} onPress={refrescar}>
            <Text style={estilos.botonPrimarioTexto}>Reintentar</Text>
          </TouchableOpacity>
        </View>
      )}

      {!cargando && !error && equipo && (
        <ScrollView showsVerticalScrollIndicator={false}>
          <View style={estilos.tarjeta}>
            <View style={estilos.filaTitulo}>
              <Text style={estilos.nombreEquipo}>{equipo.nombre}</Text>
              <View style={estilos.badges}>
                {equipo.esMiEquipo && (
                  <Text style={[estilos.badge, estilos.badgePrimario]}>Tu equipo</Text>
                )}
                {equipo.soyLider && (
                  <Text style={[estilos.badge, estilos.badgePrimario]}>Eres líder</Text>
                )}
              </View>
            </View>
            <Text style={estilos.metaLinea}>
              {equipo.tipo === "Privado" ? "Privado" : "Público"}
            </Text>
            <Text style={estilos.metaLinea}>Puntaje: {equipo.puntaje}</Text>
            <Text style={estilos.metaLinea}>
              Integrantes: {equipo.cantidadParticipantes} / {equipo.capacidadMaxima}
            </Text>
          </View>

          <Text style={estilos.tituloSeccion}>PARTICIPANTES</Text>
          {equipo.participantes.length === 0 ? (
            <View style={estilos.tarjeta}>
              <Text style={estilos.metaLinea}>Este equipo no tiene integrantes.</Text>
            </View>
          ) : (
            equipo.participantes.map((p) => (
              <FilaParticipante key={p.participanteSesionId} participante={p} />
            ))
          )}

          {equipo.soyLider && (
            <TouchableOpacity
              style={estilos.botonPrimario}
              onPress={editarEquipo}
              accessibilityRole="button"
            >
              <Text style={estilos.botonPrimarioTexto}>Editar equipo</Text>
            </TouchableOpacity>
          )}

          {!equipo.esMiEquipo && (
            <TouchableOpacity
              style={estilos.botonPrimario}
              onPress={ingresarAlEquipo}
              accessibilityRole="button"
            >
              <Text style={estilos.botonPrimarioTexto}>Ingresar al equipo</Text>
            </TouchableOpacity>
          )}
        </ScrollView>
      )}

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

function FilaParticipante({ participante }: { participante: IntegranteEquipo }) {
  const nombreCompleto = `${participante.nombre} ${participante.apellido}`.trim();
  return (
    <View style={estilos.tarjeta}>
      <View style={estilos.filaTitulo}>
        <Text style={estilos.nombreParticipante}>{nombreCompleto}</Text>
        {participante.esLider && (
          <Text style={[estilos.badge, estilos.badgePrimario]}>Líder</Text>
        )}
      </View>
      <Text style={estilos.metaLinea}>@{participante.alias}</Text>
      <Text style={estilos.metaLinea}>Puntaje: {participante.puntaje}</Text>
      <Text style={estilos.metaLinea}>
        Fecha de unión: {formatearFechaHora(participante.fechaUnion)}
      </Text>
    </View>
  );
}

const estilos = StyleSheet.create({
  encabezado: { marginBottom: tema.espacios.md, paddingTop: tema.espacios.md },
  titulo: {
    fontSize: tema.tipografia.tamanos.h2,
    fontWeight: tema.tipografia.pesos.extrabold,
    color: tema.colores.texto,
  },
  tarjeta: {
    backgroundColor: tema.colores.fondoTarjeta,
    borderRadius: tema.radios.tarjeta,
    borderWidth: 1,
    borderColor: tema.colores.bordeTarjeta,
    padding: tema.espacios.lg,
    marginBottom: tema.espacios.md,
  },
  filaTitulo: {
    flexDirection: "row",
    justifyContent: "space-between",
    alignItems: "center",
    flexWrap: "wrap",
  },
  nombreEquipo: {
    color: tema.colores.texto,
    fontSize: tema.tipografia.tamanos.h3,
    fontWeight: tema.tipografia.pesos.extrabold,
  },
  nombreParticipante: {
    color: tema.colores.texto,
    fontSize: tema.tipografia.tamanos.md,
    fontWeight: tema.tipografia.pesos.bold,
  },
  badges: { flexDirection: "row", gap: tema.espacios.xs, flexWrap: "wrap" },
  badge: {
    fontSize: tema.tipografia.tamanos.xs,
    paddingHorizontal: tema.espacios.sm,
    paddingVertical: 2,
    borderRadius: tema.radios.entrada,
    overflow: "hidden",
    fontWeight: tema.tipografia.pesos.bold,
  },
  badgePrimario: {
    backgroundColor: tema.colores.primario,
    color: tema.colores.textoBlanco,
  },
  metaLinea: {
    color: tema.colores.textoTenue,
    fontSize: tema.tipografia.tamanos.sm,
    marginTop: tema.espacios.xs,
  },
  tituloSeccion: {
    color: tema.colores.textoTenue,
    fontSize: tema.tipografia.tamanos.xs,
    letterSpacing: tema.tipografia.espaciadoLetra.sm,
    textTransform: "uppercase",
    marginBottom: tema.espacios.sm,
    marginLeft: tema.espacios.xs,
    fontWeight: tema.tipografia.pesos.bold,
  },
  contenedorEstado: { alignItems: "center", padding: tema.espacios.xl },
  textoEstado: {
    color: tema.colores.textoTenue,
    marginTop: tema.espacios.md,
    fontSize: tema.tipografia.tamanos.md,
  },
  cuadroError: {
    backgroundColor: tema.colores.errorSuave,
    borderColor: tema.colores.error,
    borderWidth: 1,
    borderRadius: tema.radios.entrada,
    padding: tema.espacios.md,
    marginBottom: tema.espacios.md,
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
    marginTop: tema.espacios.sm,
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
